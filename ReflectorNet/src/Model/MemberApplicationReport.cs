/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Model
{
    /// <summary>
    /// What happened to a single member during a two-phase member application.
    /// </summary>
    public enum MemberOutcome
    {
        /// <summary>The member resolved and its value was written to the target.</summary>
        Applied,

        /// <summary>
        /// The member's value could not be resolved. Nothing was written for it - and, because the
        /// resolve phase precedes the apply phase, nothing was written for any sibling either.
        /// </summary>
        ResolutionFailed,

        /// <summary>
        /// The member resolved but the write itself failed (a setter threw). The target is in a
        /// partially applied state; see <see cref="MemberApplicationState.PartiallyApplied"/>.
        /// </summary>
        ApplyFailed,

        /// <summary>The member was skipped (not found on the target, read-only, unnamed, ...).</summary>
        Skipped
    }

    /// <summary>
    /// The terminal state of a member-set application (<c>Populate</c> / <c>Modify</c>).
    /// </summary>
    /// <remarks>
    /// There are THREE states, not two. A setter can still throw during the apply phase (an engine
    /// validation hook, an invalid combination of values, ...) and consumer setters cannot be made
    /// transactional, so "everything or nothing" is not always achievable. Rolling back is NOT an
    /// option either: writing an old value back is not an identity operation when setters have side
    /// effects (an undo stack, an asset database, a validation callback), and the rollback itself can
    /// corrupt state. The honest answer is a third state that names exactly what landed.
    /// </remarks>
    public enum MemberApplicationState
    {
        /// <summary>
        /// Nothing was recorded at all. NOT a success - the operation either never reached a member
        /// set (a value-only payload) or ran through a converter that does not report per-member
        /// outcomes. Distinguished from <see cref="Applied"/> on purpose: "no evidence" must never
        /// read as "everything worked".
        /// </summary>
        NothingRecorded,

        /// <summary>Every recorded member resolved and every write succeeded.</summary>
        Applied,

        /// <summary>
        /// At least one member failed and NOTHING was applied. The target is unchanged.
        /// </summary>
        Rejected,

        /// <summary>
        /// At least one member failed while at least one other landed. The report names which.
        /// </summary>
        PartiallyApplied
    }

    /// <summary>
    /// Per-member record inside a <see cref="MemberApplicationReport"/>.
    /// </summary>
    public class MemberApplicationEntry
    {
        public string Name { get; }
        public MemberOutcome Outcome { get; }
        public string? Message { get; }

        /// <summary>
        /// Nesting depth of the frame that recorded this entry. A report is threaded through the
        /// WHOLE operation - including nested member sets - so entries from different levels share
        /// one flat list, and two different objects can each contribute a member of the same name.
        /// Depth is what tells them apart.
        /// </summary>
        public int Depth { get; }

        public MemberApplicationEntry(string name, MemberOutcome outcome, string? message = null, int depth = 0)
        {
            Name = name;
            Outcome = outcome;
            Message = message;
            Depth = depth;
        }

        public bool Applied => Outcome == MemberOutcome.Applied;

        public bool Failed => Outcome == MemberOutcome.ResolutionFailed || Outcome == MemberOutcome.ApplyFailed;

        public override string ToString()
        {
            var prefix = Depth > 0 ? $"[d{Depth}] " : string.Empty;
            return Message == null ? $"{prefix}{Name}: {Outcome}" : $"{prefix}{Name}: {Outcome} - {Message}";
        }
    }

    /// <summary>
    /// A <see cref="Logs"/> sink that additionally records the per-member outcome of a member-set
    /// application, so a caller can tell exactly which members landed and which did not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It derives from <see cref="Logs"/> on purpose: every ReflectorNet API already threads an
    /// optional <c>Logs? logs</c> parameter, so a caller opts into the report simply by passing a
    /// <see cref="MemberApplicationReport"/> where a <see cref="Logs"/> is expected. No public
    /// signature changes, no new interface members, nothing for an existing consumer to implement.
    /// </para>
    /// <para>
    /// A blanket "success" is never reported when any member failed - see
    /// <see cref="MemberApplicationState"/>.
    /// </para>
    /// </remarks>
    public class MemberApplicationReport : Logs
    {
        readonly List<MemberApplicationEntry> _members = new();

        /// <summary>Per-member outcomes, in the order the members were processed.</summary>
        public IReadOnlyList<MemberApplicationEntry> Members => _members;

        /// <summary>Names of the members whose values were written to the target.</summary>
        public IEnumerable<string> AppliedMembers => _members.Where(m => m.Applied).Select(m => m.Name).Distinct();

        /// <summary>Names of the members that did not land.</summary>
        public IEnumerable<string> FailedMembers => _members.Where(m => m.Failed).Select(m => m.Name).Distinct();

        /// <summary>
        /// The terminal state, derived from what was actually recorded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Derived from evidence, never asserted. A report is threaded through the WHOLE operation,
        /// nested member sets included, so a failure at one level can coexist with members that
        /// genuinely landed at another. The state is therefore defined by what happened, not by which
        /// phase failed:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>nothing recorded -> <see cref="MemberApplicationState.NothingRecorded"/> (NOT a success)</description></item>
        ///   <item><description>a failure and nothing applied -> <see cref="MemberApplicationState.Rejected"/></description></item>
        ///   <item><description>a failure alongside something applied -> <see cref="MemberApplicationState.PartiallyApplied"/></description></item>
        ///   <item><description>no failure -> <see cref="MemberApplicationState.Applied"/></description></item>
        /// </list>
        /// <para>
        /// In particular <see cref="MemberApplicationState.Rejected"/> can never be reported while
        /// <see cref="AppliedMembers"/> is non-empty - the two would contradict each other, which is
        /// the same class of defect this whole design exists to prevent.
        /// </para>
        /// </remarks>
        public MemberApplicationState State
        {
            get
            {
                if (_members.Count == 0)
                    return MemberApplicationState.NothingRecorded;

                if (!_members.Any(m => m.Failed))
                    return MemberApplicationState.Applied;

                return _members.Any(m => m.Applied)
                    ? MemberApplicationState.PartiallyApplied
                    : MemberApplicationState.Rejected;
            }
        }

        public MemberApplicationReport Record(string? name, MemberOutcome outcome, string? message = null, int depth = 0)
        {
            _members.Add(new MemberApplicationEntry(name ?? string.Empty, outcome, message, depth));
            return this;
        }

        public override string ToString()
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendLine($"[{nameof(MemberApplicationReport)}] {State}");
            foreach (var member in _members)
                stringBuilder.AppendLine($"  - {member}");
            stringBuilder.Append(base.ToString());
            return stringBuilder.ToString();
        }
    }

    public static class MemberApplicationReportExtensions
    {
        /// <summary>
        /// Records a per-member outcome when <paramref name="logs"/> happens to be a
        /// <see cref="MemberApplicationReport"/>; a no-op for a plain <see cref="Logs"/> sink or
        /// <c>null</c>. Lets the converter code record outcomes unconditionally.
        /// </summary>
        public static void RecordMember(this Logs? logs, string? name, MemberOutcome outcome, string? message = null, int depth = 0)
            => (logs as MemberApplicationReport)?.Record(name, outcome, message, depth);
    }
}

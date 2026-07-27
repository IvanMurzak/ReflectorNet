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
        /// <summary>Every member resolved and every write succeeded.</summary>
        Applied,

        /// <summary>
        /// The resolve phase failed: at least one member's value could not be built, so NOTHING was
        /// written. The target is unchanged.
        /// </summary>
        Rejected,

        /// <summary>
        /// The resolve phase succeeded but a write failed. Some members landed and some did not; the
        /// report names which.
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

        public MemberApplicationEntry(string name, MemberOutcome outcome, string? message = null)
        {
            Name = name;
            Outcome = outcome;
            Message = message;
        }

        public bool Applied => Outcome == MemberOutcome.Applied;

        public override string ToString()
            => Message == null ? $"{Name}: {Outcome}" : $"{Name}: {Outcome} - {Message}";
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
        public IEnumerable<string> AppliedMembers => _members.Where(m => m.Applied).Select(m => m.Name);

        /// <summary>Names of the members that did not land.</summary>
        public IEnumerable<string> FailedMembers => _members
            .Where(m => m.Outcome == MemberOutcome.ResolutionFailed || m.Outcome == MemberOutcome.ApplyFailed)
            .Select(m => m.Name);

        /// <summary>
        /// The terminal state, derived from the recorded members.
        /// <see cref="MemberApplicationState.Rejected"/> whenever any member failed to RESOLVE
        /// (the apply phase never started, so nothing was written);
        /// <see cref="MemberApplicationState.PartiallyApplied"/> when every member resolved but a
        /// write failed; otherwise <see cref="MemberApplicationState.Applied"/>.
        /// </summary>
        public MemberApplicationState State
        {
            get
            {
                if (_members.Any(m => m.Outcome == MemberOutcome.ResolutionFailed))
                    return MemberApplicationState.Rejected;

                if (_members.Any(m => m.Outcome == MemberOutcome.ApplyFailed))
                    return MemberApplicationState.PartiallyApplied;

                return MemberApplicationState.Applied;
            }
        }

        public MemberApplicationReport Record(string? name, MemberOutcome outcome, string? message = null)
        {
            _members.Add(new MemberApplicationEntry(name ?? string.Empty, outcome, message));
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
        public static void RecordMember(this Logs? logs, string? name, MemberOutcome outcome, string? message = null)
            => (logs as MemberApplicationReport)?.Record(name, outcome, message);
    }
}

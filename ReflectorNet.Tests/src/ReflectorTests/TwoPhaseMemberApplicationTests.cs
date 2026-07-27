/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Target whose setters are observable, so "was this member actually written?" is a fact rather
    /// than an inference. Stands in for a consumer type whose setters have side effects (an undo
    /// stack, an asset database write, a validation callback) - precisely the reason a rollback is
    /// not an option and the two-phase split has to happen BEFORE any write.
    /// </summary>
    public class TwoPhaseProbeTarget
    {
        /// <summary>Names of the members whose setter actually ran, in order.</summary>
        public static readonly List<string> SetterCalls = new();

        /// <summary>When set, the named property's setter throws - simulating an engine-side reject.</summary>
        public static string? ThrowOnSetterFor;

        public static void Reset()
        {
            SetterCalls.Clear();
            ThrowOnSetterFor = null;
        }

        int _first;
        SilentFailureProbeStruct _second;
        int _third;

        public int First
        {
            get => _first;
            set
            {
                RecordSet(nameof(First));
                _first = value;
            }
        }

        public SilentFailureProbeStruct Second
        {
            get => _second;
            set
            {
                RecordSet(nameof(Second));
                _second = value;
            }
        }

        public int Third
        {
            get => _third;
            set
            {
                RecordSet(nameof(Third));
                _third = value;
            }
        }

        static void RecordSet(string name)
        {
            SetterCalls.Add(name);
            if (ThrowOnSetterFor == name)
                throw new InvalidOperationException($"Setter for '{name}' rejected the value.");
        }
    }

    /// <summary>Two-level target, for exercising a report threaded through NESTED member sets.</summary>
    public class NestedProbeTarget
    {
        public TwoPhaseProbeTarget? Inner { get; set; }
        public SilentFailureProbeStruct Broken { get; set; }
    }

    /// <summary>
    /// Regression tests for the two-phase (RESOLVE then APPLY) member application and its
    /// terminal states.
    ///
    /// <para>
    /// The rule: deserialize EVERY member first and mutate nothing; if any member fails to resolve,
    /// abort with the target untouched (<see cref="MemberApplicationState.Rejected"/>). Only once all
    /// members resolved are the values written. A write can still fail - a consumer setter is not
    /// transactional and must not be rolled back - which is the third state,
    /// <see cref="MemberApplicationState.PartiallyApplied"/>, and the report names exactly what landed.
    /// </para>
    /// </summary>
    [Collection(ProbeStatics.Name)]
    public class TwoPhaseMemberApplicationTests : BaseTest, IDisposable
    {
        public TwoPhaseMemberApplicationTests(ITestOutputHelper output) : base(output)
        {
            TwoPhaseProbeTarget.Reset();
        }

        public void Dispose() => TwoPhaseProbeTarget.Reset();

        static SerializedMember GoodMember(Reflector reflector, string name, int value)
            => SerializedMember.FromValue(reflector, typeof(int), value, name: name);

        /// <summary>A member whose 'value' payload no converter can resolve into the target type.</summary>
        static SerializedMember UnresolvableMember(string name) => new SerializedMember
        {
            name = name,
            typeName = typeof(SilentFailureProbeStruct).GetTypeId(),
            valueJsonElement = JsonDocument.Parse("{\"instanceID\":\"12345\"}").RootElement
        };

        // ------------------------------------------------------------------------------------
        // Rejected: one member fails to resolve -> NOTHING is written.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void OneMemberFailsToResolve_LeavesTargetCompletelyUnmodified()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            var data = new SerializedMember { typeName = typeof(TwoPhaseProbeTarget).GetTypeId() };
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.First), 7));
            data.AddProperty(UnresolvableMember(nameof(TwoPhaseProbeTarget.Second)));
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.Third), 9));

            Assert.Throws<DeserializationException>(() => reflector.Deserialize(data, logs: report));

            _output.WriteLine(report.ToString());

            // THE assertion: 'First' resolves fine and comes before the failing member, but it must
            // NOT have been written. Resolve-all-then-apply, never apply-as-you-go.
            Assert.Empty(TwoPhaseProbeTarget.SetterCalls);

            Assert.Equal(MemberApplicationState.Rejected, report.State);
            Assert.Contains(report.Members, m =>
                m.Name == nameof(TwoPhaseProbeTarget.Second) && m.Outcome == MemberOutcome.ResolutionFailed);
            Assert.Contains(nameof(TwoPhaseProbeTarget.Second), report.FailedMembers);
            Assert.Empty(report.AppliedMembers);
        }

        [Fact]
        public void OneMemberFailsToResolve_ReportNamesTheOffendingMember()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            var data = new SerializedMember { typeName = typeof(TwoPhaseProbeTarget).GetTypeId() };
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.First), 7));
            data.AddProperty(UnresolvableMember(nameof(TwoPhaseProbeTarget.Second)));

            Assert.Throws<DeserializationException>(() => reflector.Deserialize(data, logs: report));

            _output.WriteLine(report.ToString());

            // Exactly one member is named as failed, however many frames recorded it. The report is
            // threaded through the whole operation, so the failing member is recorded twice - once by
            // the nested frame that diagnosed it and once by the enclosing RESOLVE loop that aborted
            // on it - and `Depth` is what tells those two records apart.
            var offendingName = Assert.Single(report.FailedMembers);
            Assert.Equal(nameof(TwoPhaseProbeTarget.Second), offendingName);

            var offending = report.Members.Where(m => m.Failed).ToList();
            Assert.All(offending, m => Assert.Equal(nameof(TwoPhaseProbeTarget.Second), m.Name));
            Assert.All(offending, m => Assert.False(m.Applied));
            Assert.All(offending, m => Assert.Contains("instanceID", m.Message));
            Assert.Equal(offending.Count, offending.Select(m => m.Depth).Distinct().Count());
        }

        // ------------------------------------------------------------------------------------
        // Applied: the happy path still applies everything and reports it.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void AllMembersResolve_EverythingIsApplied()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            var data = new SerializedMember { typeName = typeof(TwoPhaseProbeTarget).GetTypeId() };
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.First), 7));
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.Third), 9));

            var result = reflector.Deserialize(data, logs: report);

            _output.WriteLine(report.ToString());

            var target = Assert.IsType<TwoPhaseProbeTarget>(result);
            Assert.Equal(7, target.First);
            Assert.Equal(9, target.Third);

            Assert.Equal(MemberApplicationState.Applied, report.State);
            Assert.Equal(
                new[] { nameof(TwoPhaseProbeTarget.First), nameof(TwoPhaseProbeTarget.Third) },
                report.AppliedMembers);
            Assert.Empty(report.FailedMembers);
        }

        // ------------------------------------------------------------------------------------
        // PartiallyApplied: every member resolved, but a setter threw during APPLY. No rollback -
        // the report names what landed instead.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void SetterThrowsDuringApply_ReportsPartiallyApplied_AndNamesWhatLanded()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            TwoPhaseProbeTarget.ThrowOnSetterFor = nameof(TwoPhaseProbeTarget.Third);

            var data = new SerializedMember { typeName = typeof(TwoPhaseProbeTarget).GetTypeId() };
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.First), 7));
            data.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.Third), 9));

            // Reflection wraps a throwing setter, so assert the wrapper AND the real cause rather
            // than ThrowsAny<Exception>, which would pass on any unrelated failure.
            var thrown = Assert.Throws<System.Reflection.TargetInvocationException>(
                () => reflector.Deserialize(data, logs: report));
            var cause = Assert.IsType<InvalidOperationException>(thrown.InnerException);
            Assert.Contains(nameof(TwoPhaseProbeTarget.Third), cause.Message);

            _output.WriteLine(report.ToString());

            // The apply phase HAD started, so this is not a rejection - it is a partial application,
            // and lying about it in either direction would be worse than saying so.
            Assert.Equal(MemberApplicationState.PartiallyApplied, report.State);
            Assert.Contains(nameof(TwoPhaseProbeTarget.First), report.AppliedMembers);
            Assert.Contains(nameof(TwoPhaseProbeTarget.Third), report.FailedMembers);
        }

        // ------------------------------------------------------------------------------------
        // TryModify keeps its bool + Logs contract and never throws, but must never report a
        // blanket success either.
        // ------------------------------------------------------------------------------------

        // ------------------------------------------------------------------------------------
        // The report is threaded through NESTED member sets too, so its terminal state must be
        // derived from what actually happened - never asserted from "which phase failed".
        // ------------------------------------------------------------------------------------

        [Fact]
        public void NestedMemberApplied_ThenOuterMemberFails_ReportsPartiallyApplied_NotRejected()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            // The nested object is fully deserialized (and its own members genuinely written) during
            // the OUTER resolve phase. Reporting `Rejected` - "nothing was applied, the target is
            // unchanged" - while `AppliedMembers` is non-empty would be a self-contradicting report,
            // which is the very defect class this design exists to prevent.
            var nested = new SerializedMember { name = nameof(NestedProbeTarget.Inner), typeName = typeof(TwoPhaseProbeTarget).GetTypeId() };
            nested.AddProperty(GoodMember(reflector, nameof(TwoPhaseProbeTarget.First), 7));

            var data = new SerializedMember { typeName = typeof(NestedProbeTarget).GetTypeId() };
            data.AddProperty(nested);
            data.AddProperty(UnresolvableMember(nameof(NestedProbeTarget.Broken)));

            Assert.Throws<DeserializationException>(() => reflector.Deserialize(data, logs: report));

            _output.WriteLine(report.ToString());

            Assert.NotEmpty(report.AppliedMembers);
            Assert.Contains(nameof(TwoPhaseProbeTarget.First), report.AppliedMembers);
            Assert.Contains(nameof(NestedProbeTarget.Broken), report.FailedMembers);

            Assert.Equal(MemberApplicationState.PartiallyApplied, report.State);
            Assert.NotEqual(MemberApplicationState.Rejected, report.State);
        }

        [Fact]
        public void EmptyReport_IsNothingRecorded_NotApplied()
        {
            // "No evidence" must never read as "everything worked".
            var report = new MemberApplicationReport();

            Assert.Empty(report.Members);
            Assert.Equal(MemberApplicationState.NothingRecorded, report.State);
            Assert.NotEqual(MemberApplicationState.Applied, report.State);
        }

        [Fact]
        public void ValueOnlyPayload_RecordsNothing_AndDoesNotClaimApplied()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            // No fields, no props - nothing to apply, so nothing is recorded, and the report says so
            // rather than reporting a success it has no evidence for.
            var result = reflector.Deserialize(
                SerializedMember.FromValue(reflector, typeof(int), 42, name: "n"),
                logs: report);

            Assert.Equal(42, result);
            Assert.Equal(MemberApplicationState.NothingRecorded, report.State);
        }

        [Fact]
        public void TryModify_UnresolvablePayload_ReportsRejected_WithoutThrowing()
        {
            var reflector = new Reflector();
            var report = new MemberApplicationReport();

            object? target = null; // forces the "instantiate via Deserialize" branch
            var data = UnresolvableMember("probe");

            var success = reflector.TryModify(ref target, data, logs: report);

            _output.WriteLine(report.ToString());

            Assert.False(success);
            Assert.Null(target);
            Assert.Equal(MemberApplicationState.Rejected, report.State);
            Assert.Contains(report, log => log.Type == LogType.Error);
        }
    }
}

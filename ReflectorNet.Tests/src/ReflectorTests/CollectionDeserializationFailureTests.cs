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
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Consumer-simulation converter for the COLLECTION seam, the analogue of
    /// <see cref="ForeignRefReflectionConverter"/> for scalars. It owns a foreign <c>value</c> payload
    /// shape on a collection type - <c>{"assetGroup":"…"}</c> expands to a whole list - by overriding
    /// the per-link <c>TryDeserializeCollectionValue</c> and answering
    /// <see cref="DeserializationOutcome.Handled"/>.
    /// </summary>
    /// <remarks>
    /// Its existence is the proof that the collection chain end did not close the door behind it: a
    /// derived converter must still be able to see a payload the base does not understand, without the
    /// base having logged an <see cref="LogType.Error"/> or thrown on the way. Zero engine dependency,
    /// same as the scalar fixture.
    /// </remarks>
    public class ForeignGroupArrayReflectionConverter : ArrayReflectionConverter
    {
        public const string ForeignKey = "assetGroup";

        protected override DeserializationOutcome TryDeserializeCollectionValue(
            Reflector reflector,
            SerializedMember data,
            Type type,
            out object? result,
            string? name = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            var baseOutcome = base.TryDeserializeCollectionValue(
                reflector,
                data: data,
                type: type,
                result: out result,
                name: name,
                depth: depth,
                logs: logs,
                logger: logger,
                context: context);

            if (baseOutcome != DeserializationOutcome.NotApplicable)
                return baseOutcome;

            // The base declined: the payload is not a JSON array. It may still be OUR shape.
            if (data.valueJsonElement != null &&
                data.valueJsonElement.Value.ValueKind == JsonValueKind.Object &&
                data.valueJsonElement.Value.TryGetProperty(ForeignKey, out var group))
            {
                var groupName = group.GetString() ?? string.Empty;
                result = ForeignRefRegistry.FindGroup(groupName);
                return result == null
                    ? DeserializationOutcome.NotApplicable
                    : DeserializationOutcome.Handled;
            }

            return baseOutcome;
        }
    }

    /// <summary>A struct whose converter answers <c>null</c> - which no value type may legitimately be.</summary>
    public struct NullAnsweringStruct
    {
        public int N;
    }

    /// <summary>Answers <c>null</c> for a value type. Stands in for any converter that gets it wrong.</summary>
    public class NullAnsweringStructConverter : GenericReflectionConverter<NullAnsweringStruct>
    {
        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null) => null;
    }

    /// <summary>Exposes the protected seams so a test can observe <c>result</c> directly.</summary>
    public class ProbeArrayReflectionConverter : ArrayReflectionConverter
    {
        public bool CallTryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            Logs? logs = null)
            => TryDeserializeValueInternal(reflector, data, out result, type, depth: 0, logs: logs, logger: null);
    }

    /// <summary>
    /// Regression tests for the COLLECTION half of the "never report a failed deserialization as a
    /// success" contract.
    ///
    /// <para>
    /// History: <c>12f99093</c> stopped a scalar failure being answered with a fabricated default, and
    /// <c>6a26ac81</c> turned the converter answer into a tri-state so a "not my shape" decline stayed
    /// quiet while the chain END became loud. Neither reached collections:
    /// <see cref="ArrayReflectionConverter"/> overrides <c>Deserialize</c> wholesale, so a collection
    /// never passed through that chain end. A <c>value</c> payload nobody could understand came back as
    /// <c>null</c> with nothing louder than a <see cref="LogType.Warning"/>, and the two-phase member
    /// application then WROTE that <c>null</c> over a live collection and reported
    /// <see cref="MemberApplicationState.Applied"/>. Same silent substitution, different type family -
    /// and destructive on top.
    /// </para>
    ///
    /// <para>
    /// The tests below fail against <c>15fc491</c>. They also pin the half that must NOT become loud:
    /// an element resolving to <c>null</c> is legitimate for a blacklisted type or a cleared object
    /// reference, and must never log <see cref="LogType.Error"/> - the in-repo equivalent of Unity's
    /// <c>LogAssert</c> teardown check that caught the previous regression.
    /// </para>
    /// </summary>
    [Collection(ProbeStatics.Name)]
    public class CollectionDeserializationFailureTests : BaseTest, IDisposable
    {
        public CollectionDeserializationFailureTests(ITestOutputHelper output) : base(output)
        {
            ForeignRefRegistry.Reset();
        }

        public void Dispose() => ForeignRefRegistry.Reset();

        static SerializedMember Member(Type type, string valueJson, string? name = "collection") => new SerializedMember
        {
            name = name,
            typeName = type.GetTypeId(),
            valueJsonElement = JsonDocument.Parse(valueJson).RootElement
        };

        static Reflector ReflectorWithConsumerConverter()
        {
            var reflector = new Reflector();
            reflector.Converters.Add(new ForeignRefReflectionConverter());
            return reflector;
        }

        /// <summary>Target with a live collection that a silent null would destroy.</summary>
        public class CollectionHolder
        {
            public List<int> Numbers = new List<int> { 7, 8, 9 };
            public string Tag = "untouched";
        }

        // ------------------------------------------------------------------------------------
        // 1. A collection payload nobody can understand is a terminal failure, not a quiet null
        //    and not an empty collection.
        // ------------------------------------------------------------------------------------

        [Theory]
        [InlineData("{\"instanceID\":\"12345\"}")]           // foreign object shape - nobody owns it
        [InlineData("{\"typeName\":\"System.Int32\",\"bogus\":1}")] // malformed SerializedMember
        [InlineData("\"not-an-array\"")]                      // a JSON string
        [InlineData("12345")]                                 // a JSON number
        [InlineData("{}")]                                    // an empty object
        public void UnintelligibleCollectionPayload_IsATerminalFailure(string valueJson)
        {
            var reflector = new Reflector();
            var logs = new Logs();

            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(Member(typeof(List<int>), valueJson), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Equal(typeof(List<int>), exception.TargetType);
            Assert.Contains(logs, log => log.Type == LogType.Error);
        }

        [Fact]
        public void UnintelligibleCollectionPayload_NeverYieldsAFabricatedCollection()
        {
            var reflector = new Reflector();

            // Seeded with a marker rather than null so the assertion is a real discriminator: an empty
            // collection, a null, or anything else handed back would change `result`.
            var unassigned = new object();
            object? result = unassigned;

            Assert.Throws<DeserializationException>(
                () => result = reflector.Deserialize(Member(typeof(List<int>), "{\"instanceID\":\"12345\"}")));

            Assert.Same(unassigned, result);
        }

        [Fact]
        public void UnintelligibleCollectionPayload_ForAnArrayType_IsATerminalFailure()
        {
            var reflector = new Reflector();
            var logs = new Logs();

            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(Member(typeof(int[]), "{\"instanceID\":\"12345\"}"), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Equal(typeof(int[]), exception.TargetType);
            Assert.Contains("instanceID", exception.Message);
        }

        // ------------------------------------------------------------------------------------
        // 2. The destructive consequence: the member-application path used to overwrite a live
        //    collection with that null and report Applied.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void CollectionMember_WithUnintelligiblePayload_DoesNotOverwriteTheTargetWithNull()
        {
            var reflector = new Reflector();

            var data = new SerializedMember
            {
                name = "holder",
                typeName = typeof(CollectionHolder).GetTypeId(),
                valueJsonElement = JsonDocument.Parse("{}").RootElement
            };
            data.AddField(Member(typeof(List<int>), "{\"instanceID\":\"12345\"}", nameof(CollectionHolder.Numbers)));

            var report = new MemberApplicationReport();

            // Before this change: returned a CollectionHolder with Numbers == null and
            // report.State == Applied. A destructive write, reported as a success.
            var exception = Assert.Throws<DeserializationException>(() => reflector.Deserialize(data, logs: report));

            _output.WriteLine($"{exception.Message}\n{report}");

            Assert.Equal(MemberApplicationState.Rejected, report.State);
            Assert.Contains(nameof(CollectionHolder.Numbers), report.FailedMembers);
            Assert.Empty(report.AppliedMembers);
        }

        [Fact]
        public void TryModify_WithUnintelligibleCollectionPayload_ReportsFailure_AndLeavesTheTargetIntact()
        {
            var reflector = new Reflector();
            var holder = new CollectionHolder();
            object? target = holder;

            var data = new SerializedMember { name = "holder", typeName = typeof(CollectionHolder).GetTypeId() };
            data.AddField(Member(typeof(List<int>), "{\"instanceID\":\"12345\"}", nameof(CollectionHolder.Numbers)));

            var report = new MemberApplicationReport();
            var success = reflector.TryModify(ref target, data, logs: report);

            _output.WriteLine(report.ToString());

            Assert.False(success);
            Assert.Equal(new[] { 7, 8, 9 }, holder.Numbers);
            Assert.Equal("untouched", holder.Tag);
        }

        [Fact]
        public void SetField_WithUnintelligibleCollectionPayload_ReportsFailure_AndNeverProducesAnEmptyCollection()
        {
            // The reported shape of this defect was "answers a hard failure with an empty collection".
            // The seam is where that empty CreateInstance(type) used to be produced, so observe `result`
            // there directly rather than inferring it from the caller.
            var reflector = new Reflector();
            var converter = new ProbeArrayReflectionConverter();
            var logs = new Logs();

            var handled = converter.CallTryDeserializeValueInternal(
                reflector,
                Member(typeof(List<int>), "{\"instanceID\":\"12345\"}", "Numbers"),
                out var result,
                typeof(List<int>),
                logs);

            _output.WriteLine($"handled={handled}, result={result?.ToString() ?? "<null>"}\n{logs}");

            // Declined by shape: true + a null result, exactly like the scalar seam. NOT an empty list.
            Assert.True(handled);
            Assert.Null(result);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);

            // ... and the chain end that backs SetField turns the unresolved decline into one failure.
            var holder = new CollectionHolder();
            object? boxed = holder;
            var setLogs = new Logs();
            var fieldSet = reflector.Converters
                .GetConverter(typeof(List<int>))!
                .SetField(
                    reflector,
                    ref boxed,
                    typeof(List<int>),
                    typeof(CollectionHolder).GetField(nameof(CollectionHolder.Numbers))!,
                    Member(typeof(List<int>), "{\"instanceID\":\"12345\"}", nameof(CollectionHolder.Numbers)),
                    logs: setLogs);

            _output.WriteLine(setLogs.ToString());

            Assert.False(fieldSet);
            Assert.Equal(new[] { 7, 8, 9 }, holder.Numbers);
            Assert.Contains(setLogs, log => log.Type == LogType.Error);
        }

        // ------------------------------------------------------------------------------------
        // 3. Per-element failures: all-or-nothing.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void ElementThatNobodyUnderstands_AbandonsTheWholeCollection()
        {
            var reflector = ReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("1", "One");
            ForeignRefRegistry.Register("3", "Three");

            // Element [1] carries a shape NOBODY understands: not a SerializedMember, and not the
            // foreign shape the consumer converter owns either. Elements [0] and [2] resolve
            // perfectly - so a partial, hole-punched collection was the available alternative.
            var data = Member(typeof(List<ForeignRefAsset>),
                "[{\"instanceID\":\"1\"},{\"mystery\":\"x\"},{\"instanceID\":\"3\"}]");

            var report = new MemberApplicationReport();
            var unassigned = new object();
            object? result = unassigned;

            var exception = Assert.Throws<DeserializationException>(() => result = reflector.Deserialize(data, logs: report));

            _output.WriteLine($"{exception.Message}\n{report}");

            // No shorter, hole-punched collection is handed back. A collection is BUILT and returned
            // rather than written into a live target, so atomicity is achievable here - unlike the
            // member-set case, which had to settle for an honest PartiallyApplied.
            Assert.Same(unassigned, result);
            Assert.Contains("[1]", exception.Message);
            Assert.Contains(report.Members, m => m.Name == "[1]" && m.Outcome == MemberOutcome.ResolutionFailed);
            Assert.Contains(report, log => log.Type == LogType.Error);
        }

        [Fact]
        public void MistypedElement_IsRaisedAsADeserializationException_NotARawJsonException()
        {
            // A raw JsonException escapes Reflector.MethodCall / TryModify / TryDeserializeValueReporting
            // entirely - they only catch DeserializationException - so a mistyped element used to crash
            // out of the call instead of being reported.
            var reflector = new Reflector();
            var logs = new Logs();

            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(Member(typeof(int[]), "[1,\"oops\",3]"), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Contains("[1]", exception.Message);
            Assert.IsAssignableFrom<JsonException>(exception.InnerException);
            Assert.Contains(logs, log => log.Type == LogType.Error);
        }

        [Fact]
        public void ValueTypeElementResolvingToNull_IsAHardFailure()
        {
            // No converter may legitimately answer null for a non-nullable value type. Leaving the slot
            // at default(T) would hand back a full-length collection with a fabricated 0 in it - the
            // exact substitution 12f99093 removed for scalars.
            var reflector = new Reflector();
            reflector.Converters.Add(new NullAnsweringStructConverter());

            var logs = new Logs();
            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(Member(typeof(NullAnsweringStruct[]), "[{\"N\":1}]"), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Contains("[0]", exception.Message);
            Assert.Contains("non-nullable value type", exception.Message);
        }

        [Fact]
        public void ElementDeclaringItselfASerializedMember_ButUnparsable_IsNotSilentlyDegraded()
        {
            // ParseElementToMember used to swallow the JsonException with a bare `catch { }` and fall
            // back to the raw payload, throwing the element's declared type and nested members away -
            // and then deserializing something else entirely. Same swallow-and-substitute shape
            // 12f99093 removed from ExtensionsJsonElement.DeserializeValueSerializedMember.
            var reflector = new Reflector();
            var logs = new Logs();

            // 'typeName' is a string; a number cannot be parsed into it.
            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(Member(typeof(List<int>), "[{\"typeName\":123}]"), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Contains("[0]", exception.Message);
            Assert.Contains(logs, log => log.Type == LogType.Error);
        }

        // ------------------------------------------------------------------------------------
        // 4. The half that must NOT become loud. This is the LogAssert analogue: an Error here is
        //    what reddened 11 Unity tests last time.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void ForeignShapeElements_ResolvedByTheConsumerConverter_Succeed_AndLogNoError()
        {
            var reflector = ReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("1", "One");
            ForeignRefRegistry.Register("2", "Two");

            var logs = new Logs();
            var logger = new RecordingLogger();

            var result = reflector.Deserialize(
                Member(typeof(List<ForeignRefAsset>), "[{\"instanceID\":\"1\"},{\"instanceID\":\"2\"}]"),
                logs: logs,
                logger: logger);

            _output.WriteLine($"{logs}\n{string.Join("\n", logger.Entries)}");

            var list = Assert.IsType<List<ForeignRefAsset>>(result);
            Assert.Equal(new[] { "One", "Two" }, list.Select(a => a!.DisplayName));

            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Critical);
            Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Error);
        }

        [Fact]
        public void ElementTheConsumerResolvesToLegitimateNull_IsNotAFailure_AndLogsNoError()
        {
            // A cleared or deleted object reference resolves to null as its CORRECT answer. That is
            // what ConverterFallThroughTests.NullResolvingForeignRefConverter models (it owns the
            // shape and opts out of the decline classification), and it is what Unity-MCP's
            // UnityGenericReflectionConverter does when an asset lookup finds nothing. Calling that a
            // failure INSIDE a collection would re-break exactly the consumers the tri-state design
            // protects, so the collection must keep the null and stay quiet.
            var reflector = new Reflector();
            reflector.Converters.Add(new ConverterFallThroughTests.NullResolvingForeignRefConverter());

            var logs = new Logs();
            var logger = new RecordingLogger();

            var result = reflector.Deserialize(
                Member(typeof(List<ConverterFallThroughTests.NullResolvableRef>), "[{\"instanceID\":\"7\"},{\"instanceID\":\"0\"}]"),
                logs: logs,
                logger: logger);

            _output.WriteLine($"{logs}\n{string.Join("\n", logger.Entries)}");

            var list = Assert.IsType<List<ConverterFallThroughTests.NullResolvableRef>>(result);
            Assert.Equal(2, list.Count);
            Assert.Equal("7", list[0]!.Id);
            Assert.Null(list[1]);

            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
            Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Error);

            // Disclosed, not silent: the caller can see that the collection is not what it looks like.
            Assert.Contains(logs, log => log.Message.Contains("resolved to null"));
        }

        [Fact]
        public void ElementThatAConsumerConverterCannotResolve_KeepsTheCollection_AndIsDisclosed()
        {
            // The other flavour: a consumer converter that signals "I could not resolve this" by
            // returning false from TryDeserializeValueInternal. BaseReflectionConverter.Deserialize
            // turns that into a quiet null (a PRE-EXISTING scalar behaviour, identical outside a
            // collection - it is not something this converter can tell apart from a legitimate null),
            // so the collection keeps the hole rather than guessing. What it must NOT do is hide it.
            var reflector = ReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("1", "One");

            var logs = new Logs();

            var result = reflector.Deserialize(
                Member(typeof(List<ForeignRefAsset>), "[{\"instanceID\":\"1\"},{\"instanceID\":\"999\"}]"),
                logs: logs);

            _output.WriteLine(logs.ToString());

            var list = Assert.IsType<List<ForeignRefAsset>>(result);
            Assert.Equal(2, list.Count);
            Assert.Null(list[1]);

            // The collection itself adds no Error - the decline is Trace, per the tri-state rule.
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
            Assert.Contains(logs, log => log.Message.Contains("resolved to null"));
        }

        [Fact]
        public void BlacklistedElementType_RoundTripsToNull_WithoutAnError()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedElement));

            var source = new ElementBase[] { new AllowedElement { Id = 1 }, new BlacklistedElement(), new AllowedElement { Id = 3 } };
            var serialized = reflector.Serialize(source);

            var logs = new Logs();
            var result = reflector.Deserialize(serialized, fallbackType: typeof(ElementBase[]), logs: logs);

            _output.WriteLine(logs.ToString());

            var array = Assert.IsType<ElementBase[]>(result);
            Assert.Equal(3, array.Length);
            Assert.Null(array[1]);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
        }

        [Fact]
        public void AbsentCollectionPayload_IsNotAFailure()
        {
            var reflector = new Reflector();
            var logs = new Logs();

            var result = reflector.Deserialize(new SerializedMember
            {
                name = "collection",
                typeName = typeof(List<int>).GetTypeId()
            }, logs: logs);

            _output.WriteLine(logs.ToString());

            Assert.Null(result);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Warning);
        }

        [Fact]
        public void ExplicitNullCollectionPayload_IsNotAFailure()
        {
            var reflector = new Reflector();
            var logs = new Logs();

            var result = reflector.Deserialize(Member(typeof(List<int>), "null"), logs: logs);

            _output.WriteLine(logs.ToString());

            Assert.Null(result);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
        }

        // ------------------------------------------------------------------------------------
        // 5. The consumer seam for collections: a derived converter must still be able to own a
        //    payload shape the base declines, and the base must have stayed quiet on the way.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void DerivedConverter_OwningAForeignCollectionPayload_ResolvesIt_AndTheBaseLoggedNoError()
        {
            var reflector = new Reflector();
            reflector.Converters.Add(new ForeignGroupArrayReflectionConverter());
            ForeignRefRegistry.RegisterGroup("ground", new List<ForeignRefAsset>
            {
                ForeignRefRegistry.Register("1", "Grass"),
                ForeignRefRegistry.Register("2", "Rock")
            });

            var logs = new Logs();
            var logger = new RecordingLogger();

            var result = reflector.Deserialize(
                Member(typeof(List<ForeignRefAsset>), "{\"assetGroup\":\"ground\"}"),
                logs: logs,
                logger: logger);

            _output.WriteLine($"{logs}\n{string.Join("\n", logger.Entries)}");

            var list = Assert.IsType<List<ForeignRefAsset>>(result);
            Assert.Equal(new[] { "Grass", "Rock" }, list.Select(a => a.DisplayName));

            // The decline the derived converter depends on must be Trace, never Error, and must never
            // have thrown on the way through.
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
            Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Error);
        }

        [Fact]
        public void DerivedConverter_ThatAlsoDeclines_StillEndsInOneExplicitFailure()
        {
            var reflector = new Reflector();
            reflector.Converters.Add(new ForeignGroupArrayReflectionConverter());

            var logs = new Logs();
            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(Member(typeof(List<ForeignRefAsset>), "{\"assetGroup\":\"missing\"}"), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Contains("assetGroup", exception.Message);
            Assert.Single(logs, log => log.Type == LogType.Error);
        }

        // ------------------------------------------------------------------------------------
        // Fixtures
        // ------------------------------------------------------------------------------------

        public class ElementBase { }
        public class AllowedElement : ElementBase { public int Id; }
        public class BlacklistedElement : ElementBase { public int Secret = 999; }

        /// <summary>Minimal <see cref="ILogger"/> that records severity so Error logs can be asserted on.</summary>
        class RecordingLogger : ILogger
        {
            public readonly List<(LogLevel Level, string Message)> Entries = new();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Entries.Add((logLevel, formatter(state, exception)));

            class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();
                public void Dispose() { }
            }
        }
    }
}

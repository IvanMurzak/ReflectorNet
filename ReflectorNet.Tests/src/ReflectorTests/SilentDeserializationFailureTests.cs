/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// A plain struct with no registered <c>JsonConverter</c> and no dedicated reflection converter,
    /// so it falls through to the generic reflection converter. Stands in for any consumer-owned
    /// engine type (Godot.Variant, a Unity struct, an Unreal struct, ...) that reaches the same path.
    /// </summary>
    public struct SilentFailureProbeStruct
    {
        public int X;
        public int Y;
    }

    public class SilentFailureProbeClass
    {
        public SilentFailureProbeStruct Probe { get; set; }
    }

    public static class SilentFailureProbeTarget
    {
        public static bool Called;
        public static SilentFailureProbeStruct Received;

        public static void Reset()
        {
            Called = false;
            Received = default;
        }

        public static void SetProbe(SilentFailureProbeStruct value)
        {
            Called = true;
            Received = value;
        }
    }

    /// <summary>
    /// Regression tests for the "silent deserialization failure" defect.
    ///
    /// A <c>value</c> payload that is a JSON object but NOT a well-formed <see cref="SerializedMember"/>
    /// used to be swallowed by a bare <c>catch { }</c> inside
    /// <c>ExtensionsJsonElement.DeserializeValueSerializedMember</c>. That method returned
    /// <c>reflector.GetDefaultValue(type)</c> and the caller reported SUCCESS. For a value type the
    /// boxed <c>default(T)</c> even satisfies <see cref="Type.IsInstanceOfType"/>, so
    /// <c>MethodWrapper.VerifyParameters</c> accepted it and the method was invoked with a meaningless
    /// value while <c>Reflector.MethodCall</c> reported "[Success]".
    /// </summary>
    public class SilentDeserializationFailureTests : BaseTest
    {
        public SilentDeserializationFailureTests(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// 'X'/'Y' are unknown keys for <see cref="SerializedMember"/>, so
        /// <c>SerializedMemberConverter.Read</c> throws
        /// <c>JsonException("Unexpected property name: 'X'. ...")</c>.
        /// </summary>
        static SerializedMember UndeserializablePayload(string? name = "value") => new SerializedMember
        {
            name = name,
            typeName = typeof(SilentFailureProbeStruct).GetTypeId(),
            valueJsonElement = JsonDocument.Parse("{\"X\":1,\"Y\":2}").RootElement
        };

        static MethodRef ProbeTargetFilter() => new MethodRef
        {
            Namespace = typeof(SilentFailureProbeTarget).Namespace,
            TypeName = nameof(SilentFailureProbeTarget),
            MethodName = nameof(SilentFailureProbeTarget.SetProbe)
        };

        [Fact]
        public void Deserialize_UndeserializableValue_Throws_InsteadOfReturningSilentDefault()
        {
            var reflector = new Reflector();

            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(UndeserializablePayload()));

            _output.WriteLine($"Expected exception caught: {exception.Message}");

            Assert.Equal(typeof(SilentFailureProbeStruct), exception.TargetType);
            Assert.Equal("value", exception.MemberName);
            Assert.Contains(typeof(SilentFailureProbeStruct).GetTypeId(), exception.Message);
            Assert.Contains("Unexpected property name", exception.Message);
            Assert.IsAssignableFrom<JsonException>(exception.InnerException);
        }

        [Fact]
        public void Deserialize_UndeserializableValue_NeverYieldsBoxedDefault()
        {
            var reflector = new Reflector();

            object? result = null;
            var completed = false;
            try
            {
                result = reflector.Deserialize(UndeserializablePayload());
                completed = true;
            }
            catch (DeserializationException)
            {
                // expected
            }

            // The historic bug: `result` was a boxed `default(SilentFailureProbeStruct)`. That value
            // passes `IsInstanceOfType`, so nothing downstream could tell it apart from a value that
            // really was deserialized. No result at all may be handed back.
            Assert.False(completed, "Deserializing an undeserializable payload must not complete successfully.");
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_UndeserializableValue_RecordsErrorInLogs()
        {
            var reflector = new Reflector();
            var logs = new Logs();

            Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(UndeserializablePayload(), logs: logs));

            _output.WriteLine(logs.ToString());

            Assert.Contains(logs, log => log.Type == LogType.Error);
            Assert.Contains("Unexpected property name", logs.ToString());
        }

        [Fact]
        public void DeserializeValueSerializedMember_DoesNotSwallowJsonException()
        {
            var reflector = new Reflector();
            JsonElement? value = JsonDocument.Parse("{\"X\":1,\"Y\":2}").RootElement;

            // The extension method is public API. It must surface the parse failure rather than
            // silently answering `default(SilentFailureProbeStruct)`.
            var exception = Assert.ThrowsAny<JsonException>(() => value.DeserializeValueSerializedMember(
                reflector,
                type: typeof(SilentFailureProbeStruct),
                name: "value"));

            _output.WriteLine($"Expected exception caught: {exception.Message}");
        }

        [Fact]
        public void Deserialize_ValueIsNotAJsonObject_Throws()
        {
            var reflector = new Reflector();
            var data = new SerializedMember
            {
                name = "value",
                typeName = typeof(SilentFailureProbeStruct).GetTypeId(),
                valueJsonElement = JsonDocument.Parse("5").RootElement
            };

            var exception = Assert.Throws<DeserializationException>(() => reflector.Deserialize(data));

            _output.WriteLine($"Expected exception caught: {exception.Message}");
            Assert.Equal(typeof(SilentFailureProbeStruct), exception.TargetType);
        }

        [Fact]
        public void MethodCall_UndeserializableParameter_ReportsError_AndDoesNotInvokeMethod()
        {
            SilentFailureProbeTarget.Reset();

            var reflector = new Reflector();
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: ProbeTargetFilter(),
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                inputParameters: new SerializedMemberList { UndeserializablePayload() },
                executeInMainThread: false);

            _output.WriteLine(result);

            // Before the fix this returned "[Success] Execution result:" while SetProbe had already run
            // with `default(SilentFailureProbeStruct)` - a no-op reported as a success.
            Assert.StartsWith("[Error]", result);
            Assert.Contains("Failed to deserialize input parameter 'value'", result);
            Assert.Contains("Unexpected property name", result);
            Assert.False(SilentFailureProbeTarget.Called,
                "The method must not be invoked when an argument failed to deserialize.");
        }

        [Fact]
        public void MethodCall_WellFormedParameter_StillSucceeds()
        {
            SilentFailureProbeTarget.Reset();

            var reflector = new Reflector();

            // The cascade shape ReflectorNet itself produces: 'value' is an empty JSON object and the
            // members travel in 'fields'.
            var member = new SerializedMember
            {
                name = "value",
                typeName = typeof(SilentFailureProbeStruct).GetTypeId(),
                valueJsonElement = JsonDocument.Parse("{}").RootElement
            };
            member.AddField(SerializedMember.FromValue(reflector, typeof(int), 1, name: nameof(SilentFailureProbeStruct.X)));
            member.AddField(SerializedMember.FromValue(reflector, typeof(int), 2, name: nameof(SilentFailureProbeStruct.Y)));

            var result = reflector.MethodCall(
                reflector: reflector,
                filter: ProbeTargetFilter(),
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                inputParameters: new SerializedMemberList { member },
                executeInMainThread: false);

            _output.WriteLine(result);

            Assert.StartsWith("[Success]", result);
            Assert.True(SilentFailureProbeTarget.Called);
            Assert.Equal(1, SilentFailureProbeTarget.Received.X);
            Assert.Equal(2, SilentFailureProbeTarget.Received.Y);
        }

        [Fact]
        public void TryModify_UndeserializableValue_ReportsError_WithoutThrowing()
        {
            var reflector = new Reflector();
            object? target = null; // forces TryModify down the "instantiate via Deserialize" branch
            var logs = new Logs();

            // TryModify HAS an error channel (bool result + Logs), so it must keep reporting rather
            // than throwing - only the value-returning Deserialize path throws.
            var success = reflector.TryModify(ref target, UndeserializablePayload(), logs: logs);

            _output.WriteLine($"success={success}\n{logs}");

            Assert.False(success);
            Assert.Contains(logs, log => log.Type == LogType.Error);
            Assert.Contains("Unexpected property name", logs.ToString());
        }

        [Fact]
        public void SetProperty_UndeserializableValue_ReportsError_WithoutThrowing()
        {
            var reflector = new Reflector();
            var type = typeof(SilentFailureProbeClass);
            var converter = reflector.Converters.GetConverter(type);
            Assert.NotNull(converter);

            object? target = new SilentFailureProbeClass();
            var propertyInfo = type.GetProperty(nameof(SilentFailureProbeClass.Probe))!;
            var logs = new Logs();

            var success = converter!.SetProperty(
                reflector,
                obj: ref target,
                type: typeof(SilentFailureProbeStruct),
                propertyInfo: propertyInfo,
                value: UndeserializablePayload(name: nameof(SilentFailureProbeClass.Probe)),
                logs: logs);

            _output.WriteLine($"success={success}\n{logs}");

            Assert.False(success);
            Assert.Contains(logs, log => log.Type == LogType.Error);
            Assert.Contains("Unexpected property name", logs.ToString());
            // The property must be left untouched - never overwritten with a fabricated default.
            Assert.Equal(default, ((SilentFailureProbeClass)target!).Probe);
        }

        [Fact]
        public void Deserialize_ExplicitJsonNullValue_IsNotAFailure()
        {
            var reflector = new Reflector();

            // An explicit `"value": null` means "no value"; it must keep resolving to the default
            // instead of being reported as an undeserializable payload.
            var data = new SerializedMember
            {
                name = "value",
                typeName = typeof(SilentFailureProbeStruct).GetTypeId(),
                valueJsonElement = JsonDocument.Parse("null").RootElement
            };

            var result = reflector.Deserialize(data);

            Assert.Equal(default(SilentFailureProbeStruct), result);
        }
    }
}

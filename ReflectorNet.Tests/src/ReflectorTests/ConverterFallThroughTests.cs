/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
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
    /// Stand-in for a consumer-owned engine object that is addressed by reference rather than by
    /// value - the role <c>UnityEngine.Material</c> / <c>UnityEngine.Sprite</c> play in Unity-MCP.
    /// </summary>
    public class ForeignRefAsset
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;

        public override string ToString() => $"{nameof(ForeignRefAsset)}({Id}:{DisplayName})";
    }

    /// <summary>
    /// The consumer's asset lookup. Stands in for <c>UnityEditor.EditorUtility.EntityIdToObject</c> /
    /// <c>AssetDatabase.LoadAssetAtPath</c>.
    /// </summary>
    public static class ForeignRefRegistry
    {
        static readonly Dictionary<string, ForeignRefAsset> _byId = new();

        public static void Reset() => _byId.Clear();

        public static ForeignRefAsset Register(string id, string displayName)
        {
            var asset = new ForeignRefAsset { Id = id, DisplayName = displayName };
            _byId[id] = asset;
            return asset;
        }

        public static ForeignRefAsset? Find(string id)
            => _byId.TryGetValue(id, out var asset) ? asset : null;
    }

    /// <summary>
    /// Consumer-simulation converter. Reproduces, WITHOUT any engine dependency, the exact pattern
    /// used by Unity-MCP's <c>UnityEngine_Sprite_ReflectionConverter</c> /
    /// <c>UnityEngine_Texture_ReflectionConverter</c>: call
    /// <c>base.TryDeserializeValueInternal(...)</c> first and, when the base does not produce a
    /// value, apply the converter's OWN resolution of a foreign payload shape
    /// (<c>{"instanceID":"..."}</c>) against a registry.
    /// </summary>
    /// <remarks>
    /// The base declining a payload it does not recognise is the load-bearing behaviour here. It is
    /// <see cref="DeserializationOutcome.NotApplicable"/>, not an error: another link in the chain
    /// (this subclass) resolves it. See <see cref="ConverterFallThroughTests"/>.
    /// </remarks>
    public class ForeignRefReflectionConverter : GenericReflectionConverter<ForeignRefAsset>
    {
        public const string ForeignKey = "instanceID";

        protected override bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            var baseResult = base.TryDeserializeValueInternal(
                reflector: reflector,
                data: data,
                result: out result,
                type: type,
                depth: depth,
                logs: logs,
                logger: logger);

            if (result is ForeignRefAsset)
                return baseResult;

            // The base produced nothing: the payload is not a SerializedMember. It may still be OUR
            // shape - resolve it the way the consumer does.
            if (TryReadForeignId(data.valueJsonElement, out var id))
            {
                result = ForeignRefRegistry.Find(id!);
                return result != null;
            }

            return baseResult;
        }

        public static bool TryReadForeignId(JsonElement? value, out string? id)
        {
            id = null;
            if (value == null || value.Value.ValueKind != JsonValueKind.Object)
                return false;

            if (!value.Value.TryGetProperty(ForeignKey, out var idElement))
                return false;

            id = idElement.ValueKind == JsonValueKind.String
                ? idElement.GetString()
                : idElement.GetRawText();

            return !string.IsNullOrEmpty(id);
        }
    }

    /// <summary>Second stand-in engine type, for the <see cref="ForeignRefDeserializeOverrideConverter"/> pattern.</summary>
    public class ForeignRefComponent
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
    }

    /// <summary>
    /// The OTHER consumer-simulation pattern, mirroring Unity-MCP's
    /// <c>UnityEngine_Object_ReflectionConverter.Deserialize</c>: override <c>Deserialize</c>, call
    /// <c>TryDeserializeValue(...)</c> purely as a GATE, DISCARD its result, and resolve the
    /// reference from the raw payload afterwards.
    /// </summary>
    /// <remarks>
    /// This converter exists mostly as a COMPILE-TIME and behavioural lock on the consumer seam.
    /// <list type="bullet">
    ///   <item><description>
    ///     It calls the 8-argument <c>TryDeserializeValue</c> overload with the exact named-argument
    ///     form Unity-MCP uses. Adding the tri-state <c>outcome</c> overload must not make that call
    ///     ambiguous or resolve it to the wrong overload.
    ///   </description></item>
    ///   <item><description>
    ///     That gate must return <c>true</c> for a declined (NotApplicable) payload. If it ever
    ///     returns <c>false</c> - or throws - this converter returns early and the reference is
    ///     never resolved, which is exactly the regression.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public class ForeignRefDeserializeOverrideConverter : GenericReflectionConverter<ForeignRefComponent>
    {
        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            if (!TryDeserializeValue(
                reflector,
                data: data,
                result: out var result,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger))
            {
                return result;
            }

            // The base's answer is deliberately ignored - this converter owns the resolution.
            if (ForeignRefReflectionConverter.TryReadForeignId(data.valueJsonElement, out var id))
            {
                var asset = ForeignRefRegistry.Find(id!);
                if (asset != null)
                    return new ForeignRefComponent { Id = asset.Id, DisplayName = asset.DisplayName };
            }

            return result;
        }
    }

    /// <summary>
    /// Regression tests for the converter fall-through contract.
    ///
    /// <para>
    /// History: commit <c>12f99093</c> correctly stopped
    /// <c>ExtensionsJsonElement.DeserializeValueSerializedMember</c> from swallowing a
    /// <see cref="JsonException"/> and answering with a fabricated default. But the bare
    /// <c>catch { }</c> it removed was carrying TWO meanings at once - "this payload is broken" AND
    /// "this payload is not my shape, carry on" - and making it throw severed the second one. Every
    /// consumer converter that layers its own resolution on top of the base
    /// (<c>base.TryDeserializeValueInternal(...)</c>) lost the ability to see the payload at all,
    /// and ReflectorNet started logging <see cref="LogType.Error"/> for a perfectly ordinary
    /// "not mine" condition. That regression cost 11 red tests in Unity-MCP and could only be seen
    /// by building the DLL and running a Unity Editor suite.
    /// </para>
    ///
    /// <para>
    /// These tests reproduce the consumer pattern in-repo so <c>dotnet test</c> alone catches it.
    /// </para>
    /// </summary>
    public class ConverterFallThroughTests : BaseTest, IDisposable
    {
        public ConverterFallThroughTests(ITestOutputHelper output) : base(output)
        {
            ForeignRefRegistry.Reset();
        }

        public void Dispose() => ForeignRefRegistry.Reset();

        static Reflector CreateReflectorWithConsumerConverter()
        {
            var reflector = new Reflector();
            reflector.Converters.Add(new ForeignRefReflectionConverter());
            return reflector;
        }

        /// <summary>A Unity-style object reference: a JSON object with no SerializedMember key at all.</summary>
        static SerializedMember ForeignShapePayload(string id, string? name = "sharedAsset") => new SerializedMember
        {
            name = name,
            typeName = typeof(ForeignRefAsset).GetTypeId(),
            valueJsonElement = JsonDocument.Parse($"{{\"instanceID\":\"{id}\"}}").RootElement
        };

        // ------------------------------------------------------------------------------------
        // 1. The fall-through works.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void ForeignShape_BaseDeclines_SubclassResolution_Succeeds()
        {
            var reflector = CreateReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("12345", "Grass");

            var logs = new Logs();
            var result = reflector.Deserialize(ForeignShapePayload("12345"), logs: logs);

            _output.WriteLine(logs.ToString());

            var asset = Assert.IsType<ForeignRefAsset>(result);
            Assert.Equal("12345", asset.Id);
            Assert.Equal("Grass", asset.DisplayName);
        }

        [Fact]
        public void ForeignShape_BaseDeclines_WithoutThrowing()
        {
            var reflector = CreateReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("12345", "Grass");

            // The base converter must NOT throw when handed a payload that is simply not its shape:
            // throwing severs the fall-through and the subclass never gets to resolve the reference.
            var converter = reflector.Converters.GetConverter(typeof(ForeignRefAsset));
            Assert.IsType<ForeignRefReflectionConverter>(converter);

            var exception = Record.Exception(() => converter!.Deserialize(
                reflector,
                data: ForeignShapePayload("12345"),
                fallbackType: typeof(ForeignRefAsset)));

            Assert.Null(exception);
        }

        [Fact]
        public void ForeignShape_DeserializeOverridePattern_GateReturnsTrue_AndResolutionRuns()
        {
            // The second consumer pattern: TryDeserializeValue is used only as a GATE and its result
            // is discarded. The gate MUST stay open for a declined payload, or the converter returns
            // early and the reference is never resolved.
            var reflector = new Reflector();
            reflector.Converters.Add(new ForeignRefDeserializeOverrideConverter());
            ForeignRefRegistry.Register("777", "Rock");

            var logs = new Logs();
            var result = reflector.Deserialize(new SerializedMember
            {
                name = "component",
                typeName = typeof(ForeignRefComponent).GetTypeId(),
                valueJsonElement = JsonDocument.Parse("{\"instanceID\":\"777\"}").RootElement
            }, logs: logs);

            _output.WriteLine(logs.ToString());

            var component = Assert.IsType<ForeignRefComponent>(result);
            Assert.Equal("Rock", component.DisplayName);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
        }

        // ------------------------------------------------------------------------------------
        // 2. No Error log on the NotApplicable path. This is the in-repo equivalent of Unity's
        //    LogAssert teardown check, and it is what actually caught the regression.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void ForeignShape_ResolvedBySubclass_LogsNoError()
        {
            var reflector = CreateReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("12345", "Grass");

            var logs = new Logs();
            var result = reflector.Deserialize(ForeignShapePayload("12345"), logs: logs);

            _output.WriteLine(logs.ToString());

            Assert.NotNull(result);

            // "not my shape" is not an error. ReflectorNet must never log Error for it - a consumer
            // that fails its build on unexpected error logs (Unity's LogAssert) goes red otherwise.
            Assert.DoesNotContain(logs, log => log.Type == LogType.Error);
            Assert.DoesNotContain(logs, log => log.Type == LogType.Critical);
        }

        [Fact]
        public void ForeignShape_ResolvedBySubclass_DoesNotLogErrorToILogger()
        {
            var reflector = CreateReflectorWithConsumerConverter();
            ForeignRefRegistry.Register("12345", "Grass");

            var logger = new RecordingLogger();
            var result = reflector.Deserialize(ForeignShapePayload("12345"), logger: logger);

            _output.WriteLine(string.Join("\n", logger.Entries));

            Assert.NotNull(result);
            Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Error);
        }

        // ------------------------------------------------------------------------------------
        // 3. A genuine failure is still loud: some known SerializedMember keys plus an unknown one
        //    is a payload TRYING to be a SerializedMember and getting it wrong.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void MalformedSerializedMember_IsLoud_AndSkipsSubclassResolution()
        {
            var reflector = CreateReflectorWithConsumerConverter();

            // The registry COULD resolve this id. It must not be consulted: the payload is a broken
            // SerializedMember, not a foreign shape, so the chain aborts at the first Failed link.
            ForeignRefRegistry.Register("12345", "Grass");

            var data = new SerializedMember
            {
                name = "sharedAsset",
                typeName = typeof(ForeignRefAsset).GetTypeId(),
                valueJsonElement = JsonDocument
                    .Parse("{\"typeName\":\"" + typeof(ForeignRefAsset).GetTypeId() + "\",\"instanceID\":\"12345\"}")
                    .RootElement
            };

            var logs = new Logs();
            var exception = Assert.Throws<DeserializationException>(() => reflector.Deserialize(data, logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Equal(typeof(ForeignRefAsset), exception.TargetType);
            Assert.Contains("instanceID", exception.Message);
            Assert.IsAssignableFrom<JsonException>(exception.InnerException);
            Assert.Contains(logs, log => log.Type == LogType.Error);
        }

        // ------------------------------------------------------------------------------------
        // 4. No converter at all -> terminal chain failure, explicit, no fabricated default.
        // ------------------------------------------------------------------------------------

        [Fact]
        public void ForeignShape_NoConverterUnderstandsIt_IsATerminalFailure()
        {
            // Same payload, but WITHOUT the consumer converter registered: nobody in the chain
            // understands the shape, so the chain end must be loud.
            var reflector = new Reflector();

            var logs = new Logs();
            var exception = Assert.Throws<DeserializationException>(
                () => reflector.Deserialize(ForeignShapePayload("12345"), logs: logs));

            _output.WriteLine($"{exception.Message}\n{logs}");

            Assert.Equal(typeof(ForeignRefAsset), exception.TargetType);
            Assert.Contains(typeof(ForeignRefAsset).GetTypeId(), exception.Message);
            Assert.Contains("instanceID", exception.Message);
            Assert.Contains(logs, log => log.Type == LogType.Error);
        }

        [Fact]
        public void ForeignShape_NoConverterUnderstandsIt_NeverYieldsAFabricatedValue()
        {
            var reflector = new Reflector();

            object? result = null;
            var completed = false;
            try
            {
                result = reflector.Deserialize(ForeignShapePayload("12345"));
                completed = true;
            }
            catch (DeserializationException)
            {
                // expected
            }

            Assert.False(completed, "An unresolvable payload must not complete successfully.");
            Assert.Null(result);
        }

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

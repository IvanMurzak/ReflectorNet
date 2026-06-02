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
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Verifies that <see cref="Reflector.TryPatch(ref object?, string, Type?, int, Logs?, System.Reflection.BindingFlags, Microsoft.Extensions.Logging.ILogger?)"/>
    /// routes a JSON-object node through a registered converter (instead of structurally descending
    /// into its keys) when that converter reports <see cref="IReflectionConverter.TreatJsonObjectAsAtomicValue"/> == true.
    /// This is the Unity-agnostic reproduction of issue #84: a Unity object reference encoded as
    /// <c>{"instanceID":"…"}</c> was descended into key-by-key and failed with
    /// <c>Segment 'instanceID' not found</c> instead of being resolved by its converter.
    /// </summary>
    public class TryPatchAtomicConverterTests : BaseTest
    {
        public TryPatchAtomicConverterTests(ITestOutputHelper output) : base(output) { }

        // (a) The bug repro, minus Unity: a JSON OBJECT node targeting a converter-atomic type is
        //     resolved by the converter rather than throwing "Segment 'ref' not found".
        [Fact]
        public void TryPatch_ConverterAtomicObjectNode_ResolvesViaConverter()
        {
            var reflector = new Reflector();
            var registry = new AssetRegistry();
            var resolved = new AssetRef { id = "asset-1", displayName = "Material A" };
            registry.Register(resolved);
            reflector.Converters.Add(new AssetRefAtomicConverter(registry));

            var container = new AssetContainer { asset = new AssetRef { id = "stale", displayName = "Stale" } };
            object? obj = container;

            // A JSON OBJECT (not a leaf) targeting the converter-atomic AssetRef member.
            var json = @"{ ""asset"": { ""ref"": ""asset-1"" } }";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (AssetContainer)obj!;
            Assert.NotNull(result.asset);
            Assert.Equal("asset-1", result.asset!.id);
            Assert.Equal("Material A", result.asset!.displayName); // came from the registry, not the patch
            Assert.DoesNotContain("not found", logs.ToString());
        }

        // (a') Same routing at the ROOT (no enclosing member), proving the branch fires regardless of depth.
        [Fact]
        public void TryPatch_ConverterAtomicObjectNode_AtRoot_ResolvesViaConverter()
        {
            var reflector = new Reflector();
            var registry = new AssetRegistry();
            registry.Register(new AssetRef { id = "root-asset", displayName = "Root" });
            reflector.Converters.Add(new AssetRefAtomicConverter(registry));

            object? obj = new AssetRef { id = "old", displayName = "Old" };

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, @"{ ""ref"": ""root-asset"" }", logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (AssetRef)obj!;
            Assert.Equal("root-asset", result.id);
            Assert.Equal("Root", result.displayName);
        }

        // (b) A plain POCO sub-object WITHOUT the flag still descends structurally (unchanged behavior).
        [Fact]
        public void TryPatch_PlainPocoSubObject_StillDescendsStructurally()
        {
            var reflector = new Reflector(); // no atomic converter registered → POCO descent
            var container = new PocoContainer { inner = new Inner { a = 1, b = 2 } };
            object? obj = container;

            var json = @"{ ""inner"": { ""a"": 42 } }";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (PocoContainer)obj!;
            Assert.Equal(42, result.inner!.a); // descended into and changed
            Assert.Equal(2, result.inner!.b);  // untouched — proves structural descent, not atomic replace
        }

        // (c) A $type-carrying object node still flows through the structural polymorphic-replacement
        //     path even when the target type's converter is atomic — the $type guard takes precedence.
        [Fact]
        public void TryPatch_TypeHintOnAtomicTarget_StillUsesStructuralReplacement()
        {
            var reflector = new Reflector();
            var registry = new AssetRegistry();
            reflector.Converters.Add(new AssetRefAtomicConverter(registry));

            var container = new AssetContainer { asset = new AssetRef { id = "base", displayName = "Base" } };
            object? obj = container;

            // $type upgrades AssetRef → DerivedAssetRef and sets the derived field structurally.
            var derivedTypeId = typeof(DerivedAssetRef).GetTypeId();
            var json = $@"{{ ""asset"": {{ ""$type"": ""{derivedTypeId}"", ""id"": ""poly"", ""extra"": ""x"" }} }}";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (AssetContainer)obj!;
            Assert.IsType<DerivedAssetRef>(result.asset);
            Assert.Equal("poly", result.asset!.id);
            Assert.Equal("x", ((DerivedAssetRef)result.asset!).extra);
        }

        // (d) A non-object leaf still works (leaf path is unchanged by the new branch).
        [Fact]
        public void TryPatch_NonObjectLeaf_StillWorks()
        {
            var reflector = new Reflector();
            var container = new PocoContainer { inner = new Inner { a = 1, b = 2 }, label = "old" };
            object? obj = container;

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, @"{ ""label"": ""new"" }", logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            Assert.Equal("new", ((PocoContainer)obj!).label);
        }

        // (e) Error-contract lock for #84: when the converter is reached but its SetValue resolution
        //     FAILS (unresolvable ref), TryPatch must return false and surface the CONVERTER's error —
        //     it must NOT fall through to structural key descent (which would re-produce the original
        //     "Segment 'ref' not found on type 'AssetRef'" bug).
        [Fact]
        public void TryPatch_ConverterAtomicObjectNode_ResolutionFails_ReturnsFalseWithoutStructuralFallthrough()
        {
            var reflector = new Reflector();
            var registry = new AssetRegistry(); // empty — nothing resolves
            reflector.Converters.Add(new AssetRefAtomicConverter(registry));

            var container = new AssetContainer { asset = new AssetRef { id = "keep", displayName = "Keep" } };
            object? obj = container;

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, @"{ ""asset"": { ""ref"": ""missing-id"" } }", logs: logs);

            var logsText = logs.ToString();
            _output.WriteLine(logsText);

            // (1) the patch failed
            Assert.False(success);
            // (2) the converter's own error is what surfaced
            Assert.Contains("could not resolve ref 'missing-id'", logsText);
            // (3) it did NOT fall through to structural key descent (the original #84 failure mode)
            Assert.DoesNotContain("not found", logsText);
        }

        // ─── Unity-agnostic test fixtures ──────────────────────────────────────────

        // A reference-like POCO whose JSON encoding is an object ({"ref":"<id>"}) that a converter
        // resolves indivisibly — stands in for a Unity object reference ({"instanceID":"<id>"}).
        public class AssetRef
        {
            public string id = string.Empty;
            public string displayName = string.Empty;
        }

        public class DerivedAssetRef : AssetRef
        {
            public string extra = string.Empty;
        }

        public class AssetContainer
        {
            public AssetRef? asset;
        }

        // A registry resolving an id → instance, mirroring FindAssetObject(type) on the Unity side.
        public class AssetRegistry
        {
            private readonly Dictionary<string, AssetRef> _byId = new();
            public void Register(AssetRef asset) => _byId[asset.id] = asset;
            public AssetRef? Resolve(string id) => _byId.TryGetValue(id, out var a) ? a : null;
        }

        // Stub converter: reports TreatJsonObjectAsAtomicValue == true for AssetRef and resolves a
        // {"ref":"<id>"} object node via the registry inside SetValue — the same surface the Unity
        // UnityEngine_Object_ReflectionConverter uses. No Unity dependency.
        public class AssetRefAtomicConverter : GenericReflectionConverter<AssetRef>
        {
            private readonly AssetRegistry _registry;
            public AssetRefAtomicConverter(AssetRegistry registry) => _registry = registry;

            public override bool TreatJsonObjectAsAtomicValue(Type type) => true;

            protected override bool SetValue(
                Reflector reflector,
                ref object? obj,
                Type type,
                JsonElement? value,
                int depth = 0,
                Logs? logs = null,
                Microsoft.Extensions.Logging.ILogger? logger = null)
            {
                if (value == null || value.Value.ValueKind != JsonValueKind.Object)
                {
                    logs?.Error("AssetRefAtomicConverter expects a JSON object.", depth);
                    return false;
                }
                if (!value.Value.TryGetProperty("ref", out var refElement))
                {
                    logs?.Error("AssetRefAtomicConverter requires a 'ref' property.", depth);
                    return false;
                }
                var id = refElement.GetString();
                if (string.IsNullOrEmpty(id))
                {
                    logs?.Error("AssetRefAtomicConverter 'ref' is empty.", depth);
                    return false;
                }
                var resolved = _registry.Resolve(id!);
                if (resolved == null)
                {
                    logs?.Error($"AssetRefAtomicConverter could not resolve ref '{id}'.", depth);
                    return false;
                }
                obj = resolved;
                logs?.Success($"AssetRef resolved to '{id}'.", depth);
                return true;
            }
        }

        public class Inner
        {
            public int a;
            public int b;
        }

        public class PocoContainer
        {
            public Inner? inner;
            public string label = string.Empty;
        }
    }
}

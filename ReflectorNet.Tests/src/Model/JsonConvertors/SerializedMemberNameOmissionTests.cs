/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Model.JsonConvertors
{
    /// <summary>
    /// Regression tests for https://github.com/IvanMurzak/ReflectorNet/issues/86.
    /// <see cref="SerializedMemberConverter"/> used to write the "name" property unconditionally,
    /// emitting JSON null for value-only / root / unnamed members. The converter's own schema
    /// (<see cref="SerializedMemberConverter.Schema"/>) types "name" as a non-nullable "string" and
    /// does NOT list it in "required", so the correct behavior is to omit the key when the value is null.
    /// Emitting "name": null violated the advertised output schema and was rejected by strict MCP clients
    /// with error -32602.
    /// </summary>
    public class SerializedMemberNameOmissionTests : BaseTest
    {
        public SerializedMemberNameOmissionTests(ITestOutputHelper output) : base(output) { }

        static JsonObject SerializeToObject(SerializedMember member, Reflector reflector)
        {
            var json = member.ToJson(reflector);
            Assert.False(string.IsNullOrEmpty(json), "Serialization produced empty JSON.");
            var node = JsonNode.Parse(json!);
            Assert.NotNull(node);
            return node!.AsObject();
        }

        [Fact]
        public void Write_NullName_OmitsNameKey()
        {
            var reflector = new Reflector();
            var member = new SerializedMember { name = null, typeName = "System.String" };

            var obj = SerializeToObject(member, reflector);
            _output.WriteLine($"Serialized (null name): {obj.ToJsonString()}");

            // The "name" key must be ABSENT entirely (not present-with-null).
            Assert.False(obj.ContainsKey(nameof(SerializedMember.name)),
                $"Expected no 'name' key when name is null, but JSON was: {obj.ToJsonString()}");

            // The required key must still be present.
            Assert.True(obj.ContainsKey(nameof(SerializedMember.typeName)));
        }

        [Fact]
        public void Write_NonNullName_WritesNameAsString()
        {
            var reflector = new Reflector();
            var member = new SerializedMember { name = "foo", typeName = "System.String" };

            var obj = SerializeToObject(member, reflector);
            _output.WriteLine($"Serialized (non-null name): {obj.ToJsonString()}");

            Assert.True(obj.ContainsKey(nameof(SerializedMember.name)),
                $"Expected a 'name' key when name is non-null, but JSON was: {obj.ToJsonString()}");

            var nameNode = obj[nameof(SerializedMember.name)];
            Assert.NotNull(nameNode);
            // A present "name" must be a JSON string (matching the schema's {"type":"string"}), not null.
            Assert.IsType<JsonValue>(nameNode, exactMatch: false);
            Assert.Equal("foo", nameNode!.GetValue<string>());
        }

        [Fact]
        public void Schema_TypesNameAsString_AndDoesNotRequireIt()
        {
            // Guard the schema contract this fix relies on: "name" is a non-nullable "string" and is
            // NOT in "required". If a future change widens the schema (e.g. to ["string","null"]) or adds
            // "name" to "required", the omit-when-null fix would no longer be schema-valid.
            var schema = SerializedMemberConverter.Schema.AsObject();
            _output.WriteLine($"Schema: {schema.ToJsonString()}");

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(nameof(SerializedMember.name)));

            var nameSchema = properties[nameof(SerializedMember.name)]!.AsObject();
            Assert.Equal(JsonSchema.String, nameSchema[JsonSchema.Type]!.ToString());

            var required = schema[JsonSchema.Required]!.AsArray();
            Assert.DoesNotContain(required, r => r?.ToString() == nameof(SerializedMember.name));
            Assert.Contains(required, r => r?.ToString() == nameof(SerializedMember.typeName));
        }

        [Fact]
        public void Write_NullName_IsSchemaValid_AndRoundTripsToNull()
        {
            var reflector = new Reflector();
            var member = new SerializedMember { name = null, typeName = "System.String" };

            // 1. Serialized JSON must not carry a "name" key — so it cannot violate the {"type":"string"} schema.
            var obj = SerializeToObject(member, reflector);
            Assert.False(obj.ContainsKey(nameof(SerializedMember.name)));

            // 2. Round-trip: deserializing the name-less JSON yields a member with no meaningful name
            //    (Read leaves the default — string.Empty — when the key is absent; it does NOT resurrect
            //    a JSON null), and re-serializing it STILL omits the key, so the round-trip stays
            //    schema-valid and never re-introduces the "name": null the fix removed.
            var json = obj.ToJsonString();
            var deserialized = reflector.JsonSerializer.Deserialize<SerializedMember>(json);
            Assert.NotNull(deserialized);
            Assert.True(string.IsNullOrEmpty(deserialized!.name),
                $"Expected null-or-empty name after round-trip, got '{deserialized.name}'.");
            Assert.Equal("System.String", deserialized.typeName);

            // Re-serializing a null/empty-name member must not emit "name": null.
            var reserialized = SerializeToObject(deserialized, reflector);
            if (reserialized.ContainsKey(nameof(SerializedMember.name)))
            {
                // If a key is present at all (e.g. empty-string default), it must be a string, never null.
                Assert.IsType<JsonValue>(reserialized[nameof(SerializedMember.name)], exactMatch: false);
            }

            // 3. Non-null name round-trips and is preserved.
            var named = new SerializedMember { name = "foo", typeName = "System.String" };
            var namedJson = named.ToJson(reflector);
            var namedBack = reflector.JsonSerializer.Deserialize<SerializedMember>(namedJson!);
            Assert.NotNull(namedBack);
            Assert.Equal("foo", namedBack!.name);
        }

        [Fact]
        public void Write_NestedChildWithNullName_OmitsNameKeyRecursively()
        {
            // The recursive case from issue #86: a SerializedMember tree where a CHILD (under fields/props)
            // has a null name. The converter serializes children through the same converter, so the omission
            // must hold at every depth — not just at the root. This is the most material scenario because
            // real tool outputs (assets-get-data, gameobject-component-get, etc.) return nested trees whose
            // value-only leaf members legitimately have null names.
            var reflector = new Reflector();

            var fieldChild = new SerializedMember { name = null, typeName = "System.Int32" };
            var propChild = new SerializedMember { name = null, typeName = "System.Boolean" };

            var parent = new SerializedMember { name = "root", typeName = "Some.Composite.Type" };
            parent.AddField(fieldChild);
            parent.AddProperty(propChild);

            var obj = SerializeToObject(parent, reflector);
            _output.WriteLine($"Serialized (nested null-name children): {obj.ToJsonString()}");

            // Parent retains its (non-null) name and typeName.
            Assert.Equal("root", obj[nameof(SerializedMember.name)]!.GetValue<string>());
            Assert.Equal("Some.Composite.Type", obj[nameof(SerializedMember.typeName)]!.GetValue<string>());

            // Each nested child object must omit the "name" key entirely while keeping typeName.
            var fieldChildObj = obj[nameof(SerializedMember.fields)]!.AsArray()[0]!.AsObject();
            Assert.False(fieldChildObj.ContainsKey(nameof(SerializedMember.name)),
                $"Expected no 'name' key on the nested field child, but JSON was: {fieldChildObj.ToJsonString()}");
            Assert.Equal("System.Int32", fieldChildObj[nameof(SerializedMember.typeName)]!.GetValue<string>());

            var propChildObj = obj[nameof(SerializedMember.props)]!.AsArray()[0]!.AsObject();
            Assert.False(propChildObj.ContainsKey(nameof(SerializedMember.name)),
                $"Expected no 'name' key on the nested prop child, but JSON was: {propChildObj.ToJsonString()}");
            Assert.Equal("System.Boolean", propChildObj[nameof(SerializedMember.typeName)]!.GetValue<string>());
        }
    }
}

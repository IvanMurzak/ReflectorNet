/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Members of a reflection-generated object schema are never listed in <c>required</c>: deserialization leaves a
    /// missing member at its default, so every key may be left out (the RQL tool layer's contract). Hand-written
    /// converter schemas keep their own <c>required</c>.
    /// </summary>
    public class MemberRequiredTests : BaseTest
    {
        public MemberRequiredTests(ITestOutputHelper output) : base(output) { }

        public struct Point { public float x; public float y; }

        public class WaveConfig
        {
            public int count;
            public float interval;
            public Point origin;
            public bool loop;
            public string? label;
            public int? limit;

            public int Priority { get; set; }
            public Point Offset { get; set; }
            public double Speed { get; set; }
        }

        public static void Spawn(WaveConfig config) { }

        static JsonObject Definition(JsonNode schema, Type type)
            => schema[JsonSchema.Defs]![type.GetSchemaTypeId()]!.AsObject();

        [Fact]
        public void ValueTypeFieldsAndProperties_AreNotRequired()
        {
            var schema = new Reflector().GetSchema<WaveConfig>().AsObject();
            _output.WriteLine(schema.ToJsonString());

            Assert.False(schema.ContainsKey(JsonSchema.Required));
            var properties = schema[JsonSchema.Properties]!.AsObject();
            foreach (var name in new[] { "count", "interval", "origin", "loop", "label", "limit", nameof(WaveConfig.Priority), nameof(WaveConfig.Offset), nameof(WaveConfig.Speed) })
                Assert.True(properties.ContainsKey(name), name);
        }

        [Fact]
        public void NestedStructDefinition_HasNoRequired()
        {
            var schema = new Reflector().GetSchema<WaveConfig>();
            Assert.False(Definition(schema, typeof(Point)).ContainsKey(JsonSchema.Required));
        }

        [Fact]
        public void ArgumentsSchema_RequiresTheParameter_ButNotItsMembers()
        {
            var method = typeof(MemberRequiredTests).GetMethod(nameof(Spawn), BindingFlags.Public | BindingFlags.Static)!;
            var schema = new Reflector().GetArgumentsSchema(method)!;
            _output.WriteLine(schema.ToJsonString());

            Assert.Equal(new[] { "config" }, schema[JsonSchema.Required]!.AsArray().Select(n => n!.GetValue<string>()).ToArray());
            Assert.False(Definition(schema, typeof(WaveConfig)).ContainsKey(JsonSchema.Required));
            Assert.False(Definition(schema, typeof(Point)).ContainsKey(JsonSchema.Required));
        }

        [Fact]
        public void MissingMembers_DeserializeToDefaults()
        {
            // The behaviour the schema states: an object with no keys is valid and leaves every member at its default.
            var reflector = new Reflector();
            var result = Assert.IsType<WaveConfig>(reflector.JsonSerializer.Deserialize(JsonNode.Parse("{}"), typeof(WaveConfig)));
            Assert.Equal(0, result.count);
            Assert.Equal(0f, result.interval);
            Assert.Equal(default, result.origin);
            Assert.Equal(0, result.Priority);
        }

        [Theory]
        [InlineData(typeof(IPEndPoint))]
        [InlineData(typeof(Complex))]
        public void HandWrittenConverterSchema_KeepsItsRequired(Type type)
        {
            var schema = new Reflector().GetSchema(type);
            _output.WriteLine(schema.ToJsonString());
            Assert.NotEmpty(schema[JsonSchema.Required]!.AsArray());
        }
    }
}

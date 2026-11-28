/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.ComponentModel.DataAnnotations;
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
    /// Members of a reflection-generated object schema (input and output) are not listed in <c>required</c> by default,
    /// whatever their type or nullability: deserialization leaves a missing member at its default. An explicit mark
    /// (the C# 11 <c>required</c> modifier or <c>[DataAnnotations.Required]</c>) makes a member required, nullable or
    /// not. "null" is never added to a member's type. Hand-written converter schemas keep their own <c>required</c>.
    /// </summary>
    public class MemberRequiredTests : SchemaTestBase
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

        public class AnnotatedConfig
        {
            [Required] public int count;
            [Required] public string? label;
            public int other;

            [Required] public double Speed { get; set; }
            [Required] public int? Limit { get; set; }
            public string? Note { get; set; }
        }

        /// <summary>Derived from <see cref="RequiredAttribute"/>: matched through the base type chain.</summary>
        public sealed class StrictRequiredAttribute : RequiredAttribute { }

        public class DerivedAnnotation
        {
            [StrictRequired] public int id;
            public int other;
        }

        public class RenamedAndInherited : AnnotatedConfig
        {
            [Required, System.Text.Json.Serialization.JsonPropertyName("wire_name")] public int Renamed { get; set; }
        }

#if NET7_0_OR_GREATER
        public class ModifierConfig
        {
            public required int count;
            public required string? label;
            public int other;

            public required double Speed { get; set; }
            public required int? Limit { get; set; }
            public string? Note { get; set; }
        }
#endif

        public static void Spawn(WaveConfig config) { }
        public static WaveConfig Build() => new WaveConfig();
        public static AnnotatedConfig BuildAnnotated() => new AnnotatedConfig();

        public class JsonRequiredConfig
        {
            [System.Text.Json.Serialization.JsonRequired] public int count;
            [System.Text.Json.Serialization.JsonRequired] public string? Label { get; set; }
            public int other;
        }

        public class OverrideBase
        {
            [Required] public virtual int Marked { get; set; }
            public virtual int Plain { get; set; }
        }

        public class OverrideDerived : OverrideBase
        {
            public override int Marked { get; set; }
            public override int Plain { get; set; }
        }

        public class WriteAccess
        {
            [Required] public int writable;
            [Required] public readonly int readOnlyField;
            [Required] public int GetterOnly { get; }
            [Required] public int PrivateSet { get; private set; }
            [Required, System.Text.Json.Serialization.JsonInclude] public int IncludedPrivateSet { get; private set; }
            [Required] public int InitOnly { get; init; }
            [Required] public int Settable { get; set; }
        }

        public class ConstructorFilled
        {
            public ConstructorFilled(int id) { Id = id; }
            [Required] public int Id { get; }
            [Required] public int NotInConstructor { get; }
        }

        public static void Create(ConstructorFilled value) { }
        public static void Configure(WriteAccess access) { }
        public static WriteAccess Inspect() => new WriteAccess();

        static JsonObject Definition(JsonNode schema, Type type)
            => schema[JsonSchema.Defs]![type.GetSchemaTypeId()]!.AsObject();

        static string[] RequiredOf(JsonObject schema)
            => schema[JsonSchema.Required] is JsonArray required
                ? required.Select(n => n!.GetValue<string>()).OrderBy(n => n, StringComparer.Ordinal).ToArray()
                : new string[0];

        static MethodInfo Method(string name)
            => typeof(MemberRequiredTests).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

        [Fact]
        public void UnmarkedMembers_AreNotRequired_WhateverTheirType()
        {
            var schema = JsonSchemaValidation(typeof(WaveConfig))!.AsObject();

            Assert.Empty(RequiredOf(schema));
            var properties = schema[JsonSchema.Properties]!.AsObject();
            foreach (var name in new[] { "count", "interval", "origin", "loop", "label", "limit", nameof(WaveConfig.Priority), nameof(WaveConfig.Offset), nameof(WaveConfig.Speed) })
                Assert.True(properties.ContainsKey(name), name);
        }

        [Fact]
        public void NestedStructDefinition_HasNoRequired()
        {
            var schema = JsonSchemaValidation(typeof(WaveConfig))!;
            Assert.Empty(RequiredOf(Definition(schema, typeof(Point))));
        }

        [Fact]
        public void ArgumentsSchema_RequiresTheParameter_ButNotItsMembers()
        {
            var schema = new Reflector().GetArgumentsSchema(Method(nameof(Spawn)))!.AsObject();
            _output.WriteLine(schema.ToJsonString());

            Assert.Equal(new[] { "config" }, RequiredOf(schema));
            Assert.Empty(RequiredOf(Definition(schema, typeof(WaveConfig))));
            Assert.Empty(RequiredOf(Definition(schema, typeof(Point))));
        }

        [Fact]
        public void ReturnSchema_MembersAreNotRequired()
        {
            var schema = new Reflector().GetReturnSchema(Method(nameof(Build)))!;
            _output.WriteLine(schema.ToJsonString());

            Assert.Empty(RequiredOf(Definition(schema, typeof(WaveConfig))));
            Assert.Empty(RequiredOf(Definition(schema, typeof(Point))));
        }

        [Fact]
        public void DataAnnotationsRequired_MakesMembersRequired_NullableOrNot()
        {
            var schema = JsonSchemaValidation(typeof(AnnotatedConfig))!.AsObject();
            Assert.Equal(new[] { nameof(AnnotatedConfig.Limit), nameof(AnnotatedConfig.Speed), "count", "label" }, RequiredOf(schema));
        }

        [Fact]
        public void DataAnnotationsRequired_IsHonoured_InReturnSchemaDefinitions()
        {
            var schema = new Reflector().GetReturnSchema(Method(nameof(BuildAnnotated)))!;
            _output.WriteLine(schema.ToJsonString());
            Assert.Equal(new[] { nameof(AnnotatedConfig.Limit), nameof(AnnotatedConfig.Speed), "count", "label" }, RequiredOf(Definition(schema, typeof(AnnotatedConfig))));
        }

        [Fact]
        public void AttributeDerivedFromRequired_MakesMemberRequired()
        {
            var schema = JsonSchemaValidation(typeof(DerivedAnnotation))!.AsObject();
            Assert.Equal(new[] { "id" }, RequiredOf(schema));
        }

        [Fact]
        public void MarkedMember_UsesItsJsonName_AndInheritedMarksAreHonoured()
        {
            var schema = JsonSchemaValidation(typeof(RenamedAndInherited))!.AsObject();
            Assert.Equal(new[] { nameof(AnnotatedConfig.Limit), nameof(AnnotatedConfig.Speed), "count", "label", "wire_name" }, RequiredOf(schema));
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void RequiredModifier_MakesMembersRequired_NullableOrNot()
        {
            var schema = JsonSchemaValidation(typeof(ModifierConfig))!.AsObject();
            Assert.Equal(new[] { nameof(ModifierConfig.Limit), nameof(ModifierConfig.Speed), "count", "label" }, RequiredOf(schema));
        }
#endif

        [Fact]
        public void JsonRequired_MakesMembersRequired_NullableOrNot()
        {
            var schema = JsonSchemaValidation(typeof(JsonRequiredConfig))!.AsObject();
            Assert.Equal(new[] { nameof(JsonRequiredConfig.Label), "count" }, RequiredOf(schema));
        }

        [Fact]
        public void MarkOnOverriddenBaseProperty_IsHonoured()
        {
            var schema = JsonSchemaValidation(typeof(OverrideDerived))!.AsObject();
            Assert.Equal(new[] { nameof(OverrideBase.Marked) }, RequiredOf(schema));
        }

        [Fact]
        public void InputSchema_MemberTheDeserializerCannotWrite_IsNeverRequired()
        {
            var schema = new Reflector().GetArgumentsSchema(Method(nameof(Configure)))!;
            _output.WriteLine(schema.ToJsonString());
            var definition = Definition(schema, typeof(WriteAccess));

            Assert.Equal(new[] { nameof(WriteAccess.IncludedPrivateSet), nameof(WriteAccess.InitOnly), nameof(WriteAccess.Settable), "writable" }, RequiredOf(definition));
        }

        [Fact]
        public void OutputSchema_KeepsEveryMark_EvenOnMembersThatCannotBeWritten()
        {
            var schema = new Reflector().GetReturnSchema(Method(nameof(Inspect)))!;
            _output.WriteLine(schema.ToJsonString());
            var definition = Definition(schema, typeof(WriteAccess));
            var listed = definition[JsonSchema.Properties]!.AsObject().Select(kv => kv.Key).OrderBy(n => n, StringComparer.Ordinal).ToArray();

            Assert.Contains(nameof(WriteAccess.GetterOnly), listed);
            Assert.Equal(listed, RequiredOf(definition));
        }

        [Fact]
        public void InputSchema_GetterOnlyPropertyFilledByTheConstructor_StaysRequired()
        {
            var schema = new Reflector().GetArgumentsSchema(Method(nameof(Create)))!;
            Assert.Equal(new[] { nameof(ConstructorFilled.Id) }, RequiredOf(Definition(schema, typeof(ConstructorFilled))));
        }

        [Fact]
        public void InputScope_EndsWithTheArgumentsSchema()
        {
            var reflector = new Reflector();
            reflector.GetArgumentsSchema(Method(nameof(Configure)));
            var schema = JsonSchemaValidation(typeof(WriteAccess), reflector)!.AsObject();
            Assert.Contains(nameof(WriteAccess.GetterOnly), RequiredOf(schema));
        }

        [Theory]
        [InlineData(typeof(WaveConfig))]
        [InlineData(typeof(AnnotatedConfig))]
        public void NullableMembers_TypeHasNoNull(Type type)
        {
            // "null" is never added to a member's type (no ["string","null"], no anyOf null): some agents then fill
            // null with ""/0/false/[].
            var schema = JsonSchemaValidation(type)!;
            AssertNoNullType(schema);
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
            var schema = JsonSchemaValidation(type)!;
            Assert.NotEmpty(schema[JsonSchema.Required]!.AsArray());
        }
    }
}

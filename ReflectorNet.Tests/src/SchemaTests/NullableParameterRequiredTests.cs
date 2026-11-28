/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// A parameter is required in the arguments schema iff it has no default value, is not <c>[Optional]</c> and is
    /// not a <see cref="System.Nullable{T}"/> value type — the RQL tool layer's rule. A nullable reference
    /// annotation (<c>string? x</c>) alone does not make it optional. The binder stays more lenient: a missing
    /// nullable parameter (<c>string?</c> or <c>int?</c>) is still bound as null.
    /// </summary>
    public class NullableParameterRequiredTests : BaseTest
    {
        private readonly Reflector _reflector = new Reflector();

        public NullableParameterRequiredTests(ITestOutputHelper output) : base(output) { }

        public static string Mixed(string? nullableText, string text, int? nullableNumber, int number, string? withDefault = null, int numberWithDefault = 1)
            => text;

        // Most parameters nullable (void return): the compiler puts [NullableContext(2)] on the method and [Nullable(1)] on `text`.
        internal static void MostlyNullable(string? a, string? b, List<string>? c, string text) { }

        public static (string? text, int? number) EchoNullable(string? nullableText, int? nullableNumber)
            => (nullableText, nullableNumber);

        // No nullable attribute on the method or its parameters: they inherit the enclosing types' context.
        public static class Nested
        {
            public static void Inherits(string? a, string? b, string? c) { }
        }

        public static string NullableList(List<string>? items, List<string?> nonNullList) => string.Empty;

        public static string ByRef(ref string? nullableRef, ref string nonNullRef) => nonNullRef;

#nullable disable
        public static string Oblivious(string text) => text;
#nullable enable

        public static int OptionalAttribute([System.Runtime.InteropServices.Optional] int count, [System.Runtime.InteropServices.Optional] string? label)
            => count;

        // Mirrors Unity-MCP's ScreenshotIsolated(GameObjectRef? gameObjectRef, bool? includeChildren = true, ...).
        public static void ScreenshotLike(NullableParameterRequiredTests? target, bool? includeChildren = true, int? width = null) { }

        static string[] Required(string methodName) => Required(typeof(NullableParameterRequiredTests).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!);

        static string[] Required(MethodInfo method)
        {
            var schema = new Reflector().GetArgumentsSchema(method)!;
            return schema[JsonSchema.Required] is JsonArray required
                ? required.Select(node => node!.GetValue<string>()).OrderBy(name => name).ToArray()
                : new string[0];
        }

        [Fact]
        public void NullableReferenceWithoutDefault_IsRequired_NullableValueTypeAndDefaults_AreNot()
        {
            // nullableNumber (int?) and the two parameters with defaults are optional; string? is not.
            Assert.Equal(new[] { "nullableText", "number", "text" }, Required(nameof(Mixed)));
        }

        [Fact]
        public void NullableContextOnMethod_DoesNotMakeReferencesOptional()
        {
            Assert.Equal(new[] { "a", "b", "c", "text" }, Required(nameof(MostlyNullable)));
        }

        [Fact]
        public void NullableContextFromEnclosingType_DoesNotMakeReferencesOptional()
        {
            Assert.Equal(new[] { "a", "b", "c" }, Required(typeof(Nested).GetMethod(nameof(Nested.Inherits))!));
        }

        [Fact]
        public void NullableReferenceToGenericType_IsRequired()
        {
            Assert.Equal(new[] { "items", "nonNullList" }, Required(nameof(NullableList)));
        }

        [Fact]
        public void OptionalAttribute_IsNotRequired()
        {
            Assert.Empty(Required(nameof(OptionalAttribute)));
        }

        [Fact]
        public void NullableReferenceTarget_IsRequired_LikeRql()
        {
            Assert.Equal(new[] { "target" }, Required(nameof(ScreenshotLike)));
        }

        [Theory]
        [InlineData(nameof(Mixed), "nullableText", true)]
        [InlineData(nameof(Mixed), "text", true)]
        [InlineData(nameof(Mixed), "nullableNumber", false)]
        [InlineData(nameof(Mixed), "number", true)]
        [InlineData(nameof(Mixed), "withDefault", false)]
        [InlineData(nameof(Mixed), "numberWithDefault", false)]
        [InlineData(nameof(ByRef), "nullableRef", true)]
        [InlineData(nameof(OptionalAttribute), "count", false)]
        [InlineData(nameof(OptionalAttribute), "label", false)]
        public void IsParameterRequired(string methodName, string parameterName, bool expected)
        {
            var parameter = typeof(NullableParameterRequiredTests).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
                .GetParameters().Single(p => p.Name == parameterName);
            Assert.Equal(expected, MethodUtils.IsParameterRequired(parameter));
        }

        [Fact]
        public void ObliviousReferenceParameter_StaysRequired()
        {
            Assert.Equal(new[] { "text" }, Required(nameof(Oblivious)));
        }

        [Fact]
        public void NullableParameter_StillHasItsSchema()
        {
            var method = typeof(NullableParameterRequiredTests).GetMethod(nameof(Mixed))!;
            var schema = _reflector.GetArgumentsSchema(method)!;
            _output.WriteLine(schema.ToJsonString());

            Assert.Equal(JsonSchema.String, schema[JsonSchema.Properties]!["nullableText"]![JsonSchema.Type]!.GetValue<string>());
            Assert.NotNull(schema[JsonSchema.Properties]!["nullableNumber"]);
        }

        [Theory]
        [InlineData(nameof(Mixed), "nullableText", true)]
        [InlineData(nameof(Mixed), "text", false)]
        [InlineData(nameof(Mixed), "nullableNumber", true)]
        [InlineData(nameof(Mixed), "number", false)]
        [InlineData(nameof(MostlyNullable), "a", true)]
        [InlineData(nameof(MostlyNullable), "c", true)]
        [InlineData(nameof(MostlyNullable), "text", false)]
        [InlineData(nameof(ByRef), "nullableRef", true)]
        [InlineData(nameof(ByRef), "nonNullRef", false)]
        [InlineData(nameof(Oblivious), "text", false)]
        public void IsParameterNullable(string methodName, string parameterName, bool expected)
        {
            var parameter = typeof(NullableParameterRequiredTests).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
                .GetParameters().Single(p => p.Name == parameterName);
            Assert.Equal(expected, MethodUtils.IsParameterNullable(parameter));
        }

        [Fact]
        public async Task MissingNullableParameter_IsBoundAsNull_ByName()
        {
            // The binder is more lenient than the schema: a missing nullable parameter (string? included) is null.
            var wrapper = new MethodWrapper(_reflector, logger: null, typeof(NullableParameterRequiredTests).GetMethod(nameof(EchoNullable))!);
            var result = await wrapper.InvokeDict(new Dictionary<string, object?>());
            Assert.Equal(((string?)null, (int?)null), Assert.IsType<(string?, int?)>(result));
        }

        [Fact]
        public async Task MissingOptionalAttributeParameter_IsBoundAsDefault()
        {
            var wrapper = new MethodWrapper(_reflector, logger: null, typeof(NullableParameterRequiredTests).GetMethod(nameof(OptionalAttribute))!);
            var result = await wrapper.InvokeDict(new Dictionary<string, object?>());
            Assert.Equal(0, Assert.IsType<int>(result));
        }

        [Fact]
        public async Task MissingNullableParameter_IsBoundAsNull_Positionally()
        {
            var wrapper = new MethodWrapper(_reflector, logger: null, typeof(NullableParameterRequiredTests).GetMethod(nameof(EchoNullable))!);
            var result = await wrapper.Invoke("a");
            Assert.Equal(("a", (int?)null), Assert.IsType<(string?, int?)>(result));
        }
    }
}

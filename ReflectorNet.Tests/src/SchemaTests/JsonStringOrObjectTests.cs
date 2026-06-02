/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Covers issue #82: a `string` parameter annotated with [JsonStringOrObject] must
    /// (1) emit an anyOf[string, object] schema, and the binder must (2) accept a raw JSON
    /// object/array for ANY `string` parameter by coercing it to its raw JSON text.
    /// </summary>
    public class JsonStringOrObjectTests : BaseTest
    {
        private readonly Reflector _reflector;

        public JsonStringOrObjectTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        // Echo methods used by the binder tests.
        public static string TestAnnotatedStringMethod([JsonStringOrObject] string jsonPatch) => jsonPatch;
        public static string TestPlainStringMethod(string value) => value;

        #region Schema shape

        [Fact]
        public void Schema_AnnotatedStringParameter_IsAnyOfStringObject()
        {
            // Arrange
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestAnnotatedStringMethod))!;

            // Act
            var schema = _reflector.GetArgumentsSchema(methodInfo)!;
            _output.WriteLine(schema.ToString());

            // Assert
            var paramSchema = schema[JsonSchema.Properties]!["jsonPatch"]!;

            var anyOf = paramSchema[JsonSchema.AnyOf];
            Assert.NotNull(anyOf);

            var anyOfArray = anyOf!.AsArray();
            Assert.Equal(2, anyOfArray.Count);

            // One branch is a plain string, the other an object with additionalProperties:true.
            var hasString = false;
            var hasObject = false;
            foreach (var branch in anyOfArray)
            {
                var typeValue = branch![JsonSchema.Type]?.ToString();
                if (typeValue == JsonSchema.String)
                    hasString = true;
                if (typeValue == JsonSchema.Object)
                {
                    hasObject = true;
                    Assert.True(branch[JsonSchema.AdditionalProperties]!.GetValue<bool>());
                }
            }
            Assert.True(hasString, "anyOf must contain a {\"type\":\"string\"} branch");
            Assert.True(hasObject, "anyOf must contain a {\"type\":\"object\"} branch");
        }

        [Fact]
        public void Schema_UnannotatedStringParameter_IsFlatString()
        {
            // Arrange
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestPlainStringMethod))!;

            // Act
            var schema = _reflector.GetArgumentsSchema(methodInfo)!;
            _output.WriteLine(schema.ToString());

            // Assert: un-annotated string stays {"type":"string"} — strictly opt-in, no anyOf.
            var paramSchema = schema[JsonSchema.Properties]!["value"]!;
            Assert.Equal(JsonSchema.String, paramSchema[JsonSchema.Type]?.ToString());
            Assert.Null(paramSchema[JsonSchema.AnyOf]);
        }

        #endregion

        #region Binder coercion (dict overload — InvokeDict)

        [Fact]
        public async Task Binder_Dict_ObjectForStringParam_BindsRawJsonText()
        {
            // Arrange
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestAnnotatedStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("{\"op\":\"add\",\"path\":\"/x\",\"value\":1}").RootElement;
            Assert.Equal(JsonValueKind.Object, jsonElement.ValueKind);
            var parameters = new Dictionary<string, object?> { { "jsonPatch", jsonElement } };

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert: the method receives the equivalent stringified JSON.
            var resultString = Assert.IsType<string>(result);
            using var roundTrip = JsonDocument.Parse(resultString);
            Assert.Equal("add", roundTrip.RootElement.GetProperty("op").GetString());
            Assert.Equal("/x", roundTrip.RootElement.GetProperty("path").GetString());
            Assert.Equal(1, roundTrip.RootElement.GetProperty("value").GetInt32());
        }

        [Fact]
        public async Task Binder_Dict_ArrayForStringParam_BindsRawJsonText()
        {
            // Arrange
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestAnnotatedStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("[1,2,3]").RootElement;
            Assert.Equal(JsonValueKind.Array, jsonElement.ValueKind);
            var parameters = new Dictionary<string, object?> { { "jsonPatch", jsonElement } };

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            var resultString = Assert.IsType<string>(result);
            using var roundTrip = JsonDocument.Parse(resultString);
            Assert.Equal(JsonValueKind.Array, roundTrip.RootElement.ValueKind);
            Assert.Equal(3, roundTrip.RootElement.GetArrayLength());
        }

        [Fact]
        public async Task Binder_Dict_PlainJsonStringForStringParam_StillAccepted()
        {
            // Arrange
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestAnnotatedStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            // A plain JSON string value (the existing supported form) must keep working unchanged.
            var jsonElement = JsonDocument.Parse("\"already-a-string\"").RootElement;
            Assert.Equal(JsonValueKind.String, jsonElement.ValueKind);
            var parameters = new Dictionary<string, object?> { { "jsonPatch", jsonElement } };

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal("already-a-string", result);
        }

        [Fact]
        public async Task Binder_Dict_ObjectForUnannotatedStringParam_AlsoCoerced()
        {
            // The binder coercion is unconditional (does NOT require the attribute): a raw object
            // for a plain `string` param is still coerced to raw JSON text. Only the SCHEMA is opt-in.
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestPlainStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("{\"k\":\"v\"}").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            var result = await wrapper.InvokeDict(parameters);

            var resultString = Assert.IsType<string>(result);
            using var roundTrip = JsonDocument.Parse(resultString);
            Assert.Equal("v", roundTrip.RootElement.GetProperty("k").GetString());
        }

        [Fact]
        public async Task Binder_Dict_PlainStringForUnannotatedStringParam_Unaffected()
        {
            // A normal string value for a normal string param behaves exactly as before.
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestPlainStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("\"hello\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            var result = await wrapper.InvokeDict(parameters);

            Assert.Equal("hello", result);
        }

        #endregion

        #region Binder coercion (params object?[] overload — Invoke)

        [Fact]
        public async Task Binder_Positional_ObjectForStringParam_BindsRawJsonText()
        {
            // Exercises the OTHER GetParameterValue overload (BuildParameters(object?[]) via Invoke).
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestAnnotatedStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("{\"op\":\"replace\"}").RootElement;

            // Act
            var result = await wrapper.Invoke(jsonElement);

            // Assert
            var resultString = Assert.IsType<string>(result);
            using var roundTrip = JsonDocument.Parse(resultString);
            Assert.Equal("replace", roundTrip.RootElement.GetProperty("op").GetString());
        }

        [Fact]
        public async Task Binder_Positional_PlainJsonStringForStringParam_StillAccepted()
        {
            var methodInfo = typeof(JsonStringOrObjectTests).GetMethod(nameof(TestAnnotatedStringMethod))!;
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("\"plain\"").RootElement;

            var result = await wrapper.Invoke(jsonElement);

            Assert.Equal("plain", result);
        }

        #endregion
    }
}

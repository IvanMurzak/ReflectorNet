using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public abstract class SchemaTestBase : BaseTest
    {
        protected SchemaTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected JsonNode? JsonSchemaValidation(Type type, Reflector? reflector = null)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetSchema(type, justRef: false);

            _output.WriteLine($"Schema for {type.GetTypeShortName()}");
            _output.WriteLine($"{schema}");

            Assert.NotNull(schema);
            if (schema.AsObject().TryGetPropertyValue(JsonSchema.Error, out var errorValue))
            {
                Assert.Fail(errorValue!.ToString());
            }
            Assert.NotNull(schema.AsObject());
            return schema;
        }

        protected void TestMethodInputs_PropertyRefs(Reflector? reflector, MethodInfo methodInfo, params string[] parameterNames)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonSchema.Defs]);

            var defines = schema[JsonSchema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonSchema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var parameterName in parameterNames)
            {
                var methodParameter = methodInfo.GetParameters().FirstOrDefault(p => p.Name == parameterName);
                Assert.NotNull(methodParameter);

                var typeId = methodParameter.ParameterType.GetTypeId();
                var refString = $"{JsonSchema.RefValue}{typeId}";

                var targetDefine = defines[typeId];
                Assert.NotNull(targetDefine);

                var refStringValue = properties.FirstOrDefault(kvp
                        => kvp.Value!.AsObject().TryGetPropertyValue(JsonSchema.Ref, out var refValue)
                        && refString == refValue?.ToString())
                    .Value
                    ?.ToString();

                Assert.False(string.IsNullOrEmpty(refStringValue));
            }
        }

        protected JsonNode? TestMethodInputs_Defines(Reflector? reflector, MethodInfo methodInfo, params Type[] expectedTypes)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonSchema.Defs]);

            var defines = schema[JsonSchema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonSchema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var expectedType in expectedTypes)
            {
                var typeId = expectedType.GetTypeId();
                var targetDefine = defines[typeId];
                Assert.NotNull(targetDefine);
            }

            return schema;
        }

        #region Return Schema Helper Methods

        /// <summary>
        /// Gets the return schema for a method by name
        /// </summary>
        protected JsonNode? GetReturnSchemaForMethod(string methodName, bool justRef = false)
        {
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!;
            return reflector.GetReturnSchema(methodInfo, justRef);
        }

        /// <summary>
        /// Asserts that a primitive return type has the correct schema structure
        /// </summary>
        protected void AssertPrimitiveReturnSchema(JsonNode schema, string expectedJsonType, bool shouldBeRequired = true)
        {
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(expectedJsonType, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            if (shouldBeRequired)
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Required));
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.Single(required);
                Assert.Equal(JsonSchema.Result, required[0]?.ToString());
            }
            else
            {
                AssertResultNotRequired(schema);
            }
        }

        /// <summary>
        /// Asserts that "result" is NOT in the required array (for nullable types)
        /// </summary>
        protected void AssertResultNotRequired(JsonNode schema)
        {
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        /// <summary>
        /// Asserts that "result" IS in the required array (for non-nullable types
        /// </summary>
        protected void AssertResultRequired(JsonNode schema)
        {
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.Contains(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        /// <summary>
        /// Asserts that a custom type return schema has the correct structure
        /// </summary>
        protected void AssertCustomTypeReturnSchema(JsonNode schema, string[] expectedProperties, bool shouldBeRequired = true)
        {
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            var resultSchema = properties[JsonSchema.Result]!.AsObject();

            // Check if it's a ref or inline
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else if (resultSchema.ContainsKey(JsonSchema.Properties))
            {
                var typeProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                foreach (var expectedProp in expectedProperties)
                {
                    Assert.True(typeProperties.ContainsKey(expectedProp));
                }
            }
            else
            {
                // If neither ref nor inline properties, this is unexpected
                Assert.True(resultSchema.ContainsKey(JsonSchema.Ref) || resultSchema.ContainsKey(JsonSchema.Properties),
                    "Result schema should contain either $ref or properties");
            }

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);
        }

        /// <summary>
        /// Asserts that an array return type has the correct schema structure
        /// </summary>
        protected void AssertArrayReturnSchema(JsonNode schema, string expectedItemType, bool shouldBeRequired = true)
        {
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            var resultNode = properties[JsonSchema.Result]!;

            if (resultNode is JsonObject resultSchema && resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else if (resultNode is JsonObject resultInlineSchema)
            {
                Assert.Equal(JsonSchema.Array, resultInlineSchema[JsonSchema.Type]?.ToString());
                Assert.True(resultInlineSchema.ContainsKey(JsonSchema.Items));

                var items = resultInlineSchema[JsonSchema.Items]!;
                Assert.Equal(expectedItemType, items[JsonSchema.Type]?.ToString());
            }
            else
            {
                Assert.Fail("Expected result to be a schema object");
            }

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);
        }

        /// <summary>
        /// Asserts that a List<ComplexReturnType> return type has the correct schema structure
        /// </summary>
        protected void AssertComplexListReturnSchema(JsonNode schema, bool shouldBeRequired = true, bool itemsAreNullable = false)
        {
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            var resultNode = properties[JsonSchema.Result]!;

            if (resultNode is JsonObject resultSchema && resultSchema.ContainsKey(JsonSchema.Ref))
            {
                // Result is a reference to a list definition
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else if (resultNode is JsonObject resultInlineSchema)
            {
                // Result is an inline array schema
                Assert.Equal(JsonSchema.Array, resultInlineSchema[JsonSchema.Type]?.ToString());
                Assert.True(resultInlineSchema.ContainsKey(JsonSchema.Items));

                var items = resultInlineSchema[JsonSchema.Items]!;

                // Items should be either a reference to ComplexReturnType or an inline object
                if (items is JsonObject itemsSchema)
                {
                    if (itemsSchema.ContainsKey(JsonSchema.Ref))
                    {
                        // Items are a reference to ComplexReturnType
                        Assert.Contains("ComplexReturnType", itemsSchema[JsonSchema.Ref]?.ToString());
                        Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
                    }
                    else if (itemsSchema.ContainsKey(JsonSchema.Type))
                    {
                        // Items are inlined
                        Assert.Equal(JsonSchema.Object, itemsSchema[JsonSchema.Type]?.ToString());
                    }
                    else
                    {
                        Assert.Fail("Items schema should contain either $ref or type");
                    }
                }
                else
                {
                    Assert.Fail("Expected items to be a schema object");
                }
            }
            else
            {
                Assert.Fail("Expected result to be a schema object");
            }

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);
        }

        #endregion
    }
}

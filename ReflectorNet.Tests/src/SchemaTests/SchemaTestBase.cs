using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public abstract class SchemaTestBase : BaseTest
    {
        static readonly Type[] RestrictedDefineTypes = new Type[]
        {
            typeof(string),
            typeof(object),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid),
            typeof(TimeSpan),
            typeof(Uri)
        };
        protected SchemaTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected JsonNode? JsonSchemaValidation(Type type, Reflector? reflector = null)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetSchema(type);

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

            var schema = reflector.GetArgumentsSchema(methodInfo)!;

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

                var typeId = methodParameter.ParameterType.GetSchemaTypeId();
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

            var schema = reflector.GetArgumentsSchema(methodInfo)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonSchema.Defs]);

            var defines = schema[JsonSchema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonSchema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var expectedType in expectedTypes)
            {
                var typeId = expectedType.GetSchemaTypeId();
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
            var schema = reflector.GetReturnSchema(methodInfo, justRef);

            _output.WriteLine($"Return schema for {methodName}:");
            _output.WriteLine(schema?.ToString() ?? StringUtils.Null);
            _output.WriteLine(string.Empty);

            return schema;
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
        /// Asserts that "result" IS in the required array (for non-nullable types)
        /// </summary>
        protected void AssertResultRequired(JsonNode schema)
        {
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.Contains(required, r => r?.ToString() == JsonSchema.Result);
            }
            else
            {
                Assert.Fail("Schema does not contain a required array");
            }
        }

        /// <summary>
        /// Asserts that all expected types are defined in the $defs section of the schema OR referenced within the schema.
        /// This method recursively checks all $ref values in the schema to ensure nested types are properly referenced.
        /// Only non-primitive and non-enum types should be included in $defs, as primitives and enums are inlined.
        /// Verifies that at minimum the expected types are present (additional types may be included by the schema generator).
        /// </summary>
        protected void AssertResultDefines(JsonNode schema, params Type[] expectedTypes)
        {
            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs), "Schema should contain $defs section");

            var defines = schema[JsonSchema.Defs]!.AsObject();
            Assert.NotNull(defines);

            // Collect all $ref values throughout the schema (includes nested references)
            var allReferences = new HashSet<string>();
            CollectAllReferences(schema, allReferences);

            // Filter expected types to only include non-primitive, non-enum types
            var nonPrimitiveTypes = expectedTypes.Where(t => !TypeUtils.IsPrimitive(t) && !t.IsEnum).ToArray();

            // Verify all expected types are present in $defs and referenced
            foreach (var expectedType in nonPrimitiveTypes)
            {
                var expectedTypeId = expectedType.GetSchemaTypeId();

                // Check if the type is either:
                // 1. Directly defined in $defs (exact match)
                var isDirectlyDefined = defines.ContainsKey(expectedTypeId);

                // 2. Referenced somewhere in the schema (exact match in $ref)
                var expectedRef = $"{JsonSchema.RefValue}{expectedTypeId}";
                var isReferenced = allReferences.Contains(expectedRef);

                Assert.True(isDirectlyDefined && isReferenced,
                    $"Expected type '{expectedType.GetTypeShortName()}' with ID '{expectedTypeId}' should be both defined in $defs and referenced in schema. " +
                    $"Expected $ref: '{expectedRef}'. " +
                    $"Defined types: {string.Join(", ", defines.Select(d => d.Key))}. " +
                    $"Referenced types: {string.Join(", ", allReferences)}");
            }

            // Verify that if any of the expected types appear in $defs, they are not primitive or enum
            foreach (var expectedType in expectedTypes)
            {
                var expectedTypeId = expectedType.GetSchemaTypeId();
                if (defines.ContainsKey(expectedTypeId))
                {
                    Assert.False(TypeUtils.IsPrimitive(expectedType) || expectedType.IsEnum,
                        $"Type '{expectedType.GetTypeShortName()}' with ID '{expectedTypeId}' is primitive or enum and should not be in $defs. " +
                        $"Primitives and enums should be inlined in the schema.");
                }
            }
        }

        /// <summary>
        /// Helper method to recursively collect all $ref values in a JSON schema
        /// </summary>
        private void CollectAllReferences(JsonNode? node, HashSet<string> references)
        {
            if (node == null) return;

            if (node is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    if (kvp.Key == JsonSchema.Ref && kvp.Value != null)
                    {
                        references.Add(kvp.Value.ToString());
                    }
                    CollectAllReferences(kvp.Value, references);
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    CollectAllReferences(item, references);
                }
            }
        }

        /// <summary>
        /// Asserts that all $ref references found in the schema are defined in the $defs section.
        /// This method recursively scans the entire schema for all $ref values and verifies that
        /// each referenced type has a corresponding definition in $defs.
        /// </summary>
        protected void AssertAllRefsDefined(JsonNode schema)
        {
            Assert.NotNull(schema);

            // Collect all $ref values in the schema
            var allReferences = new HashSet<string>();
            CollectAllReferences(schema, allReferences);

            // If there are no references, we're done
            if (allReferences.Count == 0)
            {
                return;
            }

            // References don't include restricted types
            foreach (var reference in allReferences)
            {
                Assert.False(RestrictedDefineTypes.Any(x => JsonSchema.RefValue + x.GetSchemaTypeId() == reference),
                    $"Reference '{reference}' is for a restricted type that should not appear as a $ref.");
            }

            // Schema must have $defs if there are references
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs),
                $"Schema contains {allReferences.Count} $ref reference(s) but no $defs section. References: {string.Join(", ", allReferences)}");

            var defines = schema[JsonSchema.Defs]!.AsObject();
            Assert.NotNull(defines);

            // Check each reference to ensure it's defined
            foreach (var reference in allReferences)
            {
                // Extract the type ID from the reference (e.g., "#/$defs/TypeId" -> "TypeId")
                var typeId = reference.Replace(JsonSchema.RefValue, string.Empty);

                Assert.True(defines.ContainsKey(typeId),
                    $"Reference '{reference}' (type ID: '{typeId}') is not defined in $defs. " +
                    $"Available definitions: {string.Join(", ", defines.Select(d => d.Key))}");
            }

            // Defines don't include restricted types
            foreach (var define in defines)
            {
                Assert.False(RestrictedDefineTypes.Any(x => x.GetSchemaTypeId() == define.Key),
                    $"Reference '{define.Key}' is for a restricted type that should not appear as a $ref.");
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

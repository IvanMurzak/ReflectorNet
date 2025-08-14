using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class SchemaTests : SchemaTestBase
    {
        public SchemaTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Parameters_Object_Int_Bool()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            TestMethodInputs_Defines(
                reflector: null,
                methodInfo: methodInfo,
                expectedTypes: typeof(GameObjectRef));

            TestMethodInputs_PropertyRefs(
                reflector: null,
                methodInfo: methodInfo,
                parameterNames: "obj");
        }

        [Fact]
        public void Parameters_ListObject_ListObject()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.ListObject_ListObject))!;

            TestMethodInputs_Defines(
                reflector: null,
                methodInfo: methodInfo,
                typeof(GameObjectRef),
                typeof(GameObjectRefList),
                typeof(SerializedMember),
                typeof(SerializedMemberList));

            TestMethodInputs_PropertyRefs(
                reflector: null,
                methodInfo: methodInfo,
                "obj1",
                "obj2");
        }

        [Fact]
        public void Parameters_StringArray()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.StringArray))!;

            TestMethodInputs_Defines(
                reflector: null,
                methodInfo: methodInfo,
                typeof(string[]));

            TestMethodInputs_PropertyRefs(
                reflector: null,
                methodInfo: methodInfo,
                "stringArray");
        }

        [Fact]
        void GameObjectRef()
        {
            var reflector = new Reflector();
            JsonSchemaValidation(typeof(GameObjectRef), reflector);

            reflector.JsonSerializer.AddConverter(new GameObjectRefConverter());
            JsonSchemaValidation(typeof(GameObjectRef), reflector);

            reflector.JsonSerializer.AddConverter(new ObjectRefConverter());
            JsonSchemaValidation(typeof(GameObjectRef), reflector);
        }

        [Fact]
        void MethodData()
        {
            var reflector = new Reflector();

            // it fails without MethodDataConverter
            // JsonSchemaValidation(typeof(MethodData), reflector);

            reflector.JsonSerializer.AddConverter(new MethodDataConverter());
            JsonSchemaValidation(typeof(MethodData), reflector);
        }

        [Fact]
        void ObjectRef()
        {
            var reflector = new Reflector();
            JsonSchemaValidation(typeof(ObjectRef), reflector);

            reflector.JsonSerializer.AddConverter(new ObjectRefConverter());
            JsonSchemaValidation(typeof(ObjectRef), reflector);
        }

        [Fact]
        void MethodInfo()
        {
            var reflector = new Reflector();
            JsonSchemaValidation(typeof(MethodInfo), reflector);

            reflector.JsonSerializer.AddConverter(new MethodInfoConverter());
            JsonSchemaValidation(typeof(MethodInfo), reflector);
        }

        [Fact]
        void JsonPropertyName_Attribute()
        {
            var reflector = new Reflector();
            var schema = JsonSchemaValidation(typeof(ModelWithDifferentFieldsAndProperties), reflector);

            // Validate schema structure
            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties),
                $"Schema should contain '{JsonSchema.Properties}' property. Available properties: {string.Join(", ", schema.AsObject().Select(x => x.Key))}");

            var properties = schema[JsonSchema.Properties];
            Assert.NotNull(properties);

            // Test each expected property with custom JSON property names
            ValidateIntegerProperty(properties, ModelWithDifferentFieldsAndProperties.IntField);
            ValidateIntegerProperty(properties, ModelWithDifferentFieldsAndProperties.IntFieldNullable);
            ValidateIntegerProperty(properties, ModelWithDifferentFieldsAndProperties.IntProperty);
            ValidateIntegerProperty(properties, ModelWithDifferentFieldsAndProperties.IntPropertyNullable);
        }

        private static void ValidateIntegerProperty(JsonNode properties, string propertyName)
        {
            // Validate property exists
            Assert.True(properties.AsObject().ContainsKey(propertyName),
                $"Properties should contain '{propertyName}' property. Available properties: {string.Join(", ", properties.AsObject().Select(x => x.Key))}");

            var propertySchema = properties[propertyName];
            Assert.NotNull(propertySchema);

            // Validate property has type field
            Assert.True(propertySchema.AsObject().ContainsKey(JsonSchema.Type),
                $"Property '{propertyName}' schema should contain '{JsonSchema.Type}' field. Available fields: {string.Join(", ", propertySchema.AsObject().Select(x => x.Key))}");

            var propertyType = propertySchema[JsonSchema.Type];
            Assert.NotNull(propertyType);
            Assert.Equal(JsonSchema.Integer, propertyType.ToString());
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
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

            // Null check for schema
            Assert.NotNull(schema);

            // Validate schema has Properties section
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties),
                $"Schema should contain '{JsonSchema.Properties}' property. Available properties: {string.Join(", ", schema.AsObject().Select(x => x.Key))}");

            var properties = schema[JsonSchema.Properties];
            Assert.NotNull(properties);

            // ----------------------------------

            // Validate fields section contains expected IntField
            Assert.True(properties.AsObject().ContainsKey(ModelWithDifferentFieldsAndProperties.IntField),
                $"Fields should contain '{ModelWithDifferentFieldsAndProperties.IntField}' field. Available fields: {string.Join(", ", properties.AsObject().Select(x => x.Key))}");

            var intFieldSchema = properties[ModelWithDifferentFieldsAndProperties.IntField];
            Assert.NotNull(intFieldSchema);

            Assert.True(intFieldSchema.AsObject().ContainsKey(JsonSchema.Type),
                $"{ModelWithDifferentFieldsAndProperties.IntField} schema should contain '{JsonSchema.Type}' property. Available properties: {string.Join(", ", intFieldSchema.AsObject().Select(x => x.Key))}");

            var intFieldType = intFieldSchema[JsonSchema.Type];
            Assert.NotNull(intFieldType);
            Assert.Equal(JsonSchema.Integer, intFieldType.ToString());

            // Validate fields section contains expected IntFieldNullable
            Assert.True(properties.AsObject().ContainsKey(ModelWithDifferentFieldsAndProperties.IntFieldNullable),
                $"Fields should contain '{ModelWithDifferentFieldsAndProperties.IntFieldNullable}' field. Available fields: {string.Join(", ", properties.AsObject().Select(x => x.Key))}");

            var intFieldNullableSchema = properties[ModelWithDifferentFieldsAndProperties.IntFieldNullable];
            Assert.NotNull(intFieldNullableSchema);

            Assert.True(intFieldNullableSchema.AsObject().ContainsKey(JsonSchema.Type),
                $"{ModelWithDifferentFieldsAndProperties.IntFieldNullable} schema should contain '{JsonSchema.Type}' property. Available properties: {string.Join(", ", intFieldNullableSchema.AsObject().Select(x => x.Key))}");

            var intFieldNullableType = intFieldNullableSchema[JsonSchema.Type];
            Assert.NotNull(intFieldNullableType);
            Assert.Equal(JsonSchema.Integer, intFieldNullableType.ToString());

            // Validate props section contains expected IntProperty
            Assert.True(properties.AsObject().ContainsKey(ModelWithDifferentFieldsAndProperties.IntProperty),
                $"Props should contain '{ModelWithDifferentFieldsAndProperties.IntProperty}' property. Available props: {string.Join(", ", properties.AsObject().Select(x => x.Key))}");

            var intPropertySchema = properties[ModelWithDifferentFieldsAndProperties.IntProperty];
            Assert.NotNull(intPropertySchema);

            Assert.True(intPropertySchema.AsObject().ContainsKey(JsonSchema.Type),
                $"{ModelWithDifferentFieldsAndProperties.IntProperty} schema should contain '{JsonSchema.Type}' property. Available properties: {string.Join(", ", intPropertySchema.AsObject().Select(x => x.Key))}");

            var intPropertyType = intPropertySchema[JsonSchema.Type];
            Assert.NotNull(intPropertyType);
            Assert.Equal(JsonSchema.Integer, intPropertyType.ToString());

            // Validate props section contains expected IntPropertyNullable
            Assert.True(properties.AsObject().ContainsKey(ModelWithDifferentFieldsAndProperties.IntPropertyNullable),
                $"Props should contain '{ModelWithDifferentFieldsAndProperties.IntPropertyNullable}' property. Available props: {string.Join(", ", properties.AsObject().Select(x => x.Key))}");

            var intPropertyNullableSchema = properties[ModelWithDifferentFieldsAndProperties.IntPropertyNullable];
            Assert.NotNull(intPropertyNullableSchema);

            Assert.True(intPropertyNullableSchema.AsObject().ContainsKey(JsonSchema.Type),
                $"{ModelWithDifferentFieldsAndProperties.IntPropertyNullable} schema should contain '{JsonSchema.Type}' property. Available properties: {string.Join(", ", intPropertyNullableSchema.AsObject().Select(x => x.Key))}");

            var intPropertyNullableType = intPropertyNullableSchema[JsonSchema.Type];
            Assert.NotNull(intPropertyNullableType);
            Assert.Equal(JsonSchema.Integer, intPropertyNullableType.ToString());
        }
    }
}

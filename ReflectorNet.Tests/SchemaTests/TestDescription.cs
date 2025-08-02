using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public partial class TestDescription : BaseTest
    {
        public TestDescription(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void PropertyDescriptionOfCustomType()
        {
            TestClassMembersDescription(typeof(GameObjectRef));
            TestClassMembersDescription(typeof(GameObjectRefList));
            TestClassMembersDescription(typeof(List<GameObjectRef>));
            TestClassMembersDescription(typeof(GameObjectRef[]));
        }

        [Fact]
        public void PropertyDescriptionOfTestClassWithDescriptions()
        {
            // Test a class with various types of members and descriptions
            TestClassMembersDescription(typeof(TestClassWithDescriptions));
        }

        [Fact]
        public void PropertyDescriptionOfCollectionWithDescriptions()
        {
            // Test collection classes with descriptions - but only for complex types
            TestClassMembersDescription(typeof(TestCollectionWithDescription));

            // For List<TestClassWithDescriptions>, just test schema generation
            var listComplexSchema = JsonUtils.Schema.GetSchema(typeof(List<TestClassWithDescriptions>), justRef: false);
            Assert.NotNull(listComplexSchema);
            _output.WriteLine($"List<TestClassWithDescriptions> schema: {listComplexSchema}");
        }

        [Fact]
        public void PropertyDescriptionOfReflectorNetModels()
        {
            // Test MethodPointerRef and MethodDataRef which have Description attributes
            TestClassMembersDescription(typeof(MethodPointerRef));
            TestClassMembersDescription(typeof(MethodDataRef));

            // SerializedMember and SerializedMemberList use custom converters with descriptions
            // Let's test their schema generation separately
            var serializedMemberSchema = JsonUtils.Schema.GetSchema(typeof(SerializedMember), justRef: false);
            Assert.NotNull(serializedMemberSchema);
            _output.WriteLine($"SerializedMember schema: {serializedMemberSchema}");

            var serializedMemberListSchema = JsonUtils.Schema.GetSchema(typeof(SerializedMemberList), justRef: false);
            Assert.NotNull(serializedMemberListSchema);
            _output.WriteLine($"SerializedMemberList schema: {serializedMemberListSchema}");
        }

        [Fact]
        public void PropertyDescriptionOfArrayTypes()
        {
            // Test complex array types that have members to inspect
            TestClassMembersDescription(typeof(GameObjectRef[]));
            TestClassMembersDescription(typeof(TestClassWithDescriptions[]));

            // For primitive arrays, just test schema generation
            var stringArraySchema = JsonUtils.Schema.GetSchema(typeof(string[]), justRef: false);
            Assert.NotNull(stringArraySchema);
            _output.WriteLine($"string[] schema: {stringArraySchema}");

            var intArraySchema = JsonUtils.Schema.GetSchema(typeof(int[]), justRef: false);
            Assert.NotNull(intArraySchema);
            _output.WriteLine($"int[] schema: {intArraySchema}");
        }

        [Fact]
        public void PropertyDescriptionOfGenericCollections()
        {
            // Test generic collections - but only ones that contain complex types
            TestClassMembersDescription(typeof(List<GameObjectRef>));
            TestClassMembersDescription(typeof(List<TestClassWithDescriptions>));

            // For primitive collections, just test schema generation
            var listStringSchema = JsonUtils.Schema.GetSchema(typeof(List<string>), justRef: false);
            Assert.NotNull(listStringSchema);
            _output.WriteLine($"List<string> schema: {listStringSchema}");

            var listIntSchema = JsonUtils.Schema.GetSchema(typeof(List<int>), justRef: false);
            Assert.NotNull(listIntSchema);
            _output.WriteLine($"List<int> schema: {listIntSchema}");

            var dictionarySchema = JsonUtils.Schema.GetSchema(typeof(Dictionary<string, int>), justRef: false);
            Assert.NotNull(dictionarySchema);
            _output.WriteLine($"Dictionary<string, int> schema: {dictionarySchema}");
        }

        [Fact]
        public void PropertyDescriptionOfPrimitiveTypes()
        {
            // Primitive types don't have members to test, but we can test schema generation
            var stringSchema = JsonUtils.Schema.GetSchema(typeof(string), justRef: false);
            Assert.NotNull(stringSchema);
            _output.WriteLine($"String schema: {stringSchema}");

            var intSchema = JsonUtils.Schema.GetSchema(typeof(int), justRef: false);
            Assert.NotNull(intSchema);
            _output.WriteLine($"Int schema: {intSchema}");

            var boolSchema = JsonUtils.Schema.GetSchema(typeof(bool), justRef: false);
            Assert.NotNull(boolSchema);
            _output.WriteLine($"Bool schema: {boolSchema}");

            var doubleSchema = JsonUtils.Schema.GetSchema(typeof(double), justRef: false);
            Assert.NotNull(doubleSchema);
            _output.WriteLine($"Double schema: {doubleSchema}");
        }

        [Fact]
        public void PropertyDescriptionOfNullableTypes()
        {
            // Nullable types also don't have members to test, but we can test schema generation
            var nullableIntSchema = JsonUtils.Schema.GetSchema(typeof(int?), justRef: false);
            Assert.NotNull(nullableIntSchema);
            _output.WriteLine($"Nullable int schema: {nullableIntSchema}");

            var nullableBoolSchema = JsonUtils.Schema.GetSchema(typeof(bool?), justRef: false);
            Assert.NotNull(nullableBoolSchema);
            _output.WriteLine($"Nullable bool schema: {nullableBoolSchema}");

            var nullableDateTimeSchema = JsonUtils.Schema.GetSchema(typeof(DateTime?), justRef: false);
            Assert.NotNull(nullableDateTimeSchema);
            _output.WriteLine($"Nullable DateTime schema: {nullableDateTimeSchema}");
        }

        [Fact]
        public void ClassLevelDescription()
        {
            // Test that class-level descriptions are properly captured
            var schema = JsonUtils.Schema.GetSchema(typeof(GameObjectRef), justRef: false);
            Assert.NotNull(schema);

            var description = schema[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(description);
            Assert.Contains("Find GameObject", description);

            _output.WriteLine($"GameObjectRef class description: {description}");
        }

        [Fact]
        public void ClassLevelDescriptionForTestClass()
        {
            // Test class-level description for our test class
            var schema = JsonUtils.Schema.GetSchema(typeof(TestClassWithDescriptions), justRef: false);
            Assert.NotNull(schema);

            var description = schema[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(description);
            Assert.Contains("Test class with various member types", description);

            _output.WriteLine($"TestClassWithDescriptions class description: {description}");
        }

        [Fact]
        public void MissingDescriptionsHandledGracefully()
        {
            // Create a simple class without descriptions to test graceful handling
            var schema = JsonUtils.Schema.GetSchema(typeof(string), justRef: false);
            Assert.NotNull(schema);

            // Primitive types might not have descriptions, that's OK
            _output.WriteLine($"String schema: {schema}");
        }

        [Fact]
        public void PropertyDescriptionOfStructTypes()
        {
            // Test struct types with descriptions
            TestClassMembersDescription(typeof(TestStructWithDescriptions));
            TestClassMembersDescription(typeof(TestStructWithDescriptions[]));
            TestClassMembersDescription(typeof(List<TestStructWithDescriptions>));
        }

        [Fact]
        public void PropertyDescriptionOfEnumTypes()
        {
            // Test enum types with descriptions
            TestClassMembersDescription(typeof(TestEnumWithDescriptions));
            TestClassMembersDescription(typeof(TestEnumWithDescriptions[]));
        }

        [Fact]
        public void StructLevelDescription()
        {
            // Test that struct-level descriptions are properly captured
            var schema = JsonUtils.Schema.GetSchema(typeof(TestStructWithDescriptions), justRef: false);
            Assert.NotNull(schema);

            var description = schema[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(description);
            Assert.Contains("test struct", description);

            _output.WriteLine($"TestStructWithDescriptions description: {description}");
        }

        [Fact]
        public void EnumLevelDescription()
        {
            // Test that enum-level descriptions are properly captured
            var schema = JsonUtils.Schema.GetSchema(typeof(TestEnumWithDescriptions), justRef: false);
            Assert.NotNull(schema);

            // For enums, the schema might be different - let's just verify it generates
            _output.WriteLine($"TestEnumWithDescriptions schema: {schema}");
        }

        [Fact]
        public void NestedTypesWithDescriptions()
        {
            // Test nested complex types
            TestClassMembersDescription(typeof(List<List<GameObjectRef>>));
            TestClassMembersDescription(typeof(Dictionary<string, GameObjectRef>));
            TestClassMembersDescription(typeof(Dictionary<string, List<TestClassWithDescriptions>>));
        }

        [Fact]
        public void PropertyDescriptionWithBindingFlags()
        {
            // Test with different binding flags to include non-public members
            TestClassMembersDescription(typeof(TestClassWithDescriptions),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }

        [Fact]
        public void ValidateSpecificDescriptionContent()
        {
            // Test that specific descriptions are correctly applied to schema properties
            var schema = JsonUtils.Schema.GetSchema(typeof(GameObjectRef), justRef: false);
            Assert.NotNull(schema);

            var properties = schema[JsonUtils.Schema.Properties]?.AsObject();
            Assert.NotNull(properties);

            // Check instanceID property description
            var instanceIdProperty = properties["instanceID"];
            Assert.NotNull(instanceIdProperty);
            var instanceIdDescription = instanceIdProperty[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(instanceIdDescription);
            Assert.Contains("instanceID", instanceIdDescription);
            Assert.Contains("Priority: 1", instanceIdDescription);

            // Check path property description
            var pathProperty = properties["path"];
            Assert.NotNull(pathProperty);
            var pathDescription = pathProperty[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(pathDescription);
            Assert.Contains("path", pathDescription);
            Assert.Contains("Priority: 2", pathDescription);

            _output.WriteLine($"instanceID description: {instanceIdDescription}");
            _output.WriteLine($"path description: {pathDescription}");
        }

        [Fact]
        public void DescriptionInheritanceFromBaseType()
        {
            // Test that descriptions work with inheritance
            TestClassMembersDescription(typeof(MethodDataRef)); // inherits from MethodPointerRef
        }

        [Fact]
        public void CustomConverterDescriptions()
        {
            // Test that custom JSON converters provide proper descriptions in schema
            var serializedMemberSchema = JsonUtils.Schema.GetSchema(typeof(SerializedMember), justRef: false);
            Assert.NotNull(serializedMemberSchema);

            var properties = serializedMemberSchema[JsonUtils.Schema.Properties]?.AsObject();
            Assert.NotNull(properties);

            // Check that typeName property has the expected description from the converter
            var typeNameProperty = properties["typeName"];
            Assert.NotNull(typeNameProperty);
            var typeNameDescription = typeNameProperty[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(typeNameDescription);
            Assert.Contains("Full type name", typeNameDescription);

            // Check that name property has the expected description from the converter
            var nameProperty = properties["name"];
            Assert.NotNull(nameProperty);
            var nameDescription = nameProperty[JsonUtils.Schema.Description]?.ToString();
            Assert.NotNull(nameDescription);
            Assert.Contains("Name of the member", nameDescription);

            _output.WriteLine($"SerializedMember typeName description: {typeNameDescription}");
            _output.WriteLine($"SerializedMember name description: {nameDescription}");
        }
    }
}

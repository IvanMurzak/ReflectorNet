using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class NestedClassSchemaTests : SchemaTestBase
    {
        public NestedClassSchemaTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Schema_NestedClass_GeneratesValidSchema()
        {
            var reflector = new Reflector();
            var schema = JsonSchemaValidation(typeof(ParentClass.NestedClass), reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("NestedField"));
            Assert.True(properties.ContainsKey("NestedProperty"));
            Assert.False(properties.ContainsKey("NestedStaticField"));
            Assert.False(properties.ContainsKey("NestedStaticProperty"));
        }

        [Fact]
        public void Schema_DeeplyNestedClass_GeneratesValidSchema()
        {
            var reflector = new Reflector();
            var schema = JsonSchemaValidation(typeof(LevelOne.LevelTwo.LevelThree.LevelFour), reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("DeepProperty"));
        }

        [Fact]
        public void Schema_DeeplyNestedHierarchy_GeneratesValidSchema()
        {
            var reflector = new Reflector();
            var schema = JsonSchemaValidation(typeof(LevelOne), reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("NestedInstance"));

            var nestedInstanceSchema = properties["NestedInstance"]!.AsObject();
            Assert.True(nestedInstanceSchema.ContainsKey(JsonSchema.Ref) || nestedInstanceSchema.ContainsKey(JsonSchema.Type));

            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_GenericNestedClass_GeneratesValidSchema()
        {
            var reflector = new Reflector();
            var type = typeof(GenericOuter<int>.GenericInner<string>);
            var schema = JsonSchemaValidation(type, reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("OuterValue"));
            Assert.True(properties.ContainsKey("InnerValue"));
        }

        [Fact]
        public void Schema_GenericNestedClass_SelfReferencing_GeneratesValidSchema()
        {
            var reflector = new Reflector();
            var type = typeof(GenericOuter<int>);
            var schema = JsonSchemaValidation(type, reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("SelfReferencingInner"));

            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_NestedClass_RefsAreDefinedInDefs()
        {
            var reflector = new Reflector();
            var type = typeof(LevelOne);
            var schema = reflector.GetSchema(type);

            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs), "Schema should contain $defs for nested types");

            var defines = schema[JsonSchema.Defs]!.AsObject();
            Assert.NotNull(defines);

            var levelTwoId = typeof(LevelOne.LevelTwo).GetSchemaTypeId();
            var levelThreeId = typeof(LevelOne.LevelTwo.LevelThree).GetSchemaTypeId();
            var levelFourId = typeof(LevelOne.LevelTwo.LevelThree.LevelFour).GetSchemaTypeId();

            Assert.True(defines.ContainsKey(levelTwoId), $"$defs should contain {levelTwoId}");
            Assert.True(defines.ContainsKey(levelThreeId), $"$defs should contain {levelThreeId}");
            Assert.True(defines.ContainsKey(levelFourId), $"$defs should contain {levelFourId}");

            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_NestedClass_ReturnSchema_HasValidRefs()
        {
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NestedClassReturnMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var schema = reflector.GetReturnSchema(methodInfo);

            Assert.NotNull(schema);
            AssertCustomTypeReturnSchema(schema, new[] { "NestedField", "NestedProperty" }, shouldBeRequired: true);
            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_DeeplyNestedClass_ReturnSchema_HasValidRefs()
        {
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(DeeplyNestedClassReturnMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var schema = reflector.GetReturnSchema(methodInfo);

            Assert.NotNull(schema);
            AssertCustomTypeReturnSchema(schema, new[] { "DeepProperty" }, shouldBeRequired: true);
            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_GenericNestedClass_ReturnSchema_HasValidRefs()
        {
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(GenericNestedClassReturnMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var schema = reflector.GetReturnSchema(methodInfo);

            Assert.NotNull(schema);
            AssertCustomTypeReturnSchema(schema, new[] { "OuterValue", "InnerValue" }, shouldBeRequired: true);
            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_NestedClass_Array_ReturnSchema_HasValidRefs()
        {
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NestedClassArrayReturnMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var schema = reflector.GetReturnSchema(methodInfo);

            Assert.NotNull(schema);
            AssertArrayReturnSchema(schema, JsonSchema.Object, shouldBeRequired: true);
            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_NestedClass_ArgumentSchema_HasValidRefs()
        {
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NestedClassArgumentMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var schema = reflector.GetArgumentsSchema(methodInfo);

            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs), "Argument schema should contain $defs");
            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void Schema_CrossAssemblyNestedClass_GeneratesValidSchema()
        {
            var reflector = new Reflector();
            var schema = JsonSchemaValidation(typeof(StaticParentClass.NestedClass), reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("NestedField"));
            Assert.True(properties.ContainsKey("NestedProperty"));
        }

        private ParentClass.NestedClass NestedClassReturnMethod() => new ParentClass.NestedClass();
        private LevelOne.LevelTwo.LevelThree.LevelFour DeeplyNestedClassReturnMethod() => new LevelOne.LevelTwo.LevelThree.LevelFour();
        private GenericOuter<int>.GenericInner<string> GenericNestedClassReturnMethod() => new GenericOuter<int>.GenericInner<string>();
        private ParentClass.NestedClass[] NestedClassArrayReturnMethod() => new ParentClass.NestedClass[0];
        private void NestedClassArgumentMethod(ParentClass.NestedClass nested) { }
    }
}

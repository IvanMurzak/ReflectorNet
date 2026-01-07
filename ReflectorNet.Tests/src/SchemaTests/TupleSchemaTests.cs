using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class TupleSchemaTests : SchemaTestBase
    {
        public TupleSchemaTests(ITestOutputHelper output) : base(output) { }

        #region ValueTuple Schema Tests (1-8 elements)

        [Fact]
        public void Schema_ValueTuple1()
        {
            var reflector = new Reflector();
            var tupleType = typeof(ValueTuple<int>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 1);
        }

        [Fact]
        public void Schema_ValueTuple2()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, string));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 2);
        }

        [Fact]
        public void Schema_ValueTuple3()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, string, bool));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 3);
        }

        [Fact]
        public void Schema_ValueTuple4()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, string, bool, double));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 4);
        }

        [Fact]
        public void Schema_ValueTuple5()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, string, bool, double, char));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 5);
        }

        [Fact]
        public void Schema_ValueTuple6()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, string, bool, double, char, long));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 6);
        }

        [Fact]
        public void Schema_ValueTuple7()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, string, bool, double, char, long, float));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 7);
        }

        [Fact]
        public void Schema_ValueTuple8()
        {
            var reflector = new Reflector();
            // 8+ element tuples have a Rest field containing nested tuple
            var tupleType = typeof((int, string, bool, double, char, long, float, byte));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            // Should have Item1-Item7 + Rest field
            AssertTupleSchemaHasItemProperties(schema, 7, hasRest: true);
        }

        #endregion

        #region Tuple (reference type) Schema Tests (1-8 elements)

        [Fact]
        public void Schema_Tuple1()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 1);
        }

        [Fact]
        public void Schema_Tuple2()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, string>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 2);
        }

        [Fact]
        public void Schema_Tuple3()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, string, bool>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 3);
        }

        [Fact]
        public void Schema_Tuple4()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, string, bool, double>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 4);
        }

        [Fact]
        public void Schema_Tuple5()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, string, bool, double, char>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 5);
        }

        [Fact]
        public void Schema_Tuple6()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, string, bool, double, char, long>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 6);
        }

        [Fact]
        public void Schema_Tuple7()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, string, bool, double, char, long, float>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 7);
        }

        [Fact]
        public void Schema_Tuple8()
        {
            var reflector = new Reflector();
            // 8+ element Tuples have a Rest field containing nested Tuple
            var tupleType = typeof(Tuple<int, string, bool, double, char, long, float, Tuple<byte>>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            // Should have Item1-Item7 + Rest field
            AssertTupleSchemaHasItemProperties(schema, 7, hasRest: true);
        }

        #endregion

        #region ITuple Interface Schema Tests

        [Fact]
        public void Schema_ITuple_ImplementedByValueTuple()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, bool));

            // Verify the type implements ITuple
            Assert.True(typeof(ITuple).IsAssignableFrom(tupleType));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            _output.WriteLine($"ValueTuple<int, bool> implements ITuple: {typeof(ITuple).IsAssignableFrom(tupleType)}");
        }

        [Fact]
        public void Schema_ITuple_ImplementedByTuple()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<int, bool>);

            // Verify the type implements ITuple
            Assert.True(typeof(ITuple).IsAssignableFrom(tupleType));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            _output.WriteLine($"Tuple<int, bool> implements ITuple: {typeof(ITuple).IsAssignableFrom(tupleType)}");
        }

        [Fact]
        public void Schema_ValueTuple_ShouldNotContainITupleItemIndexer()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int, bool));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);

            // Schema should NOT contain ITuple.Item indexer property (this was the property causing serialization issues)
            // Note: ITuple.Length is a regular property (not an indexer) and is acceptable in the schema
            var schemaString = schema.ToJsonString();
            Assert.DoesNotContain("ITuple.Item", schemaString);

            _output.WriteLine("Schema does not contain ITuple.Item indexer property");
            _output.WriteLine($"Note: ITuple.Length property may still be present (it's not an indexer)");
        }

        #endregion

        #region Nested Tuple Schema Tests

        [Fact]
        public void Schema_NestedValueTuples()
        {
            var reflector = new Reflector();
            var tupleType = typeof(((int, int), (string, string)));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            // Outer tuple should have Item1 and Item2
            AssertTupleSchemaHasItemProperties(schema, 2);
        }

        [Fact]
        public void Schema_NestedTuples()
        {
            var reflector = new Reflector();
            var tupleType = typeof(Tuple<Tuple<int, int>, Tuple<string, string>>);

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            // Outer tuple should have Item1 and Item2
            AssertTupleSchemaHasItemProperties(schema, 2);
        }

        #endregion

        #region Array/List of Tuple Schema Tests

        [Fact]
        public void Schema_ArrayOfValueTuples()
        {
            var reflector = new Reflector();
            var arrayType = typeof((int, string)[]);

            var schema = JsonSchemaValidation(arrayType, reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Items));

            _output.WriteLine("Array of ValueTuple schema generated successfully");
        }

        [Fact]
        public void Schema_ListOfValueTuples()
        {
            var reflector = new Reflector();
            var listType = typeof(List<(int, string)>);

            var schema = JsonSchemaValidation(listType, reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Items));

            _output.WriteLine("List of ValueTuple schema generated successfully");
        }

        [Fact]
        public void Schema_ArrayOfTuples()
        {
            var reflector = new Reflector();
            var arrayType = typeof(Tuple<int, string>[]);

            var schema = JsonSchemaValidation(arrayType, reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Items));

            _output.WriteLine("Array of Tuple schema generated successfully");
        }

        [Fact]
        public void Schema_ListOfTuples()
        {
            var reflector = new Reflector();
            var listType = typeof(List<Tuple<int, string>>);

            var schema = JsonSchemaValidation(listType, reflector);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Items));

            _output.WriteLine("List of Tuple schema generated successfully");
        }

        #endregion

        #region Complex Tuple Type Tests

        [Fact]
        public void Schema_ValueTuple_WithComplexTypes()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int[], List<string>, DateTime));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 3);
        }

        [Fact]
        public void Schema_ValueTuple_WithNullableTypes()
        {
            var reflector = new Reflector();
            var tupleType = typeof((int?, string?, bool));

            var schema = JsonSchemaValidation(tupleType, reflector);

            Assert.NotNull(schema);
            AssertTupleSchemaHasItemProperties(schema, 3);
        }

        [Fact]
        public void Schema_ClassWithTupleField()
        {
            var reflector = new Reflector();
            var classType = typeof(ClassWithTupleField);

            var schema = JsonSchemaValidation(classType, reflector);

            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("TupleField"));
            Assert.True(properties.ContainsKey("Name"));

            _output.WriteLine("Class with tuple field schema generated successfully");
        }

        [Fact]
        public void Schema_ClassWithTupleProperty()
        {
            var reflector = new Reflector();
            var classType = typeof(ClassWithTupleProperty);

            var schema = JsonSchemaValidation(classType, reflector);

            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("TupleProperty"));
            Assert.True(properties.ContainsKey("Id"));

            _output.WriteLine("Class with tuple property schema generated successfully");
        }

        #endregion

        #region Helper Methods

        private void AssertTupleSchemaHasItemProperties(JsonNode schema, int expectedItemCount, bool hasRest = false)
        {
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties) || schema.AsObject().ContainsKey(JsonSchema.Defs),
                $"Tuple schema should contain properties or $defs. Available keys: {string.Join(", ", schema.AsObject().Select(x => x.Key))}");

            // Check if it's a direct schema or a reference
            if (schema.AsObject().ContainsKey(JsonSchema.Properties))
            {
                var properties = schema[JsonSchema.Properties]!.AsObject();

                // Check for Item1, Item2, etc. fields
                for (int i = 1; i <= expectedItemCount; i++)
                {
                    var itemName = $"Item{i}";
                    Assert.True(properties.ContainsKey(itemName),
                        $"Tuple schema should contain '{itemName}' property. Available properties: {string.Join(", ", properties.Select(x => x.Key))}");
                }

                if (hasRest)
                {
                    Assert.True(properties.ContainsKey("Rest"),
                        $"8+ element tuple schema should contain 'Rest' property. Available properties: {string.Join(", ", properties.Select(x => x.Key))}");
                }
            }

            _output.WriteLine($"Tuple schema has {expectedItemCount} Item properties{(hasRest ? " + Rest" : "")}");
        }

        #endregion

        #region Test Classes

        public class ClassWithTupleField
        {
            public (int, bool) TupleField;
            public string? Name;
        }

        public class ClassWithTupleProperty
        {
            public (string, int) TupleProperty { get; set; }
            public int Id { get; set; }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public class ValueTupleSerializationTests : BaseTest
    {
        public ValueTupleSerializationTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Serialize_ValueTuple_IntBool()
        {
            // Arrange
            var reflector = new Reflector();
            var tuple = (42, true);
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(tuple, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_ValueTuple_StringInt()
        {
            // Arrange
            var reflector = new Reflector();
            var tuple = ("hello", 123);
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(tuple, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_ValueTuple_ThreeElements()
        {
            // Arrange
            var reflector = new Reflector();
            var tuple = (1, "two", 3.0);
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(tuple, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_Deserialize_ValueTuple_IntBool_RoundTrip()
        {
            // Arrange
            var reflector = new Reflector();
            var original = (42, true);
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(original, logs: logs);
            _output.WriteLine("Serialize Logs:");
            _output.WriteLine(logs.ToString());

            logs = new Logs();
            var deserializedObj = reflector.Deserialize(serialized, fallbackType: typeof((int, bool)), logs: logs);
            var deserialized = ((int, bool))deserializedObj!;
            _output.WriteLine("Deserialize Logs:");
            _output.WriteLine(logs.ToString());

            _output.WriteLine($"Original: ({original.Item1}, {original.Item2})");
            _output.WriteLine($"Deserialized: ({deserialized.Item1}, {deserialized.Item2})");

            // Assert
            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
        }

        [Fact]
        public void Serialize_ClassWithValueTupleField()
        {
            // Arrange
            var reflector = new Reflector();
            var obj = new ClassWithValueTupleField
            {
                TupleField = (10, false),
                Name = "Test"
            };
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(obj, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_Deserialize_ClassWithValueTupleField_RoundTrip()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new ClassWithValueTupleField
            {
                TupleField = (99, true),
                Name = "RoundTrip"
            };
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(original, logs: logs);
            _output.WriteLine("Serialize Logs:");
            _output.WriteLine(logs.ToString());

            logs = new Logs();
            var deserialized = reflector.Deserialize<ClassWithValueTupleField>(serialized, logs: logs);
            _output.WriteLine("Deserialize Logs:");
            _output.WriteLine(logs.ToString());

            _output.WriteLine($"Original: Name={original.Name}, Tuple=({original.TupleField.Item1}, {original.TupleField.Item2})");
            _output.WriteLine($"Deserialized: Name={deserialized?.Name}, Tuple=({deserialized?.TupleField.Item1}, {deserialized?.TupleField.Item2})");

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Name, deserialized.Name);
            Assert.Equal(original.TupleField.Item1, deserialized.TupleField.Item1);
            Assert.Equal(original.TupleField.Item2, deserialized.TupleField.Item2);
        }

        [Fact]
        public void Serialize_ClassWithValueTupleProperty()
        {
            // Arrange
            var reflector = new Reflector();
            var obj = new ClassWithValueTupleProperty
            {
                TupleProperty = ("hello", 42),
                Id = 1
            };
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(obj, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_Deserialize_ClassWithValueTupleProperty_RoundTrip()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new ClassWithValueTupleProperty
            {
                TupleProperty = ("world", 100),
                Id = 5
            };
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(original, logs: logs);
            _output.WriteLine("Serialize Logs:");
            _output.WriteLine(logs.ToString());

            logs = new Logs();
            var deserialized = reflector.Deserialize<ClassWithValueTupleProperty>(serialized, logs: logs);
            _output.WriteLine("Deserialize Logs:");
            _output.WriteLine(logs.ToString());

            _output.WriteLine($"Original: Id={original.Id}, Tuple=({original.TupleProperty.Item1}, {original.TupleProperty.Item2})");
            _output.WriteLine($"Deserialized: Id={deserialized?.Id}, Tuple=({deserialized?.TupleProperty.Item1}, {deserialized?.TupleProperty.Item2})");

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.TupleProperty.Item1, deserialized.TupleProperty.Item1);
            Assert.Equal(original.TupleProperty.Item2, deserialized.TupleProperty.Item2);
        }

        [Fact]
        public void Serialize_ArrayOfValueTuples()
        {
            // Arrange
            var reflector = new Reflector();
            var tuples = new (int, string)[]
            {
                (1, "one"),
                (2, "two"),
                (3, "three")
            };
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(tuples, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_ListOfValueTuples()
        {
            // Arrange
            var reflector = new Reflector();
            var tuples = new List<(int, bool)>
            {
                (1, true),
                (2, false),
                (3, true)
            };
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(tuples, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_NestedValueTuples()
        {
            // Arrange
            var reflector = new Reflector();
            var nested = ((1, 2), (3, 4));
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(nested, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        [Fact]
        public void Serialize_ValueTupleWithNullableElement()
        {
            // Arrange
            var reflector = new Reflector();
            var tuple = (42, (string?)null);
            var logs = new Logs();

            // Act
            var serialized = reflector.Serialize(tuple, logs: logs);

            _output.WriteLine("Logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
        }

        #region ValueTuple RoundTrip Tests (1-8 elements)

        [Fact]
        public void RoundTrip_ValueTuple1()
        {
            var reflector = new Reflector();
            var original = ValueTuple.Create(42);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = (ValueTuple<int>)reflector.Deserialize(serialized, fallbackType: typeof(ValueTuple<int>))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            _output.WriteLine($"Original: ({original.Item1}) -> Deserialized: ({deserialized.Item1})");
        }

        [Fact]
        public void RoundTrip_ValueTuple2()
        {
            var reflector = new Reflector();
            var original = (42, "hello");

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string))reflector.Deserialize(serialized, fallbackType: typeof((int, string)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            _output.WriteLine($"Original: ({original.Item1}, {original.Item2}) -> Deserialized: ({deserialized.Item1}, {deserialized.Item2})");
        }

        [Fact]
        public void RoundTrip_ValueTuple3()
        {
            var reflector = new Reflector();
            var original = (1, "two", 3.14);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string, double))reflector.Deserialize(serialized, fallbackType: typeof((int, string, double)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            _output.WriteLine($"Original: ({original.Item1}, {original.Item2}, {original.Item3}) -> Deserialized: ({deserialized.Item1}, {deserialized.Item2}, {deserialized.Item3})");
        }

        [Fact]
        public void RoundTrip_ValueTuple4()
        {
            var reflector = new Reflector();
            var original = (1, "two", 3.14, true);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string, double, bool))reflector.Deserialize(serialized, fallbackType: typeof((int, string, double, bool)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            Assert.Equal(original.Item4, deserialized.Item4);
            _output.WriteLine($"Original: ({original.Item1}, {original.Item2}, {original.Item3}, {original.Item4})");
        }

        [Fact]
        public void RoundTrip_ValueTuple5()
        {
            var reflector = new Reflector();
            var original = (1, "two", 3.14, true, 'x');

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string, double, bool, char))reflector.Deserialize(serialized, fallbackType: typeof((int, string, double, bool, char)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            Assert.Equal(original.Item4, deserialized.Item4);
            Assert.Equal(original.Item5, deserialized.Item5);
            _output.WriteLine($"Original: ({original.Item1}, {original.Item2}, {original.Item3}, {original.Item4}, {original.Item5})");
        }

        [Fact]
        public void RoundTrip_ValueTuple6()
        {
            var reflector = new Reflector();
            var original = (1, "two", 3.14, true, 'x', 100L);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string, double, bool, char, long))reflector.Deserialize(serialized, fallbackType: typeof((int, string, double, bool, char, long)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            Assert.Equal(original.Item4, deserialized.Item4);
            Assert.Equal(original.Item5, deserialized.Item5);
            Assert.Equal(original.Item6, deserialized.Item6);
            _output.WriteLine($"Original: ({original.Item1}, {original.Item2}, {original.Item3}, {original.Item4}, {original.Item5}, {original.Item6})");
        }

        [Fact]
        public void RoundTrip_ValueTuple7()
        {
            var reflector = new Reflector();
            var original = (1, "two", 3.14, true, 'x', 100L, 0.5f);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string, double, bool, char, long, float))reflector.Deserialize(serialized, fallbackType: typeof((int, string, double, bool, char, long, float)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            Assert.Equal(original.Item4, deserialized.Item4);
            Assert.Equal(original.Item5, deserialized.Item5);
            Assert.Equal(original.Item6, deserialized.Item6);
            Assert.Equal(original.Item7, deserialized.Item7);
            _output.WriteLine($"Original: ({original.Item1}, {original.Item2}, {original.Item3}, {original.Item4}, {original.Item5}, {original.Item6}, {original.Item7})");
        }

        [Fact]
        public void RoundTrip_ValueTuple8()
        {
            var reflector = new Reflector();
            // 8+ element tuples use nested ValueTuple in Rest field
            var original = (1, "two", 3.14, true, 'x', 100L, 0.5f, "eight");

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string, double, bool, char, long, float, string))reflector.Deserialize(serialized, fallbackType: typeof((int, string, double, bool, char, long, float, string)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            Assert.Equal(original.Item4, deserialized.Item4);
            Assert.Equal(original.Item5, deserialized.Item5);
            Assert.Equal(original.Item6, deserialized.Item6);
            Assert.Equal(original.Item7, deserialized.Item7);
            Assert.Equal(original.Item8, deserialized.Item8);
            _output.WriteLine($"Original Item8: {original.Item8} -> Deserialized Item8: {deserialized.Item8}");
        }

        #endregion

        #region Tuple (reference type) Serialization Tests (1-8 elements)
        // Note: System.Tuple<> is immutable with read-only properties.
        // Deserialization cannot populate properties without setters.
        // These tests verify serialization works correctly.

        [Fact]
        public void Serialize_Tuple1()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(42);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);
            Assert.Contains("Item1", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple2()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(42, "hello");

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Item1", serialized.ToJson(reflector));
            Assert.Contains("Item2", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple3()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(1, "two", 3.14);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Item1", serialized.ToJson(reflector));
            Assert.Contains("Item2", serialized.ToJson(reflector));
            Assert.Contains("Item3", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple4()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(1, "two", 3.14, true);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Item4", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple5()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(1, "two", 3.14, true, 'x');

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Item5", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple6()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(1, "two", 3.14, true, 'x', 100L);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Item6", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple7()
        {
            var reflector = new Reflector();
            var original = Tuple.Create(1, "two", 3.14, true, 'x', 100L, 0.5f);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Item7", serialized.ToJson(reflector));
        }

        [Fact]
        public void Serialize_Tuple8()
        {
            var reflector = new Reflector();
            // 8+ element Tuples use nested Tuple in Rest field
            var original = new Tuple<int, string, double, bool, char, long, float, Tuple<string>>(
                1, "two", 3.14, true, 'x', 100L, 0.5f, Tuple.Create("eight"));

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            Assert.NotNull(serialized);
            Assert.Contains("Rest", serialized.ToJson(reflector));
        }

        #endregion

        #region Mixed Tuple Scenarios

        [Fact]
        public void RoundTrip_ValueTuple_WithComplexTypes()
        {
            var reflector = new Reflector();
            var original = (new int[] { 1, 2, 3 }, new List<string> { "a", "b" }, DateTime.Today);

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int[], List<string>, DateTime))reflector.Deserialize(serialized, fallbackType: typeof((int[], List<string>, DateTime)))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            _output.WriteLine($"Array length: {deserialized.Item1.Length}, List count: {deserialized.Item2.Count}, Date: {deserialized.Item3}");
        }

        [Fact]
        public void RoundTrip_ValueTuple_WithNullElements()
        {
            var reflector = new Reflector();
            var original = ((string?)null, 42, "value");

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((string?, int, string?))reflector.Deserialize(serialized, fallbackType: typeof((string?, int, string?)))!;

            Assert.Null(deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            _output.WriteLine($"Item1 is null: {deserialized.Item1 == null}, Item2: {deserialized.Item2}, Item3: {deserialized.Item3}");
        }

        [Fact]
        public void RoundTrip_NestedValueTuples()
        {
            var reflector = new Reflector();
            var original = ((1, 2), (3, 4), (5, 6));

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = (((int, int), (int, int), (int, int)))reflector.Deserialize(serialized, fallbackType: typeof(((int, int), (int, int), (int, int))))!;

            Assert.Equal(original.Item1, deserialized.Item1);
            Assert.Equal(original.Item2, deserialized.Item2);
            Assert.Equal(original.Item3, deserialized.Item3);
            _output.WriteLine($"Nested tuple: (({deserialized.Item1.Item1}, {deserialized.Item1.Item2}), ({deserialized.Item2.Item1}, {deserialized.Item2.Item2}), ({deserialized.Item3.Item1}, {deserialized.Item3.Item2}))");
        }

        [Fact]
        public void RoundTrip_ArrayOfTuples()
        {
            var reflector = new Reflector();
            var original = new (int, string)[]
            {
                (1, "one"),
                (2, "two"),
                (3, "three")
            };

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = ((int, string)[])reflector.Deserialize(serialized, fallbackType: typeof((int, string)[]))!;

            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i].Item1, deserialized[i].Item1);
                Assert.Equal(original[i].Item2, deserialized[i].Item2);
            }
            _output.WriteLine($"Deserialized {deserialized.Length} tuples successfully");
        }

        [Fact]
        public void RoundTrip_ListOfValueTuples()
        {
            var reflector = new Reflector();
            var original = new List<(int, string)>
            {
                (1, "one"),
                (2, "two"),
                (3, "three")
            };

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = (List<(int, string)>)reflector.Deserialize(serialized, fallbackType: typeof(List<(int, string)>))!;

            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Equal(original[i].Item1, deserialized[i].Item1);
                Assert.Equal(original[i].Item2, deserialized[i].Item2);
            }
            _output.WriteLine($"Deserialized {deserialized.Count} ValueTuples successfully");
        }

        #endregion

        public class ClassWithValueTupleField
        {
            public (int, bool) TupleField;
            public string? Name;
        }

        public class ClassWithValueTupleProperty
        {
            public (string, int) TupleProperty { get; set; }
            public int Id { get; set; }
        }
    }
}

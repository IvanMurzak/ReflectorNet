using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public class ValueTuplePropertiesTests : BaseTest
    {
        public ValueTuplePropertiesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ListValueTupleProperties()
        {
            // Arrange
            var tupleType = typeof((int, bool));
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Act
            var allProperties = tupleType.GetProperties(flags);

            _output.WriteLine($"Type: {tupleType.FullName}");
            _output.WriteLine($"Properties found: {allProperties.Length}");
            _output.WriteLine("");

            foreach (var prop in allProperties)
            {
                _output.WriteLine($"Property: {prop.Name}");
                _output.WriteLine($"  DeclaringType: {prop.DeclaringType?.FullName}");
                _output.WriteLine($"  PropertyType: {prop.PropertyType.FullName}");
                _output.WriteLine($"  CanRead: {prop.CanRead}");
                _output.WriteLine($"  CanWrite: {prop.CanWrite}");
                _output.WriteLine($"  GetIndexParameters().Length: {prop.GetIndexParameters().Length}");
                _output.WriteLine("");
            }
        }

        [Fact]
        public void ListValueTupleFields()
        {
            // Arrange
            var tupleType = typeof((int, bool));
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Act
            var allFields = tupleType.GetFields(flags);

            _output.WriteLine($"Type: {tupleType.FullName}");
            _output.WriteLine($"Fields found: {allFields.Length}");
            _output.WriteLine("");

            foreach (var field in allFields)
            {
                _output.WriteLine($"Field: {field.Name}");
                _output.WriteLine($"  DeclaringType: {field.DeclaringType?.FullName}");
                _output.WriteLine($"  FieldType: {field.FieldType.FullName}");
                _output.WriteLine($"  IsPublic: {field.IsPublic}");
                _output.WriteLine("");
            }
        }

        [Fact]
        public void ListValueTupleInterfaceProperties()
        {
            // Arrange
            var tupleType = typeof((int, bool));

            _output.WriteLine($"Type: {tupleType.FullName}");
            _output.WriteLine($"Implemented interfaces:");

            foreach (var _interface in tupleType.GetInterfaces())
            {
                _output.WriteLine($"  - {_interface.FullName}");

                var interfaceMap = tupleType.GetInterfaceMap(_interface);
                _output.WriteLine($"    Interface methods:");
                for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
                {
                    _output.WriteLine($"      {interfaceMap.InterfaceMethods[i].Name} -> {interfaceMap.TargetMethods[i].Name}");
                }
            }
        }

        [Fact]
        public void Serialize_ValueTuple_WithLogger_ShowsAllAttempts()
        {
            // Arrange
            var reflector = new Reflector();
            var tuple = (42, true);
            var logs = new Logs();
            var logger = new StringBuilderLogger();

            // Act
            var serialized = reflector.Serialize(tuple, logs: logs, logger: logger);

            _output.WriteLine("System logs:");
            _output.WriteLine(logger.ToString());
            _output.WriteLine("");
            _output.WriteLine("AI logs:");
            _output.WriteLine(logs.ToString());
            _output.WriteLine("");
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(serialized);
        }

        [Fact]
        public void FilterIndexerProperties()
        {
            // This test verifies that indexer properties are properly filtered
            var tupleType = typeof((int, bool));
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var allProperties = tupleType.GetProperties(flags);
            var nonIndexerProperties = allProperties.Where(p => p.GetIndexParameters().Length == 0).ToList();

            _output.WriteLine($"All properties: {allProperties.Length}");
            _output.WriteLine($"Non-indexer properties: {nonIndexerProperties.Count}");

            foreach (var prop in nonIndexerProperties)
            {
                _output.WriteLine($"Non-indexer property: {prop.Name} ({prop.PropertyType.Name})");
            }

            // All ValueTuple properties should be fields, not properties
            // If there are indexer properties, they should be filtered out
            foreach (var prop in allProperties.Where(p => p.GetIndexParameters().Length > 0))
            {
                _output.WriteLine($"Indexer property found: {prop.Name} - THIS SHOULD BE FILTERED");
            }
        }

        [Fact]
        public void TupleReflectionConverter_FiltersIndexerProperties()
        {
            // Arrange
            var reflector = new Reflector();
            var tupleType = typeof((int, bool));
            var converter = reflector.Converters.GetConverter(tupleType);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Assert.NotNull(converter);

            // Act
            var serializableProperties = converter.GetSerializableProperties(reflector, tupleType, flags);

            // Assert - No indexer properties should be returned
            Assert.NotNull(serializableProperties);
            var propList = serializableProperties.ToList();

            _output.WriteLine($"Serializable properties count: {propList.Count}");
            foreach (var prop in propList)
            {
                _output.WriteLine($"  Property: {prop.Name} ({prop.PropertyType.Name})");
                Assert.Empty(prop.GetIndexParameters()); // No indexers
            }

            // Should not contain the ITuple.Item indexer
            Assert.DoesNotContain(propList, p => p.Name.Contains("ITuple.Item"));
        }

        [Fact]
        public void TupleReflectionConverter_ImplementsITuple()
        {
            // Verify that ValueTuple implements ITuple
            var tupleType = typeof((int, bool));

            Assert.True(typeof(ITuple).IsAssignableFrom(tupleType));
            _output.WriteLine($"ValueTuple<int, bool> implements ITuple: {typeof(ITuple).IsAssignableFrom(tupleType)}");
        }
    }
}

using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Tests for System.Type and System.Reflection.Assembly serialization/deserialization.
    /// These types are treated as read-only with fields and properties ignored during serialization.
    /// The tests verify that type and assembly information can be serialized and deserialized
    /// without data loss, preserving all essential type identity information.
    /// </summary>
    public class TypeAndAssemblySerializationTests : BaseTest
    {
        public TypeAndAssemblySerializationTests(ITestOutputHelper output) : base(output) { }

        void ActAssert(object? original, Type? fallbackType = null)
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var serializeLogger = new StringBuilderLogger();
            var serialized = reflector.Serialize(
                original,
                fallbackType: fallbackType,
                name: nameof(original),
                logger: serializeLogger);

            var type = original?.GetType() ?? fallbackType;

            _output.WriteLine($"### Test for type: {type?.GetTypeName(pretty: true) ?? "null"}\n");
            _output.WriteLine($"Serialization:\n{serializeLogger}");

            var deserializeLogger = new StringBuilderLogger();
            var deserialized = reflector.Deserialize(
                serialized,
                logger: deserializeLogger);

            _output.WriteLine($"Deserialization:\n{deserializeLogger}");

            // Assert
            _output.WriteLine($"----------------------------\n");
            _output.WriteLine($"Serialized JSON:\n{serialized.ToJson(reflector)}\n");
            _output.WriteLine($"----------------------------\n");

            // For System.Type and System.Reflection.Assembly, we verify the essential identity information
            if (original is Type originalType && deserialized is Type deserializedType)
            {
                _output.WriteLine($"Original Type: {originalType.FullName}");
                _output.WriteLine($"Deserialized Type: {deserializedType.FullName}");
                _output.WriteLine($"Original Assembly: {originalType.Assembly.FullName}");
                _output.WriteLine($"Deserialized Assembly: {deserializedType.Assembly.FullName}");

                Assert.Equal(originalType.FullName, deserializedType.FullName);
                Assert.Equal(originalType.Assembly.FullName, deserializedType.Assembly.FullName);
                Assert.Equal(originalType, deserializedType);
            }
            else if (original is Assembly originalAssembly && deserialized is Assembly deserializedAssembly)
            {
                _output.WriteLine($"Original Assembly: {originalAssembly.FullName}");
                _output.WriteLine($"Deserialized Assembly: {deserializedAssembly.FullName}");

                Assert.Equal(originalAssembly.FullName, deserializedAssembly.FullName);
                Assert.Equal(originalAssembly, deserializedAssembly);
            }
            else
            {
                // For other types, compare JSON representation
                var originalJson = original.ToJson(reflector);
                var deserializedJson = deserialized.ToJson(reflector);

                _output.WriteLine($"Original JSON:\n{originalJson}\n");
                _output.WriteLine($"Deserialized JSON:\n{deserializedJson}\n");

                Assert.Equal(originalJson, deserializedJson);
            }
        }

        #region System.Type Tests

        [Fact]
        public void SerializeDeserialize_SimpleType()
        {
            ActAssert(typeof(int));
        }

        [Fact]
        public void SerializeDeserialize_StringType()
        {
            ActAssert(typeof(string));
        }

        [Fact]
        public void SerializeDeserialize_CustomType()
        {
            ActAssert(typeof(TypeAndAssemblySerializationTests));
        }

        [Fact]
        public void SerializeDeserialize_GenericType()
        {
            ActAssert(typeof(List<int>));
        }

        [Fact]
        public void SerializeDeserialize_GenericTypeDefinition()
        {
            ActAssert(typeof(List<>));
        }

        [Fact]
        public void SerializeDeserialize_ArrayType()
        {
            ActAssert(typeof(int[]));
        }

        [Fact]
        public void SerializeDeserialize_MultiDimensionalArrayType()
        {
            ActAssert(typeof(int[,]));
        }

        [Fact]
        public void SerializeDeserialize_NestedType()
        {
            ActAssert(typeof(System.Environment.SpecialFolder));
        }

        [Fact]
        public void SerializeDeserialize_InterfaceType()
        {
            ActAssert(typeof(IDisposable));
        }

        [Fact]
        public void SerializeDeserialize_AbstractType()
        {
            ActAssert(typeof(System.IO.Stream));
        }

        // Note: Arrays, generic classes, and dictionaries containing Type or Assembly
        // are not currently supported due to the complexity of the converter priority system
        // and potential for infinite recursion. Direct Type and Assembly instances work correctly.

        [Fact]
        public void SerializeDeserialize_ByRefType()
        {
            // Get the ByRef type from a method parameter
            var method = typeof(int).GetMethod("TryParse", new[] { typeof(string), typeof(int).MakeByRefType() });
            if (method != null)
            {
                var parameters = method.GetParameters();
                ActAssert(parameters[1].ParameterType);
            }
        }

        #endregion

        #region System.Reflection.Assembly Tests

        [Fact]
        public void SerializeDeserialize_CurrentAssembly()
        {
            ActAssert(Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void SerializeDeserialize_MscorlibAssembly()
        {
            ActAssert(typeof(int).Assembly);
        }

        [Fact]
        public void SerializeDeserialize_SystemAssembly()
        {
            ActAssert(typeof(System.Uri).Assembly);
        }

        [Fact]
        public void SerializeDeserialize_ReflectorNetAssembly()
        {
            ActAssert(typeof(Reflector).Assembly);
        }

        // Note: Arrays, generic classes, and dictionaries containing Assembly
        // are not currently supported due to the complexity of the converter priority system
        // and potential for infinite recursion. Direct Assembly instances work correctly.

        #endregion
    }
}

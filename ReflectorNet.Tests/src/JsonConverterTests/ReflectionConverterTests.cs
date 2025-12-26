using System;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for reflection type JSON converters: Assembly, PropertyInfo, ConstructorInfo, ParameterInfo.
    /// Note: These converters are primarily designed for deserialization from known JSON formats.
    /// </summary>
    public class ReflectionConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public ReflectionConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        // Sample class for testing reflection converters
        public class SampleClass
        {
            public string? Name { get; set; }
            public int Value { get; set; }
#pragma warning disable CS0169 // Field is never used - kept for reflection tests
            private string? _privateField;
#pragma warning restore CS0169

            public SampleClass() { }
            public SampleClass(string name) { Name = name; }
            public SampleClass(string name, int value) { Name = name; Value = value; }

            public void DoSomething(string input, int count) { }
            public static void StaticMethod() { }
        }

        #region AssemblyJsonConverter Tests

        [Fact]
        public void Assembly_Read_ByFullName()
        {
            var original = typeof(ReflectionConverterTests).Assembly;
            var json = $"\"{original.FullName}\"";
            var result = _reflector.JsonSerializer.Deserialize<Assembly>(json);
            _output.WriteLine($"Assembly from full name: {result?.GetName().Name}");
            Assert.NotNull(result);
            Assert.Equal(original.FullName, result.FullName);
        }

        [Fact]
        public void Assembly_Read_ByShortName()
        {
            var original = typeof(string).Assembly;
            var shortName = original.GetName().Name;
            var json = $"\"{shortName}\"";
            var result = _reflector.JsonSerializer.Deserialize<Assembly>(json);
            _output.WriteLine($"Assembly from short name '{shortName}': {result?.FullName}");
            Assert.NotNull(result);
        }

        [Fact]
        public void Assembly_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Assembly>(json);
            _output.WriteLine($"Assembly from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Assembly_Read_EmptyString_ReturnsNull()
        {
            var json = "\"\"";
            var result = _reflector.JsonSerializer.Deserialize<Assembly>(json);
            _output.WriteLine($"Assembly from empty: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Assembly_Read_NotFound_Throws()
        {
            var json = "\"NonExistent.Assembly.Name.That.Does.Not.Exist\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Assembly>(json));
        }

        [Fact]
        public void Assembly_Read_WrongTokenType_Throws()
        {
            var json = "12345";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Assembly>(json));
        }

        [Fact]
        public void Assembly_Read_TestsAssembly()
        {
            // Use a known loaded assembly
            var assemblyName = "ReflectorNet.Tests";
            var json = $"\"{assemblyName}\"";
            var result = _reflector.JsonSerializer.Deserialize<Assembly>(json);
            _output.WriteLine($"Assembly {assemblyName}: {result?.GetName().Name}");
            Assert.NotNull(result);
            Assert.Contains("ReflectorNet.Tests", result.GetName().Name ?? "");
        }

        #endregion

        #region PropertyInfoConverter Tests

        [Fact]
        public void PropertyInfo_Read_ValidProperty()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"name\":\"Name\",\"declaringType\":\"{declaringType}\"}}";
            var result = _reflector.JsonSerializer.Deserialize<PropertyInfo>(json);
            _output.WriteLine($"PropertyInfo from json: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("Name", result.Name);
            Assert.Equal(typeof(string), result.PropertyType);
        }

        [Fact]
        public void PropertyInfo_Read_IntProperty()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"name\":\"Value\",\"declaringType\":\"{declaringType}\"}}";
            var result = _reflector.JsonSerializer.Deserialize<PropertyInfo>(json);
            _output.WriteLine($"PropertyInfo int: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("Value", result.Name);
            Assert.Equal(typeof(int), result.PropertyType);
        }

        [Fact]
        public void PropertyInfo_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<PropertyInfo>(json);
            _output.WriteLine($"PropertyInfo from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void PropertyInfo_Read_MissingFields_Throws()
        {
            var json = "{\"name\":\"Name\"}"; // missing declaringType
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<PropertyInfo>(json));
        }

        [Fact]
        public void PropertyInfo_Read_PropertyNotFound_Throws()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"name\":\"NonExistentProperty\",\"declaringType\":\"{declaringType}\"}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<PropertyInfo>(json));
        }

        #endregion

        #region ConstructorInfoConverter Tests

        [Fact]
        public void ConstructorInfo_Read_DefaultConstructor()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"declaringType\":\"{declaringType}\",\"parameters\":[]}}";
            var result = _reflector.JsonSerializer.Deserialize<ConstructorInfo>(json);
            _output.WriteLine($"ConstructorInfo from default: {result}");
            Assert.NotNull(result);
            Assert.Empty(result.GetParameters());
        }

        [Fact]
        public void ConstructorInfo_Read_SingleParameter()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var stringType = typeof(string).GetTypeId();
            var json = $"{{\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{stringType}\"}}]}}";
            var result = _reflector.JsonSerializer.Deserialize<ConstructorInfo>(json);
            _output.WriteLine($"ConstructorInfo single param: {result}");
            Assert.NotNull(result);
            Assert.Single(result.GetParameters());
            Assert.Equal(typeof(string), result.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void ConstructorInfo_Read_MultipleParameters()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var stringType = typeof(string).GetTypeId();
            var intType = typeof(int).GetTypeId();
            var json = $"{{\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{stringType}\"}},{{\"type\":\"{intType}\"}}]}}";
            var result = _reflector.JsonSerializer.Deserialize<ConstructorInfo>(json);
            _output.WriteLine($"ConstructorInfo multi params: {result}");
            Assert.NotNull(result);
            Assert.Equal(2, result.GetParameters().Length);
        }

        [Fact]
        public void ConstructorInfo_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<ConstructorInfo>(json);
            _output.WriteLine($"ConstructorInfo from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void ConstructorInfo_Read_MissingDeclaringType_Throws()
        {
            var json = "{\"parameters\":[]}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<ConstructorInfo>(json));
        }

        [Fact]
        public void ConstructorInfo_Read_NotFound_Throws()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var doubleType = typeof(double).GetTypeId();
            // SampleClass doesn't have a constructor that takes double
            var json = $"{{\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{doubleType}\"}}]}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<ConstructorInfo>(json));
        }

        #endregion

        #region ParameterInfoConverter Tests

        [Fact]
        public void ParameterInfo_Read_MethodParameter()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var stringType = typeof(string).GetTypeId();
            var intType = typeof(int).GetTypeId();
            // Build the member JSON for DoSomething method
            var memberJson = $"{{\"name\":\"DoSomething\",\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{stringType}\"}},{{\"type\":\"{intType}\"}}]}}";
            var json = $"{{\"name\":\"input\",\"memberType\":\"MethodInfo\",\"member\":{memberJson}}}";
            var result = _reflector.JsonSerializer.Deserialize<ParameterInfo>(json);
            _output.WriteLine($"ParameterInfo from method: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("input", result.Name);
            Assert.Equal(typeof(string), result.ParameterType);
        }

        [Fact]
        public void ParameterInfo_Read_ConstructorParameter()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var stringType = typeof(string).GetTypeId();
            // Build the member JSON for the single-param constructor
            var memberJson = $"{{\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{stringType}\"}}]}}";
            var json = $"{{\"name\":\"name\",\"memberType\":\"ConstructorInfo\",\"member\":{memberJson}}}";
            var result = _reflector.JsonSerializer.Deserialize<ParameterInfo>(json);
            _output.WriteLine($"ParameterInfo from ctor: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("name", result.Name);
            Assert.Equal(typeof(string), result.ParameterType);
        }

        [Fact]
        public void ParameterInfo_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<ParameterInfo>(json);
            _output.WriteLine($"ParameterInfo from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void ParameterInfo_Read_MissingFields_Throws()
        {
            var json = "{\"name\":\"input\"}"; // missing member and memberType
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<ParameterInfo>(json));
        }

        #endregion

        // Sample class for field/method reflection tests
        public class FieldReflectionSampleClass
        {
            public string? Name;
            public int Value;
#pragma warning disable CS0169 // Field is never used - needed for reflection tests
            private string? _privateField;
#pragma warning restore CS0169

            public void PublicMethod(string input) { }
            public int CalculateValue(int a, int b) => a + b;
            private void PrivateMethod() { }
        }

        #region FieldInfoConverter Tests

        [Fact]
        public void FieldInfo_Deserialize_PublicField()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var json = $"{{\"name\":\"Name\",\"declaringType\":\"{declaringType}\"}}";
            var result = _reflector.JsonSerializer.Deserialize<FieldInfo>(json);
            _output.WriteLine($"FieldInfo public: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("Name", result.Name);
        }

        [Fact]
        public void FieldInfo_Deserialize_ValueField()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var json = $"{{\"name\":\"Value\",\"declaringType\":\"{declaringType}\"}}";
            var result = _reflector.JsonSerializer.Deserialize<FieldInfo>(json);
            _output.WriteLine($"FieldInfo value: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("Value", result.Name);
            Assert.Equal(typeof(int), result.FieldType);
        }

        [Fact]
        public void FieldInfo_Deserialize_PrivateField()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var json = $"{{\"name\":\"_privateField\",\"declaringType\":\"{declaringType}\"}}";
            var result = _reflector.JsonSerializer.Deserialize<FieldInfo>(json);
            _output.WriteLine($"FieldInfo private: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("_privateField", result.Name);
        }

        [Fact]
        public void FieldInfo_Deserialize_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<FieldInfo>(json);
            _output.WriteLine($"FieldInfo from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void FieldInfo_Deserialize_MissingName_Throws()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var json = $"{{\"declaringType\":\"{declaringType}\"}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<FieldInfo>(json));
        }

        [Fact]
        public void FieldInfo_Deserialize_FieldNotFound_Throws()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var json = $"{{\"name\":\"NonExistentField\",\"declaringType\":\"{declaringType}\"}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<FieldInfo>(json));
        }

        #endregion

        #region MethodInfoConverter Tests

        [Fact]
        public void MethodInfo_Deserialize_SimpleMethod()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var stringType = typeof(string).GetTypeId();
            var json = $"{{\"name\":\"PublicMethod\",\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{stringType}\"}}]}}";
            var result = _reflector.JsonSerializer.Deserialize<MethodInfo>(json);
            _output.WriteLine($"MethodInfo simple: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("PublicMethod", result.Name);
        }

        [Fact]
        public void MethodInfo_Deserialize_MethodWithMultipleParams()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var intType = typeof(int).GetTypeId();
            var json = $"{{\"name\":\"CalculateValue\",\"declaringType\":\"{declaringType}\",\"parameters\":[{{\"type\":\"{intType}\"}},{{\"type\":\"{intType}\"}}]}}";
            var result = _reflector.JsonSerializer.Deserialize<MethodInfo>(json);
            _output.WriteLine($"MethodInfo multi params: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("CalculateValue", result.Name);
            Assert.Equal(2, result.GetParameters().Length);
        }

        [Fact]
        public void MethodInfo_Deserialize_MissingDeclaringType_Throws()
        {
            var json = "{\"name\":\"PublicMethod\",\"parameters\":[]}";
            // Throws KeyNotFoundException when declaringType is missing
            Assert.ThrowsAny<Exception>(() => _reflector.JsonSerializer.Deserialize<MethodInfo>(json));
        }

        [Fact]
        public void MethodInfo_Deserialize_MethodNotFound_Throws()
        {
            var declaringType = typeof(FieldReflectionSampleClass).GetTypeId();
            var json = $"{{\"name\":\"NonExistentMethod\",\"declaringType\":\"{declaringType}\",\"parameters\":[]}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<MethodInfo>(json));
        }

        #endregion

        #region TypeJsonConverter Tests

        [Fact]
        public void Type_Serialize_Simple()
        {
            var value = typeof(string);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Type));
            _output.WriteLine($"Type simple: {json}");
            Assert.Contains("System.String", json);
        }

        [Fact]
        public void Type_Serialize_Custom()
        {
            var value = typeof(FieldReflectionSampleClass);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Type));
            _output.WriteLine($"Type custom: {json}");
            Assert.Contains("FieldReflectionSampleClass", json);
        }

        [Fact]
        public void Type_Serialize_Null()
        {
            Type? value = null;
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Type));
            _output.WriteLine($"Type null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Type_Deserialize_Simple()
        {
            var json = "\"System.String\"";
            var result = _reflector.JsonSerializer.Deserialize<Type>(json);
            _output.WriteLine($"Type from json: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal(typeof(string), result);
        }

        [Fact]
        public void Type_Deserialize_Int32()
        {
            var json = "\"System.Int32\"";
            var result = _reflector.JsonSerializer.Deserialize<Type>(json);
            _output.WriteLine($"Type Int32: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal(typeof(int), result);
        }

        [Fact]
        public void Type_Deserialize_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Type>(json);
            _output.WriteLine($"Type from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Type_RoundTrip()
        {
            var original = typeof(DateTime);
            var json = _reflector.JsonSerializer.Serialize(original, typeof(Type));
            var deserialized = _reflector.JsonSerializer.Deserialize<Type>(json);
            _output.WriteLine($"Type roundtrip: {original.Name} -> {json} -> {deserialized?.Name}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}

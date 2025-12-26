using System;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for additional JSON converters: Enum, Exception, FieldInfo, MethodInfo, Type,
    /// UInt16, UInt32, UInt64, DateTime, Guid
    /// </summary>
    public class AdditionalConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public AdditionalConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        // Sample enum for testing
        public enum TestStatus
        {
            Unknown = 0,
            Active = 1,
            Inactive = 2,
            Pending = 3
        }

        // Sample class for reflection tests
        public class SampleClass
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

        #region EnumJsonConverter Tests

        [Fact]
        public void Enum_Serialize_Value()
        {
            var value = TestStatus.Active;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Enum value: {json}");
            Assert.Contains("Active", json);
        }

        [Fact]
        public void Enum_Serialize_Null()
        {
            TestStatus? value = null;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Enum null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Enum_Deserialize_FromString()
        {
            var json = "\"Active\"";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum from string: {result}");
            Assert.Equal(TestStatus.Active, result);
        }

        [Fact]
        public void Enum_Deserialize_FromString_CaseInsensitive()
        {
            var json = "\"active\"";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum case insensitive: {result}");
            Assert.Equal(TestStatus.Active, result);
        }

        [Fact]
        public void Enum_Deserialize_FromNumber()
        {
            var json = "2";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum from number: {result}");
            Assert.Equal(TestStatus.Inactive, result);
        }

        [Fact]
        public void Enum_Deserialize_Null_ToNullable()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus?>(json);
            _output.WriteLine($"Enum null to nullable: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Enum_Deserialize_InvalidString_Throws()
        {
            var json = "\"NotAValidValue\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<TestStatus>(json));
        }

        [Fact]
        public void Enum_RoundTrip()
        {
            var original = TestStatus.Pending;
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region ExceptionJsonConverter Tests

        [Fact]
        public void Exception_Serialize_Simple()
        {
            var value = new Exception("Test error message");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Exception));
            _output.WriteLine($"Exception simple: {json}");
            Assert.Contains("Test error message", json);
            Assert.Contains("type", json);
        }

        [Fact]
        public void Exception_Serialize_WithInnerException()
        {
            var inner = new InvalidOperationException("Inner error");
            var value = new Exception("Outer error", inner);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Exception));
            _output.WriteLine($"Exception with inner: {json}");
            Assert.Contains("Outer error", json);
            Assert.Contains("innerException", json);
        }

        [Fact]
        public void Exception_Serialize_Null()
        {
            Exception? value = null;
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Exception));
            _output.WriteLine($"Exception null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Exception_Deserialize_Simple()
        {
            var json = "{\"type\":\"System.Exception\",\"message\":\"Test message\"}";
            var result = _reflector.JsonSerializer.Deserialize<Exception>(json);
            _output.WriteLine($"Exception from json: {result?.Message}");
            Assert.NotNull(result);
            Assert.Contains("Test message", result.Message);
        }

        [Fact]
        public void Exception_Deserialize_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Exception>(json);
            _output.WriteLine($"Exception from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Exception_RoundTrip()
        {
            var original = new ArgumentException("Invalid argument");
            var json = _reflector.JsonSerializer.Serialize(original, typeof(Exception));
            var deserialized = _reflector.JsonSerializer.Deserialize<Exception>(json);
            _output.WriteLine($"Exception roundtrip: {original.Message} -> {json}");
            Assert.NotNull(deserialized);
            Assert.Contains("Invalid argument", deserialized.Message);
        }

        #endregion

        #region FieldInfoConverter Tests

        [Fact]
        public void FieldInfo_Deserialize_PublicField()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"name\":\"Name\",\"declaringType\":\"{declaringType}\"}}";
            var result = _reflector.JsonSerializer.Deserialize<FieldInfo>(json);
            _output.WriteLine($"FieldInfo public: {result?.Name}");
            Assert.NotNull(result);
            Assert.Equal("Name", result.Name);
        }

        [Fact]
        public void FieldInfo_Deserialize_ValueField()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
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
            var declaringType = typeof(SampleClass).GetTypeId();
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
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"declaringType\":\"{declaringType}\"}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<FieldInfo>(json));
        }

        [Fact]
        public void FieldInfo_Deserialize_FieldNotFound_Throws()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
            var json = $"{{\"name\":\"NonExistentField\",\"declaringType\":\"{declaringType}\"}}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<FieldInfo>(json));
        }

        #endregion

        #region MethodInfoConverter Tests

        [Fact]
        public void MethodInfo_Deserialize_SimpleMethod()
        {
            var declaringType = typeof(SampleClass).GetTypeId();
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
            var declaringType = typeof(SampleClass).GetTypeId();
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
            var declaringType = typeof(SampleClass).GetTypeId();
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
            var value = typeof(SampleClass);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Type));
            _output.WriteLine($"Type custom: {json}");
            Assert.Contains("SampleClass", json);
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

        #region UInt16JsonConverter Tests

        [Fact]
        public void UInt16_Serialize_Value()
        {
            ushort value = 12345;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt16 value: {json}");
            Assert.Equal("12345", json);
        }

        [Fact]
        public void UInt16_Serialize_Zero()
        {
            ushort value = 0;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt16 zero: {json}");
            Assert.Equal("0", json);
        }

        [Fact]
        public void UInt16_Serialize_MaxValue()
        {
            ushort value = ushort.MaxValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt16 max: {json}");
            Assert.Equal("65535", json);
        }

        [Fact]
        public void UInt16_Deserialize_FromNumber()
        {
            var json = "100";
            var result = _reflector.JsonSerializer.Deserialize<ushort>(json);
            _output.WriteLine($"UInt16 from number: {result}");
            Assert.Equal((ushort)100, result);
        }

        [Fact]
        public void UInt16_Deserialize_FromString()
        {
            var json = "\"200\"";
            var result = _reflector.JsonSerializer.Deserialize<ushort>(json);
            _output.WriteLine($"UInt16 from string: {result}");
            Assert.Equal((ushort)200, result);
        }

        [Fact]
        public void UInt16_Deserialize_Null_ToNullable()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<ushort?>(json);
            _output.WriteLine($"UInt16 null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void UInt16_RoundTrip()
        {
            ushort original = 50000;
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<ushort>(json);
            _output.WriteLine($"UInt16 roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region UInt32JsonConverter Tests

        [Fact]
        public void UInt32_Serialize_Value()
        {
            uint value = 1234567890;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt32 value: {json}");
            Assert.Equal("1234567890", json);
        }

        [Fact]
        public void UInt32_Serialize_MaxValue()
        {
            uint value = uint.MaxValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt32 max: {json}");
            Assert.Equal("4294967295", json);
        }

        [Fact]
        public void UInt32_Deserialize_FromNumber()
        {
            var json = "999999";
            var result = _reflector.JsonSerializer.Deserialize<uint>(json);
            _output.WriteLine($"UInt32 from number: {result}");
            Assert.Equal(999999u, result);
        }

        [Fact]
        public void UInt32_Deserialize_FromString()
        {
            var json = "\"100000\"";
            var result = _reflector.JsonSerializer.Deserialize<uint>(json);
            _output.WriteLine($"UInt32 from string: {result}");
            Assert.Equal(100000u, result);
        }

        [Fact]
        public void UInt32_RoundTrip()
        {
            uint original = 3000000000;
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<uint>(json);
            _output.WriteLine($"UInt32 roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region UInt64JsonConverter Tests

        [Fact]
        public void UInt64_Serialize_Value()
        {
            ulong value = 12345678901234567890UL;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt64 value: {json}");
            Assert.Contains("12345678901234567890", json);
        }

        [Fact]
        public void UInt64_Serialize_MaxValue()
        {
            ulong value = ulong.MaxValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UInt64 max: {json}");
            Assert.Contains("18446744073709551615", json);
        }

        [Fact]
        public void UInt64_Deserialize_FromNumber()
        {
            var json = "999999999";
            var result = _reflector.JsonSerializer.Deserialize<ulong>(json);
            _output.WriteLine($"UInt64 from number: {result}");
            Assert.Equal(999999999UL, result);
        }

        [Fact]
        public void UInt64_Deserialize_FromString()
        {
            var json = "\"10000000000\"";
            var result = _reflector.JsonSerializer.Deserialize<ulong>(json);
            _output.WriteLine($"UInt64 from string: {result}");
            Assert.Equal(10000000000UL, result);
        }

        [Fact]
        public void UInt64_RoundTrip()
        {
            ulong original = 9999999999999UL;
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<ulong>(json);
            _output.WriteLine($"UInt64 roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region DateTimeJsonConverter Tests

        [Fact]
        public void DateTime_Serialize_Value()
        {
            var value = new DateTime(2024, 12, 25, 10, 30, 45, DateTimeKind.Utc);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateTime value: {json}");
            Assert.Contains("2024", json);
            Assert.Contains("12", json);
            Assert.Contains("25", json);
        }

        [Fact]
        public void DateTime_Serialize_MinValue()
        {
            var value = DateTime.MinValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateTime min: {json}");
            Assert.Contains("0001", json);
        }

        [Fact]
        public void DateTime_Deserialize_Iso8601()
        {
            var json = "\"2024-06-15T14:30:00Z\"";
            var result = _reflector.JsonSerializer.Deserialize<DateTime>(json);
            _output.WriteLine($"DateTime from ISO: {result}");
            Assert.Equal(2024, result.Year);
            Assert.Equal(6, result.Month);
            Assert.Equal(15, result.Day);
        }

        [Fact]
        public void DateTime_RoundTrip()
        {
            var original = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<DateTime>(json);
            _output.WriteLine($"DateTime roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original.Year, deserialized.Year);
            Assert.Equal(original.Month, deserialized.Month);
            Assert.Equal(original.Day, deserialized.Day);
        }

        #endregion

        #region GuidJsonConverter Tests

        [Fact]
        public void Guid_Serialize_Value()
        {
            var value = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Guid value: {json}");
            Assert.Contains("12345678-1234-1234-1234-123456789abc", json);
        }

        [Fact]
        public void Guid_Serialize_Empty()
        {
            var value = Guid.Empty;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Guid empty: {json}");
            Assert.Contains("00000000-0000-0000-0000-000000000000", json);
        }

        [Fact]
        public void Guid_Deserialize_WithDashes()
        {
            var json = "\"abcdef12-3456-7890-abcd-ef1234567890\"";
            var result = _reflector.JsonSerializer.Deserialize<Guid>(json);
            _output.WriteLine($"Guid from dashes: {result}");
            Assert.Equal(Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890"), result);
        }

        [Fact]
        public void Guid_Deserialize_Null_ToNullable()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Guid?>(json);
            _output.WriteLine($"Guid null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Guid_RoundTrip()
        {
            var original = Guid.NewGuid();
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Guid>(json);
            _output.WriteLine($"Guid roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}

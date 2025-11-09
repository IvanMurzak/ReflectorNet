using System;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Tests for GetReturnSchema method that generates JSON schemas for method return types
    /// </summary>
    public class ReturnSchemaTests : SchemaTestBase
    {
        // Method name constants for WrapperClass to avoid hardcoded strings
        private const string EchoMethodName = "Echo";
        private const string EchoNullableMethodName = "EchoNullable";

        public ReturnSchemaTests(ITestOutputHelper output) : base(output) { }

        #region Test Helper Methods

        // Void method
        private void VoidMethod() { }

        // Task method (async void equivalent)
        private Task TaskMethod() => Task.CompletedTask;

        // ValueTask method (async void equivalent)
        private ValueTask ValueTaskMethod() => ValueTask.CompletedTask;

        // Primitive return types
        private string StringMethod() => "test";
        private int IntMethod() => 42;
        private bool BoolMethod() => true;
        private double DoubleMethod() => 3.14;
        private DateTime DateTimeMethod() => DateTime.Now;

        // Task<T> return types (should be unwrapped)
        private Task<string> TaskStringMethod() => Task.FromResult("test");
        private Task<int> TaskIntMethod() => Task.FromResult(42);
        private Task<bool> TaskBoolMethod() => Task.FromResult(true);
        private Task<CustomReturnType> TaskCustomTypeMethod() => Task.FromResult(new CustomReturnType());
        private Task<Person> TaskPersonMethod() => Task.FromResult(new Person());
        private Task<Address> TaskAddressMethod() => Task.FromResult(new Address());
        private Task<Company> TaskCompanyMethod() => Task.FromResult(new Company());

        // ValueTask<T> return types (should be unwrapped)
        private ValueTask<string> ValueTaskStringMethod() => ValueTask.FromResult("test");
        private ValueTask<int> ValueTaskIntMethod() => ValueTask.FromResult(42);
        private ValueTask<CustomReturnType> ValueTaskCustomTypeMethod() => ValueTask.FromResult(new CustomReturnType());
        private ValueTask<Person> ValueTaskPersonMethod() => ValueTask.FromResult(new Person());
        private ValueTask<Address> ValueTaskAddressMethod() => ValueTask.FromResult(new Address());
        private ValueTask<Company> ValueTaskCompanyMethod() => ValueTask.FromResult(new Company());

        // Custom types
        private CustomReturnType CustomTypeMethod() => new CustomReturnType();
        private ComplexReturnType ComplexTypeMethod() => new ComplexReturnType();
        private Person PersonMethod() => new Person();
        private Address AddressMethod() => new Address();
        private Company CompanyMethod() => new Company();

        // Collections
        private string[] StringArrayMethod() => new[] { "test" };
        private List<int> ListIntMethod() => new List<int>();
        private Dictionary<string, int> DictionaryMethod() => new Dictionary<string, int>();

        // List<ComplexReturnType> variations
        private List<ComplexReturnType> ListComplexTypeMethod() => new List<ComplexReturnType>();
        private List<ComplexReturnType?> ListNullableComplexTypeMethod() => new List<ComplexReturnType?>();
        private List<ComplexReturnType>? NullableListComplexTypeMethod() => new List<ComplexReturnType>();
        private List<ComplexReturnType?>? NullableListNullableComplexTypeMethod() => new List<ComplexReturnType?>();
        private Task<List<ComplexReturnType>> TaskListComplexTypeMethod() => Task.FromResult(new List<ComplexReturnType>());
        private Task<List<ComplexReturnType>>? NullableTaskListComplexTypeMethod() => Task.FromResult(new List<ComplexReturnType>());
        private Task<List<ComplexReturnType>?> TaskNullableListComplexTypeMethod() => Task.FromResult<List<ComplexReturnType>?>(new List<ComplexReturnType>());
        private Task<List<ComplexReturnType?>> TaskListNullableComplexTypeMethod() => Task.FromResult(new List<ComplexReturnType?>());
        private Task<List<ComplexReturnType>?>? NullableTaskNullableListComplexTypeMethod() => Task.FromResult<List<ComplexReturnType>?>(new List<ComplexReturnType>());
        private Task<List<ComplexReturnType?>?> TaskNullableListNullableComplexTypeMethod() => Task.FromResult<List<ComplexReturnType?>?>(new List<ComplexReturnType?>());
        private Task<List<ComplexReturnType?>>? NullableTaskListNullableComplexTypeMethod() => Task.FromResult(new List<ComplexReturnType?>());
        private Task<List<ComplexReturnType?>?>? NullableTaskNullableListNullableComplexTypeMethod() => Task.FromResult<List<ComplexReturnType?>?>(new List<ComplexReturnType?>());

        // Nullable return types
        private string? NullableStringMethod() => "test";
        private int? NullableIntMethod() => 42;
        private bool? NullableBoolMethod() => true;
        private double? NullableDoubleMethod() => 3.14;
        private DateTime? NullableDateTimeMethod() => DateTime.Now;
        private CustomReturnType? NullableCustomTypeMethod() => new CustomReturnType();
        private ComplexReturnType? NullableComplexTypeMethod() => new ComplexReturnType();
        private Person? NullablePersonMethod() => new Person();
        private Address? NullableAddressMethod() => new Address();
        private Company? NullableCompanyMethod() => new Company();
        private string[]? NullableStringArrayMethod() => new[] { "test" };

        // Task<T?> return types (nullable wrapped in Task)
        private Task<string?> TaskNullableStringMethod() => Task.FromResult<string?>("test");
        private Task<int?> TaskNullableIntMethod() => Task.FromResult<int?>(42);
        private Task<bool?> TaskNullableBoolMethod() => Task.FromResult<bool?>(true);
        private Task<CustomReturnType?> TaskNullableCustomTypeMethod() => Task.FromResult<CustomReturnType?>(new CustomReturnType());
        private Task<Person?> TaskNullablePersonMethod() => Task.FromResult<Person?>(new Person());
        private Task<Address?> TaskNullableAddressMethod() => Task.FromResult<Address?>(new Address());
        private Task<Company?> TaskNullableCompanyMethod() => Task.FromResult<Company?>(new Company());

        // ValueTask<T?> return types (nullable wrapped in ValueTask)
        private ValueTask<string?> ValueTaskNullableStringMethod() => ValueTask.FromResult<string?>("test");
        private ValueTask<int?> ValueTaskNullableIntMethod() => ValueTask.FromResult<int?>(42);
        private ValueTask<CustomReturnType?> ValueTaskNullableCustomTypeMethod() => ValueTask.FromResult<CustomReturnType?>(new CustomReturnType());
        private ValueTask<Person?> ValueTaskNullablePersonMethod() => ValueTask.FromResult<Person?>(new Person());
        private ValueTask<Address?> ValueTaskNullableAddressMethod() => ValueTask.FromResult<Address?>(new Address());
        private ValueTask<Company?> ValueTaskNullableCompanyMethod() => ValueTask.FromResult<Company?>(new Company());

        // Task<T?>? return types (nullable T wrapped in nullable Task)
        private Task<string?>? NullableTaskNullableStringMethod() => Task.FromResult<string?>("test");
        private Task<int?>? NullableTaskNullableIntMethod() => Task.FromResult<int?>(42);
        private Task<CustomReturnType?>? NullableTaskNullableCustomTypeMethod() => Task.FromResult<CustomReturnType?>(new CustomReturnType());
        private Task<Person?>? NullableTaskNullablePersonMethod() => Task.FromResult<Person?>(new Person());
        private Task<Address?>? NullableTaskNullableAddressMethod() => Task.FromResult<Address?>(new Address());
        private Task<Company?>? NullableTaskNullableCompanyMethod() => Task.FromResult<Company?>(new Company());

        // NOTE: ValueTask<T?>? is not a valid scenario because ValueTask is a struct, not a class
        // So it cannot be made nullable in the reference type sense. We removed these tests.

        #endregion

        #region Test Classes

        public class CustomReturnType
        {
            public string Name { get; set; } = "Test";
            public int Value { get; set; } = 123;
        }

        public class ComplexReturnType
        {
            public string StringProperty { get; set; } = "test";
            public int IntProperty { get; set; } = 42;
            public CustomReturnType NestedObject { get; set; } = new CustomReturnType();
            public string[] StringArray { get; set; } = new[] { "a", "b" };
        }

        #endregion

        #region Helper Methods for Testing

        /// <summary>
        /// Tests that a method returns a complex object schema with expected type definitions
        /// </summary>
        private void TestComplexObjectSchema(string methodName, bool shouldBeRequired, params Type[] expectedDefinedTypes)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);

            if (expectedDefinedTypes.Length > 0)
                AssertResultDefines(schema, expectedDefinedTypes);

            AssertAllRefsDefined(schema);
        }

        /// <summary>
        /// Tests wrapper class methods with complex types
        /// </summary>
        private void TestWrapperComplexObjectSchema(Type wrapperType, string methodName, bool shouldBeRequired, params Type[] expectedDefinedTypes)
        {
            var reflector = new Reflector();
            var methodInfo = wrapperType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
            var schema = reflector.GetReturnSchema(methodInfo);

            _output.WriteLine($"Return schema for {wrapperType.GetTypeShortName()}.{methodName}:");
            _output.WriteLine(schema?.ToString() ?? "null");
            _output.WriteLine("");

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);

            if (expectedDefinedTypes.Length > 0)
                AssertResultDefines(schema, expectedDefinedTypes);

            AssertAllRefsDefined(schema);
        }

        #endregion

        #region Void Return Type Tests

        [Theory]
        [InlineData(nameof(VoidMethod))]
        [InlineData(nameof(TaskMethod))]
        [InlineData(nameof(ValueTaskMethod))]
        public void GetReturnSchema_VoidMethods_ReturnsNull(string methodName)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            Assert.Null(schema);
        }

        #endregion

        #region Primitive Return Type Tests

        [Theory]
        [InlineData(nameof(StringMethod), JsonSchema.String)]
        [InlineData(nameof(IntMethod), JsonSchema.Integer)]
        [InlineData(nameof(BoolMethod), JsonSchema.Boolean)]
        [InlineData(nameof(DoubleMethod), JsonSchema.Number)]
        public void GetReturnSchema_PrimitiveMethod_ReturnsCorrectSchema(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        #endregion

        #region Nullable Primitive Return Type Tests

        [Theory]
        [InlineData(nameof(NullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(NullableIntMethod), JsonSchema.Integer)]
        [InlineData(nameof(NullableBoolMethod), JsonSchema.Boolean)]
        [InlineData(nameof(NullableDoubleMethod), JsonSchema.Number)]
        [InlineData(nameof(NullableDateTimeMethod), JsonSchema.String)] // DateTime serialized as string
        public void GetReturnSchema_NullablePrimitiveMethod_ReturnsSchemaWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        #endregion

        #region Task<T> Unwrapping Tests

        [Theory]
        [InlineData(nameof(TaskStringMethod), JsonSchema.String)]
        [InlineData(nameof(TaskIntMethod), JsonSchema.Integer)]
        [InlineData(nameof(TaskBoolMethod), JsonSchema.Boolean)]
        public void GetReturnSchema_TaskPrimitive_UnwrapsCorrectly(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_TaskCustomType_UnwrapsToCustomTypeSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(TaskPersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(TaskAddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(TaskCompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_TaskComplexType_UnwrapsCorrectly(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: true, expectedDefinedTypes);
        }

        #endregion

        #region Task<T?> Nullable Unwrapping Tests

        [Theory]
        [InlineData(nameof(TaskNullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(TaskNullableIntMethod), JsonSchema.Integer)]
        [InlineData(nameof(TaskNullableBoolMethod), JsonSchema.Boolean)]
        public void GetReturnSchema_TaskNullablePrimitive_UnwrapsWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_TaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(TaskNullablePersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(TaskNullableAddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(TaskNullableCompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_TaskNullableComplexType_UnwrapsWithoutRequired(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: false, expectedDefinedTypes);
        }

        #endregion

        #region Nullable Task<T?> Unwrapping Tests

        [Theory]
        [InlineData(nameof(NullableTaskNullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(NullableTaskNullableIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_NullableTaskNullablePrimitive_UnwrapsWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableTaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(NullableTaskNullablePersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(NullableTaskNullableAddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(NullableTaskNullableCompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_NullableTaskNullableComplexType_UnwrapsWithoutRequired(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: false, expectedDefinedTypes);
        }

        #endregion

        #region ValueTask<T> Unwrapping Tests

        [Theory]
        [InlineData(nameof(ValueTaskStringMethod), JsonSchema.String)]
        [InlineData(nameof(ValueTaskIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_ValueTaskPrimitive_UnwrapsCorrectly(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskCustomType_UnwrapsToCustomTypeSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(ValueTaskPersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(ValueTaskAddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(ValueTaskCompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_ValueTaskComplexType_UnwrapsCorrectly(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: true, expectedDefinedTypes);
        }

        #endregion

        #region ValueTask<T?> Nullable Unwrapping Tests

        [Theory]
        [InlineData(nameof(ValueTaskNullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(ValueTaskNullableIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_ValueTaskNullablePrimitive_UnwrapsWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(ValueTaskNullablePersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(ValueTaskNullableAddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(ValueTaskNullableCompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_ValueTaskNullableComplexType_UnwrapsWithoutRequired(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: false, expectedDefinedTypes);
        }

        #endregion

        // NOTE: Nullable ValueTask<T?> tests removed because ValueTask is a struct and cannot be made nullable
        // in the reference type sense (ValueTask<T?>? is not valid)

        #region Custom Type Tests

        [Fact]
        public void GetReturnSchema_CustomType_ReturnsValidObjectSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(CustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_ComplexType_ReturnsValidNestedSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(ComplexTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "StringProperty", "IntProperty", "NestedObject", "StringArray" }, shouldBeRequired: true);
            AssertResultDefines(schema!, typeof(ComplexReturnType), typeof(CustomReturnType));
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(PersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(AddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(CompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_ComplexType_ReturnsValidObjectSchema(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: true, expectedDefinedTypes);
        }

        #endregion

        #region Nullable Custom Type Tests

        [Fact]
        public void GetReturnSchema_NullableCustomType_ReturnsValidObjectSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        [Fact]
        public void GetReturnSchema_NullableComplexType_ReturnsValidNestedSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableComplexTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "StringProperty", "IntProperty", "NestedObject", "StringArray" }, shouldBeRequired: false);
            AssertResultDefines(schema!, typeof(ComplexReturnType), typeof(CustomReturnType));
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(nameof(NullablePersonMethod), new[] { typeof(Person), typeof(Address) })]
        [InlineData(nameof(NullableAddressMethod), new[] { typeof(Address) })]
        [InlineData(nameof(NullableCompanyMethod), new[] { typeof(Company), typeof(Address), typeof(Person) })]
        public void GetReturnSchema_NullableComplexType_ReturnsValidObjectSchemaWithoutRequired(string methodName, Type[] expectedDefinedTypes)
        {
            TestComplexObjectSchema(methodName, shouldBeRequired: false, expectedDefinedTypes);
        }

        #endregion

        #region Collection Tests

        [Theory]
        [InlineData(nameof(StringArrayMethod), JsonSchema.String)]
        [InlineData(nameof(ListIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_CollectionTypes_ReturnsArraySchema(string methodName, string expectedItemType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertArrayReturnSchema(schema!, expectedItemType, shouldBeRequired: true);
            AssertAllRefsDefined(schema!);
        }

        #endregion

        #region Nullable Collection Tests

        [Fact]
        public void GetReturnSchema_NullableStringArray_ReturnsArraySchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableStringArrayMethod));
            AssertArrayReturnSchema(schema!, JsonSchema.String, shouldBeRequired: false);
            AssertAllRefsDefined(schema!);
        }

        #endregion

        #region List<ComplexReturnType> Tests

        [Theory]
        [InlineData(nameof(ListComplexTypeMethod), true, false)]
        [InlineData(nameof(ListNullableComplexTypeMethod), true, true)]
        [InlineData(nameof(NullableListNullableComplexTypeMethod), false, true)]
        [InlineData(nameof(TaskListComplexTypeMethod), true, false)]
        [InlineData(nameof(TaskNullableListComplexTypeMethod), false, false)]
        [InlineData(nameof(NullableTaskNullableListComplexTypeMethod), false, false)]
        [InlineData(nameof(TaskListNullableComplexTypeMethod), true, true)]
        [InlineData(nameof(TaskNullableListNullableComplexTypeMethod), false, true)]
        [InlineData(nameof(NullableTaskNullableListNullableComplexTypeMethod), false, true)]
        [InlineData(nameof(NullableListComplexTypeMethod), false, false)]
        [InlineData(nameof(NullableTaskListNullableComplexTypeMethod), false, true)]
        [InlineData(nameof(NullableTaskListComplexTypeMethod), false, false)]
        public void GetReturnSchema_ListComplexType_ReturnsCorrectSchema(string methodName, bool shouldBeRequired, bool itemsAreNullable)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired, itemsAreNullable);
            AssertAllRefsDefined(schema!);
        }

        #endregion

        #region JustRef Parameter Tests

        [Fact]
        public void GetReturnSchema_CustomType_WithJustRef_ReturnsRefSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(CustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo, justRef: true);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to CustomReturnType
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref));
            Assert.Contains("CustomReturnType", resultSchema[JsonSchema.Ref]?.ToString());

            // $defs should exist with the full type definition
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            AssertAllRefsDefined(schema);
        }

        [Fact]
        public void GetReturnSchema_PrimitiveType_WithJustRef_ReturnsFullSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(StringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo, justRef: true);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // Primitive types should be inlined even with justRef=true
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.Equal(JsonSchema.String, resultSchema[JsonSchema.Type]?.ToString());
            Assert.False(resultSchema.ContainsKey(JsonSchema.Ref));
            AssertAllRefsDefined(schema);
        }

        [Theory]
        [InlineData(nameof(PersonMethod), "Person")]
        [InlineData(nameof(AddressMethod), "Address")]
        [InlineData(nameof(CompanyMethod), "Company")]
        public void GetReturnSchema_OuterAssemblyType_WithJustRef_ReturnsRefSchema(string methodName, string expectedTypeName)
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo, justRef: true);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to the OuterAssembly type
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref));
            Assert.Contains(expectedTypeName, resultSchema[JsonSchema.Ref]?.ToString());

            // $defs should exist with the full type definition
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            AssertAllRefsDefined(schema);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void GetReturnSchema_NullMethodInfo_ThrowsArgumentNullException()
        {
            // Arrange
            var reflector = new Reflector();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => reflector.GetReturnSchema(null!));
        }

        #endregion

        #region WrapperClass<T> Echo Method Tests

        /// <summary>
        /// Helper to get return schema for a WrapperClass method
        /// </summary>
        private JsonNode? GetWrapperMethodReturnSchema(Type wrapperType, string methodName)
        {
            var reflector = new Reflector();
            var methodInfo = wrapperType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
            var schema = reflector.GetReturnSchema(methodInfo);

            _output.WriteLine($"Return schema for {wrapperType.GetTypeShortName()}.{methodName}:");
            _output.WriteLine(schema?.ToString() ?? "null");
            _output.WriteLine("");

            return schema;
        }

        [Theory]
        [InlineData(typeof(string), EchoMethodName, JsonSchema.String, true)] // string with Echo (T) is non-nullable due to NullableContextAttribute(1)
        [InlineData(typeof(int), EchoMethodName, JsonSchema.Integer, true)] // int is value type, T is non-nullable
        [InlineData(typeof(bool), EchoMethodName, JsonSchema.Boolean, true)] // bool is value type, T is non-nullable
        [InlineData(typeof(double), EchoMethodName, JsonSchema.Number, true)] // double is value type, T is non-nullable
#if NET8_0_OR_GREATER
        [InlineData(typeof(string), EchoNullableMethodName, JsonSchema.String, false)] // T? with reference type is nullable
        [InlineData(typeof(int), EchoNullableMethodName, JsonSchema.Integer, false)] // T? with value type is also nullable (int? becomes int, but T? context is nullable)
        [InlineData(typeof(bool), EchoNullableMethodName, JsonSchema.Boolean, false)] // T? with value type is also nullable (bool? becomes bool, but T? context is nullable)
        [InlineData(typeof(double), EchoNullableMethodName, JsonSchema.Number, false)] // T? with value type is also nullable (double? becomes double, but T? context is nullable)
#else
        [InlineData(typeof(string), EchoNullableMethodName, JsonSchema.String, true)] // T? with reference type is non-nullable due to lack of nullable context
        [InlineData(typeof(int), EchoNullableMethodName, JsonSchema.Integer, true)] // T? with value type is non-nullable due to lack of nullable context
        [InlineData(typeof(bool), EchoNullableMethodName, JsonSchema.Boolean, true)] // T? with value type is non-nullable due to lack of nullable context
        [InlineData(typeof(double), EchoNullableMethodName, JsonSchema.Number, true)] // T? with value type is non-nullable due to lack of nullable context
#endif
        public void GetReturnSchema_WrapperEchoPrimitive_ReturnsCorrectSchema(Type genericType, string methodName, string expectedType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(typeof(CustomReturnType), EchoMethodName, true)]
        [InlineData(typeof(ComplexReturnType), EchoMethodName, true)]
#if NET8_0_OR_GREATER
        [InlineData(typeof(CustomReturnType), EchoNullableMethodName, false)]
        [InlineData(typeof(ComplexReturnType), EchoNullableMethodName, false)]
#else
        [InlineData(typeof(CustomReturnType), EchoNullableMethodName, true)]
        [InlineData(typeof(ComplexReturnType), EchoNullableMethodName, true)]
#endif
        public void GetReturnSchema_WrapperEchoCustomType_ReturnsCorrectSchema(Type genericType, string methodName, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, methodName);

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);
            AssertAllRefsDefined(schema);
        }

        [Theory]
        [InlineData(typeof(string[]), EchoMethodName, JsonSchema.String, true)]
        [InlineData(typeof(int[]), EchoMethodName, JsonSchema.Integer, true)]
#if NET8_0_OR_GREATER
        [InlineData(typeof(string[]), EchoNullableMethodName, JsonSchema.String, false)]
        [InlineData(typeof(int[]), EchoNullableMethodName, JsonSchema.Integer, false)]
#else
        [InlineData(typeof(string[]), EchoNullableMethodName, JsonSchema.String, true)]
        [InlineData(typeof(int[]), EchoNullableMethodName, JsonSchema.Integer, true)]
#endif
        public void GetReturnSchema_WrapperEchoArray_ReturnsCorrectSchema(Type genericType, string methodName, string expectedItemType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, methodName);
            AssertArrayReturnSchema(schema!, expectedItemType, shouldBeRequired);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(EchoMethodName, true, false)] // WrapperClass<T>.Echo has NullableContextAttribute(1), meaning T is non-nullable
#if NET8_0_OR_GREATER
        [InlineData(EchoNullableMethodName, false, false)]
#endif
        public void GetReturnSchema_WrapperEchoListComplex_ReturnsCorrectSchema(string methodName, bool shouldBeRequired, bool itemsAreNullable)
        {
            var wrapperType = typeof(WrapperClass<List<ComplexReturnType>>);
            var schema = GetWrapperMethodReturnSchema(wrapperType, methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired, itemsAreNullable);
            AssertAllRefsDefined(schema!);
        }

        [Theory]
        [InlineData(typeof(Person), EchoMethodName, true, new[] { typeof(Person), typeof(Address) })]
        [InlineData(typeof(Address), EchoMethodName, true, new[] { typeof(Address) })]
        [InlineData(typeof(Company), EchoMethodName, true, new[] { typeof(Company), typeof(Address), typeof(Person) })]
#if NET8_0_OR_GREATER
        [InlineData(typeof(Person), EchoNullableMethodName, false, new[] { typeof(Person), typeof(Address) })]
        [InlineData(typeof(Address), EchoNullableMethodName, false, new[] { typeof(Address) })]
        [InlineData(typeof(Company), EchoNullableMethodName, false, new[] { typeof(Company), typeof(Address), typeof(Person) })]
#else
        [InlineData(typeof(Person), EchoNullableMethodName, true, new[] { typeof(Person), typeof(Address) })]
        [InlineData(typeof(Address), EchoNullableMethodName, true, new[] { typeof(Address) })]
        [InlineData(typeof(Company), EchoNullableMethodName, true, new[] { typeof(Company), typeof(Address), typeof(Person) })]
#endif
        public void GetReturnSchema_WrapperEchoComplexType_ReturnsCorrectSchema(Type genericType, string methodName, bool shouldBeRequired, Type[] expectedDefinedTypes)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            TestWrapperComplexObjectSchema(wrapperType, methodName, shouldBeRequired, expectedDefinedTypes);
        }

        #endregion
    }
}

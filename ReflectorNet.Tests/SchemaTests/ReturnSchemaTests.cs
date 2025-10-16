using System;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;
using OuterPerson = com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Person;
using OuterAddress = com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Address;
using OuterCompany = com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Company;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Tests for GetReturnSchema method that generates JSON schemas for method return types
    /// </summary>
    public class ReturnSchemaTests : SchemaTestBase
    {
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
        private Task<OuterPerson> TaskOuterPersonMethod() => Task.FromResult(new OuterPerson());
        private Task<OuterAddress> TaskOuterAddressMethod() => Task.FromResult(new OuterAddress());
        private Task<OuterCompany> TaskOuterCompanyMethod() => Task.FromResult(new OuterCompany());

        // ValueTask<T> return types (should be unwrapped)
        private ValueTask<string> ValueTaskStringMethod() => ValueTask.FromResult("test");
        private ValueTask<int> ValueTaskIntMethod() => ValueTask.FromResult(42);
        private ValueTask<CustomReturnType> ValueTaskCustomTypeMethod() => ValueTask.FromResult(new CustomReturnType());
        private ValueTask<OuterPerson> ValueTaskOuterPersonMethod() => ValueTask.FromResult(new OuterPerson());
        private ValueTask<OuterAddress> ValueTaskOuterAddressMethod() => ValueTask.FromResult(new OuterAddress());
        private ValueTask<OuterCompany> ValueTaskOuterCompanyMethod() => ValueTask.FromResult(new OuterCompany());

        // Custom types
        private CustomReturnType CustomTypeMethod() => new CustomReturnType();
        private ComplexReturnType ComplexTypeMethod() => new ComplexReturnType();
        private OuterPerson OuterPersonMethod() => new OuterPerson();
        private OuterAddress OuterAddressMethod() => new OuterAddress();
        private OuterCompany OuterCompanyMethod() => new OuterCompany();

        // Collections
        private string[] StringArrayMethod() => new[] { "test" };
        private System.Collections.Generic.List<int> ListIntMethod() => new System.Collections.Generic.List<int>();
        private System.Collections.Generic.Dictionary<string, int> DictionaryMethod() => new System.Collections.Generic.Dictionary<string, int>();

        // List<ComplexReturnType> variations
        private System.Collections.Generic.List<ComplexReturnType> ListComplexTypeMethod() => new System.Collections.Generic.List<ComplexReturnType>();
        private System.Collections.Generic.List<ComplexReturnType?> ListNullableComplexTypeMethod() => new System.Collections.Generic.List<ComplexReturnType?>();
        private System.Collections.Generic.List<ComplexReturnType>? NullableListComplexTypeMethod() => new System.Collections.Generic.List<ComplexReturnType>();
        private System.Collections.Generic.List<ComplexReturnType?>? NullableListNullableComplexTypeMethod() => new System.Collections.Generic.List<ComplexReturnType?>();
        private Task<System.Collections.Generic.List<ComplexReturnType>> TaskListComplexTypeMethod() => Task.FromResult(new System.Collections.Generic.List<ComplexReturnType>());
        private Task<System.Collections.Generic.List<ComplexReturnType>>? NullableTaskListComplexTypeMethod() => Task.FromResult(new System.Collections.Generic.List<ComplexReturnType>());
        private Task<System.Collections.Generic.List<ComplexReturnType>?> TaskNullableListComplexTypeMethod() => Task.FromResult<System.Collections.Generic.List<ComplexReturnType>?>(new System.Collections.Generic.List<ComplexReturnType>());
        private Task<System.Collections.Generic.List<ComplexReturnType?>> TaskListNullableComplexTypeMethod() => Task.FromResult(new System.Collections.Generic.List<ComplexReturnType?>());
        private Task<System.Collections.Generic.List<ComplexReturnType>?>? NullableTaskNullableListComplexTypeMethod() => Task.FromResult<System.Collections.Generic.List<ComplexReturnType>?>(new System.Collections.Generic.List<ComplexReturnType>());
        private Task<System.Collections.Generic.List<ComplexReturnType?>?> TaskNullableListNullableComplexTypeMethod() => Task.FromResult<System.Collections.Generic.List<ComplexReturnType?>?>(new System.Collections.Generic.List<ComplexReturnType?>());
        private Task<System.Collections.Generic.List<ComplexReturnType?>>? NullableTaskListNullableComplexTypeMethod() => Task.FromResult(new System.Collections.Generic.List<ComplexReturnType?>());
        private Task<System.Collections.Generic.List<ComplexReturnType?>?>? NullableTaskNullableListNullableComplexTypeMethod() => Task.FromResult<System.Collections.Generic.List<ComplexReturnType?>?>(new System.Collections.Generic.List<ComplexReturnType?>());

        // Nullable return types
        private string? NullableStringMethod() => "test";
        private int? NullableIntMethod() => 42;
        private bool? NullableBoolMethod() => true;
        private double? NullableDoubleMethod() => 3.14;
        private DateTime? NullableDateTimeMethod() => DateTime.Now;
        private CustomReturnType? NullableCustomTypeMethod() => new CustomReturnType();
        private ComplexReturnType? NullableComplexTypeMethod() => new ComplexReturnType();
        private OuterPerson? NullableOuterPersonMethod() => new OuterPerson();
        private OuterAddress? NullableOuterAddressMethod() => new OuterAddress();
        private OuterCompany? NullableOuterCompanyMethod() => new OuterCompany();
        private string[]? NullableStringArrayMethod() => new[] { "test" };

        // Task<T?> return types (nullable wrapped in Task)
        private Task<string?> TaskNullableStringMethod() => Task.FromResult<string?>("test");
        private Task<int?> TaskNullableIntMethod() => Task.FromResult<int?>(42);
        private Task<bool?> TaskNullableBoolMethod() => Task.FromResult<bool?>(true);
        private Task<CustomReturnType?> TaskNullableCustomTypeMethod() => Task.FromResult<CustomReturnType?>(new CustomReturnType());
        private Task<OuterPerson?> TaskNullableOuterPersonMethod() => Task.FromResult<OuterPerson?>(new OuterPerson());
        private Task<OuterAddress?> TaskNullableOuterAddressMethod() => Task.FromResult<OuterAddress?>(new OuterAddress());
        private Task<OuterCompany?> TaskNullableOuterCompanyMethod() => Task.FromResult<OuterCompany?>(new OuterCompany());

        // ValueTask<T?> return types (nullable wrapped in ValueTask)
        private ValueTask<string?> ValueTaskNullableStringMethod() => ValueTask.FromResult<string?>("test");
        private ValueTask<int?> ValueTaskNullableIntMethod() => ValueTask.FromResult<int?>(42);
        private ValueTask<CustomReturnType?> ValueTaskNullableCustomTypeMethod() => ValueTask.FromResult<CustomReturnType?>(new CustomReturnType());
        private ValueTask<OuterPerson?> ValueTaskNullableOuterPersonMethod() => ValueTask.FromResult<OuterPerson?>(new OuterPerson());
        private ValueTask<OuterAddress?> ValueTaskNullableOuterAddressMethod() => ValueTask.FromResult<OuterAddress?>(new OuterAddress());
        private ValueTask<OuterCompany?> ValueTaskNullableOuterCompanyMethod() => ValueTask.FromResult<OuterCompany?>(new OuterCompany());

        // Task<T?>? return types (nullable T wrapped in nullable Task)
        private Task<string?>? NullableTaskNullableStringMethod() => Task.FromResult<string?>("test");
        private Task<int?>? NullableTaskNullableIntMethod() => Task.FromResult<int?>(42);
        private Task<CustomReturnType?>? NullableTaskNullableCustomTypeMethod() => Task.FromResult<CustomReturnType?>(new CustomReturnType());
        private Task<OuterPerson?>? NullableTaskNullableOuterPersonMethod() => Task.FromResult<OuterPerson?>(new OuterPerson());
        private Task<OuterAddress?>? NullableTaskNullableOuterAddressMethod() => Task.FromResult<OuterAddress?>(new OuterAddress());
        private Task<OuterCompany?>? NullableTaskNullableOuterCompanyMethod() => Task.FromResult<OuterCompany?>(new OuterCompany());

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
        }

        [Fact]
        public void GetReturnSchema_TaskCustomType_UnwrapsToCustomTypeSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
        }

        [Fact]
        public void GetReturnSchema_TaskOuterPerson_UnwrapsCorrectly()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskOuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_TaskOuterAddress_UnwrapsCorrectly()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskOuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_TaskOuterCompany_UnwrapsCorrectly()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskOuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
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
        }

        [Fact]
        public void GetReturnSchema_TaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_TaskNullableOuterPerson_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskNullableOuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_TaskNullableOuterAddress_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskNullableOuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_TaskNullableOuterCompany_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskNullableOuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
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
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableTaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableOuterPerson_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableTaskNullableOuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableOuterAddress_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableTaskNullableOuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableOuterCompany_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableTaskNullableOuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
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
        }

        [Fact]
        public void GetReturnSchema_ValueTaskCustomType_UnwrapsToCustomTypeSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskOuterPerson_UnwrapsCorrectly()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskOuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_ValueTaskOuterAddress_UnwrapsCorrectly()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskOuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_ValueTaskOuterCompany_UnwrapsCorrectly()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskOuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
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
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableOuterPerson_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskNullableOuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableOuterAddress_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskNullableOuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableOuterCompany_UnwrapsWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskNullableOuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
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
        }

        [Fact]
        public void GetReturnSchema_ComplexType_ReturnsValidNestedSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(ComplexTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "StringProperty", "IntProperty", "NestedObject", "StringArray" }, shouldBeRequired: true);
            AssertResultDefines(schema!, typeof(ComplexReturnType), typeof(CustomReturnType));
        }

        [Fact]
        public void GetReturnSchema_OuterPerson_ReturnsValidObjectSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(OuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_OuterAddress_ReturnsValidObjectSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(OuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_OuterCompany_ReturnsValidObjectSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(OuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
        }

        #endregion

        #region Nullable Custom Type Tests

        [Fact]
        public void GetReturnSchema_NullableCustomType_ReturnsValidObjectSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_NullableComplexType_ReturnsValidNestedSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableComplexTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "StringProperty", "IntProperty", "NestedObject", "StringArray" }, shouldBeRequired: false);
            AssertResultDefines(schema!, typeof(ComplexReturnType), typeof(CustomReturnType));
        }

        [Fact]
        public void GetReturnSchema_NullableOuterPerson_ReturnsValidObjectSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableOuterPersonMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_NullableOuterAddress_ReturnsValidObjectSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableOuterAddressMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_NullableOuterCompany_ReturnsValidObjectSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableOuterCompanyMethod));
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
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
        }

        #endregion

        #region Nullable Collection Tests

        [Fact]
        public void GetReturnSchema_NullableStringArray_ReturnsArraySchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableStringArrayMethod));
            AssertArrayReturnSchema(schema!, JsonSchema.String, shouldBeRequired: false);
        }

        #endregion

        #region List<ComplexReturnType> Tests

        [Theory]
        [InlineData(nameof(ListComplexTypeMethod), true)]
        [InlineData(nameof(NullableListComplexTypeMethod), false)]
        public void GetReturnSchema_ListComplexType_ReturnsArraySchemaWithComplexItems(string methodName, bool shouldBeRequired)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired);
        }

        [Theory]
        [InlineData(nameof(ListNullableComplexTypeMethod), true)]
        [InlineData(nameof(NullableListNullableComplexTypeMethod), false)]
        public void GetReturnSchema_ListNullableComplexType_ReturnsArraySchemaWithNullableComplexItems(string methodName, bool shouldBeRequired)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired, itemsAreNullable: true);
        }

        [Theory]
        [InlineData(nameof(TaskListComplexTypeMethod), true)]
        [InlineData(nameof(NullableTaskListComplexTypeMethod), false)]
        public void GetReturnSchema_TaskListComplexType_UnwrapsToArraySchemaWithComplexItems(string methodName, bool shouldBeRequired)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired);
        }

        [Theory]
        [InlineData(nameof(TaskNullableListComplexTypeMethod), false)]
        [InlineData(nameof(NullableTaskNullableListComplexTypeMethod), false)]
        public void GetReturnSchema_TaskNullableListComplexType_UnwrapsToArraySchemaWithoutRequired(string methodName, bool shouldBeRequired)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired);
        }

        [Theory]
        [InlineData(nameof(TaskListNullableComplexTypeMethod), true)]
        [InlineData(nameof(NullableTaskListNullableComplexTypeMethod), false)]
        public void GetReturnSchema_TaskListNullableComplexType_UnwrapsToArraySchemaWithNullableItems(string methodName, bool shouldBeRequired)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired, itemsAreNullable: true);
        }

        [Theory]
        [InlineData(nameof(TaskNullableListNullableComplexTypeMethod), false)]
        [InlineData(nameof(NullableTaskNullableListNullableComplexTypeMethod), false)]
        public void GetReturnSchema_TaskNullableListNullableComplexType_UnwrapsToArraySchemaWithNullableItemsWithoutRequired(string methodName, bool shouldBeRequired)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertComplexListReturnSchema(schema!, shouldBeRequired, itemsAreNullable: true);
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
        }

        [Theory]
        [InlineData(nameof(OuterPersonMethod), "Person")]
        [InlineData(nameof(OuterAddressMethod), "Address")]
        [InlineData(nameof(OuterCompanyMethod), "Company")]
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
        [InlineData(typeof(string), nameof(WrapperClass<string>.Echo), JsonSchema.String, false)] // string is reference type, T is nullable
        [InlineData(typeof(int), nameof(WrapperClass<int>.Echo), JsonSchema.Integer, true)] // int is value type, T is non-nullable
        [InlineData(typeof(bool), nameof(WrapperClass<bool>.Echo), JsonSchema.Boolean, true)] // bool is value type, T is non-nullable
        [InlineData(typeof(double), nameof(WrapperClass<double>.Echo), JsonSchema.Number, true)] // double is value type, T is non-nullable
        [InlineData(typeof(string), nameof(WrapperClass<string>.EchoNullable), JsonSchema.String, false)] // string? is nullable reference
        [InlineData(typeof(int), nameof(WrapperClass<int>.EchoNullable), JsonSchema.Integer, true)] // int? (Nullable<int>) is itself non-nullable struct
        [InlineData(typeof(bool), nameof(WrapperClass<bool>.EchoNullable), JsonSchema.Boolean, true)] // bool? (Nullable<bool>) is itself non-nullable struct
        [InlineData(typeof(double), nameof(WrapperClass<double>.EchoNullable), JsonSchema.Number, true)] // double? (Nullable<double>) is itself non-nullable struct
        public void GetReturnSchema_WrapperEchoPrimitive_ReturnsCorrectSchema(Type genericType, string methodName, string expectedType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired);
        }

        [Theory]
        [InlineData(typeof(CustomReturnType), true)]
        [InlineData(typeof(ComplexReturnType), true)]
        public void GetReturnSchema_WrapperEchoCustomType_ReturnsCorrectSchema(Type genericType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.Echo));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);
        }

        [Theory]
        [InlineData(typeof(CustomReturnType), false)]
        [InlineData(typeof(ComplexReturnType), false)]
        public void GetReturnSchema_WrapperEchoNullableCustomType_ReturnsCorrectSchema(Type genericType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.EchoNullable));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());

            if (shouldBeRequired)
                AssertResultRequired(schema);
            else
                AssertResultNotRequired(schema);
        }

        [Theory]
        [InlineData(typeof(string[]), JsonSchema.String, true)]
        [InlineData(typeof(int[]), JsonSchema.Integer, true)]
        public void GetReturnSchema_WrapperEchoArray_ReturnsCorrectSchema(Type genericType, string expectedItemType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.Echo));
            AssertArrayReturnSchema(schema!, expectedItemType, shouldBeRequired);
        }

        [Theory]
        [InlineData(typeof(string[]), JsonSchema.String, false)]
        [InlineData(typeof(int[]), JsonSchema.Integer, false)]
        public void GetReturnSchema_WrapperEchoNullableArray_ReturnsCorrectSchema(Type genericType, string expectedItemType, bool shouldBeRequired)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericType);
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.EchoNullable));
            AssertArrayReturnSchema(schema!, expectedItemType, shouldBeRequired);
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoListComplex_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<System.Collections.Generic.List<ComplexReturnType>>);
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.Echo));
            AssertComplexListReturnSchema(schema!, shouldBeRequired: true);
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoNullableListComplex_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<System.Collections.Generic.List<ComplexReturnType>>);
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.EchoNullable));
            AssertComplexListReturnSchema(schema!, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoOuterPerson_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(typeof(OuterPerson));
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.Echo));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoOuterAddress_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(typeof(OuterAddress));
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.Echo));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoOuterCompany_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(typeof(OuterCompany));
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.Echo));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoNullableOuterPerson_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(typeof(OuterPerson));
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.EchoNullable));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterPerson), typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoNullableOuterAddress_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(typeof(OuterAddress));
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.EchoNullable));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterAddress));
        }

        [Fact]
        public void GetReturnSchema_WrapperEchoNullableOuterCompany_ReturnsCorrectSchema()
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(typeof(OuterCompany));
            var schema = GetWrapperMethodReturnSchema(wrapperType, nameof(WrapperClass<int>.EchoNullable));

            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            AssertResultNotRequired(schema);
            AssertResultDefines(schema, typeof(OuterCompany), typeof(OuterAddress), typeof(OuterPerson));
        }

        #endregion
    }
}

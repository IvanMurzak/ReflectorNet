/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class MethodUtilsTests
    {
        // Test class with various method signatures for testing
        private class TestMethods
        {
            // Value types
            public int ReturnInt() => 0;
            public bool ReturnBool() => false;
            public double ReturnDouble() => 0.0;
            public DateTime ReturnDateTime() => DateTime.Now;

            // Nullable value types
            public int? ReturnNullableInt() => null;
            public bool? ReturnNullableBool() => null;
            public double? ReturnNullableDouble() => null;
            public DateTime? ReturnNullableDateTime() => null;

            // Reference types
            public string ReturnString() => "";
            public string? ReturnNullableString() => null;
            public Company ReturnCompany() => new Company();
            public Company? ReturnNullableCompany() => null;
            public List<int> ReturnListInt() => new List<int>();
            public List<int>? ReturnNullableListInt() => null;
            public List<Company> ReturnListCompany() => new List<Company>();
            public List<Company>? ReturnNullableListCompany() => null;

            // Arrays
            public int[] ReturnIntArray() => Array.Empty<int>();
            public int[]? ReturnNullableIntArray() => null;
            public string[] ReturnStringArray() => Array.Empty<string>();
            public string[]? ReturnNullableStringArray() => null;
            public Company[] ReturnCompanyArray() => Array.Empty<Company>();
            public Company[]? ReturnNullableCompanyArray() => null;

            // Async methods with value types
            public Task<int> ReturnTaskInt() => Task.FromResult(0);
            public Task<int?> ReturnTaskNullableInt() => Task.FromResult<int?>(null);
            public Task<int>? ReturnNullableTaskInt() => null;
            public Task<int?>? ReturnNullableTaskNullableInt() => null;

            // Async methods with reference types
            public Task<string> ReturnTaskString() => Task.FromResult("");
            public Task<string?> ReturnTaskNullableString() => Task.FromResult<string?>(null);
            public Task<string>? ReturnNullableTaskString() => null;
            public Task<string?>? ReturnNullableTaskNullableString() => null;
            public Task<Company> ReturnTaskCompany() => Task.FromResult(new Company());
            public Task<Company?> ReturnTaskNullableCompany() => Task.FromResult<Company?>(null);
            public Task<Company>? ReturnNullableTaskCompany() => null;
            public Task<Company?>? ReturnNullableTaskNullableCompany() => null;

            // Async methods with collections
            public Task<List<int>> ReturnTaskListInt() => Task.FromResult(new List<int>());
            public Task<List<int>?> ReturnTaskNullableListInt() => Task.FromResult<List<int>?>(null);
            public Task<List<int>>? ReturnNullableTaskListInt() => null;
            public Task<List<Company>> ReturnTaskListCompany() => Task.FromResult(new List<Company>());
            public Task<List<Company>?> ReturnTaskNullableListCompany() => Task.FromResult<List<Company>?>(null);
            public Task<List<Company>>? ReturnNullableTaskListCompany() => null;

            // ValueTask variants
            public ValueTask<int> ReturnValueTaskInt() => ValueTask.FromResult(0);
            public ValueTask<int?> ReturnValueTaskNullableInt() => ValueTask.FromResult<int?>(null);
            public ValueTask<string> ReturnValueTaskString() => ValueTask.FromResult("");
            public ValueTask<string?> ReturnValueTaskNullableString() => ValueTask.FromResult<string?>(null);
        }

        private MethodInfo GetMethod(string methodName)
        {
            var method = typeof(TestMethods).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            return method!;
        }

        #region Value Types Tests

        [Fact]
        public void IsReturnTypeNullable_ValueType_Int_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ValueType_Bool_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnBool));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ValueType_Double_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnDouble));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ValueType_DateTime_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnDateTime));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        #endregion

        #region Nullable Value Types Tests

        [Fact]
        public void IsReturnTypeNullable_NullableValueType_Int_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableValueType_Bool_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableBool));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableValueType_Double_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableDouble));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableValueType_DateTime_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableDateTime));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region Reference Types Tests

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_String_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_NullableString_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_Company_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_NullableCompany_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_ListInt_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnListInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_NullableListInt_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableListInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_ListCompany_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnListCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ReferenceType_NullableListCompany_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableListCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region Array Tests

        [Fact]
        public void IsReturnTypeNullable_Array_IntArray_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnIntArray));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Array_NullableIntArray_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableIntArray));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Array_StringArray_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnStringArray));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Array_NullableStringArray_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableStringArray));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Array_CompanyArray_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnCompanyArray));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Array_NullableCompanyArray_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableCompanyArray));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region Task (Async) Tests with Value Types

        [Fact]
        public void IsReturnTypeNullable_Task_Int_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_NullableInt_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskNullableInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_Int_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_NullableInt_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskNullableInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region Task (Async) Tests with Reference Types

        [Fact]
        public void IsReturnTypeNullable_Task_String_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_NullableString_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskNullableString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_String_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_NullableString_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskNullableString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_Company_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_NullableCompany_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskNullableCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_Company_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_NullableCompany_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskNullableCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region Task (Async) Tests with Collections

        [Fact]
        public void IsReturnTypeNullable_Task_ListInt_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskListInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_NullableListInt_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskNullableListInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_ListInt_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskListInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_ListCompany_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskListCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_Task_NullableListCompany_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnTaskNullableListCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_NullableTask_ListCompany_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnNullableTaskListCompany));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region ValueTask Tests

        [Fact]
        public void IsReturnTypeNullable_ValueTask_Int_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnValueTaskInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ValueTask_NullableInt_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnValueTaskNullableInt));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ValueTask_String_ReturnsFalse()
        {
            var method = GetMethod(nameof(TestMethods.ReturnValueTaskString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.False(result);
        }

        [Fact]
        public void IsReturnTypeNullable_ValueTask_NullableString_ReturnsTrue()
        {
            var method = GetMethod(nameof(TestMethods.ReturnValueTaskNullableString));
            var result = MethodUtils.IsReturnTypeNullable(method);
            Assert.True(result);
        }

        #endregion

        #region Generic Type Parameter Tests

        /// <summary>
        /// Helper method to test nullability of WrapperClass&lt;T&gt; methods
        /// </summary>
        private static void AssertWrapperMethodNullability(Type genericTypeArgument, string methodName, bool expectedNullable)
        {
            var wrapperType = typeof(WrapperClass<>).MakeGenericType(genericTypeArgument);
            var method = wrapperType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);

            var result = MethodUtils.IsReturnTypeNullable(method!);
            Assert.Equal(expectedNullable, result);
        }

        [Theory]
        [InlineData(typeof(int), nameof(WrapperClass<int>.Echo), false)] // WrapperClass<int>.Echo returns T where T=int (non-nullable)
        [InlineData(typeof(string), nameof(WrapperClass<string>.Echo), false)] // WrapperClass<string>.Echo returns T where T=string (non-nullable)
        [InlineData(typeof(Company), nameof(WrapperClass<Company>.Echo), false)] // WrapperClass<Company>.Echo returns T where T=Company (non-nullable)
        [InlineData(typeof(int), nameof(WrapperClass<int>.EchoNullable), true)] // WrapperClass<int>.EchoNullable returns T? where T=int (nullable)
        [InlineData(typeof(string), nameof(WrapperClass<string>.EchoNullable), true)] // WrapperClass<string>.EchoNullable returns T? where T=string (nullable)
        [InlineData(typeof(Company), nameof(WrapperClass<Company>.EchoNullable), true)] // WrapperClass<Company>.EchoNullable returns T? where T=Company (nullable)
        public void IsReturnTypeNullable_GenericTypeParameter_WithNonNullableTypes(Type genericTypeArgument, string methodName, bool expectedNullable)
        {
            AssertWrapperMethodNullability(genericTypeArgument, methodName, expectedNullable);
        }

        [Theory]
        [InlineData(typeof(int?), nameof(WrapperClass<int?>.Echo), true)] // WrapperClass<int?>.Echo returns T where T=int? (nullable value type)
        [InlineData(typeof(int?), nameof(WrapperClass<int?>.EchoNullable), true)] // WrapperClass<int?>.EchoNullable returns T? where T=int? (nullable)
        [InlineData(typeof(bool?), nameof(WrapperClass<bool?>.Echo), true)] // WrapperClass<bool?>.Echo returns T where T=bool? (nullable value type)
        [InlineData(typeof(bool?), nameof(WrapperClass<bool?>.EchoNullable), true)] // WrapperClass<bool?>.EchoNullable returns T? where T=bool? (nullable)
        [InlineData(typeof(double?), nameof(WrapperClass<double?>.Echo), true)] // WrapperClass<double?>.Echo returns T where T=double? (nullable value type)
        [InlineData(typeof(double?), nameof(WrapperClass<double?>.EchoNullable), true)] // WrapperClass<double?>.EchoNullable returns T? where T=double? (nullable)
        [InlineData(typeof(DateTime?), nameof(WrapperClass<DateTime?>.Echo), true)] // WrapperClass<DateTime?>.Echo returns T where T=DateTime? (nullable value type)
        [InlineData(typeof(DateTime?), nameof(WrapperClass<DateTime?>.EchoNullable), true)] // WrapperClass<DateTime?>.EchoNullable returns T? where T=DateTime? (nullable)
        public void IsReturnTypeNullable_GenericTypeParameter_WithNullableValueTypes(Type genericTypeArgument, string methodName, bool expectedNullable)
        {
            AssertWrapperMethodNullability(genericTypeArgument, methodName, expectedNullable);
        }

        [Theory]
        [InlineData(nameof(WrapperClass<List<Company>>.Echo), false)] // WrapperClass<List<Company>>.Echo returns T where T=List<Company> (non-nullable)
        [InlineData(nameof(WrapperClass<List<Company>>.EchoNullable), true)] // WrapperClass<List<Company>>.EchoNullable returns T? where T=List<Company> (nullable)
        public void IsReturnTypeNullable_GenericTypeParameter_WithComplexTypes(string methodName, bool expectedNullable)
        {
            AssertWrapperMethodNullability(typeof(List<Company>), methodName, expectedNullable);
        }

        #endregion

        #region Null Argument Tests

        [Fact]
        public void IsReturnTypeNullable_NullMethod_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => MethodUtils.IsReturnTypeNullable(null!));
        }

        #endregion
    }
}

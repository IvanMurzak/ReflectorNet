/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.TypeUtilsTests
{
    /// <summary>
    /// Tests for TypeUtils.GetType(string? assemblyName, string? typeName) method.
    /// Verifies that types are correctly resolved when filtering by assembly name prefix.
    /// </summary>
    public class GetTypeWithAssemblyTests : BaseTest
    {
        public GetTypeWithAssemblyTests(ITestOutputHelper output) : base(output) { }

        #region Constants - Assembly Prefixes

        private const string ReflectorNetPrefix = "ReflectorNet";
        private const string ReflectorNetTestsPrefix = "ReflectorNet.Tests";
        private const string ReflectorNetOuterPrefix = "ReflectorNet.Tests.OuterAssembly";
        private const string SystemPrefix = "System";
        private const string NonExistentPrefix = "NonExistent.Assembly.Prefix";

        #endregion

        #region Edge Case Tests

        [Fact]
        public void GetType_NullAssemblyName_DelegatesToStandardGetType()
        {
            // When assemblyName is null, should delegate to GetType(typeName)
            var type = TypeUtils.GetType(null, "System.Int32");
            Assert.Equal(typeof(int), type);

            var reflectorType = TypeUtils.GetType(null, "com.IvanMurzak.ReflectorNet.Model.SerializedMember");
            Assert.Equal(typeof(SerializedMember), reflectorType);
        }

        [Fact]
        public void GetType_EmptyAssemblyName_DelegatesToStandardGetType()
        {
            // When assemblyName is empty, should delegate to GetType(typeName)
            var type = TypeUtils.GetType("", "System.String");
            Assert.Equal(typeof(string), type);

            var reflectorType = TypeUtils.GetType("", "com.IvanMurzak.ReflectorNet.Model.MethodRef");
            Assert.Equal(typeof(MethodRef), reflectorType);
        }

        [Fact]
        public void GetType_NullTypeName_ReturnsNull()
        {
            Assert.Null(TypeUtils.GetType(ReflectorNetPrefix, null));
            Assert.Null(TypeUtils.GetType(SystemPrefix, null));
        }

        [Fact]
        public void GetType_EmptyTypeName_ReturnsNull()
        {
            Assert.Null(TypeUtils.GetType(ReflectorNetPrefix, ""));
            Assert.Null(TypeUtils.GetType(ReflectorNetPrefix, "   "));
        }

        [Fact]
        public void GetType_NonExistentAssemblyPrefix_ReturnsNull()
        {
            // Types should not be found when assembly prefix doesn't match any loaded assembly
            Assert.Null(TypeUtils.GetType(NonExistentPrefix, "System.Int32"));
            Assert.Null(TypeUtils.GetType(NonExistentPrefix, "com.IvanMurzak.ReflectorNet.Model.SerializedMember"));
        }

        [Fact]
        public void GetType_InvalidTypeName_ReturnsNull()
        {
            Assert.Null(TypeUtils.GetType(ReflectorNetPrefix, "NonExistent.Type.That.Does.Not.Exist"));
            Assert.Null(TypeUtils.GetType(SystemPrefix, "System.NonExistentType"));
        }

        #endregion

        #region ReflectorNet Assembly Tests

        [Fact]
        public void GetType_ReflectorNetTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.Reflector"] = typeof(Reflector),
                ["com.IvanMurzak.ReflectorNet.Model.SerializedMember"] = typeof(SerializedMember),
                ["com.IvanMurzak.ReflectorNet.Model.SerializedMemberList"] = typeof(SerializedMemberList),
                ["com.IvanMurzak.ReflectorNet.Model.MethodRef"] = typeof(MethodRef),
                ["com.IvanMurzak.ReflectorNet.Model.MethodData"] = typeof(MethodData),
            };

            ValidateGetTypeWithAssembly(ReflectorNetPrefix, testCases, "ReflectorNet Types with matching prefix");
        }

        [Fact]
        public void GetType_ReflectorNetTypes_WithNonMatchingPrefix()
        {
            // ReflectorNet types should NOT be found when using OuterAssembly prefix
            var typeName = "com.IvanMurzak.ReflectorNet.Model.SerializedMember";
            var result = TypeUtils.GetType(ReflectorNetOuterPrefix, typeName);

            _output.WriteLine($"GetType(\"{ReflectorNetOuterPrefix}\", \"{typeName}\") = {result?.FullName ?? "null"}");
            Assert.Null(result);
        }

        #endregion

        #region OuterAssembly Tests

        [Fact]
        public void GetType_OuterAssemblyTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass"] = typeof(OuterSimpleClass),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleStruct"] = typeof(OuterSimpleStruct),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSealedClass"] = typeof(OuterSealedClass),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Person"] = typeof(Person),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Address"] = typeof(Address),
            };

            ValidateGetTypeWithAssembly(ReflectorNetOuterPrefix, testCases, "OuterAssembly Types with matching prefix");
        }

        [Fact]
        public void GetType_OuterAssemblyTypes_WithBroaderPrefix()
        {
            // OuterAssembly types should be found with "ReflectorNet" prefix (broader)
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass"] = typeof(OuterSimpleClass),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Person"] = typeof(Person),
            };

            ValidateGetTypeWithAssembly(ReflectorNetPrefix, testCases, "OuterAssembly Types with broader prefix");
        }

        [Fact]
        public void GetType_OuterAssemblyNestedTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedClass"] = typeof(OuterContainer.NestedClass),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedStruct"] = typeof(OuterContainer.NestedStruct),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedContainer+DoubleNestedClass"] = typeof(OuterContainer.NestedContainer.DoubleNestedClass),
            };

            ValidateGetTypeWithAssembly(ReflectorNetOuterPrefix, testCases, "OuterAssembly Nested Types");
        }

        #endregion

        #region Generic Types Tests

        [Fact]
        public void GetType_GenericTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.Int32>"] = typeof(OuterGenericClass<int>),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.String>"] = typeof(OuterGenericClass<string>),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass2<System.Int32,System.String>"] = typeof(OuterGenericClass2<int, string>),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericStruct<System.Int32>"] = typeof(OuterGenericStruct<int>),
            };

            ValidateGetTypeWithAssembly(ReflectorNetOuterPrefix, testCases, "Generic Types with matching prefix");
        }

        [Fact]
        public void GetType_NestedGenericTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedGenericClass<System.Int32>"] = typeof(OuterContainer.NestedGenericClass<int>),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericContainer<System.Int32>+NestedInGeneric"] = typeof(OuterGenericContainer<int>.NestedInGeneric),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericContainer<System.Int32>+NestedGenericInGeneric<System.String>"] = typeof(OuterGenericContainer<int>.NestedGenericInGeneric<string>),
            };

            ValidateGetTypeWithAssembly(ReflectorNetOuterPrefix, testCases, "Nested Generic Types with matching prefix");
        }

        #endregion

        #region Array Types Tests

        [Fact]
        public void GetType_ArrayTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass[]"] = typeof(OuterSimpleClass[]),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleStruct[]"] = typeof(OuterSimpleStruct[]),
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass[][]"] = typeof(OuterSimpleClass[][]),
            };

            ValidateGetTypeWithAssembly(ReflectorNetOuterPrefix, testCases, "Array Types with matching prefix");
        }

        [Fact]
        public void GetType_GenericArrayTypes_WithBroadPrefix()
        {
            // Generic array types with System type arguments need a broader prefix
            // because System.Int32 is not in ReflectorNet assemblies
            // Using no prefix (delegates to standard GetType) for these cases
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.Int32>[]"] = typeof(OuterGenericClass<int>[]),
            };

            // Use broader prefix that allows System types to resolve
            ValidateGetTypeWithAssembly(ReflectorNetPrefix, testCases, "Generic Array Types with broader prefix");
        }

        [Fact]
        public void GetType_ArrayTypes_WithNonMatchingPrefix()
        {
            // Array of OuterAssembly type should NOT be found with Test assembly prefix only
            // Note: ReflectorNet.Tests does NOT start with "ReflectorNet.Tests.OuterAssembly"
            var typeName = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass[]";

            // This should return null because OuterSimpleClass is in OuterAssembly, not in Tests
            var result = TypeUtils.GetType("ReflectorNet.Tests.Model", typeName);

            _output.WriteLine($"GetType(\"ReflectorNet.Tests.Model\", \"{typeName}\") = {result?.FullName ?? "null"}");
            Assert.Null(result);
        }

        #endregion

        #region Cross-Assembly Generic Tests

        [Fact]
        public void GetType_CrossAssemblyGenericTypes()
        {
            // Generic type from OuterAssembly with type argument from Tests assembly
            // This should work with broader prefix that covers both assemblies
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<com.IvanMurzak.ReflectorNet.Tests.Model.Vector3>"] = typeof(OuterGenericClass<Vector3>),
            };

            ValidateGetTypeWithAssembly(ReflectorNetPrefix, testCases, "Cross-Assembly Generic Types");
        }

        #endregion

        #region This Assembly Tests

        [Fact]
        public void GetType_ThisAssemblyTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.Tests.Model.Vector3"] = typeof(Vector3),
                ["com.IvanMurzak.ReflectorNet.Tests.Model.SolarSystem"] = typeof(SolarSystem),
                ["com.IvanMurzak.ReflectorNet.Tests.Model.GameObjectRef"] = typeof(GameObjectRef),
                ["com.IvanMurzak.ReflectorNet.Tests.Model.SolarSystem+CelestialBody"] = typeof(SolarSystem.CelestialBody),
            };

            ValidateGetTypeWithAssembly(ReflectorNetTestsPrefix, testCases, "This Assembly Types with matching prefix");
        }

        [Fact]
        public void GetType_ThisAssemblyArrayTypes_WithMatchingPrefix()
        {
            var testCases = new Dictionary<string, Type>
            {
                ["com.IvanMurzak.ReflectorNet.Tests.Model.Vector3[]"] = typeof(Vector3[]),
                ["com.IvanMurzak.ReflectorNet.Tests.Model.GameObjectRef[]"] = typeof(GameObjectRef[]),
            };

            ValidateGetTypeWithAssembly(ReflectorNetTestsPrefix, testCases, "This Assembly Array Types");
        }

        #endregion

        #region Filtering Verification Tests

        [Fact]
        public void GetType_VerifyAssemblyFiltering_TypeNotFoundInWrongAssembly()
        {
            // Verify that types are actually filtered by assembly
            // Vector3 is in ReflectorNet.Tests, so it should NOT be found with OuterAssembly prefix
            var vectorTypeName = "com.IvanMurzak.ReflectorNet.Tests.Model.Vector3";

            var withCorrectPrefix = TypeUtils.GetType(ReflectorNetTestsPrefix, vectorTypeName);
            var withWrongPrefix = TypeUtils.GetType(ReflectorNetOuterPrefix, vectorTypeName);

            _output.WriteLine($"With correct prefix '{ReflectorNetTestsPrefix}': {withCorrectPrefix?.FullName ?? "null"}");
            _output.WriteLine($"With wrong prefix '{ReflectorNetOuterPrefix}': {withWrongPrefix?.FullName ?? "null"}");

            Assert.NotNull(withCorrectPrefix);
            Assert.Equal(typeof(Vector3), withCorrectPrefix);
            Assert.Null(withWrongPrefix);
        }

        [Fact]
        public void GetType_VerifyAssemblyFiltering_BothDirectionsFiltered()
        {
            // OuterSimpleClass is in OuterAssembly
            // Vector3 is in Tests
            // Each should only be found with the correct prefix

            var outerType = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass";
            var testType = "com.IvanMurzak.ReflectorNet.Tests.Model.Vector3";

            // OuterSimpleClass with OuterAssembly prefix - should work
            Assert.NotNull(TypeUtils.GetType(ReflectorNetOuterPrefix, outerType));
            // OuterSimpleClass with Tests prefix - should fail
            Assert.Null(TypeUtils.GetType(ReflectorNetTestsPrefix, outerType));

            // Vector3 with Tests prefix - should work
            Assert.NotNull(TypeUtils.GetType(ReflectorNetTestsPrefix, testType));
            // Vector3 with OuterAssembly prefix - should fail
            Assert.Null(TypeUtils.GetType(ReflectorNetOuterPrefix, testType));
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public void GetType_RoundTrip_GetTypeId_GetTypeWithAssembly()
        {
            // Verify round-trip: Type -> GetTypeId -> GetType(assemblyPrefix, typeId) -> same Type
            var testCases = new (Type type, string assemblyPrefix)[]
            {
                (typeof(SerializedMember), ReflectorNetPrefix),
                (typeof(MethodRef), ReflectorNetPrefix),
                (typeof(OuterSimpleClass), ReflectorNetOuterPrefix),
                (typeof(OuterGenericClass<int>), ReflectorNetOuterPrefix),
                (typeof(Vector3), ReflectorNetTestsPrefix),
                (typeof(SolarSystem), ReflectorNetTestsPrefix),
            };

            foreach (var (originalType, prefix) in testCases)
            {
                var typeId = TypeUtils.GetTypeId(originalType);
                var resolvedType = TypeUtils.GetType(prefix, typeId);

                _output.WriteLine($"  {originalType} -> \"{typeId}\" -> {resolvedType} (prefix: {prefix})");
                Assert.Equal(originalType, resolvedType);
            }
        }

        #endregion

        #region Cache Tests

        [Fact]
        public void GetType_CacheWorksCorrectly()
        {
            // Clear cache first
            TypeUtils.ClearAssemblyTypeCache();

            var typeName = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass";

            // First call - should resolve and cache
            var result1 = TypeUtils.GetType(ReflectorNetOuterPrefix, typeName);
            Assert.NotNull(result1);

            // Second call - should use cache
            var result2 = TypeUtils.GetType(ReflectorNetOuterPrefix, typeName);
            Assert.NotNull(result2);

            // Results should be the same
            Assert.Equal(result1, result2);

            _output.WriteLine($"First call: {result1?.FullName}");
            _output.WriteLine($"Second call (cached): {result2?.FullName}");
        }

        [Fact]
        public void GetType_DifferentPrefixesCachedSeparately()
        {
            // Clear cache first
            TypeUtils.ClearAssemblyTypeCache();

            var typeName = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass";

            // Call with matching prefix - should find
            var withMatching = TypeUtils.GetType(ReflectorNetOuterPrefix, typeName);
            Assert.NotNull(withMatching);

            // Call with non-matching prefix - should NOT find (and cache the null result)
            var withNonMatching = TypeUtils.GetType(ReflectorNetTestsPrefix, typeName);
            Assert.Null(withNonMatching);

            // Call with matching prefix again - should still find (from its own cache entry)
            var withMatchingAgain = TypeUtils.GetType(ReflectorNetOuterPrefix, typeName);
            Assert.NotNull(withMatchingAgain);

            _output.WriteLine($"With matching prefix: {withMatching?.FullName}");
            _output.WriteLine($"With non-matching prefix: {withNonMatching?.FullName ?? "null"}");
            _output.WriteLine($"With matching prefix again: {withMatchingAgain?.FullName}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates all entries in a dictionary by resolving type names with assembly prefix.
        /// </summary>
        private void ValidateGetTypeWithAssembly(string assemblyPrefix, Dictionary<string, Type> typeMap, string testName)
        {
            _output.WriteLine($"### Validating {testName} with prefix '{assemblyPrefix}' ({typeMap.Count} entries)\n");

            var failedTypes = new List<(string typeName, Type? expected, Type? actual)>();
            var passedCount = 0;

            foreach (var kvp in typeMap)
            {
                var typeName = kvp.Key;
                var expectedType = kvp.Value;
                var actualType = TypeUtils.GetType(assemblyPrefix, typeName);

                if (actualType == expectedType)
                {
                    passedCount++;
                    _output.WriteLine($"  [PASS] {typeName} -> {actualType?.GetTypeId().ValueOrNull()}");
                }
                else
                {
                    failedTypes.Add((typeName, expectedType, actualType));
                    _output.WriteLine($"  [FAIL] TypeName: {typeName}");
                    _output.WriteLine($"         Expected: {expectedType?.GetTypeId().ValueOrNull()}");
                    _output.WriteLine($"         Actual:   {actualType?.GetTypeId().ValueOrNull()}");
                }
            }

            _output.WriteLine($"\n### Summary: {passedCount}/{typeMap.Count} passed\n");

            if (failedTypes.Count > 0)
            {
                var errorMessage = $"Failed {failedTypes.Count} type(s) in {testName}:\n";
                foreach (var (typeName, expected, actual) in failedTypes)
                {
                    errorMessage += $"  - TypeName '{typeName}': expected '{expected?.GetTypeId().ValueOrNull()}' but got '{actual?.GetTypeId().ValueOrNull()}'\n";
                }
                Assert.Fail(errorMessage);
            }
        }

        #endregion
    }
}

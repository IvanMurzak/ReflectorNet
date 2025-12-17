/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.TypeUtilsTests
{
    /// <summary>
    /// Tests for TypeUtils.GetType() method.
    /// Verifies that type ID strings are correctly resolved back to their Type objects.
    /// Uses the same dictionaries as GetTypeIdTests but tests in reverse direction (string → Type).
    /// </summary>
    public class GetTypeTests : BaseTest
    {
        public GetTypeTests(ITestOutputHelper output) : base(output) { }

        #region Test Methods

        [Fact]
        public void GetType_BuiltInTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.BuiltInTypes, nameof(GetTypeIdTests.BuiltInTypes));
        }

        [Fact]
        public void GetType_BuiltInArrayTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.BuiltInArrayTypes, nameof(GetTypeIdTests.BuiltInArrayTypes));
        }

        [Fact]
        public void GetType_BuiltInGenericTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.BuiltInGenericTypes, nameof(GetTypeIdTests.BuiltInGenericTypes));
        }

        [Fact]
        public void GetType_NestedGenericTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.NestedGenericTypes, nameof(GetTypeIdTests.NestedGenericTypes));
        }

        [Fact]
        public void GetType_ThisAssemblyTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.ThisAssemblyTypes, nameof(GetTypeIdTests.ThisAssemblyTypes));
        }

        [Fact]
        public void GetType_ReflectorNetTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.ReflectorNetTypes, nameof(GetTypeIdTests.ReflectorNetTypes));
        }

        [Fact]
        public void GetType_OuterAssemblyTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.OuterAssemblyTypes, nameof(GetTypeIdTests.OuterAssemblyTypes));
        }

        [Fact]
        public void GetType_OuterAssemblyArrayTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.OuterAssemblyArrayTypes, nameof(GetTypeIdTests.OuterAssemblyArrayTypes));
        }

        [Fact]
        public void GetType_ComplexCombinedTypes()
        {
            ValidateGetTypeDictionary(GetTypeIdTests.ComplexCombinedTypes, nameof(GetTypeIdTests.ComplexCombinedTypes));
        }

        /// <summary>
        /// Validates all entries in a dictionary by resolving type ID strings back to types.
        /// </summary>
        private void ValidateGetTypeDictionary(Dictionary<string, Type> typeMap, string dictionaryName)
        {
            _output.WriteLine($"### Validating {dictionaryName} ({typeMap.Count} entries)\n");

            var failedTypes = new List<(string typeId, Type? expected, Type? actual)>();
            var passedCount = 0;

            foreach (var kvp in typeMap)
            {
                var typeId = kvp.Key;
                var expectedType = kvp.Value;
                var actualType = TypeUtils.GetType(typeId);

                if (actualType == expectedType)
                {
                    passedCount++;
                    _output.WriteLine($"  [PASS] {typeId} → {actualType?.FullName ?? "null"}");
                }
                else
                {
                    failedTypes.Add((typeId, expectedType, actualType));
                    _output.WriteLine($"  [FAIL] TypeId: {typeId}");
                    _output.WriteLine($"         Expected: {expectedType?.FullName ?? "null"}");
                    _output.WriteLine($"         Actual:   {actualType?.FullName ?? "null"}");
                }
            }

            _output.WriteLine($"\n### Summary: {passedCount}/{typeMap.Count} passed\n");

            if (failedTypes.Count > 0)
            {
                var errorMessage = $"Failed {failedTypes.Count} type(s) in {dictionaryName}:\n";
                foreach (var (typeId, expected, actual) in failedTypes)
                {
                    errorMessage += $"  - TypeId '{typeId}': expected '{expected?.FullName ?? "null"}' but got '{actual?.FullName ?? "null"}'\n";
                }
                Assert.Fail(errorMessage);
            }
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void GetType_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(TypeUtils.GetType(null));
            Assert.Null(TypeUtils.GetType(""));
            Assert.Null(TypeUtils.GetType("   "));
        }

        [Fact]
        public void GetType_InvalidTypeName_ReturnsNull()
        {
            Assert.Null(TypeUtils.GetType("NonExistent.Type.That.Does.Not.Exist"));
            Assert.Null(TypeUtils.GetType("System.NonExistentType"));
            Assert.Null(TypeUtils.GetType("InvalidGeneric<>"));
        }

        [Fact]
        public void GetType_RoundTrip_GetTypeId_GetType()
        {
            // Verify round-trip: Type → GetTypeId → GetType → same Type
            var testTypes = new[]
            {
                typeof(int),
                typeof(string),
                typeof(List<int>),
                typeof(Dictionary<string, int>),
                typeof(int[]),
                typeof(List<List<int>>),
            };

            foreach (var originalType in testTypes)
            {
                var typeId = TypeUtils.GetTypeId(originalType);
                var resolvedType = TypeUtils.GetType(typeId);

                _output.WriteLine($"  {originalType} → \"{typeId}\" → {resolvedType}");
                Assert.Equal(originalType, resolvedType);
            }
        }

        #endregion
    }
}

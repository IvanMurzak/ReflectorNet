/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Tests.TypeUtilsTests;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Tests for Reflector.Registry.BlacklistTypesInAssembly method.
    /// Focuses on verifying that complex types are correctly blacklisted when batch-registered via assembly scan.
    /// </summary>
    public class BlacklistTypesInAssemblyTests : BaseTest
    {
        public BlacklistTypesInAssemblyTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BlacklistTypesInAssembly_ComplexOuterAssemblyTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // We target the loaded assembly "ReflectorNet.Tests.OuterAssembly"
            // Note: The assembly name differs from the namespace prefix "com.IvanMurzak..."
            var assemblyPrefix = "ReflectorNet.Tests.OuterAssembly";

            // Force load the assembly to ensure it's available for scanning
            var dummy = typeof(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass);

            // Collect all type names defined in the OuterAssembly test data
            var typesToBlacklist = new HashSet<string>();

            // Add simple and generic types from OuterAssembly
            foreach (var kvp in GetTypeIdTests.OuterAssemblyTypes)
            {
                typesToBlacklist.Add(kvp.Key);

                // For generic types, also blacklist the open generic definition.
                // This ensures that any constructed generic of this type (even with non-blacklisted args)
                // is considered blacklisted.
                if (kvp.Value.IsGenericType && !kvp.Value.IsGenericTypeDefinition)
                {
                    var genericDef = kvp.Value.GetGenericTypeDefinition();
                    // FullName might be null for some types, but should be fine for these class definitions
                    if (genericDef.FullName != null)
                    {
                        typesToBlacklist.Add(genericDef.FullName);
                    }
                }
            }

            // Add array types from OuterAssembly
            foreach (var typeId in GetTypeIdTests.OuterAssemblyArrayTypes.Keys)
            {
                typesToBlacklist.Add(typeId);
            }

            _output.WriteLine($"Targeting assembly prefix: {assemblyPrefix}");
            _output.WriteLine($"Blacklisting {typesToBlacklist.Count} unique type identifiers via BlacklistTypesInAssembly...");

            // Act
            // Attempt to blacklist all these types by finding them in OuterAssembly
            var changed = registry.BlacklistTypesInAssembly(assemblyPrefix, typesToBlacklist.ToArray());

            Assert.True(changed, "BlacklistTypesInAssembly should return true as types should have been added.");

            // Assert

            // 1. Verify the types we explicitly asked to blacklist are indeed blacklisted
            VerifySetIsBlacklisted(registry, GetTypeIdTests.OuterAssemblyTypes, "OuterAssemblyTypes (Direct Targets)");
            VerifySetIsBlacklisted(registry, GetTypeIdTests.OuterAssemblyArrayTypes, "OuterAssemblyArrayTypes (Direct Targets)");

            // 2. Verify derived complex types are also blacklisted.
            VerifySetIsBlacklisted(registry, GetTypeIdTests.ComplexCombinedTypes, "ComplexCombinedTypes (Implicitly Blacklisted)");
        }

        [Fact]
        public void BlacklistTypesInAssembly_BuiltInTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.BuiltInTypes, "BuiltInTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_BuiltInArrayTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.BuiltInArrayTypes, "BuiltInArrayTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_BuiltInGenericTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.BuiltInGenericTypes, "BuiltInGenericTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_NestedGenericTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.NestedGenericTypes, "NestedGenericTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_ThisAssemblyTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.ThisAssemblyTypes, "ThisAssemblyTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_ReflectorNetTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.ReflectorNetTypes, "ReflectorNetTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_OuterAssemblyTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Force load the assembly to ensure it's available for scanning
            var dummy = typeof(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass);

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.OuterAssemblyTypes, "OuterAssemblyTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_OuterAssemblyArrayTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Force load the assembly to ensure it's available for scanning
            var dummy = typeof(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass);

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.OuterAssemblyArrayTypes, "OuterAssemblyArrayTypes");
        }

        [Fact]
        public void BlacklistTypesInAssembly_ComplexCombinedTypes_AreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var registry = reflector.Converters;

            // Force load the assembly to ensure it's available for scanning
            var dummy = typeof(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass);

            // Act & Assert
            BlacklistAndVerifyByTypeId(registry, GetTypeIdTests.ComplexCombinedTypes, "ComplexCombinedTypes");
        }

        /// <summary>
        /// Blacklists types using their TypeUtils.GetTypeId() string representation with exact assembly matching,
        /// then verifies they are correctly blacklisted.
        /// </summary>
        private void BlacklistAndVerifyByTypeId(Reflector.Registry registry, Dictionary<string, Type> types, string dictionaryName)
        {
            _output.WriteLine($"### Testing {dictionaryName} ({types.Count} types)\n");

            // Group types by their root assembly to blacklist efficiently
            var typesByAssembly = new Dictionary<string, List<(string typeId, Type type)>>();

            foreach (var kvp in types)
            {
                var type = kvp.Value;
                var typeId = TypeUtils.GetTypeId(type);

                // Get the assembly name from the type itself
                // For arrays, get element type's assembly; for generics, get the generic definition's assembly
                var rootType = GetRootType(type);
                var assemblyName = rootType.Assembly.GetName().Name;

                if (string.IsNullOrEmpty(assemblyName))
                    continue;

                if (!typesByAssembly.TryGetValue(assemblyName, out var list))
                {
                    list = new List<(string, Type)>();
                    typesByAssembly[assemblyName] = list;
                }
                list.Add((typeId, type));
            }

            // Blacklist types grouped by assembly
            var totalBlacklisted = 0;
            foreach (var kvp in typesByAssembly)
            {
                var assemblyName = kvp.Key;
                var typeList = kvp.Value;
                var typeIds = typeList.Select(t => t.typeId).ToArray();

                _output.WriteLine($"  Assembly: {assemblyName}");
                foreach (var (typeId, type) in typeList)
                {
                    _output.WriteLine($"    - {typeId}");
                }

                var changed = registry.BlacklistTypesInAssembly(assemblyName, typeIds);
                if (changed)
                    totalBlacklisted += typeIds.Length;
            }

            _output.WriteLine($"\n  Blacklisted {totalBlacklisted} types across {typesByAssembly.Count} assemblies.\n");

            // Verify all types are blacklisted
            VerifySetIsBlacklisted(registry, types, dictionaryName);
        }

        /// <summary>
        /// Gets the root type for assembly resolution.
        /// For arrays, returns the element type.
        /// For generics, returns the generic type definition.
        /// </summary>
        private static Type GetRootType(Type type)
        {
            // Handle arrays - get the element type
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return elementType != null ? GetRootType(elementType) : type;
            }

            // Handle constructed generics - get the definition
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                return type.GetGenericTypeDefinition();
            }

            return type;
        }

        private void VerifySetIsBlacklisted(Reflector.Registry registry, Dictionary<string, Type> types, string dictionaryName)
        {
            _output.WriteLine($"\n### Verifying {dictionaryName} ({types.Count} types)");

            var failedTypes = new List<string>();
            var passedCount = 0;

            foreach (var kvp in types)
            {
                var typeId = kvp.Key;
                var type = kvp.Value;

                // Primary check: IsTypeBlacklisted
                var isBlacklisted = registry.IsTypeBlacklisted(type);

                if (isBlacklisted)
                {
                    passedCount++;
                }
                else
                {
                    failedTypes.Add(typeId);
                    _output.WriteLine($"  [FAIL] {typeId} is NOT blacklisted.");
                }
            }

            _output.WriteLine($"  [SUMMARY] {passedCount}/{types.Count} passed.");

            if (failedTypes.Count > 0)
            {
                Assert.Fail($"Failed to blacklist {failedTypes.Count} types in {dictionaryName}. First failure: {failedTypes[0]}");
            }
        }
    }
}

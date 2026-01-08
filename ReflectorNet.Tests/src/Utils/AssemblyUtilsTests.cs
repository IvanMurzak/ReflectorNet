/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using com.IvanMurzak.ReflectorNet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class AssemblyUtilsTests
    {
        #region GetAssemblyTypes Tests

        [Fact]
        public void GetAssemblyTypes_CurrentAssembly_ReturnsTypes()
        {
            // Arrange
            var assembly = typeof(AssemblyUtilsTests).Assembly;

            // Act
            var types = AssemblyUtils.GetAssemblyTypes(assembly);

            // Assert
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            Assert.Contains(typeof(AssemblyUtilsTests), types);
        }

        [Fact]
        public void GetAssemblyTypes_MscorlibAssembly_ReturnsTypes()
        {
            // Arrange
            var assembly = typeof(object).Assembly;

            // Act
            var types = AssemblyUtils.GetAssemblyTypes(assembly);

            // Assert
            Assert.NotNull(types);
            Assert.NotEmpty(types);
            Assert.Contains(typeof(object), types);
            Assert.Contains(typeof(string), types);
        }

        [Fact]
        public void GetAssemblyTypes_SameAssembly_ReturnsDifferentInstances()
        {
            // Arrange
            var assembly = typeof(AssemblyUtilsTests).Assembly;

            // Act
            var types1 = AssemblyUtils.GetAssemblyTypes(assembly);
            var types2 = AssemblyUtils.GetAssemblyTypes(assembly);

            // Assert - should return different array instances (no caching)
            Assert.NotSame(types1, types2);
            Assert.Equal(types1.Length, types2.Length);
        }

        [Fact]
        public void GetAssemblyTypes_MultipleAssemblies_ReturnsDifferentArrays()
        {
            // Arrange
            var testAssembly = typeof(AssemblyUtilsTests).Assembly;
            var coreAssembly = typeof(object).Assembly;

            // Act
            var testTypes = AssemblyUtils.GetAssemblyTypes(testAssembly);
            var coreTypes = AssemblyUtils.GetAssemblyTypes(coreAssembly);

            // Assert
            Assert.NotSame(testTypes, coreTypes);
        }

        [Fact]
        public void GetAssemblyTypes_ReturnsNonNullTypes()
        {
            // Arrange
            var assembly = typeof(AssemblyUtilsTests).Assembly;

            // Act
            var types = AssemblyUtils.GetAssemblyTypes(assembly);

            // Assert
            Assert.All(types, t => Assert.NotNull(t));
        }

        [Fact]
        public void GetAssemblyTypes_ReflectorNetAssembly_ContainsExpectedTypes()
        {
            // Arrange
            var assembly = typeof(AssemblyUtils).Assembly;

            // Act
            var types = AssemblyUtils.GetAssemblyTypes(assembly);

            // Assert
            Assert.Contains(types, t => t.Name == "AssemblyUtils");
            Assert.Contains(types, t => t.Name == "TypeUtils");
            Assert.Contains(types, t => t.Name == "Reflector");
        }

        #endregion

        #region AllAssemblies Tests

        [Fact]
        public void AllAssemblies_ReturnsAssemblies()
        {
            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();

            // Assert
            Assert.NotEmpty(allAssemblies);
        }

        [Fact]
        public void AllAssemblies_ContainsCurrentTestAssembly()
        {
            // Arrange
            var testAssembly = typeof(AssemblyUtilsTests).Assembly;

            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();

            // Assert
            Assert.Contains(testAssembly, allAssemblies);
        }

        [Fact]
        public void AllAssemblies_ContainsReflectorNetAssembly()
        {
            // Arrange
            var reflectorAssembly = typeof(AssemblyUtils).Assembly;

            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();

            // Assert
            Assert.Contains(reflectorAssembly, allAssemblies);
        }

        [Fact]
        public void AllAssemblies_ContainsCoreLibAssembly()
        {
            // Arrange
            var coreLibAssembly = typeof(object).Assembly;

            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();

            // Assert
            Assert.Contains(coreLibAssembly, allAssemblies);
        }

        [Fact]
        public void AllAssemblies_AllAssembliesAreNotNull()
        {
            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();

            // Assert
            Assert.All(allAssemblies, a => Assert.NotNull(a));
        }

        [Fact]
        public void AllAssemblies_MultipleEnumerations_Succeed()
        {
            // Act - enumerate multiple times
            var count1 = AssemblyUtils.AllAssemblies.Count();
            var count2 = AssemblyUtils.AllAssemblies.Count();

            // Assert - both enumerations should return assemblies
            // Note: counts may differ slightly if new assemblies are loaded between enumerations
            Assert.True(count1 > 0, "First enumeration should return assemblies");
            Assert.True(count2 > 0, "Second enumeration should return assemblies");
            // The counts should be reasonably close (within 50 assemblies)
            var difference = Math.Abs(count1 - count2);
            Assert.True(difference <= 50,
                $"Counts differ too much: {count1} vs {count2} (difference: {difference})");
        }

        [Fact]
        public void AllAssemblies_CanIterateWithForeach()
        {
            // Act
            var count = 0;
            foreach (var assembly in AssemblyUtils.AllAssemblies)
            {
                Assert.NotNull(assembly);
                count++;
            }

            // Assert
            Assert.True(count > 0, "Should iterate at least one assembly");
        }

        [Fact]
        public void AllAssemblies_ConcurrentEnumeration_Succeeds()
        {
            // Arrange
            var counts = new int[5];

            // Act - enumerate from multiple threads simultaneously
            Parallel.For(0, 5, i =>
            {
                counts[i] = AssemblyUtils.AllAssemblies.Count();
            });

            // Assert - all counts should be positive
            Assert.All(counts, c => Assert.True(c > 0));
        }

        [Fact]
        public void AllAssemblies_ContainsMultipleAssemblies()
        {
            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();

            // Assert - should have multiple assemblies loaded
            Assert.True(allAssemblies.Count > 5, $"Expected more than 5 assemblies, got {allAssemblies.Count}");
        }

        [Fact]
        public void AllAssemblies_NoDuplicates()
        {
            // Act
            var allAssemblies = AssemblyUtils.AllAssemblies.ToList();
            var distinctCount = allAssemblies.Distinct().Count();

            // Assert
            Assert.Equal(allAssemblies.Count, distinctCount);
        }

        #endregion

        #region AllTypes Tests

        [Fact]
        public void AllTypes_ReturnsTypes()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.NotEmpty(allTypes);
        }

        [Fact]
        public void AllTypes_ContainsCommonTypes()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(typeof(object), allTypes);
            Assert.Contains(typeof(string), allTypes);
            Assert.Contains(typeof(int), allTypes);
        }

        [Fact]
        public void AllTypes_ContainsTestTypes()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(typeof(AssemblyUtilsTests), allTypes);
        }

        [Fact]
        public void AllTypes_ContainsAssemblyUtilsType()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(typeof(AssemblyUtils), allTypes);
        }

        [Fact]
        public void AllTypes_AllTypesAreNotNull()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.All(allTypes, t => Assert.NotNull(t));
        }

        [Fact]
        public void AllTypes_MultipleEnumerations_Succeed()
        {
            // Act - enumerate multiple times
            var count1 = AssemblyUtils.AllTypes.Count();
            var count2 = AssemblyUtils.AllTypes.Count();

            // Assert - both enumerations should return types
            // Note: counts may differ slightly if new assemblies are loaded between enumerations
            Assert.True(count1 > 0, "First enumeration should return types");
            Assert.True(count2 > 0, "Second enumeration should return types");
            // The counts should be reasonably close (within 10% or 500 types)
            var difference = Math.Abs(count1 - count2);
            var maxAllowedDifference = Math.Max(500, count1 / 10);
            Assert.True(difference <= maxAllowedDifference,
                $"Counts differ too much: {count1} vs {count2} (difference: {difference})");
        }

        [Fact]
        public void AllTypes_CanIterateWithForeach()
        {
            // Act
            var count = 0;
            foreach (var type in AssemblyUtils.AllTypes)
            {
                Assert.NotNull(type);
                count++;
                if (count > 100) break; // Don't iterate all to keep test fast
            }

            // Assert
            Assert.True(count > 100);
        }

        [Fact]
        public void AllTypes_ContainsTypesFromMultipleAssemblies()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Get assemblies that contributed types
            var assemblies = allTypes.Select(t => t.Assembly).Distinct().ToList();

            // Assert - should have types from multiple assemblies
            Assert.True(assemblies.Count > 1, "Should contain types from multiple assemblies");
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void GetAssemblyTypes_ConcurrentAccess_ReturnsValidTypes()
        {
            // Arrange
            var assembly = typeof(AssemblyUtilsTests).Assembly;
            var results = new Type[10][];
            using (var barrier = new Barrier(10))
            {
                // Act - access from multiple threads simultaneously
                Parallel.For(0, 10, i =>
                {
                    barrier.SignalAndWait(); // Ensure all threads start at the same time
                    results[i] = AssemblyUtils.GetAssemblyTypes(assembly);
                });
            }

            // Assert - all should be valid and different instances
            for (int i = 0; i < results.Length; i++)
            {
                Assert.NotNull(results[i]);
                Assert.NotEmpty(results[i]);
                if (i > 0)
                {
                    Assert.NotSame(results[0], results[i]);
                }
            }
        }

        [Fact]
        public void GetAssemblyTypes_ConcurrentAccessDifferentAssemblies_Succeeds()
        {
            // Arrange
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Take(10).ToArray();
            var results = new Dictionary<Assembly, Type[]>();
            var lockObj = new object();

            // Act - access different assemblies from multiple threads
            Parallel.ForEach(assemblies, assembly =>
            {
                var types = AssemblyUtils.GetAssemblyTypes(assembly);
                lock (lockObj)
                {
                    results[assembly] = types;
                }
            });

            // Assert
            Assert.Equal(assemblies.Length, results.Count);
            foreach (var kvp in results)
            {
                Assert.NotNull(kvp.Value);
            }
        }

        [Fact]
        public void AllTypes_ConcurrentEnumeration_Succeeds()
        {
            // Arrange
            var counts = new int[5];

            // Act - enumerate from multiple threads simultaneously
            Parallel.For(0, 5, i =>
            {
                counts[i] = AssemblyUtils.AllTypes.Count();
            });
            for (int i = 1; i < counts.Length; i++)
            {
                var diff = Math.Abs(counts[0] - counts[i]);
                Assert.Equal(counts[0], counts[i]);
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void AllTypes_ContainsGenericTypeDefinitions()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert - should contain generic type definitions like List<>
            Assert.Contains(allTypes, t => t.IsGenericTypeDefinition);
        }

        [Fact]
        public void AllTypes_ContainsNestedTypes()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert - should contain nested types
            Assert.Contains(allTypes, t => t.IsNested);
        }

        [Fact]
        public void AllTypes_ContainsInterfaces()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(allTypes, t => t.IsInterface);
        }

        [Fact]
        public void AllTypes_ContainsAbstractClasses()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(allTypes, t => t.IsAbstract && t.IsClass);
        }

        [Fact]
        public void AllTypes_ContainsEnums()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(allTypes, t => t.IsEnum);
        }

        [Fact]
        public void AllTypes_ContainsStructs()
        {
            // Act
            var allTypes = AssemblyUtils.AllTypes.ToList();

            // Assert
            Assert.Contains(allTypes, t => t.IsValueType && !t.IsEnum && !t.IsPrimitive);
        }

        [Fact]
        public void GetAssemblyTypes_SystemAssembly_ContainsExceptionTypes()
        {
            // Arrange
            var assembly = typeof(Exception).Assembly;

            // Act
            var types = AssemblyUtils.GetAssemblyTypes(assembly);

            // Assert
            Assert.Contains(types, t => typeof(Exception).IsAssignableFrom(t));
        }

        #endregion

    }
}

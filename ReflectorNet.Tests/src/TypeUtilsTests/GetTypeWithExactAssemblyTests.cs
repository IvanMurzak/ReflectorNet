/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.TypeUtilsTests
{
    /// <summary>
    /// Tests for TypeUtils.GetType(Assembly assembly, string? typeName) method.
    /// Verifies that types are correctly resolved when searching within a specific assembly.
    /// </summary>
    public class GetTypeWithExactAssemblyTests : BaseTest
    {
        public GetTypeWithExactAssemblyTests(ITestOutputHelper output) : base(output) { }

        private readonly Assembly _reflectorNetAssembly = typeof(SerializedMember).Assembly;
        private readonly Assembly _testsAssembly = typeof(GetTypeWithExactAssemblyTests).Assembly;
        private readonly Assembly _outerAssembly = typeof(ParentClass).Assembly;
        private readonly Assembly _systemAssembly = typeof(int).Assembly;

        #region Edge Case Tests

        [Fact]
        public void GetType_NullAssembly_ReturnsNull()
        {
            var type = TypeUtils.GetType((Assembly)null!, "System.Int32");
            Assert.Null(type);
        }

        [Fact]
        public void GetType_NullTypeName_ReturnsNull()
        {
            var type = TypeUtils.GetType(_systemAssembly, null);
            Assert.Null(type);
        }

        [Fact]
        public void GetType_EmptyTypeName_ReturnsNull()
        {
            var type = TypeUtils.GetType(_systemAssembly, "");
            Assert.Null(type);

            type = TypeUtils.GetType(_systemAssembly, "   ");
            Assert.Null(type);
        }

        [Fact]
        public void GetType_ExistingType_InWrongAssembly_ReturnsNull()
        {
            // System.Int32 exists, but is not in ReflectorNet assembly
            var type = TypeUtils.GetType(_reflectorNetAssembly, "System.Int32");
            Assert.Null(type);
        }

        #endregion

        #region Simple Type Tests

        [Fact]
        public void GetType_SimpleType_InCorrectAssembly_Found()
        {
            // Test existing type in ReflectorNet assembly
            var type = TypeUtils.GetType(_reflectorNetAssembly, typeof(SerializedMember).FullName);
            Assert.Equal(typeof(SerializedMember), type);

            // Test existing type in System assembly
            var intType = TypeUtils.GetType(_systemAssembly, "System.Int32");
            Assert.Equal(typeof(int), intType);
        }

        [Fact]
        public void GetType_SimpleType_ByAssemblyQualifiedName_Found()
        {
            var typeName = typeof(SerializedMember).AssemblyQualifiedName;
            var type = TypeUtils.GetType(_reflectorNetAssembly, typeName);
            Assert.Equal(typeof(SerializedMember), type);
        }

        #endregion

        #region Array Type Tests

        [Fact]
        public void GetType_ArrayType_Simple_Found()
        {
            // SerializedMember[]
            var expectedType = typeof(SerializedMember[]);
            var typeName = expectedType.FullName;

            var type = TypeUtils.GetType(_reflectorNetAssembly, typeName);
            Assert.Equal(expectedType, type);
        }

        [Fact]
        public void GetType_MultiDimensionalArray_Found()
        {
            // SerializedMember[,]
            var expectedType = typeof(SerializedMember[,]);
            var typeName = expectedType.FullName;

            var type = TypeUtils.GetType(_reflectorNetAssembly, typeName);
            Assert.Equal(expectedType, type);
        }

        #endregion

        #region Generic Type Tests

        [Fact]
        public void GetType_GenericType_StandardNotation_Found()
        {
            // Note: C# notation List<int> is usually resolved by special logic if supported,
            // but standard reflection expects `1 notations or assembly qualified ones.
            // Our implementation has TryResolveCSharpGenericType support.

            // Testing List<SerializedMember>
            // Since List<> is in System.Private.CoreLib (or similar) but T is in ReflectorNet,
            // finding this via "Assembly" based lookup is tricky.
            // The method signature is GetType(Assembly assembly, ...)
            // If we ask for List<SerializedMember> on SystemAssembly, it might fail because it can't resolve SerializedMember easily
            // unless fully qualified.

            // Let's test a generic type DEFINED in the target assembly.
            // We need a generic type in one of our assemblies.
            // Let's look for one or generic usage.
            // Assuming TypeUtils.Cache.cs uses LruCache<K,V> which is in ReflectorNet assembly?
            // No, LruCache is in Utils namespace.

            var expectedType = typeof(com.IvanMurzak.ReflectorNet.Utils.LruCache<string, Type>);

            // Test C# syntax
            var csharpName = "com.IvanMurzak.ReflectorNet.Utils.LruCache<System.String, System.Type>";
            var type = TypeUtils.GetType(_reflectorNetAssembly, csharpName);
            Assert.Equal(expectedType, type);
        }

        [Fact]
        public void GetType_GenericType_ReflectionNotation_Found()
        {
            var expectedType = typeof(com.IvanMurzak.ReflectorNet.Utils.LruCache<string, Type>);
            var fullName = expectedType.FullName; // Has `2[[...]] formatting

            var type = TypeUtils.GetType(_reflectorNetAssembly, fullName);
            Assert.Equal(expectedType, type);
        }

        #endregion

        #region Nested Type Tests

        [Fact]
        public void GetType_NestedType_Found()
        {
            var expectedType = typeof(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass.NestedClass);

            // Standard Reflection nested notation: Parent+Child
            var standardName = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass";
            var type = TypeUtils.GetType(_outerAssembly, standardName);
            Assert.Equal(expectedType, type);
        }

        #endregion
    }
}

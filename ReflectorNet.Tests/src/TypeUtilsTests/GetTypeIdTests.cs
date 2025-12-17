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
    /// Tests for TypeUtils.GetTypeId() method.
    /// Verifies that types are correctly converted to their string representation.
    /// </summary>
    public class GetTypeIdTests : BaseTest
    {
        public GetTypeIdTests(ITestOutputHelper output) : base(output) { }

        #region Test Data - Built-in .NET Types

        /// <summary>
        /// Built-in .NET primitive and common types.
        /// </summary>
        public static readonly Dictionary<string, Type> BuiltInTypes = new Dictionary<string, Type>
        {
            // Primitive types
            ["System.Boolean"] = typeof(bool),
            ["System.Byte"] = typeof(byte),
            ["System.SByte"] = typeof(sbyte),
            ["System.Int16"] = typeof(short),
            ["System.UInt16"] = typeof(ushort),
            ["System.Int32"] = typeof(int),
            ["System.UInt32"] = typeof(uint),
            ["System.Int64"] = typeof(long),
            ["System.UInt64"] = typeof(ulong),
            ["System.Single"] = typeof(float),
            ["System.Double"] = typeof(double),
            ["System.Decimal"] = typeof(decimal),
            ["System.Char"] = typeof(char),
            ["System.String"] = typeof(string),
            ["System.Object"] = typeof(object),

            // Common value types
            ["System.DateTime"] = typeof(DateTime),
            ["System.DateTimeOffset"] = typeof(DateTimeOffset),
            ["System.TimeSpan"] = typeof(TimeSpan),
            ["System.Guid"] = typeof(Guid),
            ["System.IntPtr"] = typeof(IntPtr),
            ["System.UIntPtr"] = typeof(UIntPtr),

            // Common reference types
            ["System.Type"] = typeof(Type),
            ["System.Exception"] = typeof(Exception),
            ["System.Uri"] = typeof(Uri),
            ["System.Version"] = typeof(Version),

            // Abstract types
            ["System.Array"] = typeof(Array),
            ["System.Enum"] = typeof(Enum),
            ["System.ValueType"] = typeof(ValueType),
            ["System.IO.Stream"] = typeof(System.IO.Stream),

            // Interfaces
            ["System.IDisposable"] = typeof(IDisposable),
            ["System.IComparable"] = typeof(IComparable),
            ["System.ICloneable"] = typeof(ICloneable),

            // Nested types
            ["System.Environment+SpecialFolder"] = typeof(Environment.SpecialFolder),
        };

        #endregion

        #region Test Data - Built-in Array Types

        /// <summary>
        /// Array types derived from built-in types.
        /// </summary>
        public static readonly Dictionary<string, Type> BuiltInArrayTypes = new Dictionary<string, Type>
        {
            // Simple arrays
            ["System.Int32[]"] = typeof(int[]),
            ["System.String[]"] = typeof(string[]),
            ["System.Boolean[]"] = typeof(bool[]),
            ["System.Double[]"] = typeof(double[]),
            ["System.Object[]"] = typeof(object[]),
            ["System.Byte[]"] = typeof(byte[]),
            ["System.DateTime[]"] = typeof(DateTime[]),
            ["System.Guid[]"] = typeof(Guid[]),

            // Jagged arrays
            ["System.Int32[][]"] = typeof(int[][]),
            ["System.String[][]"] = typeof(string[][]),
            ["System.Object[][]"] = typeof(object[][]),

            // Triple jagged arrays
            ["System.Int32[][][]"] = typeof(int[][][]),

            // Multi-dimensional arrays
            ["System.Int32[,]"] = typeof(int[,]),
            ["System.String[,]"] = typeof(string[,]),
            ["System.Object[,]"] = typeof(object[,]),

            // Multi-dimensional arrays (Rank 3)
            ["System.Int32[,,]"] = typeof(int[,,]),
            ["System.Double[,,]"] = typeof(double[,,]),

            // Multi-dimensional arrays (Rank 4)
            ["System.Int32[,,,]"] = typeof(int[,,,]),

            // Mixed arrays (Array of 2D arrays)
            // Note: C# syntax int[][,] is (int[,])[] -> System.Int32[,][]
            ["System.Int32[,][]"] = typeof(int[][,]),

            // Mixed arrays (2D array of arrays)
            // Note: C# syntax int[,][] is (int[])[,] -> System.Int32[][,]
            ["System.Int32[][,]"] = typeof(int[,][]),
        };

        #endregion

        #region Test Data - Built-in Generic Types

        /// <summary>
        /// Generic types from System.Collections.Generic.
        /// </summary>
        public static readonly Dictionary<string, Type> BuiltInGenericTypes = new Dictionary<string, Type>
        {
            // List<T>
            ["System.Collections.Generic.List<System.Int32>"] = typeof(List<int>),
            ["System.Collections.Generic.List<System.String>"] = typeof(List<string>),
            ["System.Collections.Generic.List<System.Object>"] = typeof(List<object>),
            ["System.Collections.Generic.List<System.DateTime>"] = typeof(List<DateTime>),

            // Dictionary<TKey, TValue>
            ["System.Collections.Generic.Dictionary<System.String,System.Int32>"] = typeof(Dictionary<string, int>),
            ["System.Collections.Generic.Dictionary<System.Int32,System.String>"] = typeof(Dictionary<int, string>),
            ["System.Collections.Generic.Dictionary<System.String,System.Object>"] = typeof(Dictionary<string, object>),
            ["System.Collections.Generic.Dictionary<System.Guid,System.String>"] = typeof(Dictionary<Guid, string>),

            // HashSet<T>
            ["System.Collections.Generic.HashSet<System.Int32>"] = typeof(HashSet<int>),
            ["System.Collections.Generic.HashSet<System.String>"] = typeof(HashSet<string>),

            // Queue<T> and Stack<T>
            ["System.Collections.Generic.Queue<System.Int32>"] = typeof(Queue<int>),
            ["System.Collections.Generic.Stack<System.String>"] = typeof(Stack<string>),

            // LinkedList<T>
            ["System.Collections.Generic.LinkedList<System.Double>"] = typeof(LinkedList<double>),

            // KeyValuePair<TKey, TValue>
            ["System.Collections.Generic.KeyValuePair<System.String,System.Int32>"] = typeof(KeyValuePair<string, int>),

            // Nullable<T> struct (note: Nullable types are unwrapped by GetTypeId)
            // ["System.Nullable<System.Int32>"] = typeof(Nullable<int>), // This returns "System.Int32"

            // Generic interfaces
            ["System.Collections.Generic.IList<System.Int32>"] = typeof(IList<int>),
            ["System.Collections.Generic.ICollection<System.String>"] = typeof(ICollection<string>),
            ["System.Collections.Generic.IEnumerable<System.Object>"] = typeof(IEnumerable<object>),
            ["System.Collections.Generic.IDictionary<System.String,System.Object>"] = typeof(IDictionary<string, object>),
            ["System.Collections.Generic.ISet<System.Int32>"] = typeof(ISet<int>),

            // Tuple types
            ["System.Tuple<System.Int32,System.String>"] = typeof(Tuple<int, string>),
            ["System.Tuple<System.Int32,System.String,System.Boolean>"] = typeof(Tuple<int, string, bool>),
        };

        #endregion

        #region Test Data - Nested Generic Types

        /// <summary>
        /// Complex nested generic types.
        /// </summary>
        public static readonly Dictionary<string, Type> NestedGenericTypes = new Dictionary<string, Type>
        {
            // List of arrays
            ["System.Collections.Generic.List<System.Int32[]>"] = typeof(List<int[]>),
            ["System.Collections.Generic.List<System.String[]>"] = typeof(List<string[]>),

            // Array of lists
            ["System.Collections.Generic.List<System.Int32>[]"] = typeof(List<int>[]),
            ["System.Collections.Generic.List<System.String>[]"] = typeof(List<string>[]),

            // List of lists
            ["System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>"] = typeof(List<List<int>>),
            ["System.Collections.Generic.List<System.Collections.Generic.List<System.String>>"] = typeof(List<List<string>>),

            // Dictionary with list value
            ["System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.List<System.Int32>>"] = typeof(Dictionary<string, List<int>>),

            // Dictionary with dictionary value
            ["System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.Dictionary<System.Int32,System.Boolean>>"] = typeof(Dictionary<string, Dictionary<int, bool>>),

            // List of dictionaries
            ["System.Collections.Generic.List<System.Collections.Generic.Dictionary<System.String,System.Int32>>"] = typeof(List<Dictionary<string, int>>),

            // Triple nested generics
            ["System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>>"] = typeof(List<List<List<int>>>),

            // Complex combinations
            ["System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.List<System.Collections.Generic.Dictionary<System.Int32,System.Boolean>>>"] = typeof(Dictionary<string, List<Dictionary<int, bool>>>),
        };

        #endregion

        #region Test Data - This Assembly Types

        /// <summary>
        /// Types from ReflectorNet.Tests assembly.
        /// </summary>
        public static readonly Dictionary<string, Type> ThisAssemblyTypes = new Dictionary<string, Type>
        {
            // Simple classes
            ["com.IvanMurzak.ReflectorNet.Tests.Model.Vector3"] = typeof(Vector3),
            ["com.IvanMurzak.ReflectorNet.Tests.Model.SolarSystem"] = typeof(SolarSystem),
            ["com.IvanMurzak.ReflectorNet.Tests.Model.GameObjectRef"] = typeof(GameObjectRef),
            ["com.IvanMurzak.ReflectorNet.Tests.Model.ObjectRef"] = typeof(ObjectRef),
            ["com.IvanMurzak.ReflectorNet.Tests.TestClass"] = typeof(TestClass),

            // Nested classes
            ["com.IvanMurzak.ReflectorNet.Tests.Model.SolarSystem+CelestialBody"] = typeof(SolarSystem.CelestialBody),

            // Arrays of custom types
            ["com.IvanMurzak.ReflectorNet.Tests.Model.Vector3[]"] = typeof(Vector3[]),
            ["com.IvanMurzak.ReflectorNet.Tests.Model.GameObjectRef[]"] = typeof(GameObjectRef[]),

            // Generic with custom types
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Tests.Model.Vector3>"] = typeof(List<Vector3>),
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Tests.Model.GameObjectRef>"] = typeof(List<GameObjectRef>),
            ["System.Collections.Generic.Dictionary<System.String,com.IvanMurzak.ReflectorNet.Tests.Model.Vector3>"] = typeof(Dictionary<string, Vector3>),
        };

        #endregion

        #region Test Data - ReflectorNet Types

        /// <summary>
        /// Types from the main ReflectorNet library.
        /// </summary>
        public static readonly Dictionary<string, Type> ReflectorNetTypes = new Dictionary<string, Type>
        {
            // Core types
            ["com.IvanMurzak.ReflectorNet.Reflector"] = typeof(Reflector),
            ["com.IvanMurzak.ReflectorNet.Model.SerializedMember"] = typeof(SerializedMember),
            ["com.IvanMurzak.ReflectorNet.Model.SerializedMemberList"] = typeof(SerializedMemberList),
            ["com.IvanMurzak.ReflectorNet.Model.MethodRef"] = typeof(MethodRef),
            ["com.IvanMurzak.ReflectorNet.Model.MethodData"] = typeof(MethodData),

            // Arrays
            ["com.IvanMurzak.ReflectorNet.Model.SerializedMember[]"] = typeof(SerializedMember[]),

            // Generics with ReflectorNet types
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.Model.SerializedMember>"] = typeof(List<SerializedMember>),
            ["System.Collections.Generic.Dictionary<System.String,com.IvanMurzak.ReflectorNet.Model.SerializedMember>"] = typeof(Dictionary<string, SerializedMember>),
        };

        #endregion

        #region Test Data - Outer Assembly Types

        /// <summary>
        /// Types from ReflectorNet.Tests.OuterAssembly (cross-assembly testing).
        /// </summary>
        public static readonly Dictionary<string, Type> OuterAssemblyTypes = new Dictionary<string, Type>
        {
            // Simple types
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass"] = typeof(OuterSimpleClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleStruct"] = typeof(OuterSimpleStruct),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterAbstractClass"] = typeof(OuterAbstractClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSealedClass"] = typeof(OuterSealedClass),

            // Generic types (open generic definitions)
            // Note: Open generic types have `N suffix in FullName
            // ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass`1"] = typeof(OuterGenericClass<>),

            // Generic types (closed/constructed)
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.Int32>"] = typeof(OuterGenericClass<int>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.String>"] = typeof(OuterGenericClass<string>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.DateTime>"] = typeof(OuterGenericClass<DateTime>),

            // Generic with two type parameters
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass2<System.Int32,System.String>"] = typeof(OuterGenericClass2<int, string>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass2<System.String,System.Boolean>"] = typeof(OuterGenericClass2<string, bool>),

            // Generic with three type parameters
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass3<System.Int32,System.String,System.Boolean>"] = typeof(OuterGenericClass3<int, string, bool>),

            // Generic struct
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericStruct<System.Int32>"] = typeof(OuterGenericStruct<int>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericStruct<System.Double>"] = typeof(OuterGenericStruct<double>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericStruct2<System.Int32,System.String>"] = typeof(OuterGenericStruct2<int, string>),

            // Nested types
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedClass"] = typeof(OuterContainer.NestedClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedStruct"] = typeof(OuterContainer.NestedStruct),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedAbstractClass"] = typeof(OuterContainer.NestedAbstractClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedSealedClass"] = typeof(OuterContainer.NestedSealedClass),

            // Nested generic types
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedGenericClass<System.Int32>"] = typeof(OuterContainer.NestedGenericClass<int>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedGenericStruct<System.String>"] = typeof(OuterContainer.NestedGenericStruct<string>),

            // Double nested types
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedContainer+DoubleNestedClass"] = typeof(OuterContainer.NestedContainer.DoubleNestedClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedContainer+DoubleNestedStruct"] = typeof(OuterContainer.NestedContainer.DoubleNestedStruct),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedContainer+DoubleNestedGenericClass<System.Int32>"] = typeof(OuterContainer.NestedContainer.DoubleNestedGenericClass<int>),

            // Generic container with nested types
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericContainer<System.Int32>+NestedInGeneric"] = typeof(OuterGenericContainer<int>.NestedInGeneric),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericContainer<System.String>+NestedStructInGeneric"] = typeof(OuterGenericContainer<string>.NestedStructInGeneric),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericContainer<System.Int32>+NestedGenericInGeneric<System.String>"] = typeof(OuterGenericContainer<int>.NestedGenericInGeneric<string>),

            // Interfaces
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.IOuterInterface"] = typeof(IOuterInterface),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.IOuterGenericInterface<System.Int32>"] = typeof(IOuterGenericInterface<int>),

            // Enums
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterEnum"] = typeof(OuterEnum),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterFlagsEnum"] = typeof(OuterFlagsEnum),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterEnumContainer+NestedEnum"] = typeof(OuterEnumContainer.NestedEnum),

            // Existing types from OuterAssembly
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Person"] = typeof(Person),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Address"] = typeof(Address),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.Company"] = typeof(Company),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass"] = typeof(ParentClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass"] = typeof(ParentClass.NestedClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedStaticClass"] = typeof(ParentClass.NestedStaticClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.StaticParentClass+NestedClass"] = typeof(StaticParentClass.NestedClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.StaticParentClass+NestedStaticClass"] = typeof(StaticParentClass.NestedStaticClass),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.WrapperClass<System.Int32>"] = typeof(WrapperClass<int>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.WrapperClass<System.String>"] = typeof(WrapperClass<string>),
        };

        #endregion

        #region Test Data - Outer Assembly Array Types

        /// <summary>
        /// Array types from OuterAssembly.
        /// </summary>
        public static readonly Dictionary<string, Type> OuterAssemblyArrayTypes = new Dictionary<string, Type>
        {
            // Simple arrays
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass[]"] = typeof(OuterSimpleClass[]),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleStruct[]"] = typeof(OuterSimpleStruct[]),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterEnum[]"] = typeof(OuterEnum[]),

            // Jagged arrays
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass[][]"] = typeof(OuterSimpleClass[][]),

            // Generic arrays
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.Int32>[]"] = typeof(OuterGenericClass<int>[]),

            // Nested type arrays
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedClass[]"] = typeof(OuterContainer.NestedClass[]),
        };

        #endregion

        #region Test Data - Complex Combined Types

        /// <summary>
        /// Complex combinations of generics, arrays, and nested types.
        /// </summary>
        public static readonly Dictionary<string, Type> ComplexCombinedTypes = new Dictionary<string, Type>
        {
            // Generic with outer assembly type
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass>"] = typeof(List<OuterSimpleClass>),
            ["System.Collections.Generic.Dictionary<System.String,com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass>"] = typeof(Dictionary<string, OuterSimpleClass>),

            // Generic with nested type
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterContainer+NestedClass>"] = typeof(List<OuterContainer.NestedClass>),

            // Dictionary with two custom types
            ["System.Collections.Generic.Dictionary<com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterEnum,com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass>"] = typeof(Dictionary<OuterEnum, OuterSimpleClass>),

            // Nested generic with outer assembly types
            ["System.Collections.Generic.List<System.Collections.Generic.Dictionary<System.String,com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass>>"] = typeof(List<Dictionary<string, OuterSimpleClass>>),

            // Generic with generic type argument from outer assembly
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.Int32>>"] = typeof(List<OuterGenericClass<int>>),

            // Array of generic outer assembly type
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<System.String>[]"] = typeof(OuterGenericClass<string>[]),

            // Generic with array type argument
            ["System.Collections.Generic.List<com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass[]>"] = typeof(List<OuterSimpleClass[]>),

            // Dictionary with array value type
            ["System.Collections.Generic.Dictionary<System.String,com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleStruct[]>"] = typeof(Dictionary<string, OuterSimpleStruct[]>),

            // Cross-assembly generic combinations
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass<com.IvanMurzak.ReflectorNet.Tests.Model.Vector3>"] = typeof(OuterGenericClass<Vector3>),
            ["com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterGenericClass2<com.IvanMurzak.ReflectorNet.Tests.Model.Vector3,com.IvanMurzak.ReflectorNet.OuterAssembly.Model.OuterSimpleClass>"] = typeof(OuterGenericClass2<Vector3, OuterSimpleClass>),
        };

        #endregion

        #region Test Methods

        [Fact]
        public void GetTypeId_BuiltInTypes()
        {
            ValidateTypeIdDictionary(BuiltInTypes, nameof(BuiltInTypes));
        }

        [Fact]
        public void GetTypeId_BuiltInArrayTypes()
        {
            ValidateTypeIdDictionary(BuiltInArrayTypes, nameof(BuiltInArrayTypes));
        }

        [Fact]
        public void GetTypeId_BuiltInGenericTypes()
        {
            ValidateTypeIdDictionary(BuiltInGenericTypes, nameof(BuiltInGenericTypes));
        }

        [Fact]
        public void GetTypeId_NestedGenericTypes()
        {
            ValidateTypeIdDictionary(NestedGenericTypes, nameof(NestedGenericTypes));
        }

        [Fact]
        public void GetTypeId_ThisAssemblyTypes()
        {
            ValidateTypeIdDictionary(ThisAssemblyTypes, nameof(ThisAssemblyTypes));
        }

        [Fact]
        public void GetTypeId_ReflectorNetTypes()
        {
            ValidateTypeIdDictionary(ReflectorNetTypes, nameof(ReflectorNetTypes));
        }

        [Fact]
        public void GetTypeId_OuterAssemblyTypes()
        {
            ValidateTypeIdDictionary(OuterAssemblyTypes, nameof(OuterAssemblyTypes));
        }

        [Fact]
        public void GetTypeId_OuterAssemblyArrayTypes()
        {
            ValidateTypeIdDictionary(OuterAssemblyArrayTypes, nameof(OuterAssemblyArrayTypes));
        }

        [Fact]
        public void GetTypeId_ComplexCombinedTypes()
        {
            ValidateTypeIdDictionary(ComplexCombinedTypes, nameof(ComplexCombinedTypes));
        }

        /// <summary>
        /// Validates all entries in a dictionary mapping expected type IDs to types.
        /// </summary>
        private void ValidateTypeIdDictionary(Dictionary<string, Type> typeMap, string dictionaryName)
        {
            _output.WriteLine($"### Validating {dictionaryName} ({typeMap.Count} entries)\n");

            var failedTypes = new List<(string expected, string actual, Type type)>();
            var passedCount = 0;

            foreach (var kvp in typeMap)
            {
                var expectedTypeId = kvp.Key;
                var type = kvp.Value;
                var actualTypeId = TypeUtils.GetTypeId(type);

                if (expectedTypeId == actualTypeId)
                {
                    passedCount++;
                    _output.WriteLine($"  [PASS] {expectedTypeId}");
                }
                else
                {
                    failedTypes.Add((expectedTypeId, actualTypeId, type));
                    _output.WriteLine($"  [FAIL] Expected: {expectedTypeId}");
                    _output.WriteLine($"         Actual:   {actualTypeId}");
                    _output.WriteLine($"         Type:     {type}");
                }
            }

            _output.WriteLine($"\n### Summary: {passedCount}/{typeMap.Count} passed\n");

            if (failedTypes.Count > 0)
            {
                var errorMessage = $"Failed {failedTypes.Count} type(s) in {dictionaryName}:\n";
                foreach (var (expected, actual, type) in failedTypes)
                {
                    errorMessage += $"  - Expected '{expected}' but got '{actual}' for type {type}\n";
                }
                Assert.Fail(errorMessage);
            }
        }

        #endregion
    }
}

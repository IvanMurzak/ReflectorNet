/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

namespace com.IvanMurzak.ReflectorNet.OuterAssembly.Model
{
    // Simple class
    public class OuterSimpleClass
    {
        public int Value { get; set; }
    }

    // Simple struct
    public struct OuterSimpleStruct
    {
        public int Value { get; set; }
    }

    // Abstract class
    public abstract class OuterAbstractClass
    {
        public abstract int Value { get; set; }
    }

    // Sealed class
    public sealed class OuterSealedClass
    {
        public int Value { get; set; }
    }

    // Static class (cannot be used as type argument, but can be referenced)
    public static class OuterStaticClass
    {
        public static int Value { get; set; }
    }

    // Generic class with single type parameter
    public class OuterGenericClass<T>
    {
        public T? Value { get; set; }
    }

    // Generic class with two type parameters
    public class OuterGenericClass2<T1, T2>
    {
        public T1? Value1 { get; set; }
        public T2? Value2 { get; set; }
    }

    // Generic class with three type parameters
    public class OuterGenericClass3<T1, T2, T3>
    {
        public T1? Value1 { get; set; }
        public T2? Value2 { get; set; }
        public T3? Value3 { get; set; }
    }

    // Generic struct
    public struct OuterGenericStruct<T>
    {
        public T? Value { get; set; }
    }

    // Generic struct with two type parameters
    public struct OuterGenericStruct2<T1, T2>
    {
        public T1? Value1 { get; set; }
        public T2? Value2 { get; set; }
    }

    // Container class with nested types
    public class OuterContainer
    {
        // Nested class
        public class NestedClass
        {
            public int Value { get; set; }
        }

        // Nested struct
        public struct NestedStruct
        {
            public int Value { get; set; }
        }

        // Nested abstract class
        public abstract class NestedAbstractClass
        {
            public abstract int Value { get; set; }
        }

        // Nested sealed class
        public sealed class NestedSealedClass
        {
            public int Value { get; set; }
        }

        // Nested generic class
        public class NestedGenericClass<T>
        {
            public T? Value { get; set; }
        }

        // Nested generic struct
        public struct NestedGenericStruct<T>
        {
            public T? Value { get; set; }
        }

        // Double nested container
        public class NestedContainer
        {
            // Double nested class
            public class DoubleNestedClass
            {
                public int Value { get; set; }
            }

            // Double nested struct
            public struct DoubleNestedStruct
            {
                public int Value { get; set; }
            }

            // Double nested generic
            public class DoubleNestedGenericClass<T>
            {
                public T? Value { get; set; }
            }
        }
    }

    // Generic container with nested types
    public class OuterGenericContainer<T>
    {
        public class NestedInGeneric
        {
            public T? Value { get; set; }
        }

        public struct NestedStructInGeneric
        {
            public T? Value { get; set; }
        }

        public class NestedGenericInGeneric<U>
        {
            public T? Value1 { get; set; }
            public U? Value2 { get; set; }
        }
    }

    // Interface
    public interface IOuterInterface
    {
        int Value { get; set; }
    }

    // Generic interface
    public interface IOuterGenericInterface<T>
    {
        T? Value { get; set; }
    }

    // Enum
    public enum OuterEnum
    {
        None = 0,
        First = 1,
        Second = 2
    }

    // Flags enum
    [System.Flags]
    public enum OuterFlagsEnum
    {
        None = 0,
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4
    }

    // Nested enum container
    public class OuterEnumContainer
    {
        public enum NestedEnum
        {
            None = 0,
            Value = 1
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public static partial class TestUtils
    {
        public static partial class Types
        {

            public static readonly Type[] BaseStaticTypes = new[]
            {
                typeof(StaticParentClass),
                typeof(StaticParentClass.NestedStaticClass),
                typeof(ParentClass.NestedStaticClass),
            };
            public static readonly Type[] BaseNonStaticTypes = new[]
            {
                typeof(string),
                typeof(bool),
                typeof(int),
                typeof(double),
                typeof(float),
                typeof(decimal),
                typeof(System.Single),

                typeof(DateTime),
                typeof(Guid),
                typeof(TimeSpan),

                typeof(ParentClass),
                typeof(ParentClass.NestedClass),
                typeof(StaticParentClass.NestedClass),
            };
            public static readonly Type[] NonStaticReflectorModelTypes = new[]
            {
                typeof(SerializedMember),
                typeof(SerializedMemberList),
                typeof(MethodData),
                typeof(MethodRef),
                typeof(MethodInfo)
            };

            public static IEnumerable<Type> GetCollectionTypes(IEnumerable<Type> baseTypes) => baseTypes.SelectMany(type => new[]
            {
                // Simple arrays
                type.MakeArrayType(),

                // Arrays of arrays
                type.MakeArrayType().MakeArrayType(),

                // Arrays of arrays of arrays
                type.MakeArrayType().MakeArrayType().MakeArrayType(),

                // Simple lists
                typeof(List<>).MakeGenericType(type),

                // Lists of lists
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type)),

                // Lists of lists of lists
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type))),

                // Mixed: Arrays of lists
                typeof(List<>).MakeGenericType(type).MakeArrayType(),

                // Mixed: Lists of arrays
                typeof(List<>).MakeGenericType(type.MakeArrayType()),

                // Mixed: Arrays of lists of arrays
                typeof(List<>).MakeGenericType(type.MakeArrayType()).MakeArrayType(),

                // Mixed: Lists of arrays of lists
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type).MakeArrayType()),

                // Mixed: Arrays of arrays of lists
                typeof(List<>).MakeGenericType(type).MakeArrayType().MakeArrayType(),

                // Mixed: Lists of lists of arrays
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type.MakeArrayType())),

                // Deep mixed combinations
                typeof(List<>).MakeGenericType(type.MakeArrayType().MakeArrayType()),
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type.MakeArrayType()).MakeArrayType()),
                typeof(List<>).MakeGenericType(type.MakeArrayType()).MakeArrayType(),
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type)).MakeArrayType().MakeArrayType(),

                // Arrays of lists of lists
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type)).MakeArrayType(),

                // Complex deep nesting combinations
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type.MakeArrayType()).MakeArrayType()).MakeArrayType(),
                typeof(List<>).MakeGenericType(type.MakeArrayType().MakeArrayType()).MakeArrayType(),
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type)).MakeArrayType()),
                typeof(List<>).MakeGenericType(typeof(List<>).MakeGenericType(type).MakeArrayType().MakeArrayType()),
            });

            public static IEnumerable<Type> GetWrapperTypes(Type wrapperType, IEnumerable<Type> baseTypes) => baseTypes.SelectMany(type => new[]
            {
                // Single wrapper: Wrapper<T>
                wrapperType.MakeGenericType(type),

                // Double wrapper: Wrapper<Wrapper<T>>
                wrapperType.MakeGenericType(wrapperType.MakeGenericType(type)),

                // Triple wrapper: Wrapper<Wrapper<Wrapper<T>>>
                wrapperType.MakeGenericType(wrapperType.MakeGenericType(wrapperType.MakeGenericType(type))),
            });

            public static readonly IEnumerable<Type> AllBaseNonStaticTypes = CombineTypes(BaseNonStaticTypes);
            public static readonly IEnumerable<Type> AllNonStaticReflectorModelTypes = CombineTypes(NonStaticReflectorModelTypes);

            public static IEnumerable<Type> CombineTypes(IEnumerable<Type> sourceTypes)
            {
                return sourceTypes
                    .Concat(GetCollectionTypes(sourceTypes))
                    .Concat(GetWrapperTypes(typeof(WrapperClass<>), sourceTypes))
                    .Concat(GetCollectionTypes(GetWrapperTypes(typeof(WrapperClass<>), sourceTypes)))
                    .Concat(GetWrapperTypes(typeof(WrapperClass<>), GetCollectionTypes(sourceTypes)));
            }
        }
    }
}
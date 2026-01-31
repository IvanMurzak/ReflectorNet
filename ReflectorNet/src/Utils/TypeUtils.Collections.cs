using System;
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Determines whether the specified type is a generic dictionary (e.g. Dictionary&lt;,&gt; or IDictionary&lt;,&gt;).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is a generic dictionary; otherwise, <see langword="false"/>.</returns>
        public static bool IsDictionary(Type type)
        {
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                 type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                return true;
            }

            return type.GetInterfaces()
                .Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));
        }

        /// <summary>
        /// Gets the generic arguments of the dictionary type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>An array of <see cref="Type"/> representing the generic arguments, or <see langword="null"/> if the type is not a generic dictionary.</returns>
        public static Type[]? GetDictionaryGenericArguments(Type type)
        {
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                 type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                return type.GetGenericArguments();
            }

            var dictionaryInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IDictionary<,>)));

            return dictionaryInterface?.GetGenericArguments();
        }

        /// <summary>
        /// Determines whether the specified type is an enumerable type (array or implements <see cref="IEnumerable{T}"/>).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is enumerable; otherwise, <see langword="false"/>.</returns>
        public static bool IsIEnumerable(Type type)
        {
            if (type.IsArray)
                return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;

            return type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        /// <summary>
        /// Gets the element type of an enumerable type.
        /// </summary>
        /// <param name="type">The enumerable type.</param>
        /// <returns>The <see cref="Type"/> of the elements, or <see langword="null"/> if the type is not enumerable or the element type cannot be determined.</returns>
        public static Type? GetEnumerableItemType(Type type)
        {
            return _enumerableItemTypeCache.GetOrAdd(type, GetEnumerableItemTypeInternal);
        }

        private static Type? GetEnumerableItemTypeInternal(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments().FirstOrDefault();

            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
                return enumerableInterface.GetGenericArguments().FirstOrDefault();

            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return baseType.GetGenericArguments().FirstOrDefault();

                enumerableInterface = baseType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                if (enumerableInterface != null)
                    return enumerableInterface.GetGenericArguments().FirstOrDefault();

                baseType = baseType.BaseType;
            }

            return null;
        }
    }
}

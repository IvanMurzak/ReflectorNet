using System;
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
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

        public static bool IsIEnumerable(Type type)
        {
            if (type.IsArray)
                return true;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;

            return type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

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

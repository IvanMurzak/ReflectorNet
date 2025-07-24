using System;
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        const string Null = "null";
        const string ArraySuffix = "Array";

        public static string GetTypeName<T>(bool pretty = false) => GetTypeName(typeof(T), pretty);
        public static string GetTypeName(Type? type, bool pretty = false)
        {
            if (type == null)
                return Null;

            // Handle nullable types
            var underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
                type = underlyingNullableType;

            return pretty
                ? type.FullName ?? Null
                : type.AssemblyQualifiedName ?? Null;
        }
        public static string GetTypeId<T>() => GetTypeId(typeof(T));
        public static string GetTypeId(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Handle nullable types
            var underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
                type = underlyingNullableType;

            return GetTypeIdRecursive(type);
        }
        public static bool IsNameMatch(Type? type, string? typeName)
        {
            if (type == null || string.IsNullOrEmpty(typeName))
                return false;

            // Check if the type name matches the full name of the type
            return type.GetTypeName(pretty: true) == typeName ||
                   type.GetTypeName(pretty: false) == typeName;
        }

        /// <summary>
        /// Returns the type name without namespace or assembly, but includes generic arguments if any.
        /// For example: List<int>, Dictionary<string, List<double?>>
        /// </summary>
        public static string GetTypeShortName(Type? type)
        {
            if (type == null)
                return Null;

            // Handle nullable types
            var underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
                return $"{GetTypeShortName(underlyingNullableType)}?";

            if (type.IsGenericType)
            {
                var genericTypeName = type.Name;
                var tickIndex = genericTypeName.IndexOf('`');
                if (tickIndex > 0)
                    genericTypeName = genericTypeName.Substring(0, tickIndex);
                var genericArgs = type.GetGenericArguments().Select(GetTypeShortName);
                return $"{genericTypeName}<{string.Join(", ", genericArgs)}}}";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{GetTypeShortName(elementType)}[]";
            }

            return type.Name;
        }

        static string GetTypeIdRecursive(Type type)
        {
            // Special case: string is technically IEnumerable<char> but shouldn't be treated as an array
            if (type == typeof(string))
                return type.GetTypeName(pretty: true);

            // Check if this is a collection type we want to treat as an array
            var itemType = GetArrayLikeItemType(type);
            if (itemType != null)
            {
                // Handle nullable element types - unwrap them for the recursive call
                var underlyingItemType = Nullable.GetUnderlyingType(itemType);
                if (underlyingItemType != null)
                    itemType = underlyingItemType;

                // Recursively get the type ID for the item type and append "Array"
                return GetTypeIdRecursive(itemType) + ArraySuffix;
            }

            // If type is a generic type, use its full name with generic arguments
            if (type.IsGenericType)
            {
                var genericTypeName = type.GetGenericTypeDefinition().GetTypeName(pretty: true);
                if (StringUtils.IsNullOrEmpty(genericTypeName))
                    throw new InvalidOperationException($"Generic type '{type}' does not have a full name.");
                // Recursively get the type ID for each generic argument
                var genericArgs = type.GetGenericArguments().Select(GetTypeIdRecursive);
                return $"{genericTypeName}<{string.Join(",", genericArgs)}";
            }

            return type.GetTypeName(pretty: true);
        }

        static Type? GetArrayLikeItemType(Type type)
        {
            // Handle arrays directly
            if (type.IsArray)
                return type.GetElementType();

            // Only treat specific generic types as "array-like"
            if (type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();

                // List<T>, IList<T>, ICollection<T>, IEnumerable<T> should be treated as arrays
                if (genericDefinition == typeof(List<>) ||
                    genericDefinition == typeof(IList<>) ||
                    genericDefinition == typeof(ICollection<>) ||
                    genericDefinition == typeof(IEnumerable<>))
                {
                    return type.GetGenericArguments().FirstOrDefault();
                }
            }

            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Determine if the given object is assignable to the given type.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="targetType">The type to check against.</param>
        /// <returns><c>true</c> if the object is assignable to the type; otherwise, <c>false</c>.</returns>
        public static bool IsAssignableTo(object? obj, Type targetType)
        {
            if (targetType == null)
                return false;

            if (obj == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            return targetType.IsAssignableFrom(obj.GetType());
        }

        /// <summary>
        /// Checks if a source type can be cast or converted to a target type.
        /// </summary>
        /// <param name="type">The source type.</param>
        /// <param name="to">The target type.</param>
        /// <returns><c>true</c> if the cast or conversion is possible; otherwise, <c>false</c>.</returns>
        public static bool IsCastable(Type? type, Type to)
        {
            if (type == null || to == null)
                return false;

            type = Nullable.GetUnderlyingType(type) ?? type;
            to = Nullable.GetUnderlyingType(to) ?? to;

            if (to.IsAssignableFrom(type))
                return true;

            if (type.IsPrimitive && to.IsPrimitive)
                return true;

            if (type == typeof(string) && to == typeof(object))
                return true;

            return false;
        }

        /// <summary>
        /// Calculates the inheritance distance between a base type and a target type.
        /// </summary>
        /// <param name="baseType">The base type.</param>
        /// <param name="targetType">The target type which should inherit from <paramref name="baseType"/>.</param>
        /// <returns>The number of inheritance steps, or -1 if the types are not related or <paramref name="targetType"/> does not inherit from <paramref name="baseType"/>.</returns>
        public static int GetInheritanceDistance(Type baseType, Type targetType)
        {
            if (!baseType.IsAssignableFrom(targetType))
                return -1;

            var distance = 0;
            var current = targetType;
            while (current != null && current != baseType)
            {
                current = current.BaseType;
                distance++;
            }
            return current == baseType ? distance : -1;
        }

        /// <summary>
        /// Checks if a type is considered primitive (including enums, strings, decimals, dates, timespans, and GUIDs).
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type is primitive or one of the supported simple types; otherwise, <c>false</c>.</returns>
        public static bool IsPrimitive(Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid);
        }

        /// <summary>
        /// Recursively retrieves all generic type arguments from a type and its hierarchy.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="visited">Top-level call should pass null. Used internally to prevent infinite recursion.</param>
        /// <returns>An enumeration of all generic types found in the type and its base classes.</returns>
        public static IEnumerable<Type> GetGenericTypes(Type type, HashSet<int>? visited = null)
        {
            visited ??= new HashSet<int>();
            if (visited.Contains(type.GetHashCode()))
                yield break;

            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                if (genericArguments != null)
                {
                    foreach (var genericArgument in genericArguments)
                    {
                        var compositeHashCode = type.GetHashCode() ^ (genericArgument.GetHashCode() * 397);
                        if (visited.Contains(compositeHashCode))
                            continue;

                        visited.Add(compositeHashCode);
                        yield return genericArgument;

                        foreach (var nestedGenericType in GetGenericTypes(genericArgument, visited))
                            yield return nestedGenericType;
                    }
                }
            }

            if (type.BaseType == null)
                yield break;
            if (visited.Contains(type.BaseType.GetHashCode()))
                yield break;

            foreach (var baseGenericType in GetGenericTypes(type.BaseType, visited))
                yield return baseGenericType;
        }

        /// <summary>
        /// Resolves a type based on an object instance, falling back to a specified type if the object is null.
        /// </summary>
        /// <param name="obj">The object instance to determine the type from.</param>
        /// <param name="fallbackType">The type to return if <paramref name="obj"/> is null.</param>
        /// <param name="error">On failure, contains an error message describing why the type could not be resolved.</param>
        /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if resolution fails.</returns>
        public static Type? GetTypeWithObjectPriority(object? obj, Type? fallbackType, out string? error)
        {
            var type = obj?.GetType() ?? fallbackType;
            if (type == null)
            {
                error = $"Object is null and type is unknown. Provide proper {nameof(SerializedMember.typeName)}.";
                return null;
            }

            error = null;
            return type;
        }

        /// <summary>
        /// Resolves a type prioritizing the type name defined in <paramref name="member"/>, falling back to <paramref name="fallbackType"/>.
        /// </summary>
        /// <param name="member">The serialized member info usually containing the type name.</param>
        /// <param name="fallbackType">The type to return if the member type name is missing or invalid.</param>
        /// <param name="error">On failure, contains an error message describing why the type could not be resolved.</param>
        /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if resolution fails.</returns>
        public static Type? GetTypeWithNamePriority(SerializedMember? member, Type? fallbackType, out string? error)
        {
            if (StringUtils.IsNullOrEmpty(member?.typeName) && fallbackType == null)
            {
                error = $"{nameof(SerializedMember)}.{nameof(SerializedMember.typeName)} is null or empty. Provide proper {nameof(SerializedMember.typeName)}.";
                return null;
            }

            var type = GetType(member?.typeName);
            if (type == null)
            {
                if (fallbackType == null)
                {
                    error = $"Type '{member!.typeName}' not found.";
                    return null;
                }
                error = null;
                return fallbackType;
            }

            error = null;
            return type;
        }

        /// <summary>
        /// Resolves a type prioritizing the provided <paramref name="type"/>, falling back to the type definition in <paramref name="fallbackMember"/>.
        /// </summary>
        /// <param name="type">The preferred type.</param>
        /// <param name="fallbackMember">The member to resolve the type from if <paramref name="type"/> is null.</param>
        /// <param name="error">On failure, contains an error message describing why the type could not be resolved.</param>
        /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if resolution fails.</returns>
        public static Type? GetTypeWithValuePriority(Type? type, SerializedMember? fallbackMember, out string? error)
        {
            if (type == null)
            {
                if (fallbackMember == null)
                {
                    error = $"Type is unknown and {nameof(SerializedMember)}.{nameof(SerializedMember.typeName)} is null or empty.";
                    return null;
                }
                type = GetType(fallbackMember.typeName);
                if (type == null)
                {
                    error = $"Type '{fallbackMember.typeName}' not found.";
                    return null;
                }
                error = null;
            }

            error = null;
            return type;
        }
    }
}

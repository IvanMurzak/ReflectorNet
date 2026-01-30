using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        public static bool IsAssignableTo(object? obj, Type targetType)
        {
            if (targetType == null)
                return false;

            if (obj == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            return targetType.IsAssignableFrom(obj.GetType());
        }

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

/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        public static IEnumerable<Type> AllTypes => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes());

        public static Type? GetType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            // First try built-in Type.GetType() which handles many formats
            var type = Type.GetType(typeName, throwOnError: false);
            if (type != null)
                return type;

            // If Type.GetType() fails, try to find the type in all loaded assemblies
            type = AllTypes.FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName);

            return type;
        }

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

        public static string? GetDescription(Type type)
        {
            return type
                .GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description
                ?? (type.BaseType != null
                    ? GetDescription(type.BaseType!)
                    : null);
        }
        public static string? GetDescription(ParameterInfo? parameterInfo)
        {
            return parameterInfo
                ?.GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description
                ?? (parameterInfo != null
                    ? GetDescription(parameterInfo.ParameterType)
                    : null);
        }
        public static string? GetDescription(MemberInfo? memberInfo)
        {
            if (memberInfo == null)
                return null;

            var description = memberInfo
                .GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description;

            if (description != null)
                return description;

            return memberInfo.MemberType switch
            {
                MemberTypes.Field => GetFieldDescription((FieldInfo)memberInfo),
                MemberTypes.Property => GetPropertyDescription((PropertyInfo)memberInfo),
                _ => null
            };
        }
        public static string? GetFieldDescription(FieldInfo? fieldInfo)
        {
            if (fieldInfo == null)
                return null;

            return fieldInfo
                .GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description
                ?? (fieldInfo.FieldType != null
                    ? GetDescription(fieldInfo.FieldType)
                    : null);
        }
        public static string? GetPropertyDescription(PropertyInfo? propertyInfo)
        {
            if (propertyInfo == null)
                return null;

            return propertyInfo
                .GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description
                ?? (propertyInfo.PropertyType != null
                    ? GetDescription(propertyInfo.PropertyType)
                    : null);
        }
        public static string? GetPropertyDescription(Type type, string propertyName)
        {
            var propertyInfo = type.GetProperty(propertyName);
            return propertyInfo != null ? GetPropertyDescription(propertyInfo) : null;
        }
#if NETSTANDARD2_1_OR_GREATER || NET8_0_OR_GREATER
        public static string? GetPropertyDescription(System.Text.Json.Schema.JsonSchemaExporterContext context)
        {
            if (context.PropertyInfo == null || context.PropertyInfo.DeclaringType == null)
                return null;

            // First try to find the member by the exact name (in case no naming policy is applied)
            var memberInfo = context.PropertyInfo.DeclaringType
                .GetMember(
                    name: context.PropertyInfo.Name,
                    bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault();

            // If not found by exact name, try to convert camelCase back to PascalCase
            // This handles the case where JSON naming policy transforms the property name (e.g., PascalCase -> camelCase)
            if (memberInfo == null)
            {
                var pascalCaseName = ToPascalCase(context.PropertyInfo.Name);
                memberInfo = context.PropertyInfo.DeclaringType
                    .GetMember(
                        name: pascalCaseName,
                        bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault();
            }

            // If still not found, try to find by case-insensitive name match
            if (memberInfo == null)
            {
                var allMembers = context.PropertyInfo.DeclaringType.GetMembers(BindingFlags.Public | BindingFlags.Instance);
                memberInfo = allMembers.FirstOrDefault(m =>
                    string.Equals(m.Name, context.PropertyInfo.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (memberInfo == null)
                return null;

            return GetDescription(memberInfo);
        }
#endif

        private static string ToPascalCase(string camelCase)
        {
            if (string.IsNullOrEmpty(camelCase))
                return camelCase;

            return char.ToUpperInvariant(camelCase[0]) + camelCase.Substring(1);
        }
        public static string? GetFieldDescription(Type type, string fieldName)
        {
            var fieldInfo = type.GetField(fieldName);
            return fieldInfo != null ? GetFieldDescription(fieldInfo) : null;
        }

        public static bool IsCastable(Type type, Type to)
        {
            if (type == null || to == null)
                return false;

            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;

            // Handle nullable types
            to = Nullable.GetUnderlyingType(to) ?? to;

            // Check if the type is assignable to the target type
            if (to.IsAssignableFrom(type))
                return true;

            // Check for primitive types
            if (type.IsPrimitive && to.IsPrimitive)
                return true;

            // Check for string conversion
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
                        // HashCode.Combine is not available in netstandard2.0, so use a simple combination
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
        public static bool IsIEnumerable(Type type)
        {
            if (type.IsArray)
                return true; // Arrays are IEnumerable

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;

            return type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
        public static Type? GetEnumerableItemType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType(); // For arrays, return the element type

            // Check if the type itself is IEnumerable<T>
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments().FirstOrDefault();

            // Check if the type directly implements IEnumerable<T>
            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
                return enumerableInterface.GetGenericArguments().FirstOrDefault();

            // Check base types recursively
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return baseType.GetGenericArguments().FirstOrDefault();

                // Check if base type implements IEnumerable<T>
                enumerableInterface = baseType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                if (enumerableInterface != null)
                    return enumerableInterface.GetGenericArguments().FirstOrDefault();

                baseType = baseType.BaseType;
            }

            return null;
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
                    error = $"Type '{member?.typeName}' not found.";
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
                type = GetType(fallbackMember?.typeName);
                if (type == null)
                {
                    error = $"Type '{fallbackMember?.typeName}' not found.";
                    return null;
                }
                error = null;
            }

            error = null;
            return type;
        }
    }
}
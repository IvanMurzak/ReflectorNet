using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Schema;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class TypeUtils
    {
        public static Type? GetType(string? typeFullName) => string.IsNullOrEmpty(typeFullName)
            ? null
            : Type.GetType(typeFullName) ??
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeFullName);

        public static T? GetDefaultValue<T>()
            => (T?)GetDefaultValue(typeof(T));
        public static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (type.GetConstructor(Type.EmptyTypes) != null)
                return Activator.CreateInstance(type);

            return null;
        }

        public static string? GetDescription(Type type)
        {
            var descriptionAttribute = type.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute?.Description;
        }
        public static string? GetDescription(MemberInfo memberInfo)
        {
            var descriptionAttribute = memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute?.Description;
        }
        public static string? GetPropertyDescription(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return null;

            var descriptionAttribute = propertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute?.Description;
        }
        public static string? GetPropertyDescription(Type type, string propertyName)
        {
            var propertyInfo = type.GetProperty(propertyName);
            return propertyInfo != null ? GetPropertyDescription(propertyInfo) : null;
        }
        public static string? GetPropertyDescription(JsonSchemaExporterContext context)
        {
            if (context.PropertyInfo == null)
                return null;

            var memberInfo = context.PropertyInfo.DeclaringType
                .GetMember(
                    name: context.PropertyInfo.Name,
                    bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault();

            if (memberInfo == null)
                return null;

            return GetDescription(memberInfo);
        }
        public static string? GetFieldDescription(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                return null;

            var descriptionAttribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() as DescriptionAttribute;
            return descriptionAttribute?.Description;
        }
        public static string? GetFieldDescription(Type type, string fieldName)
        {
            var fieldInfo = type.GetField(fieldName);
            return fieldInfo != null ? GetFieldDescription(fieldInfo) : null;
        }

        public static object? CastTo(object obj, string typeFullName, out string? error)
        {
            var type = GetType(typeFullName) ??
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeFullName);
            if (type == null)
            {
                error = $"[Error] Type '{typeFullName ?? "null"}' not found during casting.";
                return default;
            }
            return CastTo(obj, type, out error);
        }

        public static T? CastTo<T>(object obj, out string? error)
            => CastTo(obj, typeof(T), out error) is T typedObj ? typedObj : default;

        public static object? CastTo(object obj, Type type, out string? error)
        {
            if (obj == null)
            {
                error = $"[Error] Object is null.";
                return default;
            }
            if (!type.IsAssignableFrom(obj.GetType()))
            {
                error = $"[Error] Type mismatch between '{type.FullName}' and '{obj.GetType().FullName}'.";
                return default;
            }

            error = null;
            return obj;
        }
        public static int GetInheritanceDistance(Type baseType, Type targetType)
        {
            if (!baseType.IsAssignableFrom(targetType))
                return -1;

            int distance = 0;
            Type? current = targetType;
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
                        var compositeHashCode = type.GetHashCode() ^ genericArgument.GetHashCode();
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
    }
}
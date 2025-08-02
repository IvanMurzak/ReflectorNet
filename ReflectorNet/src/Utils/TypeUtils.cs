using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Schema;
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

        public static T? CreateInstance<T>() => (T?)CreateInstance(typeof(T));
        public static object? CreateInstance(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (type.IsPrimitive)
                return Activator.CreateInstance(type);

            // Handle arrays
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == null)
                    throw new ArgumentException($"Array type '{type.FullName}' has no element type.");
                return Array.CreateInstance(elementType, 0); // Create empty array
            }

            if (type.GetConstructor(Type.EmptyTypes) != null)
                return Activator.CreateInstance(type);

            // Make empty string for string types
            if (type == typeof(string))
                return string.Empty;

            throw new ArgumentException($"Type '{type.FullName}' does not have a parameterless constructor or is not a value type or primitive type.");
            // return null;
        }

        public static T? GetDefaultValue<T>() => (T?)GetDefaultValue(typeof(T));
        public static object? GetDefaultValue(Type type)
        {
            // Handle nullable types first
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            // For value types (structs, primitives, enums), use Activator.CreateInstance
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            // For reference types (classes, interfaces, delegates), return null
            return null;


            // Older version ---------------------------------------
            // if (type.IsValueType)
            //     return Activator.CreateInstance(type);

            // if (type.IsPrimitive)
            //     return Activator.CreateInstance(type);

            // if (type.GetConstructor(Type.EmptyTypes) != null)
            //     return Activator.CreateInstance(type);

            // return null;
            // -----------------------------------------------------
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

        private static string ToPascalCase(string camelCase)
        {
            if (string.IsNullOrEmpty(camelCase))
                return camelCase;

            return char.ToUpperInvariant(camelCase[0]) + camelCase.Substring(1);
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

        public static bool IsCastable(Type type, Type to)
        {
            if (type == null || to == null)
                return false;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                type = underlyingType;

            // Handle nullable types
            var underlyingType2 = Nullable.GetUnderlyingType(to);
            if (underlyingType2 != null)
                to = underlyingType2;

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
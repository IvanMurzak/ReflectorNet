using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#if NETSTANDARD2_1_OR_GREATER || NET8_0_OR_GREATER
using System.Text.Json.Schema;
#endif

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Retrieves the description of the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type to retrieve the description for.</param>
        /// <returns>The description of the type, or the description of its base type if defined; otherwise, <see langword="null"/>.</returns>
        public static string? GetDescription(Type type)
        {
            return type
                .GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description
                ?? (type.BaseType != null
                    ? GetDescription(type.BaseType!)
                    : null);
        }

        /// <summary>
        /// Retrieves the description of the specified <see cref="ParameterInfo"/>.
        /// </summary>
        /// <param name="parameterInfo">The parameter info to retrieve the description for.</param>
        /// <returns>The description of the parameter, or the description of its parameter type if defined; otherwise, <see langword="null"/>.</returns>
        public static string? GetDescription(ParameterInfo? parameterInfo)
        {
            return parameterInfo
                ?.GetCustomAttribute<DescriptionAttribute>(true)
                ?.Description
                ?? (parameterInfo != null
                    ? GetDescription(parameterInfo.ParameterType)
                    : null);
        }

        /// <summary>
        /// Retrieves the description of the specified <see cref="MemberInfo"/>.
        /// </summary>
        /// <param name="memberInfo">The member info to retrieve the description for.</param>
        /// <returns>The description of the member, trying to fall back to field or property specific descriptions logic; otherwise <see langword="null"/>.</returns>
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

        /// <summary>
        /// Retrieves the description of the specified <see cref="FieldInfo"/>.
        /// </summary>
        /// <param name="fieldInfo">The field info to retrieve the description for.</param>
        /// <returns>The description of the field, or the description of its field type; otherwise, <see langword="null"/>.</returns>
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

        /// <summary>
        /// Retrieves the description of the specified <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="propertyInfo">The property info to retrieve the description for.</param>
        /// <returns>The description of the property, or the description of its property type; otherwise, <see langword="null"/>.</returns>
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

        /// <summary>
        /// Retrieves the description of a property specified by its name within a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type containing the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The description of the property if found; otherwise, <see langword="null"/>.</returns>
        public static string? GetPropertyDescription(Type type, string propertyName)
        {
            var propertyInfo = type.GetProperty(propertyName);
            return propertyInfo != null ? GetPropertyDescription(propertyInfo) : null;
        }

#if NETSTANDARD2_1_OR_GREATER || NET8_0_OR_GREATER
        /// <summary>
        /// Retrieves the description of a property from a <see cref="JsonSchemaExporterContext"/>.
        /// </summary>
        /// <param name="context">The JSON schema exporter context.</param>
        /// <returns>The description of the property associated with the context; otherwise, <see langword="null"/>.</returns>
        public static string? GetPropertyDescription(JsonSchemaExporterContext context)
        {
            if (context.PropertyInfo == null || context.PropertyInfo.DeclaringType == null)
                return null;

            var memberInfo = context.PropertyInfo.DeclaringType
                .GetMember(
                    name: context.PropertyInfo.Name,
                    bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault();

            if (memberInfo == null)
            {
                var pascalCaseName = ToPascalCase(context.PropertyInfo.Name);
                memberInfo = context.PropertyInfo.DeclaringType
                    .GetMember(
                        name: pascalCaseName,
                        bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault();
            }

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

        /// <summary>
        /// Retrieves the description of a field specified by its name within a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type containing the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The description of the field if found; otherwise, <see langword="null"/>.</returns>
        public static string? GetFieldDescription(Type type, string fieldName)
        {
            var fieldInfo = type.GetField(fieldName);
            return fieldInfo != null ? GetFieldDescription(fieldInfo) : null;
        }
    }
}

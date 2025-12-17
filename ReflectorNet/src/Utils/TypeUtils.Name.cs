using System;
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        public const string ArraySuffix = "[]";

        /// <summary>
        /// Returns the sanitized type name.
        /// 1. Unwraps nullable types.
        /// 2. Returns FullName.
        /// </summary>
        public static string Sanitize<T>() => Sanitize(typeof(T));

        /// <summary>
        /// Returns the sanitized type name.
        /// 1. Unwraps nullable types.
        /// 2. Returns FullName.
        /// </summary>
        public static string Sanitize(Type? type)
        {
            if (type == null)
                return StringUtils.Null;

            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type.FullName ?? StringUtils.Null;
        }
        public static string GetTypeId<T>() => GetTypeId(typeof(T));
        public static string GetTypeId(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.IsGenericParameter)
                return type.Name;

            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;

            // Special case: string is technically IEnumerable<char> but shouldn't be treated as an array
            if (type == typeof(string))
                return Sanitize(type);

            if (type.IsNested)
            {
                var declaringType = type.DeclaringType;
                if (declaringType != null)
                {
                    if (declaringType.IsGenericTypeDefinition && type.IsGenericType && !type.IsGenericTypeDefinition)
                    {
                        var allArgs = type.GetGenericArguments();
                        var declArgs = declaringType.GetGenericArguments();
                        if (allArgs.Length >= declArgs.Length)
                        {
                            var properDeclArgs = allArgs.Take(declArgs.Length).ToArray();
                            declaringType = declaringType.MakeGenericType(properDeclArgs);
                        }
                    }

                    var declaringTypeId = GetTypeId(declaringType);
                    var name = type.Name;
                    var tickIndex = name.IndexOf('`');
                    if (tickIndex > 0)
                        name = name.Substring(0, tickIndex);

                    if (type.IsGenericType)
                    {
                        var totalArgs = type.GetGenericArguments();
                        var outerArgsCount = declaringType.IsGenericType ? declaringType.GetGenericArguments().Length : 0;

                        if (totalArgs.Length > outerArgsCount)
                        {
                            var localArgs = totalArgs.Skip(outerArgsCount).Select(GetTypeId);
                            return $"{declaringTypeId}+{name}<{string.Join(",", localArgs)}>";
                        }
                    }

                    return $"{declaringTypeId}+{name}";
                }
            }

            // If type is a generic type, use its full name with generic arguments
            if (type.IsGenericType)
            {
                var genericTypeName = type.GetGenericTypeDefinition().Sanitize();
                if (StringUtils.IsNullOrEmpty(genericTypeName))
                    throw new InvalidOperationException($"Generic type '{type}' does not have a full name.");

                var tickIndex = genericTypeName.IndexOf('`');
                if (tickIndex > 0)
                    genericTypeName = genericTypeName.Substring(0, tickIndex);

                // Recursively get the type ID for each generic argument
                var genericArgs = type.GetGenericArguments().Select(GetTypeId);
                return $"{genericTypeName}<{string.Join(",", genericArgs)}>";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == null)
                    throw new InvalidOperationException($"Array type '{type}' has no element type.");

                var rank = type.GetArrayRank();
                if (rank == 1)
                    return $"{GetTypeId(elementType)}{ArraySuffix}";

                return $"{GetTypeId(elementType)}[{new string(',', rank - 1)}]";
            }

            return Sanitize(type);
        }

        public static string GetSchemaTypeId<T>() => GetTypeId(typeof(T));
        public static string GetSchemaTypeId(Type type) => GetTypeId(type);

        public static bool IsNameMatch(Type? type, string? typeName)
        {
            if (type == null || string.IsNullOrEmpty(typeName))
                return false;

            // Check if the type name matches the full name of the type
            return type.GetTypeId() == typeName;
        }


        /// <summary>
        /// Returns the type name without namespace or assembly, but includes generic arguments if any.
        /// For example: List<int>, Dictionary<string, List<double?>>
        /// </summary>
        public static string GetTypeShortName<T>() => GetTypeShortName(typeof(T));

        /// <summary>
        /// Returns the type name without namespace or assembly, but includes generic arguments if any.
        /// For example: List<int>, Dictionary<string, List<double?>>
        /// </summary>
        public static string GetTypeShortName(Type? type)
        {
            if (type == null)
                return StringUtils.Null;

            if (type.IsGenericParameter)
                return type.Name;

            // Handle nullable types
            var underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
                return $"{GetTypeShortName(underlyingNullableType)}?";

            if (type.IsNested)
            {
                var declaringType = type.DeclaringType;
                if (declaringType != null)
                {
                    if (declaringType.IsGenericTypeDefinition && type.IsGenericType && !type.IsGenericTypeDefinition)
                    {
                        var allArgs = type.GetGenericArguments();
                        var declArgs = declaringType.GetGenericArguments();
                        if (allArgs.Length >= declArgs.Length)
                        {
                            var properDeclArgs = allArgs.Take(declArgs.Length).ToArray();
                            declaringType = declaringType.MakeGenericType(properDeclArgs);
                        }
                    }

                    var declaringTypeName = GetTypeShortName(declaringType);
                    var name = type.Name;
                    var tickIndex = name.IndexOf('`');
                    if (tickIndex > 0)
                        name = name.Substring(0, tickIndex);

                    if (type.IsGenericType)
                    {
                        var totalArgs = type.GetGenericArguments();
                        var outerArgsCount = declaringType.IsGenericType ? declaringType.GetGenericArguments().Length : 0;

                        if (totalArgs.Length > outerArgsCount)
                        {
                            var localArgs = totalArgs.Skip(outerArgsCount).Select(GetTypeShortName);
                            return $"{declaringTypeName}+{name}<{string.Join(", ", localArgs)}>";
                        }
                    }

                    return $"{declaringTypeName}+{name}";
                }
            }

            if (type.IsGenericType)
            {
                var genericTypeName = type.Name;
                var tickIndex = genericTypeName.IndexOf('`');
                if (tickIndex > 0)
                    genericTypeName = genericTypeName.Substring(0, tickIndex);
                var genericArgs = type.GetGenericArguments().Select(GetTypeShortName);
                return $"{genericTypeName}<{string.Join(", ", genericArgs)}>";
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var rank = type.GetArrayRank();
                if (rank == 1)
                    return $"{GetTypeShortName(elementType)}[]";

                return $"{GetTypeShortName(elementType)}[{new string(',', rank - 1).Replace(",", ", ")}]";
            }

            return string.IsNullOrEmpty(type.Name) ? StringUtils.Null : type.Name;
        }
    }
}

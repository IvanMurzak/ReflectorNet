/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Gets all types from all loaded assemblies.
        /// </summary>
        /// <remarks>
        /// This property delegates to <see cref="AssemblyUtils.AllTypes"/> and provides
        /// exception-safe enumeration of types across all loaded assemblies.
        /// </remarks>
        public static IEnumerable<Type> AllTypes => AssemblyUtils.AllTypes;

        // Cache for resolved type names to avoid repeated AllTypes enumeration (thread-safe)
        private static readonly ConcurrentDictionary<string, Type?> _typeCache = new();

        // Cache for enumerable item types to avoid repeated interface/inheritance walks (thread-safe)
        private static readonly ConcurrentDictionary<Type, Type?> _enumerableItemTypeCache = new();

        /// <summary>
        /// Clears the type name resolution cache.
        /// </summary>
        public static void ClearTypeCache()
        {
            _typeCache.Clear();
        }

        /// <summary>
        /// Clears the enumerable item type cache.
        /// </summary>
        public static void ClearEnumerableItemTypeCache()
        {
            _enumerableItemTypeCache.Clear();
        }

        /// <summary>
        /// Resolves a <see cref="Type"/> from its string representation.
        /// </summary>
        /// <remarks>
        /// This method attempts to resolve types using multiple strategies in order:
        /// <list type="number">
        /// <item><description>Built-in <see cref="Type.GetType(string)"/></description></item>
        /// <item><description>Array type resolution (e.g., "Namespace.Type[]", "int[,]")</description></item>
        /// <item><description>C#-style generic types (e.g., "List&lt;int&gt;", "Dictionary&lt;string, int&gt;")</description></item>
        /// <item><description>CLR-style generic types (e.g., "System.Collections.Generic.List`1[[System.Int32]]")</description></item>
        /// <item><description>Search across all loaded assemblies by FullName, AssemblyQualifiedName, or TypeId</description></item>
        /// </list>
        /// Results are cached for performance. Use <see cref="ClearTypeCache"/> to clear the cache.
        /// </remarks>
        /// <param name="typeName">The type name to resolve. Can be a simple name, full name, assembly-qualified name,
        /// or C#-style generic syntax.</param>
        /// <returns>The resolved <see cref="Type"/>, or <c>null</c> if the type cannot be found.</returns>
        public static Type? GetType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            // Check cache first
            if (_typeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;

            // First try built-in Type.GetType() which handles many formats
            Type? type = null;
            try
            {
                type = Type.GetType(typeName, throwOnError: false);
            }
            catch
            {
                // Ignore exceptions (e.g. invalid assembly name) and try other resolution methods
            }

            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            // Try resolving array types (e.g., "Namespace.Type[]")
            type = TryResolveArrayType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            // Try resolving C#-style generic types (e.g., "Namespace.Generic<TypeArg>" or "Namespace.Generic<TypeArg1, TypeArg2>")
            type = TryResolveCSharpGenericType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            // Try resolving generic types (e.g., "Namespace.Generic`1[[TypeArg]]")
            type = TryResolveClassicGenericType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            // If Type.GetType() fails, try to find the type in all loaded assemblies
            type = AssemblyUtils.AllTypes.FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                typeName == t.GetTypeId());

            // Caching the result (even if null)
            _typeCache[typeName] = type;

            return type;
        }

        /// <summary>
        /// Attempts to resolve a simple (non-generic, non-array) type by name.
        /// </summary>
        private static Type? ResolveSimpleType(string name)
        {
            var type = Type.GetType(name, throwOnError: false);
            if (type != null)
                return type;

            return AssemblyUtils.AllTypes.FirstOrDefault(t =>
                name == t.AssemblyQualifiedName ||
                name == t.FullName ||
                name == t.Name);
        }

        /// <summary>
        /// Attempts to resolve array type names (e.g., "Namespace.Type[]").
        /// </summary>
        private static Type? TryResolveArrayType(string typeName)
        {
            if (!typeName.EndsWith("]"))
                return null;

            var lastOpenBracket = typeName.LastIndexOf('[');
            if (lastOpenBracket < 0)
                return null;

            var suffix = typeName.Substring(lastOpenBracket);
            // Check if content contains only commas
            var content = suffix.Substring(1, suffix.Length - 2);
            if (content.Length > 0 && content.Any(c => c != ','))
                return null;

            var commas = content.Length;
            var elementTypeName = typeName.Substring(0, lastOpenBracket);
            var elementType = GetType(elementTypeName);

            if (elementType == null) return null;

            return commas == 0
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(commas + 1);
        }

        /// <summary>
        /// Attempts to resolve C#-style generic types.
        /// Handles formats like: "Namespace.Generic&lt;TypeArg&gt;" or "Namespace.Generic&lt;TypeArg1, TypeArg2&gt;"
        /// Also handles nested types: "Namespace.Generic&lt;TypeArg&gt;+Nested" or "Namespace.Generic&lt;TypeArg&gt;+Nested&lt;TypeArg2&gt;"
        /// Space after comma is optional.
        /// </summary>
        private static Type? TryResolveCSharpGenericType(string typeName)
        {
            // Find the opening angle bracket
            var openBracketIndex = typeName.IndexOf('<');
            if (openBracketIndex < 0)
                return null;

            // Find the matching closing angle bracket
            var closeBracketIndex = FindMatchingCloseBracket(typeName, openBracketIndex);
            if (closeBracketIndex < 0)
                return null;

            // Extract the base type name (everything before '<')
            var baseTypeName = typeName.Substring(0, openBracketIndex);
            if (string.IsNullOrWhiteSpace(baseTypeName))
                return null;

            // Extract the type arguments string (between '<' and '>')
            var typeArgsString = typeName.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);

            // Parse the type arguments
            var typeArgNames = ParseCSharpGenericArguments(typeArgsString);
            if (typeArgNames == null || typeArgNames.Length == 0)
                return null;

            // Construct the generic type definition name (e.g., "Namespace.Generic`2")
            var genericDefName = $"{baseTypeName}`{typeArgNames.Length}";

            // Resolve the generic type definition
            var genericDef = ResolveSimpleType(genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            // Resolve each type argument
            var typeArgs = new Type[typeArgNames.Length];
            for (int i = 0; i < typeArgNames.Length; i++)
            {
                var argType = GetType(typeArgNames[i].Trim());
                if (argType == null)
                    return null;
                typeArgs[i] = argType;
            }

            Type? currentType;
            try
            {
                currentType = genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }

            // Handle nested types appended after the generic arguments
            var remaining = typeName.Substring(closeBracketIndex + 1);
            while (!string.IsNullOrEmpty(remaining))
            {
                if (!remaining.StartsWith("+") && !remaining.StartsWith("."))
                    return null;

                remaining = remaining.Substring(1); // Remove separator

                // Check for generic args
                var open = remaining.IndexOf('<');
                string nestedName;
                Type[]? nestedArgs = null;
                int nextRemainingIndex;

                if (open > 0)
                {
                    var close = FindMatchingCloseBracket(remaining, open);
                    if (close < 0) return null;

                    nestedName = remaining.Substring(0, open);
                    var argsStr = remaining.Substring(open + 1, close - open - 1);
                    var argNames = ParseCSharpGenericArguments(argsStr);
                    if (argNames == null) return null;

                    nestedArgs = new Type[argNames.Length];
                    for (int i = 0; i < argNames.Length; i++)
                    {
                        var tempType = GetType(argNames[i]?.Trim());
                        if (tempType == null) return null;
                        nestedArgs[i] = tempType;
                    }

                    nextRemainingIndex = close + 1;
                }
                else
                {
                    // No generic args, but check if there are more separators
                    var nextSep = remaining.IndexOfAny(new[] { '+', '.' });
                    if (nextSep > 0)
                    {
                        nestedName = remaining.Substring(0, nextSep);
                        nextRemainingIndex = nextSep;
                    }
                    else
                    {
                        nestedName = remaining;
                        nextRemainingIndex = remaining.Length;
                    }
                }

                // Find nested type
                Type? nestedType;
                Type[] allArgs;

                if (nestedArgs != null)
                {
                    nestedType = currentType.GetNestedType($"{nestedName}`{nestedArgs.Length}");
                    if (nestedType == null) return null;

                    if (currentType.IsGenericType && !currentType.IsGenericTypeDefinition)
                    {
                        var parentArgs = currentType.GetGenericArguments();
                        allArgs = new Type[parentArgs.Length + nestedArgs.Length];
                        Array.Copy(parentArgs, allArgs, parentArgs.Length);
                        Array.Copy(nestedArgs, 0, allArgs, parentArgs.Length, nestedArgs.Length);
                    }
                    else
                    {
                        allArgs = nestedArgs;
                    }
                }
                else
                {
                    nestedType = currentType.GetNestedType(nestedName);
                    if (nestedType == null) return null;

                    allArgs = currentType.IsGenericType && !currentType.IsGenericTypeDefinition
                        ? currentType.GetGenericArguments()
                        : Type.EmptyTypes;
                }

                if (nestedType.IsGenericTypeDefinition)
                {
                    try
                    {
                        currentType = nestedType.GetGenericArguments().Length == allArgs.Length
                            ? nestedType.MakeGenericType(allArgs)
                            : nestedType;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    currentType = nestedType;
                }

                remaining = remaining.Substring(nextRemainingIndex);
            }

            return currentType;
        }

        /// <summary>
        /// Finds the matching closing angle bracket for an opening bracket.
        /// Handles nested generic types properly.
        /// </summary>
        private static int FindMatchingCloseBracket(string typeName, int openIndex)
        {
            var depth = 0;
            for (int i = openIndex; i < typeName.Length; i++)
            {
                if (typeName[i] == '<')
                    depth++;
                else if (typeName[i] == '>')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Parses C#-style generic arguments from a string like "TypeArg1, TypeArg2" or "List&lt;int&gt;, string".
        /// Handles nested generic types by tracking bracket depth.
        /// </summary>
        private static string[]? ParseCSharpGenericArguments(string argsString)
        {
            if (string.IsNullOrWhiteSpace(argsString))
                return null;

            var args = new List<string>();
            var depth = 0;
            var currentArg = new System.Text.StringBuilder();

            for (int i = 0; i < argsString.Length; i++)
            {
                var c = argsString[i];

                if (c == '<')
                {
                    depth++;
                    currentArg.Append(c);
                }
                else if (c == '>')
                {
                    depth--;
                    currentArg.Append(c);
                }
                else if (c == ',' && depth == 0)
                {
                    // Top-level comma - separator between type arguments
                    var arg = currentArg.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg))
                        args.Add(arg);
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            // Add the last argument
            var lastArg = currentArg.ToString().Trim();
            if (!string.IsNullOrEmpty(lastArg))
                args.Add(lastArg);

            return args.Count > 0 ? args.ToArray() : null;
        }

        /// <summary>
        /// Attempts to resolve constructed generic types by parsing and reconstructing them.
        /// Handles formats like: "Namespace.Generic`1[[TypeArg, Assembly]]"
        /// </summary>
        private static Type? TryResolveClassicGenericType(string typeName)
        {
            // Find generic arity marker (backtick)
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex < 0)
                return null;

            // Find the start of generic arguments [[...]]
            var argsStart = typeName.IndexOf("[[", backtickIndex);
            if (argsStart < 0)
                return null;

            // Extract generic definition name (e.g., "Namespace.WrapperClass`1")
            var genericDefName = typeName.Substring(0, argsStart);

            // Resolve the generic type definition
            var genericDef = ResolveSimpleType(genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            // Parse and resolve type arguments
            var typeArgs = ParseGenericArguments(typeName, argsStart);
            if (typeArgs == null || typeArgs.Length != genericDef.GetGenericArguments().Length)
                return null;

            try
            {
                return genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses generic arguments from the [[Type1, Assembly], [Type2, Assembly]] format.
        /// The format uses double brackets where:
        /// - Outer [] wraps all type arguments
        /// - Inner [] wraps each individual type argument
        /// - Nested generic types have their own [[]] inside
        /// </summary>
        private static Type[]? ParseGenericArguments(string typeName, int startIndex)
        {
            var args = new List<Type>();
            var depth = 0;
            var currentArg = new System.Text.StringBuilder();

            for (int i = startIndex; i < typeName.Length; i++)
            {
                var c = typeName[i];

                if (c == '[')
                {
                    depth++;
                    // Only append brackets for nested generics (depth > 2)
                    // depth 1 = outer wrapper for all args
                    // depth 2 = wrapper for individual type arg (don't include)
                    // depth 3+ = nested generic brackets (include)
                    if (depth > 2)
                        currentArg.Append(c);
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 1)
                    {
                        // End of one type argument
                        var argTypeName = currentArg.ToString().Trim();
                        if (!string.IsNullOrEmpty(argTypeName))
                        {
                            var argType = GetType(argTypeName);
                            if (argType == null)
                                return null;
                            args.Add(argType);
                        }
                        currentArg.Clear();
                    }
                    else if (depth > 1)
                    {
                        // Append closing brackets for nested generics
                        currentArg.Append(c);
                    }
                    else if (depth == 0)
                    {
                        break; // End of all arguments
                    }
                }
                else if (c == ',' && depth == 1)
                {
                    // Separator between type arguments at the top level - skip it
                }
                else if (depth > 1)
                {
                    currentArg.Append(c);
                }
            }

            return args.Count > 0 ? args.ToArray() : null;
        }

        /// <summary>
        /// Determines whether the specified type is a dictionary type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type is <see cref="Dictionary{TKey, TValue}"/>,
        /// <see cref="IDictionary{TKey, TValue}"/>, or implements <see cref="IDictionary{TKey, TValue}"/>.</returns>
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
        /// Gets the key and value types from a dictionary type.
        /// </summary>
        /// <param name="type">The dictionary type to inspect.</param>
        /// <returns>An array containing [TKey, TValue] types, or <c>null</c> if the type is not a dictionary.</returns>
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
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a type.
        /// </summary>
        /// <param name="type">The type to get the description from.</param>
        /// <returns>The description string, or <c>null</c> if no description is found.
        /// Falls back to the base type's description if the type itself has none.</returns>
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
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a parameter.
        /// </summary>
        /// <param name="parameterInfo">The parameter to get the description from.</param>
        /// <returns>The description string, or <c>null</c> if no description is found.
        /// Falls back to the parameter type's description if the parameter itself has none.</returns>
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
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a member.
        /// </summary>
        /// <param name="memberInfo">The member to get the description from.</param>
        /// <returns>The description string, or <c>null</c> if no description is found.
        /// For fields and properties, falls back to the member type's description.</returns>
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
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a field.
        /// </summary>
        /// <param name="fieldInfo">The field to get the description from.</param>
        /// <returns>The description string, or <c>null</c> if no description is found.
        /// Falls back to the field type's description if the field itself has none.</returns>
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
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a property.
        /// </summary>
        /// <param name="propertyInfo">The property to get the description from.</param>
        /// <returns>The description string, or <c>null</c> if no description is found.
        /// Falls back to the property type's description if the property itself has none.</returns>
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
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a property by name.
        /// </summary>
        /// <param name="type">The type containing the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The description string, or <c>null</c> if the property is not found or has no description.</returns>
        public static string? GetPropertyDescription(Type type, string propertyName)
        {
            var propertyInfo = type.GetProperty(propertyName);
            return propertyInfo != null ? GetPropertyDescription(propertyInfo) : null;
        }
#if NETSTANDARD2_1_OR_GREATER || NET8_0_OR_GREATER
        /// <summary>
        /// Gets the description from a <see cref="DescriptionAttribute"/> using JSON schema exporter context.
        /// </summary>
        /// <remarks>
        /// This method handles JSON naming policy transformations by attempting to match:
        /// <list type="number">
        /// <item><description>Exact name match</description></item>
        /// <item><description>PascalCase conversion from camelCase</description></item>
        /// <item><description>Case-insensitive match</description></item>
        /// </list>
        /// </remarks>
        /// <param name="context">The JSON schema exporter context containing property information.</param>
        /// <returns>The description string, or <c>null</c> if no description is found.</returns>
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

        /// <summary>
        /// Gets the description from a <see cref="DescriptionAttribute"/> on a field by name.
        /// </summary>
        /// <param name="type">The type containing the field.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The description string, or <c>null</c> if the field is not found or has no description.</returns>
        public static string? GetFieldDescription(Type type, string fieldName)
        {
            var fieldInfo = type.GetField(fieldName);
            return fieldInfo != null ? GetFieldDescription(fieldInfo) : null;
        }

        /// <summary>
        /// Checks if an object's runtime type is assignable to the target type.
        /// This is a cross-platform alternative to Type.IsAssignableTo which is only available in .NET 5+.
        /// </summary>
        /// <param name="obj">The object to check (can be null)</param>
        /// <param name="targetType">The target type to check assignability to</param>
        /// <returns>True if the object can be assigned to the target type</returns>
        public static bool IsAssignableTo(object? obj, Type targetType)
        {
            if (targetType == null)
                return false;

            // Null is assignable to any reference type or nullable value type
            if (obj == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            // Check if the object's type is assignable to the target type
            return targetType.IsAssignableFrom(obj.GetType());
        }

        /// <summary>
        /// Determines whether a type can be cast to another type.
        /// </summary>
        /// <remarks>
        /// This method checks for:
        /// <list type="bullet">
        /// <item><description>Direct assignability (including inheritance)</description></item>
        /// <item><description>Primitive type conversions</description></item>
        /// <item><description>String to object conversion</description></item>
        /// </list>
        /// Nullable types are unwrapped before comparison.
        /// </remarks>
        /// <param name="type">The source type to cast from.</param>
        /// <param name="to">The target type to cast to.</param>
        /// <returns><c>true</c> if the cast is possible; otherwise, <c>false</c>.</returns>
        public static bool IsCastable(Type? type, Type to)
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

        /// <summary>
        /// Calculates the inheritance distance between two types.
        /// </summary>
        /// <param name="baseType">The base type (ancestor).</param>
        /// <param name="targetType">The target type (descendant).</param>
        /// <returns>The number of inheritance levels between the types, or -1 if <paramref name="targetType"/>
        /// does not inherit from <paramref name="baseType"/>.</returns>
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
        /// Determines whether a type is considered a primitive type for serialization purposes.
        /// </summary>
        /// <remarks>
        /// This includes CLR primitives, enums, and common value types:
        /// <see cref="string"/>, <see cref="decimal"/>, <see cref="DateTime"/>,
        /// <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/>, and <see cref="Guid"/>.
        /// </remarks>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type is a primitive or primitive-like type.</returns>
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
        /// Recursively enumerates all generic type arguments from a type and its base types.
        /// </summary>
        /// <param name="type">The type to extract generic arguments from.</param>
        /// <param name="visited">Optional set to track visited types and prevent infinite recursion.</param>
        /// <returns>An enumerable of all generic type arguments found in the type hierarchy.</returns>
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

        /// <summary>
        /// Determines whether a type implements <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type is an array or implements <see cref="IEnumerable{T}"/>.</returns>
        public static bool IsIEnumerable(Type type)
        {
            if (type.IsArray)
                return true; // Arrays are IEnumerable

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;

            return type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        /// <summary>
        /// Gets the element type of an enumerable or array type.
        /// Results are cached for performance when processing large collections.
        /// </summary>
        /// <param name="type">The enumerable type to inspect.</param>
        /// <returns>The element type (T in <see cref="IEnumerable{T}"/>), or <c>null</c> if the type is not enumerable.</returns>
        public static Type? GetEnumerableItemType(Type type)
        {
            return _enumerableItemTypeCache.GetOrAdd(type, GetEnumerableItemTypeInternal);
        }

        /// <summary>
        /// Internal implementation of GetEnumerableItemType without caching.
        /// </summary>
        private static Type? GetEnumerableItemTypeInternal(Type type)
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

        /// <summary>
        /// Resolves a type with priority given to the object's runtime type.
        /// </summary>
        /// <param name="obj">The object whose runtime type to use (if not null).</param>
        /// <param name="fallbackType">The fallback type to use if the object is null.</param>
        /// <param name="error">Set to an error message if the type cannot be resolved; otherwise, <c>null</c>.</param>
        /// <returns>The resolved type, or <c>null</c> if resolution fails.</returns>
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
        /// Resolves a type with priority given to the <see cref="SerializedMember.typeName"/>.
        /// </summary>
        /// <param name="member">The serialized member containing the type name.</param>
        /// <param name="fallbackType">The fallback type to use if the type name cannot be resolved.</param>
        /// <param name="error">Set to an error message if the type cannot be resolved; otherwise, <c>null</c>.</param>
        /// <returns>The resolved type, or <c>null</c> if resolution fails.</returns>
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

        /// <summary>
        /// Resolves a type with priority given to the provided type parameter.
        /// </summary>
        /// <param name="type">The primary type to use (if not null).</param>
        /// <param name="fallbackMember">The fallback serialized member to extract type name from.</param>
        /// <param name="error">Set to an error message if the type cannot be resolved; otherwise, <c>null</c>.</param>
        /// <returns>The resolved type, or <c>null</c> if resolution fails.</returns>
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

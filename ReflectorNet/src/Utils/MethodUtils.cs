/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /// <summary>
    /// Utility methods for working with MethodInfo and method reflection.
    /// </summary>
    public static class MethodUtils
    {
        // Cache the NullableContextAttribute type for performance
        // This is a compiler-generated internal attribute, so we must access it by name
#if NET8_0_OR_GREATER
        private static readonly Type NullableContextAttributeType = typeof(System.Runtime.CompilerServices.NullableContextAttribute);
#elif NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER // Maybe some environment would support it
        private static readonly Type? NullableContextAttributeType = Type.GetType("System.Runtime.CompilerServices.NullableContextAttribute, System.Private.CoreLib");
#endif

        // NullableContext attribute values
        private const byte NullableContextOblivious = 0; // No annotation - treat as non-nullable
        private const byte NullableContextNotNull = 1;   // Non-nullable
        private const byte NullableContextNullable = 2;  // Nullable

        /// <summary>
        /// Determines whether the return type of a method is nullable.
        /// Handles generic type parameters (T vs T?), value types, reference types, and async wrappers.
        /// </summary>
        /// <param name="methodInfo">The method to check.</param>
        /// <returns>True if the return type is nullable, false otherwise.</returns>
        public static bool IsReturnTypeNullable(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var returnType = methodInfo.ReturnType;

            // Check if it's an async wrapper (Task<T> or ValueTask<T>)
            var isAsyncWrapper = returnType.IsGenericType &&
                (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                 returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

            var isNullable = false;
            var nullabilityDeterminedFromGenericParameter = false;

#if NET5_0_OR_GREATER
            // First, check if the async wrapper itself (Task or ValueTask) is nullable (e.g., Task<int>?)
            // This must be checked BEFORE unwrapping
            if (isAsyncWrapper && !returnType.IsValueType)
            {
                try
                {
                    var nullabilityContext = new NullabilityInfoContext();
                    var nullabilityInfo = nullabilityContext.Create(methodInfo.ReturnParameter);

                    // If the wrapper itself is nullable (e.g., Task<int>?), return true immediately
                    if (nullabilityInfo.ReadState == NullabilityState.Nullable)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // If we can't determine nullability, continue with other checks
                }
            }
#endif

            // Unwrap Task<T>/ValueTask<T> to get the inner T
            var unwrappedType = isAsyncWrapper
                ? returnType.GetGenericArguments()[0]
                : returnType;

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            // First, check if this is a method on a generic type - we need to inspect the generic definition
            // to correctly determine nullability for generic type parameters (T vs T?)
            MethodInfo? methodToInspect = null;
            Type? originalReturnType = null;

            if (methodInfo.DeclaringType != null && methodInfo.DeclaringType.IsGenericType && !methodInfo.DeclaringType.IsGenericTypeDefinition)
            {
                // This is a method on a constructed generic type (e.g., WrapperClass<int>)
                // Get the generic type definition (e.g., WrapperClass<T>)
                var genericTypeDefinition = methodInfo.DeclaringType.GetGenericTypeDefinition();
                // Get the corresponding method from the generic type definition
                var genericMethod = genericTypeDefinition.GetMethod(methodInfo.Name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (genericMethod != null)
                {
                    methodToInspect = genericMethod;
                    originalReturnType = genericMethod.ReturnType;
                }
            }

            // Check if the generic definition has a nullable generic type parameter (T?)
            // This must be checked BEFORE the value type check because T? where T=int appears as int (value type)
            if (methodToInspect != null && originalReturnType != null)
            {
                // Unwrap async return types to get the actual type
                var typeToCheck = originalReturnType;
                if (isAsyncWrapper && typeToCheck.IsGenericType)
                    typeToCheck = typeToCheck.GetGenericArguments()[0];

                // ONLY check nullability context if the return type in the generic definition is a generic parameter
                // For example, WrapperClass<T>.Echo returns T (generic parameter) - check this
                if (typeToCheck.IsGenericParameter)
                {
                    try
                    {
                        // The C# compiler emits:
                        // - NullableContextAttribute(1) on methods with non-nullable return types (T)
                        // - No attribute (or inherited context) for nullable return types (T?)
                        var hasNullableContextAttr = false;
                        var nullableContextValue = NullableContextNotNull; // Default to non-nullable

                        // Check method-level NullableContextAttribute
                        var methodAttrs = methodToInspect.GetCustomAttributesData();
                        foreach (var attr in methodAttrs)
                        {
                            if (NullableContextAttributeType != null && attr.AttributeType == NullableContextAttributeType)
                            {
                                hasNullableContextAttr = true;
                                if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is byte value)
                                    nullableContextValue = value;
                                break;
                            }
                        }

                        // If no method-level attribute, check the declaring type
                        if (!hasNullableContextAttr && methodToInspect.DeclaringType != null)
                        {
                            var typeAttrs = methodToInspect.DeclaringType.GetCustomAttributesData();
                            foreach (var attr in typeAttrs)
                            {
                                if (NullableContextAttributeType != null && attr.AttributeType == NullableContextAttributeType)
                                {
                                    if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is byte value)
                                        nullableContextValue = value;
                                    break;
                                }
                            }
                        }

                        // Check if the context value indicates nullable
                        isNullable = nullableContextValue == NullableContextNullable;
                        nullabilityDeterminedFromGenericParameter = true;
                    }
                    catch (Exception)
                    {
                        // If we can't determine nullability, assume not nullable
                        isNullable = false;
                    }
                }
            }
#endif

            // Check for Nullable<T> (value types like int?, DateTime?)
            if (!isNullable)
            {
                var nullableUnderlyingType = Nullable.GetUnderlyingType(unwrappedType);
                if (nullableUnderlyingType != null)
                {
                    // This handles Nullable<T> for value types (e.g., Task<int?>, int?)
                    isNullable = true;
                }
            }

            // Only check reference type nullability if we haven't already determined it from a generic parameter
            if (!nullabilityDeterminedFromGenericParameter && !unwrappedType.IsValueType)
            {
#if NET5_0_OR_GREATER
                try
                {
                    // For concrete reference types (non-generic parameters), use NullabilityInfoContext
                    var nullabilityContext = new NullabilityInfoContext();
                    var nullabilityInfo = nullabilityContext.Create(methodInfo.ReturnParameter);

                    isNullable = nullabilityInfo.ReadState == NullabilityState.Nullable;

                    // If the return type is Task<T> or ValueTask<T>, check the generic argument nullability
                    if (isAsyncWrapper)
                    {
                        // Check the nullability of the T inside Task<T> or ValueTask<T>
                        if (nullabilityInfo.GenericTypeArguments.Length > 0)
                            isNullable |= nullabilityInfo.GenericTypeArguments[0].ReadState == NullabilityState.Nullable;
                    }
                }
                catch (Exception)
                {
                    // If we can't determine nullability, assume not nullable
                    isNullable = false;
                }
#else
                // For .NET Standard 2.1 and earlier, we cannot determine reference type nullability
                // Assume not nullable by default
                isNullable = false;
#endif
            }

            return isNullable;
        }

        /// <summary>
        /// Whether a parameter accepts null: a <see cref="Nullable{T}"/> value type (<c>int?</c>), or a reference
        /// type annotated nullable (<c>string?</c>) in a nullable-enabled context. A reference type in a
        /// nullable-oblivious context, and any non-nullable value type, is not. Reads the compiler's nullable
        /// attributes by name, so it works on every target framework.
        /// </summary>
        public static bool IsParameterNullable(ParameterInfo parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            var type = parameter.ParameterType;
            if (type.IsByRef)
                type = type.GetElementType()!;
            if (Nullable.GetUnderlyingType(type) != null)
                return true;
            if (type.IsValueType)
                return false;

            // The parameter's own [Nullable] wins; without one it inherits [NullableContext] from the method, then
            // from the declaring type and the types that enclose it.
            var flag = ReadNullableFlag(parameter.GetCustomAttributesData(), NullableAttributeName);
            for (var member = parameter.Member; flag == null && member != null; member = member.DeclaringType)
                flag = ReadNullableFlag(member.GetCustomAttributesData(), NullableContextAttributeName);
            return flag == NullableContextNullable;
        }

        private const string NullableAttributeName = "System.Runtime.CompilerServices.NullableAttribute";
        private const string NullableContextAttributeName = "System.Runtime.CompilerServices.NullableContextAttribute";

        /// <summary>
        /// The top-level nullable flag of the named compiler attribute (its <c>byte</c> argument, or the first
        /// element of its <c>byte[]</c>), or null when the attribute is absent. Matched by name: each assembly may
        /// embed its own copy of these attributes.
        /// </summary>
        private static byte? ReadNullableFlag(System.Collections.Generic.IList<CustomAttributeData> attributes, string attributeName)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeType.FullName != attributeName || attribute.ConstructorArguments.Count == 0)
                    continue;
                var argument = attribute.ConstructorArguments[0].Value;
                if (argument is byte flag)
                    return flag;
                if (argument is System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument> flags && flags.Count > 0 && flags[0].Value is byte first)
                    return first;
                return null;
            }
            return null;
        }
    }
}

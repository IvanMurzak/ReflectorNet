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

#if NET5_0_OR_GREATER
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
                        var nullableContextValue = (byte)1; // Default to non-nullable

                        // Check method-level NullableContextAttribute
                        var methodAttrs = methodToInspect.GetCustomAttributesData();
                        foreach (var attr in methodAttrs)
                        {
                            if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute")
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
                                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute")
                                {
                                    if (attr.ConstructorArguments.Count > 0 && attr.ConstructorArguments[0].Value is byte value)
                                        nullableContextValue = value;
                                    break;
                                }
                            }
                        }

                        // NullableContext values:
                        // 0 = Oblivious (no annotation) - treat as non-nullable
                        // 1 = NotNull
                        // 2 = Nullable
                        isNullable = nullableContextValue == 2;
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
                // For .NET Standard 2.0 and earlier, we cannot determine reference type nullability
                // Assume not nullable by default
                isNullable = false;
#endif
            }

            return isNullable;
        }
    }
}

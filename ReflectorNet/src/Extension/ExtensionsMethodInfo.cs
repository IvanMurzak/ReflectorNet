/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsMethodInfo
    {
        public static MethodInfo? FilterByParameters(this IEnumerable<MethodInfo> methods, SerializedMemberList? parameters = null)
        {
            if (parameters == null || parameters.Count == 0)
                return methods.FirstOrDefault(m => m.GetParameters().Length == 0);

            return methods.FirstOrDefault(method =>
            {
                var methodParameters = method.GetParameters();
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    var methodParameter = methodParameters[i];
                    if (i >= parameters.Count)
                    {
                        if (methodParameter.IsOptional)
                            break;
                        return false;
                    }
                    var parameter = parameters[i];

                    if (methodParameter.Name != parameter.name || methodParameter.ParameterType != TypeUtils.GetType(parameter.typeName))
                        return false;
                }
                return true;
            });
        }
    }
}
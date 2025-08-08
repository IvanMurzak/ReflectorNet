/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsMethodPointerRef
    {
        public static void EnhanceInputParameters(this MethodRef? methodPointer, SerializedMemberList? parameters = null)
        {
            if (methodPointer == null)
                return;

            methodPointer.InputParameters ??= new List<MethodRef.Parameter>();

            if (parameters == null || parameters.Count == 0)
                return;

            foreach (var parameter in parameters)
            {
                var methodParameter = methodPointer.InputParameters.FirstOrDefault(p => p.Name == parameter.name);
                if (methodParameter == null)
                {
                    methodPointer.InputParameters.Add(new MethodRef.Parameter(
                        typeName: parameter.typeName,
                        name: parameter.name
                    ));
                }
                else
                {
                    methodParameter.TypeName = parameter.typeName;
                }
            }
        }
    }
}
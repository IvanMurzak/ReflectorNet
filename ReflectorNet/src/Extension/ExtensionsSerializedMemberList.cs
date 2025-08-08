/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsSerializedMemberList
    {
        public static bool IsValidTypeNames(this SerializedMemberList? parameters, string fieldName, out string? error)
        {
            if (parameters == null || parameters.Count == 0)
            {
                error = null;
                return true;
            }

            var result = true;
            var stringBuilder = new StringBuilder();

            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (string.IsNullOrEmpty(parameter.typeName))
                {
                    stringBuilder.AppendLine($"[Error] {fieldName}[{i}].{nameof(parameter.typeName)} is empty. Please specify the '{nameof(parameter.name)}' properly.");
                    result = false;
                    continue;
                }

                var parameterType = TypeUtils.GetType(parameter.typeName);
                if (parameterType == null)
                {
                    stringBuilder.AppendLine($"[Error] {fieldName}[{i}].{nameof(parameter.typeName)} type '{parameter.typeName}' not found. Please specify the '{nameof(parameter.name)}' properly.");
                    result = false;
                    continue;
                }
            }

            error = stringBuilder.ToString();

            if (string.IsNullOrEmpty(error))
                error = null;

            return result;
        }

        public static void EnhanceNames(this SerializedMemberList? parameters, MethodInfo method)
        {
            if (parameters == null || parameters.Count == 0)
                return;

            var methodParameters = method.GetParameters();

            for (int i = 0; i < parameters.Count && i < methodParameters.Length; i++)
            {
                var parameter = parameters[i];
                if (string.IsNullOrEmpty(parameter.name))
                {
                    var methodParameter = methodParameters[i];
                    parameter.name = methodParameter.Name;
                }
            }
        }

        public static void EnhanceTypes(this SerializedMemberList? parameters, MethodInfo method)
        {
            if (parameters == null || parameters.Count == 0)
                return;

            var methodParameters = method.GetParameters();

            for (int i = 0; i < parameters.Count && i < methodParameters.Length; i++)
            {
                var parameter = parameters[i];
                if (string.IsNullOrEmpty(parameter.typeName))
                {
                    var methodParameter = methodParameters[i];
                    var typeName = methodParameter?.ParameterType?.GetTypeName(pretty: false);
                    if (typeName == null)
                        continue;
                    parameter.typeName = typeName;
                }
            }
        }
    }
}
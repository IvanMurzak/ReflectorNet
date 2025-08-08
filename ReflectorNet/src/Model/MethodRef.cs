/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Model
{
    [Description(@"Method reference. Used to find method in codebase of the project.")]
    public class MethodRef
    {
        [JsonInclude, JsonPropertyName("namespace")]
        [Description("Namespace of the class. It may be empty if the class is in the global namespace or the namespace is unknown.")]
        public string? Namespace { get; set; }

        [JsonInclude, JsonPropertyName("typeName")]
        [Description("Class name, or substring a class name. It may be empty if the class is unknown.")]
        public string TypeName { get; set; } = string.Empty;

        [JsonInclude, JsonPropertyName("methodName")]
        [Description("Method name, or substring of the method name. It may be empty if the method is unknown.")]
        public string MethodName { get; set; } = string.Empty;

        [JsonInclude, JsonPropertyName("inputParameters")]
        [Description("List of input parameters. Can be null if the method has no parameters or the parameters are unknown.")]
        public List<Parameter>? InputParameters { get; set; }

        [JsonIgnore]
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(TypeName))
                    return false;
                if (string.IsNullOrEmpty(MethodName))
                    return false;

                if (InputParameters != null && InputParameters.Count > 0)
                {
                    foreach (var parameter in InputParameters)
                    {
                        if (parameter == null)
                            return false;
                        if (string.IsNullOrEmpty(parameter.TypeName))
                            return false;
                        if (string.IsNullOrEmpty(parameter.Name))
                            return false;
                    }
                }
                return true;
            }
        }

        public MethodRef() { }
        public MethodRef(MethodInfo methodInfo)
        {
            Namespace = methodInfo.DeclaringType?.Namespace;
            TypeName = methodInfo.DeclaringType?.Name ?? string.Empty;
            MethodName = methodInfo.Name;
            InputParameters = methodInfo.GetParameters()
                ?.Select(parameter => new Parameter(parameter))
                ?.ToList();
        }
        public MethodRef(PropertyInfo methodInfo)
        {
            Namespace = methodInfo.DeclaringType?.Namespace;
            TypeName = methodInfo.DeclaringType?.Name ?? string.Empty;
            MethodName = methodInfo.Name;
            InputParameters = null;
        }

        public override string ToString() => InputParameters == null
            ? string.IsNullOrEmpty(Namespace)
                ? $"{TypeName}.{MethodName}()"
                : $"{Namespace}.{TypeName}.{MethodName}()"
            : string.IsNullOrEmpty(Namespace)
                ? $"{TypeName}.{MethodName}({string.Join(", ", InputParameters)})"
                : $"{Namespace}.{TypeName}.{MethodName}({string.Join(", ", InputParameters)})";

        [Description("Parameter of a method. Contains type and name of the parameter.")]
        public class Parameter
        {
            [JsonInclude, JsonPropertyName("typeName")]
            [Description("Type of the parameter including namespace. Sample: 'System.String', 'System.Int32', 'UnityEngine.GameObject', etc.")]
            public string? TypeName { get; set; }

            [JsonInclude, JsonPropertyName("name")]
            [Description("Name of the parameter. It may be empty if the name is unknown.")]
            public string? Name { get; set; }

            public Parameter() { }
            public Parameter(string typeName, string? name)
            {
                this.TypeName = typeName;
                this.Name = name;
            }
            public Parameter(ParameterInfo parameter)
            {
                TypeName = parameter.ParameterType.GetTypeName(pretty: false);
                Name = parameter.Name;
            }
            public override string ToString()
            {
                return $"{TypeName} {Name}";
            }
        }
    }
}
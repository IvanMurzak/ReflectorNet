#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Data.Unity
{
    [Description(@"Method reference. Used to find method in codebase of the project.
'namespace' (string) - namespace of the class. It may be empty if the class is in the global namespace or the namespace is unknown.
'typeName' (string) - class name. Or substring of the class name.
'methodName' (string) - method name. Or substring of the method name.
'inputParameters' (List<Parameter>) - list of parameters. Each parameter is represented by a 'Parameter' object.

'Parameter' object contains two fields:
'typeName' (string) - type of the parameter including namespace. Sample: 'System.String', 'System.Int32', 'UnityEngine.GameObject', etc.
'name' (string) - name of the parameter. It may be empty if the name is unknown.")]
    public class MethodPointerRef
    {
        public string? Namespace { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
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

        public MethodPointerRef() { }
        public MethodPointerRef(MethodInfo methodInfo)
        {
            Namespace = methodInfo.DeclaringType?.Namespace;
            TypeName = methodInfo.DeclaringType?.Name ?? string.Empty;
            MethodName = methodInfo.Name;
            InputParameters = methodInfo.GetParameters()
                ?.Select(parameter => new Parameter(parameter))
                ?.ToList();
        }
        public MethodPointerRef(PropertyInfo methodInfo)
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

        public class Parameter
        {
            public string? TypeName { get; set; }
            public string? Name { get; set; }

            public Parameter() { }
            public Parameter(string typeName, string? name)
            {
                this.TypeName = typeName;
                this.Name = name;
            }
            public Parameter(ParameterInfo parameter)
            {
                TypeName = parameter.ParameterType.FullName;
                Name = parameter.Name;
            }
            public override string ToString()
            {
                return $"{TypeName} {Name}";
            }
        }
    }
}
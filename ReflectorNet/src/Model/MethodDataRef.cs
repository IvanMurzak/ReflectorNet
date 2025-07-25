using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Model
{
    [Description(@"Method reference. Used to find method in codebase of the project.
'namespace' (string) - namespace of the class. It may be empty if the class is in the global namespace or the namespace is unknown.
'typeName' (string) - class name. Or substring of the class name.
'methodName' (string) - method name. Or substring of the method name.
'inputParameters' (List<Parameter>) - list of parameters. Each parameter is represented by a 'Parameter' object.

'Parameter' object contains two fields:
'typeName' (string) - type of the parameter including namespace. Sample: 'System.String', 'System.Int32', 'UnityEngine.GameObject', etc.
'name' (string) - name of the parameter. It may be empty if the name is unknown.")]
    public class MethodDataRef : MethodPointerRef
    {
        public bool IsPublic { get; set; }
        public bool IsStatic { get; set; }
        public string? ReturnType { get; set; }
        public JsonNode? ReturnSchema { get; set; }
        public List<JsonNode>? InputParametersSchema { get; set; }

        public MethodDataRef() : base() { }
        public MethodDataRef(MethodInfo methodInfo, bool justRef = false) : base(methodInfo)
        {
            IsStatic = methodInfo.IsStatic;
            IsPublic = methodInfo.IsPublic;
            ReturnType = methodInfo.ReturnType.GetTypeName(pretty: false);
            ReturnSchema = methodInfo.ReturnType == typeof(void)
                ? null
                : JsonUtils.Schema.GetSchema(methodInfo.ReturnType, justRef: justRef);
            InputParametersSchema = methodInfo.GetParameters()
                ?.Select(parameter => JsonUtils.Schema.GetSchema(parameter.ParameterType, justRef: justRef)!)
                ?.ToList();
        }
    }
}
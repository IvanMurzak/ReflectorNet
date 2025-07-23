using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsMethodPointerRef
    {
        public static void EnhanceInputParameters(this MethodPointerRef? methodPointer, SerializedMemberList? parameters = null)
        {
            if (methodPointer == null)
                return;

            methodPointer.InputParameters ??= new List<MethodPointerRef.Parameter>();

            if (parameters == null || parameters.Count == 0)
                return;

            foreach (var parameter in parameters)
            {
                var methodParameter = methodPointer.InputParameters.FirstOrDefault(p => p.Name == parameter.name);
                if (methodParameter == null)
                {
                    methodPointer.InputParameters.Add(new MethodPointerRef.Parameter(
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
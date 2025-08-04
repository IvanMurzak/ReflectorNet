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
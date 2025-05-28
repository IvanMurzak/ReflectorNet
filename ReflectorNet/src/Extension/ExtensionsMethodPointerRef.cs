using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Data.Unity;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsMethodPointerRef
    {
        public static void EnhanceInputParameters(this MethodPointerRef? methodPointer, SerializedMemberList? parameters = null)
        {
            if (methodPointer == null)
                return;

            if (parameters == null || parameters.Count == 0)
                return;

            methodPointer.InputParameters ??= new List<MethodPointerRef.Parameter>();

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
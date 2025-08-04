using System;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        public string MethodCall
        (
            Reflector reflector,
            MethodRef filter,
            bool knownNamespace = false,
            int typeNameMatchLevel = 1,
            int methodNameMatchLevel = 1,
            int parametersMatchLevel = 2,
            SerializedMember? targetObject = null,
            SerializedMemberList? inputParameters = null,
            bool executeInMainThread = true,
            ILogger? logger = null
        )
        {
            // Enhance filter with input parameters if no input parameters specified in the filter.
            if ((filter.InputParameters?.Count ?? 0) == 0 && (inputParameters?.Count ?? 0) > 0)
                filter.EnhanceInputParameters(inputParameters);

            var methodEnumerable = FindMethod(
                filter: filter,
                knownNamespace: knownNamespace,
                typeNameMatchLevel: typeNameMatchLevel,
                methodNameMatchLevel: methodNameMatchLevel,
                parametersMatchLevel: parametersMatchLevel
            );

            var methods = methodEnumerable.ToList();
            if (methods.Count == 0)
                return $"[Error] Method not found.\n{filter}";

            var method = default(MethodInfo);

            if (methods.Count > 1)
            {
                var isValidParameterTypeName = inputParameters.IsValidTypeNames(
                    fieldName: nameof(inputParameters),
                    out var error
                );

                // Lets try to filter methods by parameters
                method = isValidParameterTypeName
                    ? methods.FilterByParameters(inputParameters)
                    : null;

                if (method == null)
                    return Error.MoreThanOneMethodFound(methods);
            }
            else
            {
                method = methods.First();
            }

            inputParameters?.EnhanceNames(method);
            inputParameters?.EnhanceTypes(method);

            // if (!inputParameters.IsMatch(method, out var matchError))
            //     return $"[Error] {matchError}";

            Func<string> action = () =>
            {
                var dictInputParameters = inputParameters?.ToDictionary(
                    keySelector: p => p.name!,
                    elementSelector: p => reflector.Deserialize(p, logger: logger)
                );

                var methodWrapper = default(MethodWrapper);

                if (method.IsStatic)
                {
                    // Static method - no object instance needed
                    methodWrapper = new MethodWrapper(reflector, logger: logger, method);
                }
                else if (targetObject != null && !string.IsNullOrEmpty(targetObject.typeName))
                {
                    // Instance method with target object provided
                    var obj = reflector.Deserialize(
                        targetObject,
                        fallbackType: method.DeclaringType,
                        logger: logger);
                    if (obj == null)
                        return $"[Error] '{nameof(targetObject)}' deserialized instance is null. Please specify the '{nameof(targetObject)}' properly.";

                    methodWrapper = new MethodWrapper(reflector, logger: logger, targetInstance: obj, method);
                }
                else
                {
                    // Instance method without target object - create instance from type
                    methodWrapper = new MethodWrapper(reflector, logger: logger, method.DeclaringType!, method);
                }

                if (!methodWrapper.VerifyParameters(dictInputParameters, out var error))
                    return $"[Error] {error}";

                var task = dictInputParameters != null
                    ? methodWrapper.InvokeDict(dictInputParameters)
                    : methodWrapper.Invoke();

                var result = task.Result;
                return $"[Success] Execution result:\n```json\n{JsonUtils.ToJson(result)}\n```";
            };

            if (executeInMainThread)
                return MainThread.Instance.Run(action);

            return action();
        }
    }
}

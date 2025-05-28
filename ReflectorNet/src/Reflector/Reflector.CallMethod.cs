#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Data.Unity;
using com.IvanMurzak.ReflectorNet.MCP;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Reflection
{
    /// <summary>
    /// Serializes Unity components to JSON format.
    /// </summary>
    public partial class Reflector
    {
        public string MethodCall
        (
            Reflector reflector,
            MethodPointerRef filter,
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
                var dictInputParameters = inputParameters?.ToImmutableDictionary(
                    keySelector: p => p.name!,
                    elementSelector: p => reflector.Deserialize(p, logger)
                );

                var methodWrapper = default(MethodWrapper);

                if (string.IsNullOrEmpty(targetObject?.typeName))
                {
                    // No object instance needed. Probably static method.
                    methodWrapper = new MethodWrapper(reflector, logger: logger, method);
                }
                else
                {
                    // Object instance needed. Probably instance method.
                    var obj = reflector.Deserialize(targetObject, logger);
                    if (obj == null)
                        return $"[Error] '{nameof(targetObject)}' deserialized instance is null. Please specify the '{nameof(targetObject)}' properly.";

                    methodWrapper = new MethodWrapper(reflector, logger: logger, targetInstance: obj, method);
                }

                if (!methodWrapper.VerifyParameters(dictInputParameters, out var error))
                    return $"[Error] {error}";

                var task = dictInputParameters != null
                    ? methodWrapper.InvokeDict(dictInputParameters)
                    : methodWrapper.Invoke();

                var result = task.Result;
                return $"[Success] Execution result:\n```json\n{JsonUtils.Serialize(result)}\n```";
            };

            if (executeInMainThread)
                return MainThread.Run(action);

            return action();
        }
    }
}

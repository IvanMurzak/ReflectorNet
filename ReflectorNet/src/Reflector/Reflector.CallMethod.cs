using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Dynamically discovers and invokes methods at runtime with comprehensive parameter handling and error reporting.
        /// This method provides a complete method invocation pipeline that handles method discovery, parameter resolution,
        /// instance creation, and execution with detailed feedback and error handling.
        ///
        /// Workflow:
        /// 1. Method Discovery: Uses FindMethod with configurable matching levels to locate target methods
        /// 2. Parameter Enhancement: Automatically enhances filter parameters from provided input parameters
        /// 3. Disambiguation: Handles multiple method matches through parameter-based filtering
        /// 4. Instance Management: Creates instances for non-static methods or uses provided target objects
        /// 5. Parameter Binding: Converts and validates input parameters against method signatures
        /// 6. Execution: Invokes method with proper parameter binding and exception handling
        /// 7. Result Serialization: Returns execution results as formatted JSON strings
        ///
        /// Instance Method Handling:
        /// - If targetObject is provided: Uses the deserialized instance for method invocation
        /// - If no targetObject: Creates a new instance of the declaring type using Reflector.CreateInstance
        /// - Supports both parameterless and parameterized constructors
        ///
        /// Error Scenarios:
        /// - Method not found: Returns detailed error with search criteria
        /// - Multiple methods found: Returns formatted list of candidates for disambiguation
        /// - Parameter mismatch: Returns validation errors with expected vs actual parameter information
        /// - Parameter deserialization failure: Returns "[Error] Failed to deserialize input parameter ..."
        ///   and does NOT invoke the method (see <see cref="DeserializationException"/>)
        /// - Execution failure: Returns exception details with stack trace information
        ///
        /// Thread Safety:
        /// - executeInMainThread=true: Ensures execution on the main thread (useful for UI operations)
        /// - executeInMainThread=false: Executes immediately on current thread
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type resolution and object creation.</param>
        /// <param name="filter">MethodRef containing method search criteria and optional parameter definitions.</param>
        /// <param name="knownNamespace">Whether to restrict method search to the exact namespace. Default is false.</param>
        /// <param name="typeNameMatchLevel">Minimum match level for type name filtering (0-6). Default is 1.</param>
        /// <param name="methodNameMatchLevel">Minimum match level for method name filtering (0-6). Default is 1.</param>
        /// <param name="parametersMatchLevel">Minimum match level for parameter signature matching (0-6). Default is 2.</param>
        /// <param name="targetObject">Optional SerializedMember representing the target instance for non-static methods.</param>
        /// <param name="inputParameters">Optional SerializedMemberList containing method arguments as serialized data.</param>
        /// <param name="executeInMainThread">Whether to execute the method on the main thread. Default is true.</param>
        /// <param name="logger">Optional logger for tracing method discovery and execution operations.</param>
        /// <returns>A formatted string containing either execution results as JSON or detailed error information.</returns>
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
                    return Error.MoreThanOneMethodFound(reflector, methods);
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
                var dictInputParameters = default(Dictionary<string, object?>);
                if (inputParameters != null)
                {
                    dictInputParameters = new Dictionary<string, object?>(inputParameters.Count);
                    foreach (var inputParameter in inputParameters)
                    {
                        try
                        {
                            dictInputParameters.Add(inputParameter.name!, reflector.Deserialize(inputParameter, logger: logger));
                        }
                        catch (DeserializationException ex)
                        {
                            // Never invoke the method with a value we failed to build. A default value
                            // would be indistinguishable from a real one (a boxed `default(T)` even
                            // passes MethodWrapper.VerifyParameters) and the call would be reported as
                            // a success while doing nothing.
                            return $"[Error] Failed to deserialize input parameter '{inputParameter.name.ValueOrNull()}'"
                                + $" of type '{inputParameter.typeName.ValueOrNull()}'.\n{ex.Message}";
                        }
                    }
                }

                var methodWrapper = default(MethodWrapper);

                if (method.IsStatic)
                {
                    // Static method - no object instance needed
                    methodWrapper = new MethodWrapper(reflector, logger: logger, method);
                }
                else if (targetObject != null && !string.IsNullOrEmpty(targetObject.typeName))
                {
                    // Instance method with target object provided
                    object? obj;
                    try
                    {
                        obj = reflector.Deserialize(
                            targetObject,
                            fallbackType: method.DeclaringType,
                            logger: logger);
                    }
                    catch (DeserializationException ex)
                    {
                        return $"[Error] Failed to deserialize '{nameof(targetObject)}'"
                            + $" of type '{targetObject.typeName.ValueOrNull()}'.\n{ex.Message}";
                    }
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
                return $"[Success] Execution result:\n```json\n{result.ToJson(reflector, depth: 0, logger: logger)}\n```";
            };

            if (executeInMainThread)
                return MainThread.Instance.Run(action);

            return action();
        }
    }
}

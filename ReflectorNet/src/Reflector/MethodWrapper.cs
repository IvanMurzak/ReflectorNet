using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public class MethodWrapper
    {
        protected readonly Reflector _reflector;
        protected readonly MethodInfo _methodInfo;
        protected readonly object? _targetInstance;
        protected readonly Type? _classType;

        protected readonly string? _description;
        protected readonly ILogger? _logger;
        protected readonly JsonNode? _inputSchema;

        public JsonNode? InputSchema => _inputSchema;
        public string? Description => _description;

        public static MethodWrapper Create(Reflector reflector, ILogger? logger, MethodInfo methodInfo)
        {
            if (methodInfo.IsStatic)
                return new MethodWrapper(reflector, logger, methodInfo);
            else
                return new MethodWrapper(reflector, logger, methodInfo.DeclaringType!, methodInfo);
        }
        public static MethodWrapper CreateFromInstance(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo)
        {
            if (methodInfo.IsStatic)
                return new MethodWrapper(reflector, logger, methodInfo);
            else
                return new MethodWrapper(reflector, logger, targetInstance, methodInfo);
        }

        public MethodWrapper(Reflector reflector, ILogger? logger, MethodInfo methodInfo)
        {
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _logger = logger;
            _methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            if (!methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be static.");

            _description = methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
            _inputSchema = JsonUtils.Schema.GetArgumentsSchema(methodInfo);
        }

        public MethodWrapper(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo)
        {
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _logger = logger;
            _targetInstance = targetInstance ?? throw new ArgumentNullException(nameof(targetInstance));
            _methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            if (methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be an instance method. Use the other constructor for static methods.");

            _description = methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
            _inputSchema = JsonUtils.Schema.GetArgumentsSchema(methodInfo);
        }

        public MethodWrapper(Reflector reflector, ILogger? logger, Type classType, MethodInfo methodInfo)
        {
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _logger = logger;
            _classType = classType ?? throw new ArgumentNullException(nameof(classType));
            _methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            if (methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be an instance method. Use the other constructor for static methods.");

            _description = methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
            _inputSchema = JsonUtils.Schema.GetArgumentsSchema(methodInfo);
        }

        public virtual async Task<object?> Invoke(params object?[] parameters)
        {
            // If _targetInstance is null and _targetType is set, create an instance of the target type
            var instance = _targetInstance ?? (_classType != null ? Activator.CreateInstance(_classType) : null);

            // Build the final parameters array, filling in default values where necessary
            var finalParameters = BuildParameters(_reflector, parameters);

            // _if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
            //     ? $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}({string.Join(", ", namedParameters!.Select(x => $"{x.Value?.GetType()?.Name ?? "null"} {x.Key}"))})"
            //     : $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}()");

            PrintParameters(finalParameters);

            // Invoke the method (static or instance)
            var result = _methodInfo.Invoke(instance, finalParameters);

            // Handle Task, Task<T>, or synchronous return types
            if (result is Task task)
            {
                await task.ConfigureAwait(continueOnCapturedContext: false);

                // If it's a Task<T>, extract the result
                var resultProperty = task.GetType().GetProperty(nameof(Task<int>.Result));
                return resultProperty?.GetValue(task);
            }

            // For synchronous methods, return the result directly
            return result;
        }

        public virtual async Task<object?> InvokeDict(IReadOnlyDictionary<string, object?>? namedParameters)
        {
            // If _targetInstance is null and _targetType is set, create an instance of the target type
            var instance = _targetInstance ?? (_classType != null ? Activator.CreateInstance(_classType) : null);

            // Build the final parameters array, filling in default values where necessary
            var finalParameters = BuildParameters(_reflector, namedParameters);

            // if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
            //     _logger.LogTrace((namedParameters?.Count ?? 0) > 0
            //         ? $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}({string.Join(", ", namedParameters!.Select(x => $"{x.Value?.GetType()?.Name ?? "null"} {x.Key}"))})"
            //         : $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}()");

            PrintParameters(finalParameters);

            // Invoke the method (static or instance)
            var result = _methodInfo.Invoke(instance, finalParameters);

            // Handle Task, Task<T>, or synchronous return types
            if (result is Task task)
            {
                await task.ConfigureAwait(continueOnCapturedContext: false);

                // If it's a Task<T>, extract the result
                var resultProperty = task.GetType().GetProperty(nameof(Task<int>.Result));
                return resultProperty?.GetValue(task);
            }

            // For synchronous methods, return the result directly
            return result;
        }

        public bool VerifyParameters(IReadOnlyDictionary<string, object?>? namedParameters, out string? error)
        {
            var methodParameters = _methodInfo.GetParameters();
            if (methodParameters.Length == 0)
            {
                if ((namedParameters?.Count ?? 0) == 0)
                {
                    error = null;
                    return true;
                }
                else
                {
                    error = $"Method '{_methodInfo.Name}' does not accept any parameters, but {namedParameters?.Count} were provided.";
                    return false;
                }
            }

            if (namedParameters == null)
            {
                error = $"Method '{_methodInfo.Name}' requires parameters, but none were provided.";
                return false;
            }

            foreach (var parameter in namedParameters)
            {
                var methodParameter = methodParameters.FirstOrDefault(p => p.Name == parameter.Key);
                if (methodParameter == null)
                {
                    error = $"Method '{_methodInfo.Name}' does not have a parameter named '{parameter.Key}'.";
                    return false;
                }

                if (parameter.Value == null)
                    continue;

                if (!methodParameter.ParameterType.IsInstanceOfType(parameter.Value))
                {
                    error = $"Parameter '{parameter.Key}' type mismatch. Expected '{methodParameter.ParameterType}', but got '{parameter.Value.GetType()}'.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        protected object?[]? BuildParameters(Reflector reflector, object?[]? parameters)
        {
            if (parameters == null)
                return null;

            var methodParameters = _methodInfo.GetParameters();

            // Prepare the final arguments array, filling in default values where necessary
            var finalParameters = new object?[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i < parameters.Length)
                {
                    // Handle JsonElement conversion
                    if (parameters[i] is JsonElement jsonElement)
                    {
                        try
                        {
                            // Try #1: Parsing as the parameter type directly
                            finalParameters[i] = JsonUtils.Deserialize(jsonElement, methodParameters[i].ParameterType);
                        }
                        catch
                        {
                            // Try #2: Parsing as SerializedMember
                            var serializedParameter = JsonUtils.Deserialize<SerializedMember>(jsonElement);
                            if (serializedParameter == null)
                                throw new ArgumentException($"Failed to parse {nameof(SerializedMember)} for parameter '{methodParameters[i].Name}'");

                            finalParameters[i] = reflector.Deserialize(serializedParameter, _logger);
                        }
                    }
                    else
                    {
                        // Use the provided parameter value
                        finalParameters[i] = parameters[i];
                    }
                }
                else if (methodParameters[i].HasDefaultValue)
                {
                    // Use the default value if no value is provided
                    finalParameters[i] = methodParameters[i].DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"No value provided for parameter '{methodParameters[i].Name}' and no default value is defined.");
                }
            }

            // Validate parameters
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                if (finalParameters[i] == null)
                    continue;

                if (!parameter.ParameterType.IsInstanceOfType(finalParameters[i]))
                    throw new ArgumentException($"Parameter '{parameter.Name}' type mismatch. Expected '{parameter.ParameterType}', but got '{finalParameters[i]?.GetType()}'.");
            }

            return finalParameters;
        }

        protected object?[]? BuildParameters(Reflector reflector, IReadOnlyDictionary<string, object?>? namedParameters)
        {
            if (namedParameters == null)
                return null;

            var methodParameters = _methodInfo.GetParameters();

            // Prepare the final arguments array
            var finalParameters = new object?[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];

                if (namedParameters != null && namedParameters.TryGetValue(parameter.Name!, out var value))
                {
                    if (value is JsonElement jsonElement)
                    {
                        try
                        {
                            // Try #1: Parsing as the parameter type directly
                            finalParameters[i] = JsonUtils.Deserialize(jsonElement, methodParameters[i].ParameterType);
                        }
                        catch
                        {
                            // Try #2: Parsing as SerializedMember
                            var serializedParameter = JsonUtils.Deserialize<SerializedMember>(jsonElement);
                            if (serializedParameter == null)
                                throw new ArgumentException($"Failed to parse {nameof(SerializedMember)} for parameter '{methodParameters[i].Name}'");

                            finalParameters[i] = reflector.Deserialize(serializedParameter, _logger);
                        }
                    }
                    else
                    {
                        // Use the provided parameter value
                        finalParameters[i] = value;
                    }
                }
                else if (parameter.HasDefaultValue)
                {
                    // Use the default value if no value is provided
                    finalParameters[i] = parameter.DefaultValue;
                }
                else
                {
                    // Use the type's default value if no value is provided
                    finalParameters[i] = parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                }
            }

            // Validate parameters
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                if (finalParameters[i] == null)
                    continue;

                if (!parameter.ParameterType.IsInstanceOfType(finalParameters[i]))
                    throw new ArgumentException($"Parameter '{parameter.Name}' type mismatch. Expected '{parameter.ParameterType}', but got '{finalParameters[i]?.GetType()}'.");
            }

            return finalParameters;
        }
        void PrintParameters(object?[]? parameters)
        {
            if (!(_logger?.IsEnabled(LogLevel.Debug) ?? false))
                return;

            _logger.LogDebug((parameters?.Length ?? 0) > 0
                ? $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}({string.Join(", ", parameters!.Select(x => $"{x?.GetType()?.Name ?? "null"}"))})"
                : $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}()");

            var methodParameters = _methodInfo.GetParameters();
            var maxLength = Math.Max(methodParameters.Length, parameters?.Length ?? 0);
            var result = new string[maxLength];

            for (var i = 0; i < maxLength; i++)
            {
                var parameterType = i < methodParameters.Length ? methodParameters[i].ParameterType.ToString() : "N/A";
                var parameterName = i < methodParameters.Length ? methodParameters[i].Name : "N/A";
                var parameterValue = i < (parameters?.Length ?? 0) ? parameters?[i]?.ToString() ?? "null" : "null";

                result[i] = $"{parameterType} {parameterName} = {parameterValue}";
            }

            var parameterLogs = string.Join(Environment.NewLine, result);

            _logger?.LogDebug(parameterLogs.Length > 0
                    ? "Invoke method: Input: {0}, Provided: {1}\n{2}"
                    : "Invoke method: Input: {0}, Provided: {1}{2}",
                methodParameters.Length,
                parameters?.Length,
                parameterLogs);
        }
    }
}
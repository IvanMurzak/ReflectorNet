using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
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
        protected readonly JsonNode? _outputSchema;

        public JsonNode? InputSchema => _inputSchema;
        public JsonNode? OutputSchema => _outputSchema;
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
            _logger = logger;
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            if (!methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be static.");

            _description = methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
            _inputSchema = reflector.GetArgumentsSchema(methodInfo);
            _outputSchema = reflector.GetReturnSchema(methodInfo);
        }

        public MethodWrapper(Reflector reflector, ILogger? logger, object targetInstance, MethodInfo methodInfo)
        {
            _logger = logger;
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _targetInstance = targetInstance ?? throw new ArgumentNullException(nameof(targetInstance));
            _methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            if (methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be an instance method. Use the other constructor for static methods.");

            _description = methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
            _inputSchema = reflector.GetArgumentsSchema(methodInfo);
            _outputSchema = reflector.GetReturnSchema(methodInfo);
        }

        public MethodWrapper(Reflector reflector, ILogger? logger, Type classType, MethodInfo methodInfo)
        {
            _logger = logger;
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _classType = classType ?? throw new ArgumentNullException(nameof(classType));
            _methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));

            if (methodInfo.IsStatic)
                throw new ArgumentException("The provided method must be an instance method. Use the other constructor for static methods.");

            _description = methodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
            _inputSchema = reflector.GetArgumentsSchema(methodInfo);
            _outputSchema = reflector.GetReturnSchema(methodInfo);
        }

        public virtual Task<object?> Invoke(params object?[] parameters) => Invoke(CancellationToken.None, parameters);
        public virtual async Task<object?> Invoke(CancellationToken cancellationToken, params object?[] parameters)
        {
            // If _targetInstance is null and _targetType is set, create an instance of the target type
            var instance = _targetInstance ?? (_classType != null ? Activator.CreateInstance(_classType) : null); // TODO: replace with Reflector.CreateInstance

            // Build the final parameters array, filling in default values where necessary
            var finalParameters = BuildParameters(parameters);

            // _if (_logger?.IsEnabled(LogLevel.Debug) ?? false)
            //     ? $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}({string.Join(", ", namedParameters!.Select(x => $"{x.Value?.GetType()?.Name ?? "null"} {x.Key}"))})"
            //     : $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}()");

            PrintParameters(finalParameters);

            // Invoke the method (static or instance)
            var result = _methodInfo.Invoke(instance, finalParameters);

            // Handle Task, Task<T>, or synchronous return types
            if (result is Task task)
            {
                await task
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // If it's a Task<T>, extract the result
                var resultProperty = task.GetType().GetProperty(nameof(Task<int>.Result));
                return resultProperty?.GetValue(task);
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask
                    .AsTask()
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // If it's a ValueTask<T>, extract the result
                var resultProperty = valueTask.GetType().GetProperty(nameof(ValueTask<int>.Result));
                return resultProperty?.GetValue(valueTask);
            }

            // For synchronous methods, return the result directly
            return result;
        }

        public virtual async Task<object?> InvokeDict(IReadOnlyDictionary<string, object?>? namedParameters, CancellationToken cancellationToken = default)
        {
            // If _targetInstance is null and _targetType is set, create an instance of the target type
            var instance = _targetInstance ?? (_classType != null ? _reflector.CreateInstance(_classType) : null);

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
                await task
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);

                // If it's a Task<T>, extract the result
                var resultProperty = task.GetType().GetProperty(nameof(Task<int>.Result));
                return resultProperty?.GetValue(task);
            }

            // For synchronous methods, return the result directly
            return result;
        }

        public virtual bool VerifyParameters(IReadOnlyDictionary<string, object?>? namedParameters, out string? error)
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
                    error = $"Parameter '{parameter.Key}' type mismatch. Expected '{methodParameter.ParameterType.GetTypeName(pretty: true)}', but got '{parameter.Value.GetType()}'.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        protected virtual object?[]? BuildParameters(object?[]? parameters)
        {
            var methodParameters = _methodInfo.GetParameters();

            // Prepare the final arguments array, filling in default values where necessary
            var finalParameters = new object?[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (parameters != null && i < parameters.Length)
                {
                    finalParameters[i] = GetParameterValue(_reflector, methodParameters[i], parameters[i]);
                }
                else
                {
                    finalParameters[i] = GetDefaultParameterValue(_reflector, methodParameters[i]);
                }
            }

            // Validate parameters
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                if (finalParameters[i] == null)
                    continue;

                if (!parameter.ParameterType.IsInstanceOfType(finalParameters[i]))
                    throw new ArgumentException($"Parameter '{parameter.Name}' type mismatch. Expected '{parameter.ParameterType.GetTypeName(pretty: true)}', but got '{finalParameters[i]?.GetType()}'.");
            }

            return finalParameters;
        }

        protected virtual object? GetDefaultParameterValue(Reflector reflector, ParameterInfo methodParameter)
        {
            if (methodParameter.HasDefaultValue)
            {
                // Use the default value if no value is provided
                return methodParameter.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"No value provided for parameter '{methodParameter.Name}' and no default value is defined.");
            }
        }

        protected virtual object? GetParameterValue(Reflector reflector, ParameterInfo methodParameter, object? value)
        {
            var underlyingType = Nullable.GetUnderlyingType(methodParameter.ParameterType) ?? methodParameter.ParameterType;

            // Handle JsonElement conversion
            if (value is JsonElement jsonElement)
            {
                var isPrimitive = TypeUtils.IsPrimitive(underlyingType);
                if (!isPrimitive)
                {
                    // Handle stringified json
                    if (JsonUtils.TryUnstringifyJson(jsonElement, out var unstringifiedJson))
                    {
                        value = unstringifiedJson;
                        jsonElement = unstringifiedJson!.Value;
                    }
                }
                try
                {
                    // Try #1: Parsing as the parameter type directly
                    return jsonElement.Deserialize(
                        returnType: methodParameter.ParameterType,
                        options: _reflector.JsonSerializerOptions);
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Try #2: Parsing as SerializedMember
                        var serializedParameter = jsonElement.Deserialize<SerializedMember>();
                        if (serializedParameter == null)
                            throw new ArgumentException($"Failed to parse {nameof(SerializedMember)} for parameter '{methodParameter.Name}'.\nInput value: {jsonElement}\nOriginal exception: {ex.Message}");

                        return _reflector.Deserialize(serializedParameter, fallbackType: methodParameter.ParameterType, logger: _logger);
                    }
                    catch (Exception ex2)
                    {
                        // If all parsing attempts fail, throw ArgumentException as expected by tests
                        throw new ArgumentException($"Unable to convert value to parameter '{methodParameter.Name}' of type '{methodParameter.ParameterType.GetTypeName(pretty: true)}'.\nInput value: {jsonElement}\nOriginal exception: {ex.Message}\nSecond exception: {ex2.Message}");
                    }
                }
            }
            else
            {
                if (underlyingType.IsInstanceOfType(value))
                    return value;

                if (underlyingType.IsEnum)
                {
                    // Handle enum conversion for string values and return the provided parameter value
                    return ConvertStringToEnum(value, underlyingType, methodParameter.Name!);
                }

                throw new ArgumentException($"Parameter '{methodParameter.Name}' type mismatch. Expected '{methodParameter.ParameterType.GetTypeName(pretty: true)}', but got '{value?.GetType()}'.");
            }
        }

        protected virtual object?[]? BuildParameters(Reflector reflector, IReadOnlyDictionary<string, object?>? namedParameters)
        {
            var methodParameters = _methodInfo.GetParameters();

            // Prepare the final arguments array
            var finalParameters = new object?[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                finalParameters[i] = GetParameterValue(reflector, parameter, namedParameters);
            }

            // Validate parameters
            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                if (finalParameters[i] == null)
                    continue;

                if (!parameter.ParameterType.IsInstanceOfType(finalParameters[i]))
                    throw new ArgumentException($"Parameter '{parameter.Name}' type mismatch. Expected '{parameter.ParameterType.GetTypeName(pretty: true)}', but got '{finalParameters[i]?.GetType()}'.");
            }

            return finalParameters;
        }
        protected virtual object? GetParameterValue(Reflector reflector, ParameterInfo parameter, IReadOnlyDictionary<string, object?>? namedParameters)
        {
            var underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

            if (namedParameters != null && namedParameters.TryGetValue(parameter.Name!, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    var isPrimitive = TypeUtils.IsPrimitive(underlyingType);
                    if (!isPrimitive)
                    {
                        // Handle stringified json
                        if (JsonUtils.TryUnstringifyJson(jsonElement, out var unstringifiedJson))
                        {
                            value = unstringifiedJson;
                            jsonElement = unstringifiedJson!.Value;
                        }
                    }
                    try
                    {
                        // Try #1: Parsing as the parameter type directly
                        return jsonElement.Deserialize(
                            returnType: parameter.ParameterType,
                            options: _reflector.JsonSerializerOptions);
                    }
                    catch (Exception ex)
                    {
                        // Try #2: Parsing as SerializedMember
                        try
                        {
                            var serializedParameter = jsonElement.Deserialize<SerializedMember>();
                            if (serializedParameter == null)
                                throw new ArgumentException($"Failed to parse {nameof(SerializedMember)} for parameter '{parameter.Name}'.\nInput value: {jsonElement}\nOriginal exception: {ex.Message}");

                            return reflector.Deserialize(serializedParameter, fallbackType: parameter.ParameterType, logger: _logger);
                        }
                        catch (Exception ex2)
                        {
                            // If all parsing attempts fail, throw ArgumentException as expected by tests
                            throw new ArgumentException($"Unable to convert value to parameter '{parameter.Name}' of type '{parameter.ParameterType.GetTypeName(pretty: true)}'.\nInput value: {jsonElement}\nOriginal exception: {ex.Message}\nSecond exception: {ex2.Message}");
                        }
                    }
                }
                else
                {
                    if (underlyingType.IsInstanceOfType(value))
                        return value;

                    if (underlyingType.IsEnum)
                    {
                        // Handle enum conversion for string values and return the provided parameter value
                        return ConvertStringToEnum(value, underlyingType, parameter.Name!);
                    }

                    throw new ArgumentException($"Parameter '{parameter.Name}' type mismatch. Expected '{parameter.ParameterType.GetTypeName(pretty: true)}', but got '{value?.GetType()}'.");
                }
            }
            else if (parameter.HasDefaultValue)
            {
                // Use the default value if no value is provided
                return parameter.DefaultValue;
            }
            else
            {
                // Use the type's default value if no value is provided
                return parameter.ParameterType.IsValueType
                    ? Activator.CreateInstance(parameter.ParameterType) // TODO: replace with Reflector.CreateInstance
                    : null;
            }
        }

        private static object? ConvertStringToEnum(object? value, Type parameterType, string parameterName)
        {
            if (value is string stringValue && parameterType.IsEnum)
            {
                if (Enum.TryParse(parameterType, stringValue, ignoreCase: true, out var result))
                {
                    if (Enum.IsDefined(parameterType, result!))
                    {
                        return result;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Value '{stringValue}' for parameter '{parameterName}' was parsed but is not a defined member of '{parameterType.GetTypeName(pretty: true)}'. Valid values are: {string.Join(", ", Enum.GetNames(parameterType))}");
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"Value '{stringValue}' for parameter '{parameterName}' could not be parsed as '{parameterType.GetTypeName(pretty: true)}'. Valid values are: {string.Join(", ", Enum.GetNames(parameterType))}");
                }
            }
            throw new ArgumentException($"Parameter '{parameterName}' type mismatch. Expected '{parameterType.GetTypeName(pretty: true)}', but got '{value?.GetType()}'.");
        }

        protected virtual void PrintParameters(object?[]? parameters)
        {
            if (!(_logger?.IsEnabled(LogLevel.Debug) ?? false))
                return;

            _logger.LogDebug((parameters?.Length ?? 0) > 0
                ? $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}({string.Join(", ", parameters!.Select(x => $"{x?.GetType()?.Name.ValueOrNull()}"))})"
                : $"Invoke method: {_methodInfo.ReturnType.Name} {_methodInfo.Name}()");

            var methodParameters = _methodInfo.GetParameters();
            var maxLength = Math.Max(methodParameters.Length, parameters?.Length ?? 0);
            var result = new string[maxLength];

            for (var i = 0; i < maxLength; i++)
            {
                var parameterType = i < methodParameters.Length ? methodParameters[i].ParameterType.ToString() : "N/A";
                var parameterName = i < methodParameters.Length ? methodParameters[i].Name : "N/A";
                var parameterValue = i < (parameters?.Length ?? 0) ? parameters?[i]?.ToString().ValueOrNull() : "null";

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
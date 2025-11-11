using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class MethodWrapper
    {
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
    }
}
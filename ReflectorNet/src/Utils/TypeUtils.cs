#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Common.Utils
{
    public static class TypeUtils
    {
        public static Type? GetType(string? typeFullName) => string.IsNullOrEmpty(typeFullName)
            ? null
            : Type.GetType(typeFullName) ??
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeFullName);

        public static T? GetDefaultValue<T>() where T : class
            => (T?)GetDefaultValue(typeof(T));
        public static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (type.GetConstructor(Type.EmptyTypes) != null)
                return Activator.CreateInstance(type);

            return null;
        }

        public static object? CastTo(object obj, string typeFullName, out string? error)
        {
            var type = GetType(typeFullName) ??
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == typeFullName);
            if (type == null)
            {
                error = $"[Error] Type '{typeFullName}' not found.";
                return default;
            }
            return CastTo(obj, type, out error);
        }

        public static T? CastTo<T>(object obj, out string? error) where T : class
            => CastTo(obj, typeof(T), out error) is T typedObj ? typedObj : default;

        public static object? CastTo(object obj, Type type, out string? error)
        {
            if (obj == null)
            {
                error = $"[Error] Object is null.";
                return default;
            }
            if (!type.IsAssignableFrom(obj.GetType()))
            {
                error = $"[Error] Type mismatch between '{type.FullName}' and '{obj.GetType().FullName}'.";
                return default;
            }

            error = null;
            return obj;
        }
        public static int GetInheritanceDistance(Type baseType, Type targetType)
        {
            if (!baseType.IsAssignableFrom(targetType))
                return -1;

            int distance = 0;
            Type? current = targetType;
            while (current != null && current != baseType)
            {
                current = current.BaseType;
                distance++;
            }
            return current == baseType ? distance : -1;
        }
    }
}
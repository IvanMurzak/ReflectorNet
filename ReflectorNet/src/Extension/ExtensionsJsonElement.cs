using System;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJsonElement
    {
        public static T? Deserialize<T>(this JsonElement? jsonElement, Reflector reflector)
        {
            return reflector.JsonSerializer.Deserialize<T>(
                reflector: reflector,
                jsonElement: jsonElement);
        }
        public static object? Deserialize(this JsonElement? jsonElement, Type type, Reflector reflector)
        {
            return reflector.JsonSerializer.Deserialize(
                reflector: reflector,
                jsonElement: jsonElement,
                type: type);
        }
        public static T? DeserializeValueSerializedMember<T>(this JsonElement? jsonElement,
            Reflector reflector,
            string? name = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            return (T?)DeserializeValueSerializedMember(jsonElement, reflector,
                type: typeof(T),
                name: name,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger);
        }
        public static object? DeserializeValueSerializedMember(this JsonElement? jsonElement,
            Reflector reflector,
            Type type,
            string? name = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (jsonElement == null)
                return null;

            SerializedMember? serializedMember = null;

            try
            {
                serializedMember = jsonElement.Deserialize<SerializedMember>(reflector);
            }
            catch
            {
                // ignore
            }

            if (serializedMember == null)
                return reflector.GetDefaultValue(type);

            if (serializedMember.valueJsonElement == null)
                return reflector.CreateInstance(type);

            return reflector.Deserialize(serializedMember,
                fallbackType: type,
                fallbackName: name,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger);
        }

    }
}
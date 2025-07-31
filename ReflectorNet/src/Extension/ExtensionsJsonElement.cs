using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJsonElement
    {
        public static T? Deserialize<T>(this JsonElement? jsonElement)
            => JsonUtils.Deserialize<T>(jsonElement);
        public static object? Deserialize(this JsonElement? jsonElement, Type type)
            => JsonUtils.Deserialize(jsonElement, type);
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

            var serializedMember = jsonElement.Deserialize<SerializedMember>();
            if (serializedMember?.valueJsonElement == null)
                return TypeUtils.GetDefaultNonNullValue(type);

            return reflector.Deserialize(serializedMember,
                fallbackType: type,
                fallbackName: name,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger);
        }

    }
}
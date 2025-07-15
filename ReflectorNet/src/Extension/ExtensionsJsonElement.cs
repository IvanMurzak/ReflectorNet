using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJsonElement
    {
        public static T? Deserialize<T>(this JsonElement? jsonElement)
        {
            return jsonElement != null && jsonElement.HasValue
                ? JsonUtils.Deserialize<T>(jsonElement.Value)
                : TypeUtils.GetDefaultValue<T>();
        }
        public static object? Deserialize(this JsonElement? jsonElement, Type type)
        {
            return jsonElement != null && jsonElement.HasValue
                ? JsonUtils.Deserialize(jsonElement.Value, type)
                : TypeUtils.GetDefaultValue(type);
        }
    }
}
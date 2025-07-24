using System;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsType
    {
        public static JsonNode? GetSchema(this Type type, bool justRef = false) => JsonUtils.Schema.GetSchema(type, justRef);
        public static string GetTypeShortName(this Type? type) => TypeUtils.GetTypeShortName(type);
        public static string GetTypeName(this Type? type, bool pretty = false) => TypeUtils.GetTypeName(type, pretty);
        public static string GetTypeId(this Type type) => TypeUtils.GetTypeId(type);
        public static bool IsMatch(this Type? type, string? typeName) => TypeUtils.IsNameMatch(type, typeName);
    }
}
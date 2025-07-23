using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class JsonUtils
    {
        public static JsonElement? ToJsonElement(this JsonNode? node)
        {
            if (node == null)
                return null;

            // Convert JsonNode to JsonElement
            var jsonString = node.ToJsonString();

            // Parse the JSON string into a JsonElement
            using var document = JsonDocument.Parse(jsonString);
            return document.RootElement.Clone();
        }

        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType)
                return true; // Reference types are nullable
            return Nullable.GetUnderlyingType(type) != null; // Nullable value types
        }
    }
}
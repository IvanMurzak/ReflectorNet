/*
* ReflectorNet
* Author: Ivan Murzak (https://github.com/IvanMurzak)
* Copyright (c) 2025 Ivan Murzak
* Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
*/

using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class JsonUtils
    {
        public static bool TryUnstringifyJson(JsonElement jsonElement, out JsonElement? result)
        {
            if (jsonElement.ValueKind == JsonValueKind.String)
            {
                var str = jsonElement.GetString() ?? string.Empty;

                result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(str);
                return true;
            }
            result = null;
            return false;
        }
        // public static bool TryUnstringifyJson(object? json, out JsonElement? result)
        // {
        //     if (json is JsonElement jsonElement)
        //     {
        //         if (jsonElement.ValueKind == JsonValueKind.String)
        //         {
        //             var str = jsonElement.GetString() ?? string.Empty;

        //             result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(str);
        //             return true;
        //         }
        //         result = null;
        //         return false;
        //     }
        //     else if (json is string str && !string.IsNullOrEmpty(str))
        //     {
        //         var newStr = str
        //             .Replace("\"{", "{")
        //             .Replace("}\"", "}")
        //             .Replace("\\\"", "\"");

        //         if (newStr == str)
        //         {
        //             result = null;
        //             return false;
        //         }

        //         result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(newStr);
        //         return true;
        //     }
        //     result = null;
        //     return false;
        // }
    }
}
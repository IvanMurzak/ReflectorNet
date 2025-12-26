/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON strings and System.Reflection.Assembly.
    /// </summary>
    public class AssemblyJsonConverter : JsonConverter<Assembly>
    {
        public override Assembly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string token for Assembly, but got {reader.TokenType}");

            var assemblyName = reader.GetString();
            if (string.IsNullOrWhiteSpace(assemblyName))
                return null;

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.FullName == assemblyName || a.GetName().Name == assemblyName);

            if (assembly != null)
                return assembly;

            try
            {
                return Assembly.Load(assemblyName);
            }
            catch
            {
                throw new JsonException($"Unable to find or load assembly: {assemblyName}");
            }
        }

        public override void Write(Utf8JsonWriter writer, Assembly value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStringValue(value.FullName);
        }
    }
}

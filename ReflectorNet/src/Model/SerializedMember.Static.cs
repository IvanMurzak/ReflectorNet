/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public partial class SerializedMember
    {
        public static SerializedMember FromReference(string path, string? name)
        {
            var jsonObject = new JsonObject { [JsonSchema.Ref] = path };

            return new SerializedMember
            {
                name = name,
                typeName = JsonSchema.Reference,
                valueJsonElement = jsonObject.ToJsonElement()
            };
        }

        public static SerializedMember Null(Type type, string? name = null)
            => new SerializedMember(type, name);

        public static SerializedMember FromJson(Type type, JsonElement json, string? name = null)
            => new SerializedMember(type, name).SetJsonValue(json);

        public static SerializedMember FromJson(Type type, string? json, string? name = null)
            => new SerializedMember(type, name).SetJsonValue(json);

        public static SerializedMember FromValue(Reflector reflector, Type type, object? value, string? name = null)
            => new SerializedMember(type, name).SetValue(reflector, value);

        public static SerializedMember FromValue<T>(Reflector reflector, T? value, string? name = null)
            => new SerializedMember(typeof(T), name).SetValue(reflector, value);
    }
}
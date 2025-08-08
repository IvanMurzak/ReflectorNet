/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public interface IJsonSchemaConverter
    {
        string Id { get; }
        JsonNode GetScheme();
        JsonNode GetSchemeRef();
    }
}
/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public abstract class JsonSchemaConverter<T> : JsonConverter<T>, IJsonSchemaConverter
    {
        public static string StaticId => TypeUtils.GetSchemaTypeId<T>();

        private static Type[] _emptyTypes = new Type[] { };

        public virtual string Id => StaticId;
        public abstract JsonNode GetSchema();
        public abstract JsonNode GetSchemaRef();
        public virtual IEnumerable<Type> GetDefinedTypes() => _emptyTypes;
    }
}
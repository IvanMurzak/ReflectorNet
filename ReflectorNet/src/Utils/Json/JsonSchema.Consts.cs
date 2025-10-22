/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public partial class JsonSchema
    {
        public const string Type = "type";
        public const string Object = "object";
        public const string Description = "description";
        public const string Properties = "properties";
        public const string Items = "items";
        public const string Array = "array";
        public const string AnyOf = "anyOf";
        public const string Required = "required";
        public const string Result = "result";
        public const string Error = "error";
        public const string AdditionalProperties = "additionalProperties";

        public const string Null = "null";
        public const string String = "string";
        public const string Integer = "integer"; // int, long
        public const string Number = "number"; // float, double, supports int as well
        public const string Boolean = "boolean";
        public const string Minimum = "minimum";
        public const string Maximum = "maximum";

        public const string Id = "$id";
        public const string Defs = "$defs";
        public const string Ref = "$ref";
        public const string RefValue = "#/$defs/";
        public const string SchemaDraft = "$schema";
        public const string SchemaDraftValue = "https://json-schema.org/draft/2020-12/schema";
    }
}
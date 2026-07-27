/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJsonElement
    {
        public static T? Deserialize<T>(this JsonElement? jsonElement, Reflector reflector)
        {
            return reflector.JsonSerializer.Deserialize<T>(
                reflector: reflector,
                jsonElement: jsonElement);
        }
        public static object? Deserialize(this JsonElement? jsonElement, Type type, Reflector reflector)
        {
            return reflector.JsonSerializer.Deserialize(
                reflector: reflector,
                jsonElement: jsonElement,
                type: type);
        }
        /// <inheritdoc cref="DeserializeValueSerializedMember(JsonElement?, Reflector, Type, string?, int, Logs?, ILogger?)"/>
        public static T? DeserializeValueSerializedMember<T>(this JsonElement? jsonElement,
            Reflector reflector,
            string? name = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            return (T?)DeserializeValueSerializedMember(jsonElement, reflector,
                type: typeof(T),
                name: name,
                depth: depth,
                logs: logs,
                logger: logger);
        }

        /// <summary>
        /// Reads <paramref name="jsonElement"/> as a nested <see cref="SerializedMember"/> and
        /// deserializes it into <paramref name="type"/>.
        /// </summary>
        /// <remarks>
        /// A payload that is not a well-formed <see cref="SerializedMember"/> is a hard failure and
        /// the resulting <see cref="JsonException"/> is propagated to the caller.
        /// <para>
        /// This method used to swallow that exception in a bare <c>catch { }</c> and return
        /// <c>reflector.GetDefaultValue(type)</c> instead. The caller had no way to tell that
        /// meaningless default apart from a genuinely deserialized value - for a value type the
        /// boxed <c>default(T)</c> even passes <see cref="Type.IsInstanceOfType"/> - so an
        /// undeserializable argument was silently reported as a successful one.
        /// </para>
        /// <para>
        /// ⚠ That bare <c>catch</c> was ALSO doing a second, unrelated job: it was the "this payload
        /// is not a <see cref="SerializedMember"/> at all - carry on" signal that consumer converters
        /// depend on when they layer their own resolution of a foreign shape (e.g. a Unity object
        /// reference <c>{"instanceID":"12345"}</c>) on top of the base one. Removing the catch fixed
        /// the first job and severed the second. Do not restore a catch here to fix that: the two
        /// questions are now separated by RESPONSIBILITY, not by a try/catch. This method's contract
        /// is narrow and unchanged - "read this payload AS a <see cref="SerializedMember"/>", so
        /// anything else is a failure FOR IT. The "is this even my shape?" question is answered one
        /// level up, by the converter, via
        /// <c>BaseReflectionConverter.DeclinesValueByShape</c> /
        /// <see cref="SerializedMemberShape.Classify"/>, BEFORE it delegates here. See
        /// <see cref="com.IvanMurzak.ReflectorNet.Converter.DeserializationOutcome"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="JsonException">
        /// The JSON payload is not a valid <see cref="SerializedMember"/>.
        /// </exception>
        public static object? DeserializeValueSerializedMember(this JsonElement? jsonElement,
            Reflector reflector,
            Type type,
            string? name = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            if (jsonElement == null)
                return null;

            // Deliberately NOT wrapped in try/catch: a JsonException here means the payload cannot be
            // understood, and returning a default value in its place is indistinguishable from success.
            var serializedMember = jsonElement.Deserialize<SerializedMember>(reflector);

            // A literal JSON `null` deserializes to a null SerializedMember - that is an explicit
            // "no value", not a failure.
            if (serializedMember == null)
                return reflector.GetDefaultValue(type);

            if (serializedMember.valueJsonElement == null)
                return reflector.CreateInstance(type);

            return reflector.Deserialize(serializedMember,
                fallbackType: type,
                fallbackName: name,
                depth: depth,
                logs: logs,
                logger: logger);
        }

    }
}
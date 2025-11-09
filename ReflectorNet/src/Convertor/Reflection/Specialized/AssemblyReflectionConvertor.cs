/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    /// <summary>
    /// Specialized converter for System.Reflection.Assembly that serializes assemblies as their
    /// full name and deserializes them back using Assembly.Load(). This ensures that Assembly
    /// instances can be serialized and deserialized without data loss while treating them as read-only.
    /// </summary>
    public class AssemblyReflectionConvertor : IgnoreFieldsAndPropertiesReflectionConvertor<Assembly>
    {
        public AssemblyReflectionConvertor() : base(ignoreFields: true, ignoreProperties: true)
        {
        }

        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (obj is Assembly assemblyObj)
            {
                var assemblyName = assemblyObj.FullName ?? assemblyObj.GetName().Name ?? string.Empty;
                return SerializedMember.FromValue(reflector, type, assemblyName, name: name);
            }

            return base.InternalSerialize(reflector, obj, type, name, recursive, flags, depth, stringBuilder, logger);
        }

        public override object? CreateInstance(Reflector reflector, Type type)
        {
            // Return executing assembly as placeholder - actual deserialization happens in TryDeserializeValueInternal
            return Assembly.GetExecutingAssembly();
        }

        protected override bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            result = null;

            if (!data.valueJsonElement.HasValue)
            {
                return false;
            }

            try
            {
                var assemblyName = data.valueJsonElement.Value.GetString();
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    // Try to find the assembly in currently loaded assemblies
                    var assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.FullName == assemblyName || a.GetName().Name == assemblyName);

                    // If not found, try to load it
                    if (assembly == null)
                    {
                        try
                        {
                            assembly = Assembly.Load(assemblyName);
                        }
                        catch
                        {
                            // Try loading by name only
                            var name = new AssemblyName(assemblyName);
                            assembly = Assembly.Load(name);
                        }
                    }

                    if (assembly != null)
                    {
                        result = assembly;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("Failed to deserialize Assembly from value '{Value}': {Message}", data.valueJsonElement, ex.Message);
                return false;
            }

            return false;
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            System.Text.Json.JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!value.HasValue)
            {
                return false;
            }

            try
            {
                var assemblyName = value.Value.GetString();
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    // Try to find the assembly in currently loaded assemblies
                    var assembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.FullName == assemblyName || a.GetName().Name == assemblyName);

                    // If not found, try to load it
                    if (assembly == null)
                    {
                        try
                        {
                            assembly = Assembly.Load(assemblyName);
                        }
                        catch
                        {
                            // Try loading by name only
                            var name = new AssemblyName(assemblyName);
                            assembly = Assembly.Load(name);
                        }
                    }

                    if (assembly != null)
                    {
                        obj = assembly;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("Failed to deserialize Assembly from value '{Value}': {Message}", value, ex.Message);
                return false;
            }

            return false;
        }
    }
}

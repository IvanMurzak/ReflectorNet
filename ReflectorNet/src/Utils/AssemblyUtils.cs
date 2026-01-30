/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class AssemblyUtils
    {
        /// <summary>
        /// Gets all assemblies loaded in the current application domain with exception protection.
        /// </summary>
        public static IEnumerable<Assembly> AllAssemblies
        {
            get
            {
                Assembly[] assemblies;
                try
                {
                    assemblies = AppDomain.CurrentDomain.GetAssemblies();
                }
                catch (AppDomainUnloadedException)
                {
                    yield break;
                }

                for (int i = 0; i < assemblies.Length; i++)
                {
                    yield return assemblies[i];
                }
            }
        }

        /// <summary>
        /// Gets all assemblies whose names start with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to match against assembly names.</param>
        /// <param name="comparison">The string comparison type to use. Defaults to <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>An enumerable of assemblies whose names start with the specified prefix.</returns>
        public static IEnumerable<Assembly> GetAssembliesStartingWith(string prefix, StringComparison comparison = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(prefix))
                yield break;

            foreach (var assembly in AllAssemblies)
            {
                var name = assembly.GetName().Name;
                if (name != null && name.StartsWith(prefix, comparison))
                    yield return assembly;
            }
        }

        /// <summary>
        /// Gets all types from all loaded assemblies with exception protection.
        /// </summary>
        public static IEnumerable<Type> AllTypes
        {
            get
            {
                foreach (var assembly in AllAssemblies)
                {
                    var types = GetAssemblyTypes(assembly);
                    for (int i = 0; i < types.Length; i++)
                    {
                        yield return types[i];
                    }
                }
            }
        }

        /// <summary>
        /// Gets all types from assemblies whose names start with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to match against assembly names.</param>
        /// <param name="comparison">The string comparison type to use. Defaults to <see cref="StringComparison.Ordinal"/>.</param>
        /// <returns>An enumerable of types from assemblies whose names start with the specified prefix.</returns>
        public static IEnumerable<Type> GetTypesStartingWith(string prefix, StringComparison comparison = StringComparison.Ordinal)
        {
            foreach (var assembly in GetAssembliesStartingWith(prefix, comparison))
            {
                var types = GetAssemblyTypes(assembly);
                for (int i = 0; i < types.Length; i++)
                {
                    yield return types[i];
                }
            }
        }

        /// <summary>
        /// Gets all types from an assembly.
        /// </summary>
        /// <remarks>
        /// This method handles exceptions gracefully to ensure robust type enumeration:
        /// <list type="bullet">
        /// <item><description><see cref="ReflectionTypeLoadException"/>: Returns only the types that loaded successfully.
        /// This commonly occurs when an assembly references types from dependencies that are not loaded.</description></item>
        /// <item><description>Other exceptions (e.g., <see cref="System.IO.FileNotFoundException"/>, <see cref="BadImageFormatException"/>):
        /// Returns an empty array. These can occur with dynamic assemblies, native assemblies, or corrupted modules.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="assembly">The assembly to get types from.</param>
        /// <returns>Array of types from the assembly. May be empty if the assembly cannot be inspected.</returns>
        public static Type[] GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types failed to load (e.g., missing dependencies).
                // Return the types that did load successfully.
                return ex.Types
                    ?.Where(t => t != null)
                    .Select(x => x!)
                    .ToArray() ?? Array.Empty<Type>();
            }
            catch
            {
                // Other exceptions (FileNotFoundException, BadImageFormatException, etc.)
                // can occur with dynamic assemblies, native assemblies, or corrupted modules.
                // Return empty array to allow enumeration to continue with other assemblies.
                return Array.Empty<Type>();
            }
        }
    }
}

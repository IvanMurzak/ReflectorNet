/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class AssemblyUtils
    {
        // Thread-safe cache for types per assembly
        private static readonly ConcurrentDictionary<Assembly, Type[]> _assemblyTypesCache = new();

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
        /// Gets all types from an assembly with thread-safe caching.
        /// </summary>
        /// <param name="assembly">The assembly to get types from.</param>
        /// <returns>Array of types from the assembly.</returns>
        public static Type[] GetAssemblyTypes(Assembly assembly)
        {
            return _assemblyTypesCache.GetOrAdd(assembly, asm =>
            {
                try
                {
                    return asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null).ToArray()!;
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            });
        }

        /// <summary>
        /// Clears the assembly types cache.
        /// </summary>
        public static void ClearAssemblyTypesCache()
        {
            _assemblyTypesCache.Clear();
        }
    }
}

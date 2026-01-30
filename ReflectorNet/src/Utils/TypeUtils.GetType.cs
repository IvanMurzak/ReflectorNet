using System;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Retrieves a <see cref="Type"/> by its name.
        /// </summary>
        /// <param name="typeName">The name of the type to retrieve. Can be a full name, assembly qualified name, or a custom identifier.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name, or <see langword="null"/> if the type cannot be found.</returns>
        public static Type? GetType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            if (_typeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;

            Type? type = null;
            try
            {
                type = Type.GetType(typeName, throwOnError: false);
            }
            catch
            {
            }

            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = TryResolveArrayType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = TryResolveCSharpGenericType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = TryResolveClassicGenericType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = AssemblyUtils.AllTypes.FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                typeName == t.GetTypeId());

            _typeCache[typeName] = type;

            return type;
        }

        /// <summary>
        /// Retrieves a <see cref="Type"/> by its name and (optionally) an assembly name prefix.
        /// </summary>
        /// <param name="assemblyName">
        /// The name, or prefix of the name, of the assembly containing the type. The value is used as a prefix,
        /// and the method will match any assembly whose name starts with this value.
        /// </param>
        /// <param name="typeName">The name of the type to retrieve.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name and assembly name prefix, or <see langword="null"/> if the type cannot be found.</returns>
        public static Type? GetType(string? assemblyName, string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            if (string.IsNullOrEmpty(assemblyName))
                return GetType(typeName);

            var cacheKey = $"{assemblyName}|{typeName}";
            if (_assemblyTypeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            Type? type = null;
            try
            {
                type = Type.GetType(typeName, throwOnError: false);
                if (type != null && !IsTypeInMatchingAssembly(type, assemblyName))
                    type = null;
            }
            catch
            {
            }

            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveArrayType(assemblyName, typeName);
            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveCSharpGenericType(assemblyName, typeName);
            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveClassicGenericType(assemblyName, typeName);
            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = AssemblyUtils.GetTypesStartingWith(assemblyName).FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                typeName == t.GetTypeId());

            _assemblyTypeCache[cacheKey] = type;

            return type;
        }

        /// <summary>
        /// Retrieves a <see cref="Type"/> by its name within a specific assembly.
        /// </summary>
        /// <param name="assembly">The assembly to search in.</param>
        /// <param name="typeName">The name of the type to retrieve.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name in the specified assembly, or <see langword="null"/> if the type cannot be found.</returns>
        public static Type? GetType(Assembly assembly, string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName) || assembly == null)
                return null;

            var cacheKey = $"{assembly.GetName().Name}|{typeName}";
            if (_exactAssemblyTypeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            Type? type = null;
            try
            {
                type = assembly.GetType(typeName, throwOnError: false);
            }
            catch
            {
            }

            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveArrayType(assembly, typeName);
            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveCSharpGenericType(assembly, typeName);
            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveClassicGenericType(assembly, typeName);
            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = AssemblyUtils.GetAssemblyTypes(assembly).FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                typeName == t.GetTypeId());

            _exactAssemblyTypeCache[cacheKey] = type;

            return type;
        }
    }
}

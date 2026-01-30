using System;
using System.Linq;

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
        /// Retrieves a <see cref="Type"/> by its name and assembly name.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly containing the type.</param>
        /// <param name="typeName">The name of the type to retrieve.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name and assembly, or <see langword="null"/> if the type cannot be found.</returns>
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
    }
}

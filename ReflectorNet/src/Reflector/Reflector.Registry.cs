using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Converter;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Manages the converter registry for the Reflector instance, implementing a priority-based
        /// Chain of Responsibility pattern for type conversion operations. This registry maintains
        /// a collection of reflection converters and provides efficient converter selection based
        /// on type compatibility and priority scoring.
        ///
        /// Core Functionality:
        /// - Converter Management: Add, remove, and enumerate registered converters
        /// - Priority-Based Selection: Automatically selects the best converter based on priority scores
        /// - Thread Safety: Uses ConcurrentBag for safe multi-threaded access
        /// - Default Converters: Pre-registers essential converters for common .NET types
        /// - Extensibility: Supports registration of custom converters for specialized types
        ///
        /// Selection Algorithm:
        /// 1. Queries all registered converters for their priority score with the target type
        /// 2. Filters converters with priority > 0 (indicating they can handle the type)
        /// 3. Orders converters by priority score in descending order
        /// 4. Returns the highest priority converter that can handle the type
        ///
        /// Default Converters:
        /// - PrimitiveReflectionConverter: Handles built-in .NET types (int, string, DateTime, etc.)
        /// - GenericReflectionConverter<object>: Handles custom classes and structs
        /// - ArrayReflectionConverter: Specialized handling for arrays and collections
        ///
        /// This architecture ensures that the most appropriate converter is always selected
        /// while maintaining flexibility for custom type handling through converter registration.
        /// </summary>
        public class Registry
        {
            ConcurrentBag<IReflectionConverter> _serializers = new ConcurrentBag<IReflectionConverter>();
            ConcurrentDictionary<Type, byte> _blacklistedTypes = new ConcurrentDictionary<Type, byte>();

            /// <summary>
            /// Initializes a new Registry instance with default converters for common .NET types.
            /// The default converters are registered in order of increasing specificity to ensure
            /// proper priority-based selection.
            /// </summary>
            public Registry()
            {
                // Basics
                Add(new PrimitiveReflectionConverter());
                Add(new ArrayReflectionConverter());
                Add(new GenericReflectionConverter<object>());

                // Specialized converters for read-only system types
                Add(new TypeReflectionConverter());
                Add(new AssemblyReflectionConverter());
            }

            /// <summary>
            /// Adds a new converter to the registry. The converter will be included in future
            /// converter selection operations based on its priority scoring for specific types.
            /// </summary>
            /// <param name="serializer">The converter to add to the registry. Null values are ignored.</param>
            public void Add(IReflectionConverter serializer)
            {
                if (serializer == null)
                    return;

                _serializers.Add(serializer);
            }

            /// <summary>
            /// Removes the first converter of the specified type from the registry.
            /// This operation creates a new ConcurrentBag excluding the removed converter.
            /// </summary>
            /// <typeparam name="T">The type of converter to remove.</typeparam>
            public void Remove<T>() where T : IReflectionConverter
            {
                var serializer = _serializers.FirstOrDefault(s => s is T);
                if (serializer == null)
                    return;

                _serializers = new ConcurrentBag<IReflectionConverter>(_serializers.Where(s => s != serializer));
            }

            /// <summary>
            /// Returns a read-only list of all registered converters.
            /// This method creates a snapshot of the current converter collection.
            /// </summary>
            /// <returns>A read-only list containing all registered converters.</returns>
            public IReadOnlyList<IReflectionConverter> GetAllSerializers() => _serializers.ToList();

            /// <summary>
            /// Adds a type to the blacklist, preventing it from being processed by any converter.
            /// </summary>
            /// <param name="type"></param>
            public void BlacklistType(Type type)
            {
                if (type == null)
                    return;

                _blacklistedTypes.TryAdd(type, 0);
            }

            /// <summary>
            /// Checks if a type is blacklisted. This includes:
            /// - The type itself being blacklisted
            /// - The type extending from a blacklisted type
            /// - Arrays of blacklisted types (or types extending from blacklisted types)
            /// - Generics containing blacklisted type arguments (or types extending from blacklisted types)
            /// </summary>
            /// <param name="type">The type to check.</param>
            /// <returns>True if the type is blacklisted; otherwise, false.</returns>
            public bool IsTypeBlacklisted(Type type)
            {
                if (type == null)
                    return false;

                // Check if the exact type is blacklisted
                if (_blacklistedTypes.ContainsKey(type))
                    return true;

                // Check if any base type in the inheritance chain is blacklisted
                var baseType = type.BaseType;
                while (baseType != null)
                {
                    if (_blacklistedTypes.ContainsKey(baseType))
                        return true;
                    baseType = baseType.BaseType;
                }

                // Check if any implemented interface is blacklisted
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (_blacklistedTypes.ContainsKey(interfaceType))
                        return true;
                }

                // Check if it's an array and the element type is blacklisted
                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    if (elementType != null && IsTypeBlacklisted(elementType))
                        return true;
                }

                // Check if it's a generic type and any type argument is blacklisted
                if (type.IsGenericType)
                {
                    foreach (var typeArg in type.GetGenericArguments())
                    {
                        if (IsTypeBlacklisted(typeArg))
                            return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Removes a type from the blacklist.
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public bool RemoveBlacklistedType(Type type)
            {
                return _blacklistedTypes.TryRemove(type, out _);
            }

            /// <summary>
            /// Returns a read-only list of all blacklisted types.
            /// </summary>
            /// <returns>A read-only list containing all blacklisted types.</returns>
            public IReadOnlyList<Type> GetAllBlacklistedTypes() => _blacklistedTypes.Keys.ToList();

            /// <summary>
            /// Finds all converters that can handle the specified type, ordered by priority.
            /// This method implements the core converter selection algorithm by querying
            /// each registered converter for its priority score and filtering/ordering the results.
            /// </summary>
            /// <param name="type">The type to find converters for.</param>
            /// <returns>An enumerable of converters ordered by descending priority score.</returns>
            IEnumerable<IReflectionConverter> FindRelevantConverters(Type type) => _serializers
                .Select(s => (s, s.SerializationPriority(type)))
                .Where(s => s.Item2 > 0)
                .OrderByDescending(s => s.Item2)
                .Select(s => s.s);

            /// <summary>
            /// Gets the highest priority converter that can handle the specified type.
            /// This method returns the most appropriate converter based on the priority
            /// scoring system implemented by each registered converter.
            /// </summary>
            /// <param name="type">The type to find a converter for.</param>
            /// <returns>The highest priority converter that can handle the type, or null if no suitable converter is found.</returns>
            public IReflectionConverter? GetConverter(Type type)
            {
                return FindRelevantConverters(type)
                    .FirstOrDefault();
            }
        }
    }
}

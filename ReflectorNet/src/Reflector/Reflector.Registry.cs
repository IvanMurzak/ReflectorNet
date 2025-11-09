using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Convertor;

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
        /// - PrimitiveReflectionConvertor: Handles built-in .NET types (int, string, DateTime, etc.)
        /// - GenericReflectionConvertor<object>: Handles custom classes and structs
        /// - ArrayReflectionConvertor: Specialized handling for arrays and collections
        ///
        /// This architecture ensures that the most appropriate converter is always selected
        /// while maintaining flexibility for custom type handling through converter registration.
        /// </summary>
        public class Registry
        {
            ConcurrentBag<IReflectionConvertor> _serializers = new ConcurrentBag<IReflectionConvertor>();

            /// <summary>
            /// Initializes a new Registry instance with default converters for common .NET types.
            /// The default converters are registered in order of increasing specificity to ensure
            /// proper priority-based selection.
            /// </summary>
            public Registry()
            {
                // Basics
                Add(new PrimitiveReflectionConvertor());
                Add(new ArrayReflectionConvertor());
                Add(new GenericReflectionConvertor<object>());

                // Specialized converters for read-only system types
                Add(new TypeReflectionConvertor());
                Add(new AssemblyReflectionConvertor());
            }

            /// <summary>
            /// Adds a new converter to the registry. The converter will be included in future
            /// converter selection operations based on its priority scoring for specific types.
            /// </summary>
            /// <param name="serializer">The converter to add to the registry. Null values are ignored.</param>
            public void Add(IReflectionConvertor serializer)
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
            public void Remove<T>() where T : IReflectionConvertor
            {
                var serializer = _serializers.FirstOrDefault(s => s is T);
                if (serializer == null)
                    return;

                _serializers = new ConcurrentBag<IReflectionConvertor>(_serializers.Where(s => s != serializer));
            }

            /// <summary>
            /// Returns a read-only list of all registered converters.
            /// This method creates a snapshot of the current converter collection.
            /// </summary>
            /// <returns>A read-only list containing all registered converters.</returns>
            public IReadOnlyList<IReflectionConvertor> GetAllSerializers() => _serializers.ToList();

            /// <summary>
            /// Finds all converters that can handle the specified type, ordered by priority.
            /// This method implements the core converter selection algorithm by querying
            /// each registered converter for its priority score and filtering/ordering the results.
            /// </summary>
            /// <param name="type">The type to find converters for.</param>
            /// <returns>An enumerable of converters ordered by descending priority score.</returns>
            IEnumerable<IReflectionConvertor> FindRelevantConvertors(Type type) => _serializers
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
            public IReflectionConvertor? GetConvertor(Type type)
            {
                return FindRelevantConvertors(type)
                    .FirstOrDefault();
            }
        }
    }
}

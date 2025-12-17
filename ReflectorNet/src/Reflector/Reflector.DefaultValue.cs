using System;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Creates a new instance of the specified type using the registered converter chain.
        /// This method provides intelligent instance creation with support for complex constructors,
        /// dependency injection scenarios, and custom instantiation logic defined by converters.
        ///
        /// Behavior:
        /// - Nullable handling: Automatically unwraps nullable types to their underlying type
        /// - Converter delegation: Uses the registered converter chain to handle type-specific instantiation
        /// - Constructor resolution: Supports parameterless constructors and constructors with parameters
        /// - Recursive creation: For parameterized constructors, recursively creates parameter instances
        /// - Value type handling: Properly handles structs, enums, and primitive types
        /// - Interface/abstract support: Provides intelligent defaults for interfaces and abstract classes
        /// - Custom logic: Allows converters to implement specialized instantiation for specific types
        ///
        /// This method is essential for deserialization scenarios where new object instances
        /// need to be created before population with serialized data.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <returns>A new instance of type T, or null if T is a reference type that cannot be instantiated.</returns>
        /// <exception cref="ArgumentException">Thrown when no converter supports the specified type.</exception>
        public T? CreateInstance<T>() => (T?)CreateInstance(typeof(T));

        /// <summary>
        /// Creates a new instance of the specified type using the registered converter chain.
        /// This method provides intelligent instance creation with support for complex constructors,
        /// dependency injection scenarios, and custom instantiation logic defined by converters.
        ///
        /// Behavior:
        /// - Nullable handling: Automatically unwraps nullable types to their underlying type
        /// - Converter delegation: Uses the registered converter chain to handle type-specific instantiation
        /// - Constructor resolution: Supports parameterless constructors and constructors with parameters
        /// - Recursive creation: For parameterized constructors, recursively creates parameter instances
        /// - Value type handling: Properly handles structs, enums, and primitive types
        /// - Interface/abstract support: Provides intelligent defaults for interfaces and abstract classes
        /// - Custom logic: Allows converters to implement specialized instantiation for specific types
        ///
        /// This method is essential for deserialization scenarios where new object instances
        /// need to be created before population with serialized data.
        /// </summary>
        /// <param name="type">The Type of instance to create.</param>
        /// <returns>A new instance of the specified type, or null if it's a reference type that cannot be instantiated.</returns>
        /// <exception cref="ArgumentException">Thrown when no converter supports the specified type.</exception>
        public object? CreateInstance(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            var converter = Converters.GetConverter(type);
            if (converter == null)
                throw new ArgumentException($"[Error] Type '{type?.GetTypeId().ValueOrNull()}' not supported for creating instance.");

            return converter.CreateInstance(this, type);
        }

        /// <summary>
        /// Retrieves the default value for the specified type using the registered converter chain.
        /// This method provides intelligent default value generation that goes beyond simple null/zero defaults,
        /// allowing converters to implement custom default value logic for specific types.
        ///
        /// Behavior:
        /// - Nullable handling: Returns null for nullable types regardless of underlying type
        /// - Value types: Returns the default value (typically zero/false) for structs and primitives
        /// - Enums: Returns the first enum value if available, otherwise the default enum value
        /// - Reference types: Returns null for classes, interfaces, and delegates
        /// - Custom defaults: Allows converters to override default value logic for specific types
        /// - Type safety: Ensures returned values are compatible with the requested type
        ///
        /// This method is crucial for scenarios where null is not appropriate (e.g., value types)
        /// or when custom default values are needed for specific business logic.
        /// </summary>
        /// <typeparam name="T">The type for which to get the default value.</typeparam>
        /// <returns>The default value for type T.</returns>
        /// <exception cref="ArgumentException">Thrown when no converter supports the specified type.</exception>
        public T? GetDefaultValue<T>() => (T?)GetDefaultValue(typeof(T));

        /// <summary>
        /// Retrieves the default value for the specified type using the registered converter chain.
        /// This method provides intelligent default value generation that goes beyond simple null/zero defaults,
        /// allowing converters to implement custom default value logic for specific types.
        ///
        /// Behavior:
        /// - Nullable handling: Returns null for nullable types regardless of underlying type
        /// - Value types: Returns the default value (typically zero/false) for structs and primitives
        /// - Enums: Returns the first enum value if available, otherwise the default enum value
        /// - Reference types: Returns null for classes, interfaces, and delegates
        /// - Custom defaults: Allows converters to override default value logic for specific types
        /// - Type safety: Ensures returned values are compatible with the requested type
        ///
        /// This method is crucial for scenarios where null is not appropriate (e.g., value types)
        /// or when custom default values are needed for specific business logic.
        /// </summary>
        /// <param name="type">The Type for which to get the default value.</param>
        /// <returns>The default value for the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when no converter supports the specified type.</exception>
        public object? GetDefaultValue(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            var converter = Converters.GetConverter(type);
            if (converter == null)
                throw new ArgumentException($"[Error] Type '{type?.GetTypeId().ValueOrNull()}' not supported for default value.");

            return converter.GetDefaultValue(this, type);
        }
    }
}

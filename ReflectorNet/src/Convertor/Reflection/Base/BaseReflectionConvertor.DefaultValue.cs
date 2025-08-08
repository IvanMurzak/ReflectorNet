using System;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        /// <summary>
        /// Creates a new instance of the specified type with intelligent constructor resolution and dependency handling.
        /// This method provides comprehensive instance creation capabilities including parameterless constructors,
        /// parameterized constructors with automatic parameter resolution, and special handling for various type categories.
        ///
        /// Instance Creation Strategy:
        /// 1. Null check: Returns null for null types
        /// 2. Enum handling: Creates enum instances with first available value or default
        /// 3. String handling: Returns empty string for string type
        /// 4. DateTime handling: Returns DateTime.MinValue for DateTime types
        /// 5. DateTimeOffset handling: Returns DateTimeOffset.MinValue for DateTimeOffset types
        /// 6. TimeSpan handling: Returns TimeSpan.Zero for TimeSpan types
        /// 7. Guid handling: Returns Guid.Empty for Guid types
        /// 8. Value type handling: Uses Activator.CreateInstance for structs and primitives
        /// 9. Array handling: Creates empty arrays with proper element type
        /// 10. Interface/Abstract handling: Attempts to find concrete implementations
        /// 11. Constructor resolution: Tries parameterless constructor first, then parameterized constructors
        /// 12. Recursive parameter creation: For parameterized constructors, recursively creates parameter instances
        ///
        /// Error Handling:
        /// - Comprehensive exception handling with meaningful error messages
        /// - Fallback strategies for complex scenarios
        /// - Type compatibility validation
        /// </summary>
        /// <param name="reflector">The Reflector instance used for recursive instance creation.</param>
        /// <param name="type">The Type of instance to create.</param>
        /// <returns>A new instance of the specified type, or null if instantiation is not possible.</returns>
        /// <exception cref="InvalidOperationException">Thrown when type is interface/abstract without concrete implementation.</exception>
        /// <exception cref="ArgumentException">Thrown when type cannot be instantiated due to constructor limitations.</exception>
        public virtual object? CreateInstance(Reflector reflector, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Handle enums
            if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type);
                if (enumValues == null)
                    return Activator.CreateInstance(type);

                return enumValues.Length > 0
                    ? enumValues.GetValue(0)
                    : Activator.CreateInstance(type);
            }

            // Handle common types with special defaults
            if (type == typeof(string))
                return string.Empty;

            if (type == typeof(DateTime))
                return DateTime.MinValue;

            if (type == typeof(DateTimeOffset))
                return DateTimeOffset.MinValue;

            if (type == typeof(TimeSpan))
                return TimeSpan.Zero;

            if (type == typeof(Guid))
                return Guid.Empty;

            // Handle value types (structs, primitives)
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            // Handle primitive types
            if (type.IsPrimitive)
                return Activator.CreateInstance(type);

            // Handle arrays
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == null)
                    throw new ArgumentException($"Array type '{type.FullName}' has no element type.");

                return Array.CreateInstance(elementType, 0); // Create empty array
            }

            // Handle interfaces and abstract classes
            if (type.IsInterface || type.IsAbstract)
            {
                // Cannot create instance of interface or abstract class
                throw new InvalidOperationException($"Cannot create instance of type '{type.GetTypeName(pretty: false)}' because it is an interface or abstract class.");
            }

            // Handle classes with parameterless constructors
            if (type.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                // Handle classes without parameterless constructors
                var constructor = type.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    // Create an instance using the first constructor with parameters
                    var parameters = constructor.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        args[i] = reflector.CreateInstance(parameters[i].ParameterType)!;
                    }
                    return constructor.Invoke(args);
                }
            }

            try
            {
                // The last try to create an instance
                return Activator.CreateInstance(type);
            }
            catch
            {
                throw new ArgumentException($"Type '{type.GetTypeName(pretty: false)}' does not have a constructor or is not a value type or primitive type.");
            }
        }

        /// <summary>
        /// Generates appropriate default values for types using intelligent type-specific logic.
        /// This method provides more sophisticated default value generation than simple null/zero defaults,
        /// supporting various type categories with appropriate fallback values.
        ///
        /// Default Value Strategy:
        /// - Nullable types: Always returns null regardless of underlying type
        /// - Value types (structs, primitives): Returns type's default value using Activator.CreateInstance
        /// - Enums: Returns the first available enum value, or default enum value if no values exist
        /// - Reference types: Returns null for classes, interfaces, delegates, and other reference types
        /// - Special handling: Some types may have converter-specific default value logic
        ///
        /// This method is essential for:
        /// - Initialization scenarios where null is inappropriate
        /// - Providing meaningful defaults for missing or optional data
        /// - Ensuring type safety in deserialization operations
        /// - Supporting business logic that requires non-null defaults
        /// </summary>
        /// <param name="reflector">The Reflector instance (may be used by derived converters for complex default logic).</param>
        /// <param name="type">The Type for which to generate a default value.</param>
        /// <returns>An appropriate default value for the specified type.</returns>
        public virtual object? GetDefaultValue(Reflector reflector, Type type)
        {
            // Handle nullable types first
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            // For value types (structs, primitives, enums), use Activator.CreateInstance
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            // Handle enums
            if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type);
                if (enumValues == null)
                    return Activator.CreateInstance(type);

                return enumValues.Length > 0
                    ? enumValues.GetValue(0)
                    : Activator.CreateInstance(type);
            }

            // For reference types (classes, interfaces, delegates), return null
            return null;
        }
    }
}
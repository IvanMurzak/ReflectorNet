using System;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
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
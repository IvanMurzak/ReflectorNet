using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    /// <summary>
    /// Provides methods to call methods on objects using reflection.
    /// This class allows you to find methods by their names, types, and parameters,
    /// </summary>
    public partial class Reflector
    {
        public static Reflector? Instance { get; private set; } = null;
        public Registry Convertors { get; }

        public Reflector()
        {
            Instance ??= this;
            Convertors = new Registry();
        }

        /// <summary>
        /// Serializes an object to a SerializedMember.
        /// </summary>
        public SerializedMember Serialize(object? obj, Type? type = null, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            type ??= obj?.GetType();

            if (type == null)
                throw new ArgumentException($"No type provided for serialization.");

            if (obj == null)
                return SerializedMember.FromJson(type, json: null, name: name);

            foreach (var serializer in Convertors.BuildSerializersChain(type))
            {
                logger?.LogTrace("[Serializer] {0} for type {1}", serializer.GetType().Name, type?.FullName);

                var serializedMember = serializer.Serialize(this, obj, type: type, name: name, recursive, flags, logger);
                if (serializedMember != null)
                    return serializedMember;
            }
            throw new ArgumentException($"[Error] Type '{type?.FullName ?? "null"}' not supported for serialization.");
        }
        /// <summary>
        /// Deserializes a SerializedMember to an object.
        /// </summary>
        public object? Deserialize(SerializedMember data, ILogger? logger = null)
        {
            if (data == null || string.IsNullOrEmpty(data.typeName))
                throw new ArgumentException(Error.DataTypeIsEmpty());

            var type = TypeUtils.GetType(data.typeName);
            if (type == null)
                throw new ArgumentException(Error.NotFoundType(data.typeName));

            var deserializer = Convertors.BuildDeserializersChain(type);
            if (deserializer == null)
                throw new ArgumentException($"[Error] Type '{type?.FullName ?? "null"}' not supported for deserialization.");

            logger?.LogTrace($"[Serializer] {deserializer.GetType().Name} for type {type?.FullName}");

            var obj = deserializer.Deserialize(this, data, logger);
            return obj;
        }

        /// <summary>
        /// Finds methods in the specified type that match the given criteria.
        /// </summary>
        public IEnumerable<FieldInfo>? GetSerializableFields(Type type,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
            => Convertors.BuildDeserializersChain(type)?.GetSerializableFields(this, type, flags, logger);

        /// <summary>
        /// Finds properties in the specified type that match the given criteria.
        /// </summary>
        public IEnumerable<PropertyInfo>? GetSerializableProperties(Type type,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
            => Convertors.BuildDeserializersChain(type)?.GetSerializableProperties(this, type, flags, logger);

        /// <summary>
        /// Populates an object with data from a SerializedMember.
        /// </summary>
        /// <param name="obj">The object to populate.</param>
        /// <param name="data">The SerializedMember containing the data.</param>
        /// <param name="dataType">The type of the data, if known.</param>
        /// <param name="stringBuilder">Optional StringBuilder to append errors or messages.</param>
        /// <param name="depth">The depth of the hierarchy to include.</param>
        /// <param name="flags">Binding flags to control the visibility of members.</param>
        /// <param name="logger">Optional logger for logging messages.</param>
        public StringBuilder Populate(ref object? obj, SerializedMember data, Type? dataType = null, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            stringBuilder ??= new StringBuilder();
            var padding = StringUtils.GetPadding(depth);

            var type = dataType;
            if (type == null)
            {
                if (string.IsNullOrEmpty(data?.typeName))
                    return stringBuilder.AppendLine(padding + Error.DataTypeIsEmpty());

                type = TypeUtils.GetType(data!.typeName);
                if (type == null)
                    return stringBuilder.AppendLine(padding + Error.NotFoundType(data.typeName));
            }

            if (obj == null)
                return stringBuilder.AppendLine(padding + Error.TargetObjectIsNull());

            TypeUtils.CastTo(obj, type, out var error);
            if (error != null)
                return stringBuilder.AppendLine(padding + error);

            if (!type.IsAssignableFrom(obj.GetType()))
                return stringBuilder.AppendLine(padding + Error.TypeMismatch(data.typeName, obj.GetType().FullName ?? string.Empty));

            foreach (var convertor in Convertors.BuildPopulatorsChain(type))
                convertor.Populate(this, ref obj, data, depth: depth, stringBuilder: stringBuilder, flags: flags, logger: logger);

            return stringBuilder;
        }

        /// <summary>
        /// Populates a property of an object with data from a SerializedMember.
        /// </summary>
        /// <param name="obj">The object containing the property to populate.</param>
        /// <param name="propertyInfo">The PropertyInfo of the property to populate.</param>
        /// <param name="data">The SerializedMember containing the data.</param>
        /// <param name="dataType">The type of the data, if known.</param>
        /// <param name="stringBuilder">Optional StringBuilder to append errors or messages.</param>
        /// <param name="depth">The depth of the hierarchy to include.</param>
        /// <param name="flags">Binding flags to control the visibility of members.</param>
        /// <param name="logger">Optional logger for logging messages.</param>
        public StringBuilder PopulateAsProperty(ref object? obj, PropertyInfo propertyInfo, SerializedMember data, Type? dataType = null, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            stringBuilder ??= new StringBuilder();
            var padding = StringUtils.GetPadding(depth);

            var type = dataType;
            if (type == null)
            {
                if (string.IsNullOrEmpty(data?.typeName))
                    return stringBuilder.AppendLine(padding + Error.DataTypeIsEmpty());

                type = TypeUtils.GetType(data!.typeName);
                if (type == null)
                    return stringBuilder.AppendLine(padding + Error.NotFoundType(data.typeName));
            }

            if (obj == null)
                return stringBuilder.AppendLine(padding + Error.TargetObjectIsNull());

            TypeUtils.CastTo(obj, type, out var error);
            if (error != null)
                return stringBuilder.AppendLine(padding + error);

            if (!type.IsAssignableFrom(obj.GetType()))
                return stringBuilder.AppendLine(padding + Error.TypeMismatch(data.typeName, obj.GetType().FullName ?? string.Empty));

            foreach (var convertor in Convertors.BuildPopulatorsChain(type))
                convertor.Populate(this, ref obj, data, depth: depth, stringBuilder: stringBuilder, flags: flags, logger: logger);

            return stringBuilder;
        }
    }
}

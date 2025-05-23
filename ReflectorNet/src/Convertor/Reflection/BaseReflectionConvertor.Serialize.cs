#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Data.Unity;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Reflection.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        protected virtual IEnumerable<string> ignoredFields => Enumerable.Empty<string>();
        protected virtual IEnumerable<string> ignoredProperties => Enumerable.Empty<string>();

        public virtual SerializedMember Serialize(Reflector reflector, object? obj, Type? type = null, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            type ??= obj?.GetType() ?? typeof(T);

            if (obj == null)
                return SerializedMember.FromJson(type, json: null, name: name);

            return InternalSerialize(reflector, obj, type, name, recursive, flags, logger);
        }

        protected virtual List<SerializedMember>? SerializeFields(Reflector reflector, object obj, BindingFlags flags, ILogger? logger = null)
        {
            var serializedFields = default(List<SerializedMember>);
            var objType = obj.GetType();

            var fields = GetSerializableFields(reflector, objType, flags, logger);
            if (fields == null)
                return null;

            foreach (var field in fields)
            {
                if (ignoredFields.Contains(field.Name))
                    continue;

                var value = field.GetValue(obj);
                var fieldType = field.FieldType;

                serializedFields ??= new List<SerializedMember>();
                serializedFields.Add(reflector.Serialize(value, fieldType, name: field.Name, recursive: false, flags: flags, logger: logger));
            }
            return serializedFields;
        }
        public abstract IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null);

        protected virtual List<SerializedMember>? SerializeProperties(Reflector reflector, object obj, BindingFlags flags, ILogger? logger = null)
        {
            var serializedProperties = default(List<SerializedMember>);
            var objType = obj.GetType();

            var properties = GetSerializableProperties(reflector, objType, flags, logger);
            if (properties == null)
                return null;

            foreach (var prop in properties)
            {
                if (ignoredProperties.Contains(prop.Name))
                    continue;
                try
                {
                    var value = prop.GetValue(obj);
                    var propType = prop.PropertyType;

                    serializedProperties ??= new List<SerializedMember>();
                    serializedProperties.Add(reflector.Serialize(value, propType, name: prop.Name, recursive: false, flags: flags, logger: logger));
                }
                catch { /* skip inaccessible properties */ }
            }
            return serializedProperties;
        }
        public abstract IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null);

        protected abstract SerializedMember InternalSerialize(Reflector reflector, object obj, Type type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);
    }
}
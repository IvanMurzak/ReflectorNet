using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        protected virtual IEnumerable<string> GetIgnoredFields() => Enumerable.Empty<string>();
        protected virtual IEnumerable<string> GetIgnoredProperties() => Enumerable.Empty<string>();

        public virtual SerializedMember Serialize(Reflector reflector, object? obj, Type? type = null, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0, StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            return InternalSerialize(reflector, obj,
                type: type ?? obj?.GetType() ?? typeof(T),
                name: name,
                recursive: recursive,
                flags: flags,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger);
        }

        protected virtual SerializedMemberList? SerializeFields(Reflector reflector, object obj, BindingFlags flags,
            int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            var serializedFields = default(SerializedMemberList);
            var objType = obj.GetType();

            var fields = GetSerializableFields(reflector, objType, flags, logger);
            if (fields == null)
                return null;

            foreach (var field in fields)
            {
                if (GetIgnoredFields().Contains(field.Name))
                    continue;

                var value = field.GetValue(obj);
                var fieldType = field.FieldType;

                serializedFields ??= new SerializedMemberList();
                serializedFields.Add(reflector.Serialize(value, fieldType,
                    name: field.Name,
                    recursive: AllowCascadeFieldsConversion,
                    flags: flags,
                    depth: depth + 1,
                    stringBuilder: stringBuilder,
                    logger: logger)
                );
            }
            return serializedFields;
        }
        public abstract IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null);

        protected virtual SerializedMemberList? SerializeProperties(Reflector reflector, object obj, BindingFlags flags,
            int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            var serializedProperties = default(SerializedMemberList);
            var objType = obj.GetType();

            var properties = GetSerializableProperties(reflector, objType, flags, logger);
            if (properties == null)
                return null;

            foreach (var prop in properties)
            {
                if (GetIgnoredProperties().Contains(prop.Name))
                    continue;
                try
                {
                    var value = prop.GetValue(obj);
                    var propType = prop.PropertyType;

                    serializedProperties ??= new SerializedMemberList();
                    serializedProperties.Add(reflector.Serialize(value, propType,
                        name: prop.Name,
                        recursive: AllowCascadePropertiesConversion,
                        flags: flags,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger)
                    );
                }
                catch { /* skip inaccessible properties */ }
            }
            return serializedProperties;
        }
        public abstract IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null);

        protected abstract SerializedMember InternalSerialize(Reflector reflector, object? obj, Type type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0, StringBuilder? stringBuilder = null,
            ILogger? logger = null);
    }
}
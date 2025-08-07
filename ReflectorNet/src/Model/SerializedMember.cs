using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Model
{
    [Serializable]
    public class SerializedMember
    {
        public const string ValueName = "value";

        [JsonInclude]
        [Description($"Object name.")]
        public string? name = string.Empty;

        [JsonInclude]
        [Description($"Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc.")]
        public string typeName = string.Empty;

        [JsonInclude]
        [Description($"Fields of the object, serialized as a list of '{nameof(SerializedMember)}'.")]
        public SerializedMemberList? fields;

        [JsonInclude]
        [Description($"Properties of the object, serialized as a list of '{nameof(SerializedMember)}'.")]
        public SerializedMemberList? props;

        [JsonInclude, JsonPropertyName(ValueName)]
        [Description($"Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json.")]
        public JsonElement? valueJsonElement = null;

        public SerializedMember() { }

        protected SerializedMember(Type type, string? name = null)
        {
            this.name = name;
            this.typeName = type.GetTypeName(pretty: false) ?? throw new ArgumentNullException(nameof(type));
        }

        public SerializedMember SetName(string? name)
        {
            this.name = name;
            return this;
        }

        public SerializedMember? GetField(string name)
            => fields?.FirstOrDefault(x => x.name == name);

        public SerializedMember SetFieldValue<T>(Reflector reflector, string name, T value)
        {
            var field = GetField(name);
            if (field == null)
            {
                field = SerializedMember.FromValue(reflector, typeof(T), value, name: name);
                fields ??= new SerializedMemberList();
                fields.Add(field);
                return this;
            }
            field.SetValue(reflector, value);
            return this;
        }

        public SerializedMember AddField(SerializedMember field)
        {
            fields ??= new SerializedMemberList();
            fields.Add(field);
            return this;
        }

        public SerializedMember? GetProperty(string name)
            => props?.FirstOrDefault(x => x.name == name);

        public SerializedMember SetPropertyValue<T>(Reflector reflector, string name, T value)
        {
            var property = GetProperty(name);
            if (property == null)
            {
                property = SerializedMember.FromValue(reflector, typeof(T), value, name: name);
                props ??= new SerializedMemberList();
                props.Add(property);
                return this;
            }
            property.SetValue(reflector, value);
            return this;
        }

        public SerializedMember AddProperty(SerializedMember property)
        {
            props ??= new SerializedMemberList();
            props.Add(property);
            return this;
        }

        public T? GetValue<T>(Reflector reflector) => valueJsonElement.Deserialize<T>(reflector);

        public SerializedMember SetValue(Reflector reflector, object? value)
        {
            var json = reflector.JsonSerializer.Serialize(value);
            return SetJsonValue(json);
        }
        public SerializedMember SetJsonValue(string? json)
        {
            if (StringUtils.IsNullOrEmpty(json))
            {
                valueJsonElement = null;
                return this;
            }
            using (var doc = JsonDocument.Parse(json!))
            {
                valueJsonElement = doc.RootElement.Clone();
            }
            return this;
        }
        public SerializedMember SetJsonValue(JsonElement jsonElement)
        {
            valueJsonElement = jsonElement;
            return this;
        }

        public static SerializedMember FromJson(Type type, JsonElement json, string? name = null)
            => new SerializedMember(type, name).SetJsonValue(json);

        public static SerializedMember FromJson(Type type, string? json, string? name = null)
            => new SerializedMember(type, name).SetJsonValue(json);

        public static SerializedMember FromValue(Reflector reflector, Type type, object? value, string? name = null)
            => new SerializedMember(type, name).SetValue(reflector, value);

        public static SerializedMember FromValue<T>(Reflector reflector, T? value, string? name = null)
            => new SerializedMember(typeof(T), name).SetValue(reflector, value);
    }
}
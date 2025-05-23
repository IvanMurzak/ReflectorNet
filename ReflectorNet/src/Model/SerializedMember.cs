#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Common.Data.Unity
{
    [Serializable]
    public class SerializedMember
    {
        public const string ValueName = "value";

        [JsonInclude] public string? name = string.Empty; // needed for Unity's JsonUtility serialization
        [JsonInclude] public string typeName = string.Empty; // needed for Unity's JsonUtility serialization
        [JsonInclude] public List<SerializedMember>? fields; // needed for Unity's JsonUtility serialization
        [JsonInclude] public List<SerializedMember>? props; // needed for Unity's JsonUtility serialization

        [JsonInclude, JsonPropertyName(ValueName)]
        public JsonElement? valueJsonElement = null; // System.Text.Json serialization

        public SerializedMember() { }

        protected SerializedMember(Type type, string? name = null)
        {
            this.name = name;
            this.typeName = type.FullName ?? throw new ArgumentNullException(nameof(type));
        }

        public SerializedMember SetName(string? name)
        {
            this.name = name;
            return this;
        }

        public SerializedMember? GetField(string name)
            => fields?.FirstOrDefault(x => x.name == name);

        public SerializedMember SetFieldValue<T>(string name, T value)
        {
            var field = GetField(name);
            if (field == null)
            {
                field = SerializedMember.FromValue(typeof(T), value, name: name);
                fields ??= new List<SerializedMember>();
                fields.Add(field);
                return this;
            }
            field.SetValue(value);
            return this;
        }

        public SerializedMember AddField(SerializedMember field)
        {
            fields ??= new List<SerializedMember>();
            fields.Add(field);
            return this;
        }

        public SerializedMember? GetProperty(string name)
            => props?.FirstOrDefault(x => x.name == name);

        public SerializedMember SetPropertyValue<T>(string name, T value)
        {
            var property = GetProperty(name);
            if (property == null)
            {
                property = SerializedMember.FromValue(typeof(T), value, name: name);
                props ??= new List<SerializedMember>();
                props.Add(property);
                return this;
            }
            property.SetValue(value);
            return this;
        }

        public SerializedMember AddProperty(SerializedMember property)
        {
            props ??= new List<SerializedMember>();
            props.Add(property);
            return this;
        }

        public T? GetValue<T>() where T : class
        {
            if (valueJsonElement == null)
                return default;
            try
            {
                return JsonUtils.Deserialize<T>(valueJsonElement.Value);
            }
            catch
            {
                return default;
            }
        }
        public SerializedMember SetValue(object? value)
        {
            var json = JsonUtils.Serialize(value);
            return SetJsonValue(json);
        }
        public SerializedMember SetJsonValue(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                valueJsonElement = null;
                return this;
            }
            using (var doc = JsonDocument.Parse(json))
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

        public static SerializedMember FromValue(Type type, object? value, string? name = null)
            => new SerializedMember(type, name).SetValue(value);

        public static SerializedMember FromValue<T>(T? value, string? name = null) where T : class
            => new SerializedMember(typeof(T), name).SetValue(value);
    }
}
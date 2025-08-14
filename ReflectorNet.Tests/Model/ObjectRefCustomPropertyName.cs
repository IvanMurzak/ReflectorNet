using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public class ModelWithDifferentFieldsAndProperties
    {
        public const string IntField = "int_Field";
        public const string IntFieldNullable = "int_Field_Nullable";
        public const string IntProperty = "int_Property";
        public const string IntPropertyNullable = "int_Property_Nullable";

        [JsonInclude, JsonPropertyName(IntField)]
        public int intField;

        [JsonInclude, JsonPropertyName(IntFieldNullable)]
        public int? intFieldNullable;

        [JsonInclude, JsonPropertyName(IntProperty)]
        public int intProperty { get; set; }

        [JsonInclude, JsonPropertyName(IntPropertyNullable)]
        public int? intPropertyNullable { get; set; }
    }
}
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public class ModelWithDifferentFieldsAndProperties
    {
        public const string IntField = "int_Field";
        public const string IntFieldNullable = "int_Field_Nullable";
        public const string IntFieldIgnored = "int_Field_Ignored";

        [JsonInclude, JsonPropertyName(IntField)]
        public int intField;

        [JsonInclude, JsonPropertyName(IntFieldNullable)]
        public int? intFieldNullable;

        [JsonIgnore, JsonPropertyName(IntFieldIgnored)]
        public int intFieldIgnored;

        // -------------------------------------------------------------

        public const string IntProperty = "int_Property";
        public const string IntPropertyNullable = "int_Property_Nullable";
        public const string IntPropertyIgnored = "int_Property_Ignored";
        public const string IntPropertyIgnoredReadOnly = "int_Property_Ignored_ReadOnly";

        [JsonInclude, JsonPropertyName(IntProperty)]
        public int intProperty { get; set; }

        [JsonInclude, JsonPropertyName(IntPropertyNullable)]
        public int? intPropertyNullable { get; set; }

        [JsonIgnore, JsonPropertyName(IntPropertyIgnored)]
        public int intPropertyIgnored { get; set; }

        [JsonIgnore, JsonPropertyName(IntPropertyIgnoredReadOnly)]
        public int intPropertyIgnoredReadOnly { get; }
    }
}
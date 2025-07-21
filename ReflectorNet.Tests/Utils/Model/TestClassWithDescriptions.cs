using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ReflectorNet.Tests.Schema.Model
{
    [Description("Test class with various member types for description testing.")]
    public class TestClassWithDescriptions
    {
        [Description("A simple string field with description.")]
        public string stringField = "default";

        [Description("An integer property with custom description.")]
        public int IntProperty { get; set; } = 42;

        [Description("A boolean field marked for JSON serialization.")]
        public bool booleanField;

        [JsonIgnore]
        [Description("This property should be ignored in JSON serialization.")]
        public string IgnoredProperty { get; set; } = "ignored";

        [Description("A field without getter/setter.")]
        public readonly double ReadOnlyField = 3.14;

        // Field without description
        public string noDescriptionField = "no desc";

        // Property without description
        public int NoDescriptionProperty { get; set; }
    }
}

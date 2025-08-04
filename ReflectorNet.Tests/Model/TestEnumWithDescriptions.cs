using System.ComponentModel;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    [Description("A test enum with descriptions for each value.")]
    public enum TestEnumWithDescriptions
    {
        [Description("The first option.")]
        Option1 = 1,

        [Description("The second option.")]
        Option2 = 2,

        [Description("The third option with a longer description that spans multiple words.")]
        Option3 = 3,

        // No description for this one
        Option4 = 4
    }
}

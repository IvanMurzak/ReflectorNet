using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Tests.Model;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public static partial class MethodHelper
    {
        [Description("Test method that accepts an enum parameter.")]
        public static string ProcessEnum
        (
            [Description("An enum value to process.")]
            TestEnumWithDescriptions enumValue
        )
        {
            return $"Processed enum: {enumValue}";
        }

        [Description("Test method that accepts an enum parameter with default value.")]
        public static string ProcessEnumWithDefault
        (
            [Description("An enum value to process with default.")]
            TestEnumWithDescriptions enumValue = TestEnumWithDescriptions.Option2
        )
        {
            return $"Processed enum with default: {enumValue}";
        }

        [Description("Test method that accepts both string and enum parameters.")]
        public static string ProcessStringAndEnum
        (
            [Description("A string parameter.")]
            string text,
            [Description("An enum parameter.")]
            TestEnumWithDescriptions enumValue
        )
        {
            return $"Text: {text}, Enum: {enumValue}";
        }
    }
}
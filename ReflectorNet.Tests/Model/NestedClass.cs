
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    // Non static class with nested classes and static members
    public class ParentClass
    {
        public class NestedClass
        {
            public static string NestedStaticField = "I am static field";
            public static string NestedStaticProperty { get; set; } = "I am static property";

            [JsonInclude]
            public string NestedField = "I am field";
            public string NestedProperty { get; set; } = "I am property";
        }
        public static class NestedStaticClass
        {
            public static string NestedStaticField = "I am static field";
            public static string NestedStaticProperty { get; set; } = "I am static property";
        }
    }
    // Static class with nested classes and static members
    public static class StaticParentClass
    {
        public class NestedClass
        {
            public static string NestedStaticField = "I am static field";
            public static string NestedStaticProperty { get; set; } = "I am static property";

            [JsonInclude]
            public string NestedField = "I am field";
            public string NestedProperty { get; set; } = "I am property";
        }
        public static class NestedStaticClass
        {
            public static string NestedStaticField = "I am static field";
            public static string NestedStaticProperty { get; set; } = "I am static property";
        }
    }
    // Wrapper class
    public class WrapperClass<T>
    {
        [JsonInclude]
        public T? ValueField;
        public T? ValueProperty { get; set; }

        public WrapperClass() { }
        public WrapperClass(T? valueField, T? valueProperty)
        {
            ValueField = valueField;
            ValueProperty = valueProperty;
        }
    }
}
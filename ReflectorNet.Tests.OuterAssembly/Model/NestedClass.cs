
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.OuterAssembly.Model
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

        /// <summary>
        /// Echo method that returns the provided value unchanged.
        /// Used for testing return type schemas with various generic types.
        /// </summary>
        public T Echo(T value) => value;

        /// <summary>
        /// Echo method that returns a nullable version of the provided value.
        /// Used for testing return type schemas with nullable generic types.
        /// </summary>
        public T? EchoNullable(T? value) => value;
    }
}
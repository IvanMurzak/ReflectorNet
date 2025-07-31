using System.Text;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Tests.Utils.Model;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Model;
using System.Collections.Generic;
using System;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class DeserializationTests : BaseTest
    {
        List<Type> _typesToTest = new()
        {
            typeof(ParentClass.NestedClass),
            typeof(StaticParentClass.NestedClass),
            typeof(WrapperClass<ParentClass.NestedClass[]>),
            typeof(WrapperClass<StaticParentClass.NestedClass[]>),
            typeof(ParentClass.NestedStaticClass),
            typeof(StaticParentClass.NestedStaticClass),
            typeof(int),
            typeof(string),
            typeof(List<string>),
            typeof(List<int>),
            typeof(List<ParentClass.NestedClass>),
            typeof(List<StaticParentClass.NestedClass>),
            typeof(List<WrapperClass<ParentClass.NestedClass[]>>),
            typeof(List<WrapperClass<StaticParentClass.NestedClass[]>>)
        };

        public DeserializationTests(ITestOutputHelper output) : base(output) { }

        void DeserializeNullValue<T>() where T : class
        {
            // Arrange
            var reflector = new Reflector();
            var jsonElement = JsonUtils.ToJsonElement(null);
            var serializedMember = SerializedMember.FromValue(default(T), name: TypeUtils.GetTypeShortName<T>());

            // Act
            var result = reflector.Deserialize<T>(serializedMember);

            // Assert
            Assert.Equal(TypeUtils.GetDefaultValue<T>(), result);
        }
        void DeserializeNullValue(Type type)
        {
            // Arrange
            var reflector = new Reflector();
            var jsonElement = JsonUtils.ToJsonElement(null);
            var serializedMember = SerializedMember.FromValue(
                type: type,
                value: null,
                name: TypeUtils.GetTypeShortName(type));

            // Act
            var result = reflector.Deserialize(serializedMember);

            // Assert
            Assert.Equal(TypeUtils.GetDefaultValue(type), result);
        }

        [Fact]
        public void Deserialize_NullValue_ReturnsDefault()
        {
            foreach (var type in _typesToTest)
            {
                DeserializeNullValue(type);
            }
        }
    }
}

using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;
using System.Collections.Generic;
using System;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class SerializeDeserializationTests : BaseTest
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

        public SerializeDeserializationTests(ITestOutputHelper output) : base(output) { }

        void ActAssert(object? original, Type? fallbackType = null)
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var serializeLogger = new StringBuilderLogger();
            var serialized = reflector.Serialize(
                original,
                fallbackType: fallbackType,
                name: nameof(original),
                logger: serializeLogger);

            _output.WriteLine($"Serialization:\n{serializeLogger}");

            var deserializeLogger = new StringBuilderLogger();
            var deserialized = reflector.Deserialize(
                serialized,
                logger: deserializeLogger);

            _output.WriteLine($"Deserialization:\n{deserializeLogger}");

            // Assert
            var originalJson = JsonUtils.ToJson(original);
            var deserializedJson = JsonUtils.ToJson(deserialized);

            _output.WriteLine($"----------------------------\n");

            _output.WriteLine($"Original JSON:\n{originalJson}\n");
            _output.WriteLine($"Deserialized JSON:\n{deserializedJson}\n");

            _output.WriteLine($"----------------------------\n");

            _output.WriteLine($"Serialized JSON:\n{JsonUtils.ToJson(serialized)}\n");

            Assert.Equal(originalJson, deserializedJson);
        }

        [Fact]
        public void DefaultNonNullValues()
        {
            foreach (var type in _typesToTest)
                ActAssert(TypeUtils.GetDefaultNonNullValue(type), fallbackType: type);
        }

        [Fact]
        public void DefaultValues()
        {
            foreach (var type in _typesToTest)
                ActAssert(TypeUtils.GetDefaultValue(type), fallbackType: type);
        }

        [Fact]
        public void SolarSystem()
        {
            ActAssert(new SolarSystem()
            {
                sun = new GameObjectRef() { instanceID = 1 },
                celestialBodies = new SolarSystem.CelestialBody[]
                {
                    new SolarSystem.CelestialBody
                    {
                        gameObject = new GameObjectRef() { instanceID = 2 },
                        orbitRadius = 149.6f,
                        orbitSpeed = 1.0f,
                        rotationSpeed = 365.25f
                    },
                    new SolarSystem.CelestialBody
                    {
                        gameObject = new GameObjectRef() { instanceID = 3 },
                        orbitRadius = 227.9f,
                        orbitSpeed = 0.53f,
                        rotationSpeed = 687.0f
                    }
                },
                globalOrbitSpeedMultiplier = 1.0f,
                globalSizeMultiplier = 1.0f
            });
        }
        [Fact]
        public void WrappedField_SolarSystem()
        {
            ActAssert(new WrapperClass<SolarSystem>()
            {
                ValueField = new SolarSystem()
                {
                    sun = new GameObjectRef() { instanceID = 1 },
                    celestialBodies = new SolarSystem.CelestialBody[]
                    {
                        new SolarSystem.CelestialBody
                        {
                            gameObject = new GameObjectRef() { instanceID = 2 },
                            orbitRadius = 149.6f,
                            orbitSpeed = 1.0f,
                            rotationSpeed = 365.25f
                        },
                        new SolarSystem.CelestialBody
                        {
                            gameObject = new GameObjectRef() { instanceID = 3 },
                            orbitRadius = 227.9f,
                            orbitSpeed = 0.53f,
                            rotationSpeed = 687.0f
                        }
                    },
                    globalOrbitSpeedMultiplier = 1.0f,
                    globalSizeMultiplier = 1.0f
                }
            });
        }
        [Fact]
        public void WrappedProperty_SolarSystem()
        {
            ActAssert(new WrapperClass<SolarSystem>()
            {
                ValueProperty = new SolarSystem()
                {
                    sun = new GameObjectRef() { instanceID = 1 },
                    celestialBodies = new SolarSystem.CelestialBody[]
                    {
                        new SolarSystem.CelestialBody
                        {
                            gameObject = new GameObjectRef() { instanceID = 2 },
                            orbitRadius = 149.6f,
                            orbitSpeed = 1.0f,
                            rotationSpeed = 365.25f
                        },
                        new SolarSystem.CelestialBody
                        {
                            gameObject = new GameObjectRef() { instanceID = 3 },
                            orbitRadius = 227.9f,
                            orbitSpeed = 0.53f,
                            rotationSpeed = 687.0f
                        }
                    },
                    globalOrbitSpeedMultiplier = 1.0f,
                    globalSizeMultiplier = 1.0f
                }
            });
        }

        [Fact]
        public void SolarSystemArray()
        {
            ActAssert(new SolarSystem[]
            {
                new SolarSystem()
                {
                    sun = new GameObjectRef() { instanceID = 1 },
                    celestialBodies = new SolarSystem.CelestialBody[]
                    {
                        new SolarSystem.CelestialBody
                        {
                            gameObject = new GameObjectRef() { instanceID = 2 },
                            orbitRadius = 149.6f,
                            orbitSpeed = 1.0f,
                            rotationSpeed = 365.25f
                        },
                        new SolarSystem.CelestialBody
                        {
                            gameObject = new GameObjectRef() { instanceID = 3 },
                            orbitRadius = 227.9f,
                            orbitSpeed = 0.53f,
                            rotationSpeed = 687.0f
                        }
                    },
                    globalOrbitSpeedMultiplier = 1.0f,
                    globalSizeMultiplier = 1.0f
                }
            });
        }
        [Fact]
        public void SolarSystemArrayArray()
        {
            ActAssert(new SolarSystem[][]
            {
                new SolarSystem[]
                {
                    new SolarSystem()
                    {
                        sun = new GameObjectRef() { instanceID = 1 },
                        celestialBodies = new SolarSystem.CelestialBody[]
                        {
                            new SolarSystem.CelestialBody
                            {
                                gameObject = new GameObjectRef() { instanceID = 2 },
                                orbitRadius = 149.6f,
                                orbitSpeed = 1.0f,
                                rotationSpeed = 365.25f
                            },
                            new SolarSystem.CelestialBody
                            {
                                gameObject = new GameObjectRef() { instanceID = 3 },
                                orbitRadius = 227.9f,
                                orbitSpeed = 0.53f,
                                rotationSpeed = 687.0f
                            }
                        },
                        globalOrbitSpeedMultiplier = 1.0f,
                        globalSizeMultiplier = 1.0f
                    }
                }
            });
        }
    }
}

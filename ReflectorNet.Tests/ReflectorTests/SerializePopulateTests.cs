using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;
using System;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class SerializePopulateTests : BaseTest
    {
        public SerializePopulateTests(ITestOutputHelper output) : base(output) { }

        void ActAssertPopulate(object? original, Type? fallbackType = null)
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

            var type = original?.GetType() ?? fallbackType ?? throw new ArgumentNullException(nameof(original));
            var targetObject = TypeUtils.CreateInstance(type);

            var populateLogger = new StringBuilderLogger();
            var populateOutput = reflector.Populate(
                obj: ref targetObject,
                data: serialized,
                fallbackObjType: type,
                stringBuilder: new(),
                logger: populateLogger);

            _output.WriteLine($"Population:\n{populateLogger}");

            // Assert
            var originalJson = JsonUtils.ToJson(original);
            var populatedJson = JsonUtils.ToJson(targetObject);

            _output.WriteLine($"----------------------------\n");

            _output.WriteLine($"Original JSON:\n{originalJson}\n");
            _output.WriteLine($"Populated JSON:\n{populatedJson}\n");

            _output.WriteLine($"----------------------------\n");

            _output.WriteLine($"Serialized JSON:\n{JsonUtils.ToJson(serialized)}\n");

            Assert.Equal(originalJson, populatedJson);
        }

        [Fact]
        public void DefaultNonNullValues()
        {
            foreach (var type in TestTypeGroups.AllNonStaticTypes)
                ActAssertPopulate(TypeUtils.CreateInstance(type), fallbackType: type);
        }

        [Fact]
        public void DefaultValues()
        {
            foreach (var type in TestTypeGroups.AllNonStaticTypes)
                ActAssertPopulate(TypeUtils.GetDefaultValue(type), fallbackType: type);
        }

        [Fact]
        public void SolarSystem()
        {
            ActAssertPopulate(new SolarSystem()
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
            ActAssertPopulate(new WrapperClass<SolarSystem>()
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
            ActAssertPopulate(new WrapperClass<SolarSystem>()
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
            ActAssertPopulate(new SolarSystem[]
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
            ActAssertPopulate(new SolarSystem[][]
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

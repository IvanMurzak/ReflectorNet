using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;
using System;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class SerializeModifyTests : BaseTest
    {
        public SerializeModifyTests(ITestOutputHelper output) : base(output) { }

        void ActAssertModify(object? original, Type? fallbackType = null)
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
            var targetObject = reflector.CreateInstance(type);

            var modifyLogger = new StringBuilderLogger();
            var success = reflector.TryModify(
                obj: ref targetObject,
                data: serialized,
                fallbackObjType: type,
                logs: new Logs(),
                logger: modifyLogger);

            Assert.True(success);

            _output.WriteLine($"Modification:\n{modifyLogger}");

            // Assert
            var originalJson = original.ToJson(reflector);
            var modifiedJson = targetObject.ToJson(reflector);

            _output.WriteLine($"----------------------------\n");

            _output.WriteLine($"Original JSON:\n{originalJson}\n");
            _output.WriteLine($"Modified JSON:\n{modifiedJson}\n");

            _output.WriteLine($"----------------------------\n");

            _output.WriteLine($"Serialized JSON:\n{serialized.ToJson(reflector)}\n");

            Assert.Equal(originalJson, modifiedJson);

            _output.WriteLine($"============================\n");
        }

        [Fact]
        public void DefaultNonNullValues()
        {
            var reflector = new Reflector();

            foreach (var type in TestUtils.Types.AllBaseNonStaticTypes)
                ActAssertModify(reflector.CreateInstance(type), fallbackType: type);
        }

        [Fact]
        public void DefaultValues()
        {
            var reflector = new Reflector();

            foreach (var type in TestUtils.Types.AllBaseNonStaticTypes)
                ActAssertModify(reflector.GetDefaultValue(type), fallbackType: type);
        }

        [Fact]
        public void SolarSystem()
        {
            ActAssertModify(new SolarSystem()
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
            ActAssertModify(new WrapperClass<SolarSystem>()
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
            ActAssertModify(new WrapperClass<SolarSystem>()
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
            ActAssertModify(new SolarSystem[]
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
            ActAssertModify(new SolarSystem[][]
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

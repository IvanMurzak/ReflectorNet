using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public class AtomicModifyTests : BaseTest
    {
        public AtomicModifyTests(ITestOutputHelper output) : base(output) { }

        // ─── TryModifyAt — root field ─────────────────────────────────────────────

        [Fact]
        public void TryModifyAt_RootField()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f, globalSizeMultiplier = 2f };
            object? obj = system;

            var success = reflector.TryModifyAt<float>(ref obj, "globalOrbitSpeedMultiplier", 5f);

            Assert.True(success);
            var result = (SolarSystem)obj!;
            Assert.Equal(5f, result.globalOrbitSpeedMultiplier);
            Assert.Equal(2f, result.globalSizeMultiplier); // untouched
        }

        // ─── TryModifyAt — two-level nested field ─────────────────────────────────

        [Fact]
        public void TryModifyAt_TwoLevelField_Property()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { sun = new GameObjectRef { instanceID = 1 } };
            object? obj = system;

            var success = reflector.TryModifyAt<int>(ref obj, "sun/instanceID", 99);

            Assert.True(success);
            Assert.Equal(99, ((SolarSystem)obj!).sun!.instanceID);
        }

        // ─── TryModifyAt — array element field ────────────────────────────────────

        [Fact]
        public void TryModifyAt_ArrayElementField()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f, orbitSpeed = 1f },
                    new SolarSystem.CelestialBody { orbitRadius = 20f, orbitSpeed = 2f },
                }
            };
            object? obj = system;

            // Modify only [0].orbitRadius — nothing else should change
            var success = reflector.TryModifyAt<float>(ref obj, "celestialBodies/[0]/orbitRadius", 999f);

            Assert.True(success);
            var result = (SolarSystem)obj!;
            Assert.Equal(999f, result.celestialBodies![0].orbitRadius);
            Assert.Equal(1f,   result.celestialBodies![0].orbitSpeed);  // untouched
            Assert.Equal(20f,  result.celestialBodies![1].orbitRadius); // untouched
            Assert.Equal(2f,   result.celestialBodies![1].orbitSpeed);  // untouched

            _output.WriteLine($"celestialBodies[0].orbitRadius = {result.celestialBodies[0].orbitRadius}");
        }

        // ─── TryModifyAt — partial update of array element (SerializedMember) ─────

        [Fact]
        public void TryModifyAt_PartialPatch_ArrayElement()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f, orbitSpeed = 3f },
                    new SolarSystem.CelestialBody { orbitRadius = 20f, orbitSpeed = 4f },
                }
            };
            object? obj = system;

            // Navigate to [1] and apply a partial patch — only orbitRadius changes
            var patch = new SerializedMember { typeName = typeof(SolarSystem.CelestialBody).GetTypeId() ?? string.Empty };
            patch.SetFieldValue(reflector, "orbitRadius", 777f);

            var logs = new Logs();
            var success = reflector.TryModifyAt(ref obj, "celestialBodies/[1]", patch, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (SolarSystem)obj!;
            Assert.Equal(777f, result.celestialBodies![1].orbitRadius);
            Assert.Equal(4f,   result.celestialBodies![1].orbitSpeed);  // untouched
            Assert.Equal(10f,  result.celestialBodies![0].orbitRadius); // untouched
        }

        // ─── TryModifyAt — invalid member — detailed error ────────────────────────

        [Fact]
        public void TryModifyAt_InvalidMember_DetailedError()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;
            var logs = new Logs();

            var success = reflector.TryModifyAt<float>(ref obj, "doesNotExist", 5f, logs: logs);

            Assert.False(success);
            var logsText = logs.ToString();
            _output.WriteLine(logsText);
            Assert.Contains("doesNotExist", logsText);
            Assert.Contains("not found", logsText);
        }

        [Fact]
        public void TryModifyAt_NestedInvalidSegment_DetailedError()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { sun = new GameObjectRef { instanceID = 1 } };
            object? obj = system;
            var logs = new Logs();

            var success = reflector.TryModifyAt<int>(ref obj, "sun/badSegment", 0, logs: logs);

            Assert.False(success);
            var logsText = logs.ToString();
            _output.WriteLine(logsText);
            Assert.Contains("badSegment", logsText);
        }

        // ─── TryModifyAt — out-of-bounds index — detailed error ───────────────────

        [Fact]
        public void TryModifyAt_OutOfBoundsIndex_DetailedError()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 1f },
                    new SolarSystem.CelestialBody { orbitRadius = 2f },
                }
            };
            object? obj = system;
            var logs = new Logs();

            var success = reflector.TryModifyAt<float>(ref obj, "celestialBodies/[99]/orbitRadius", 0f, logs: logs);

            Assert.False(success);
            var logsText = logs.ToString();
            _output.WriteLine(logsText);
            Assert.Contains("[99]", logsText);
            Assert.Contains("out of range", logsText);
            // Array should be unchanged
            Assert.Equal(1f, ((SolarSystem)obj!).celestialBodies![0].orbitRadius);
        }

        // ─── TryModifyAt — Dictionary (string key) ────────────────────────────────

        [Fact]
        public void TryModifyAt_DictionaryStringKey()
        {
            var reflector = new Reflector();

            var container = new DictionaryContainer
            {
                config = new Dictionary<string, int> { ["timeout"] = 10, ["retries"] = 3 }
            };
            object? obj = container;

            var success = reflector.TryModifyAt<int>(ref obj, "config/[timeout]", 60);

            Assert.True(success);
            var result = (DictionaryContainer)obj!;
            Assert.Equal(60, result.config["timeout"]);
            Assert.Equal(3,  result.config["retries"]); // untouched
        }

        // ─── TryModifyAt — Dictionary (integer key) ───────────────────────────────

        [Fact]
        public void TryModifyAt_DictionaryIntKey()
        {
            var reflector = new Reflector();

            var container = new IntDictionaryContainer
            {
                lookup = new Dictionary<int, string> { [1] = "one", [2] = "two", [3] = "three" }
            };
            object? obj = container;

            var success = reflector.TryModifyAt<string>(ref obj, "lookup/[2]", "TWO");

            Assert.True(success);
            var result = (IntDictionaryContainer)obj!;
            Assert.Equal("TWO",   result.lookup[2]);
            Assert.Equal("one",   result.lookup[1]); // untouched
            Assert.Equal("three", result.lookup[3]); // untouched
        }

        // ─── TryModifyAt — generic overload (leaf value) ──────────────────────────

        [Fact]
        public void TryModifyAt_GenericOverload_LeafValue()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalSizeMultiplier = 1f };
            object? obj = system;

            var success = reflector.TryModifyAt<float>(ref obj, "globalSizeMultiplier", 3.14f);

            Assert.True(success);
            Assert.Equal(3.14f, ((SolarSystem)obj!).globalSizeMultiplier, precision: 5);
        }

        // ─── TryModifyAt — hash-prefixed path ────────────────────────────────────

        [Fact]
        public void TryModifyAt_HashPrefixedPath_IsStripped()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            var success = reflector.TryModifyAt<float>(ref obj, "#/globalOrbitSpeedMultiplier", 7f);

            Assert.True(success);
            Assert.Equal(7f, ((SolarSystem)obj!).globalOrbitSpeedMultiplier);
        }

        // ─── ArrayReflectionConverter.TryModify — partial element via SerializedMember ──

        [Fact]
        public void ArrayConverter_PartialElementModify_ViaSerializedMember()
        {
            var reflector = new Reflector();
            object? celestialBodies = new SolarSystem.CelestialBody[]
            {
                new SolarSystem.CelestialBody { orbitRadius = 10f, orbitSpeed = 1f },
                new SolarSystem.CelestialBody { orbitRadius = 20f, orbitSpeed = 2f },
            };

            // Build a SerializedMember that only specifies element [1].orbitRadius
            var elem1 = new SerializedMember
            {
                name = "[1]",
                typeName = typeof(SolarSystem.CelestialBody).GetTypeId() ?? string.Empty
            };
            elem1.SetFieldValue(reflector, "orbitRadius", 88f);

            var data = new SerializedMember
            {
                typeName = typeof(SolarSystem.CelestialBody[]).GetTypeId() ?? string.Empty
            };
            data.AddField(elem1);

            var logs = new Logs();
            var success = reflector.TryModify(ref celestialBodies, data, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var arr = (SolarSystem.CelestialBody[])celestialBodies!;
            Assert.Equal(88f, arr[1].orbitRadius);
            Assert.Equal(2f,  arr[1].orbitSpeed);  // untouched
            Assert.Equal(10f, arr[0].orbitRadius); // untouched
            Assert.Equal(1f,  arr[0].orbitSpeed);  // untouched
        }

        // ─── TryPatch — multiple fields at different depths ────────────────────────

        [Fact]
        public void TryPatch_MultipleFieldsAtOnce()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                globalOrbitSpeedMultiplier = 1f,
                globalSizeMultiplier = 1f,
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f, orbitSpeed = 1f },
                    new SolarSystem.CelestialBody { orbitRadius = 20f, orbitSpeed = 2f },
                }
            };
            object? obj = system;

            var json = @"{
                ""globalOrbitSpeedMultiplier"": 5.0,
                ""globalSizeMultiplier"": 2.0
            }";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (SolarSystem)obj!;
            Assert.Equal(5f, result.globalOrbitSpeedMultiplier);
            Assert.Equal(2f, result.globalSizeMultiplier);
            Assert.Equal(10f, result.celestialBodies![0].orbitRadius); // untouched
        }

        [Fact]
        public void TryPatch_ArrayElementAndField()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f, orbitSpeed = 1f },
                    new SolarSystem.CelestialBody { orbitRadius = 20f, orbitSpeed = 2f },
                }
            };
            object? obj = system;

            // Modify celestialBodies[0].orbitRadius via JSON patch
            var json = @"{
                ""celestialBodies"": {
                    ""[0]"": {
                        ""orbitRadius"": 42.0
                    }
                }
            }";

            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, json, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            var result = (SolarSystem)obj!;
            Assert.Equal(42f, result.celestialBodies![0].orbitRadius);
            Assert.Equal(1f,  result.celestialBodies![0].orbitSpeed);  // untouched
            Assert.Equal(20f, result.celestialBodies![1].orbitRadius); // untouched
        }

        [Fact]
        public void TryPatch_JsonElement_Overload()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            using var doc = JsonDocument.Parse(@"{ ""globalOrbitSpeedMultiplier"": 9.0 }");
            var logs = new Logs();
            var success = reflector.TryPatch(ref obj, doc.RootElement, logs: logs);

            _output.WriteLine(logs.ToString());
            Assert.True(success);
            Assert.Equal(9f, ((SolarSystem)obj!).globalOrbitSpeedMultiplier);
        }

        // ─── Test-local helper types ───────────────────────────────────────────────

        private class DictionaryContainer
        {
            public Dictionary<string, int> config = new Dictionary<string, int>();
        }

        private class IntDictionaryContainer
        {
            public Dictionary<int, string> lookup = new Dictionary<int, string>();
        }
    }
}

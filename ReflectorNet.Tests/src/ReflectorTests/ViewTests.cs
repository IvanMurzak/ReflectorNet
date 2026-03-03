using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public class ViewTests : BaseTest
    {
        public ViewTests(ITestOutputHelper output) : base(output) { }

        // ─── View — default (no query) ────────────────────────────────────────────

        [Fact]
        public void View_Default_MatchesSerialize()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                globalOrbitSpeedMultiplier = 2f,
                globalSizeMultiplier = 3f,
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f, orbitSpeed = 1f }
                }
            };
            object? obj = system;

            var viewed = reflector.View(obj);
            var serialized = reflector.Serialize(system);

            Assert.NotNull(viewed);
            Assert.Equal(serialized.typeName, viewed.typeName);
            Assert.NotNull(viewed.fields);
            Assert.Equal(serialized.fields?.Count, viewed.fields?.Count);
        }

        // ─── View — Path navigates to subtree ────────────────────────────────────

        [Fact]
        public void View_Path_ReturnsSubTree()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 42f, orbitSpeed = 7f },
                    new SolarSystem.CelestialBody { orbitRadius = 99f, orbitSpeed = 9f },
                }
            };
            object? obj = system;

            var result = reflector.View(obj, new ViewQuery { Path = "celestialBodies/[0]" });

            Assert.NotNull(result);
            // Should have typeName of CelestialBody
            Assert.Contains("CelestialBody", result.typeName);
            // Should NOT contain data from [1]
            var orbitRadius = result.fields?.FirstOrDefault(f => f.name == "orbitRadius");
            Assert.NotNull(orbitRadius);
            Assert.Equal(42f, orbitRadius.GetValue<float>(reflector));
        }

        // ─── View — MaxDepth = 0 strips all children ─────────────────────────────

        [Fact]
        public void View_MaxDepth_Zero_RootOnly()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            var result = reflector.View(obj, new ViewQuery { MaxDepth = 0 });

            Assert.NotNull(result);
            Assert.Contains("SolarSystem", result.typeName);
            Assert.True(result.fields == null || result.fields.Count == 0,
                "MaxDepth=0 should strip all nested fields");
        }

        // ─── View — MaxDepth = 1 keeps top-level fields, strips their children ───

        [Fact]
        public void View_MaxDepth_One_TopLevel()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 5f }
                }
            };
            object? obj = system;

            var result = reflector.View(obj, new ViewQuery { MaxDepth = 1 });

            Assert.NotNull(result);
            // celestialBodies field should be present ...
            var bodiesField = result.fields?.FirstOrDefault(f => f.name == "celestialBodies");
            Assert.NotNull(bodiesField);
            // ... but its own fields should be stripped (MaxDepth=1 means children of root are visible, grandchildren stripped)
            // The array element [0] field should have no further fields
            var elem0 = bodiesField.fields?.FirstOrDefault();
            if (elem0 != null)
                Assert.True(elem0.fields == null || elem0.fields.Count == 0,
                    "MaxDepth=1 means grandchildren are stripped");
        }

        // ─── View — NamePattern keeps only matching direct fields ────────────────
        // Note: NamePattern filters the SerializedMember tree (fields/props).
        // Array elements stored in valueJsonElement are not individually filterable via NamePattern;
        // use Grep for deep pattern search across the live object graph.

        [Fact]
        public void View_NamePattern_SparseTree()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                globalOrbitSpeedMultiplier = 2f,
                globalSizeMultiplier = 3f,
            };
            object? obj = system;

            // "globalOrbit" (contains match, case-insensitive regex) matches globalOrbitSpeedMultiplier
            // but not globalSizeMultiplier, sun, or celestialBodies
            var result = reflector.View(obj, new ViewQuery { NamePattern = "globalOrbit" });

            Assert.NotNull(result);
            // globalOrbitSpeedMultiplier should be present
            var orbitField = result.fields?.FirstOrDefault(f => f.name == "globalOrbitSpeedMultiplier");
            Assert.NotNull(orbitField);
            // globalSizeMultiplier should be pruned (does not match "globalOrbit")
            Assert.True(result.fields == null || result.fields.All(f => f.name != "globalSizeMultiplier"),
                "globalSizeMultiplier should be pruned");
        }

        // ─── View — NamePattern with no matches returns root envelope ─────────────

        [Fact]
        public void View_NamePattern_NoMatch_EmptyEnvelope()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            var result = reflector.View(obj, new ViewQuery { NamePattern = "doesNotMatchAnything_xyz" });

            Assert.NotNull(result);
            Assert.Contains("SolarSystem", result.typeName);
            Assert.True(result.fields == null || result.fields.Count == 0,
                "No matching fields → empty envelope");
        }

        // ─── View — combined Path + NamePattern ───────────────────────────────────

        [Fact]
        public void View_CombinedPathAndPattern()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 7f, orbitSpeed = 2f, rotationSpeed = 9f }
                }
            };
            object? obj = system;

            // Navigate to [0] then filter by "^orbit"
            var result = reflector.View(obj, new ViewQuery { Path = "celestialBodies/[0]", NamePattern = "^orbit" });

            Assert.NotNull(result);
            Assert.Contains("CelestialBody", result.typeName);
            Assert.NotNull(result.fields);
            Assert.True(result.fields.All(f => f.name == null || f.name.StartsWith("orbit")),
                "Only orbit* fields should remain after pattern filter");
            Assert.True(result.fields.All(f => f.name != "rotationSpeed"),
                "rotationSpeed should be pruned");
        }

        // ─── TryReadAt — root field ───────────────────────────────────────────────

        [Fact]
        public void TryReadAt_RootField()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 5f };
            object? obj = system;

            var ok = reflector.TryReadAt(obj, "globalOrbitSpeedMultiplier", out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Equal(5f, result.GetValue<float>(reflector));
        }

        // ─── TryReadAt — array element ────────────────────────────────────────────

        [Fact]
        public void TryReadAt_ArrayElement()
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

            var ok = reflector.TryReadAt(obj, "celestialBodies/[0]", out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Contains("CelestialBody", result.typeName);
            var orbitRadius = result.fields?.FirstOrDefault(f => f.name == "orbitRadius");
            Assert.NotNull(orbitRadius);
            Assert.Equal(10f, orbitRadius.GetValue<float>(reflector));
        }

        // ─── TryReadAt — nested field ─────────────────────────────────────────────

        [Fact]
        public void TryReadAt_NestedField()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 42f }
                }
            };
            object? obj = system;

            var ok = reflector.TryReadAt(obj, "celestialBodies/[0]/orbitRadius", out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Equal(42f, result.GetValue<float>(reflector));
        }

        // ─── TryReadAt — dictionary string key ───────────────────────────────────

        [Fact]
        public void TryReadAt_DictionaryStringKey()
        {
            var reflector = new Reflector();
            var container = new DictionaryContainer
            {
                config = new Dictionary<string, int> { ["timeout"] = 30, ["retries"] = 5 }
            };
            object? obj = container;

            var ok = reflector.TryReadAt(obj, "config/[timeout]", out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Equal(30, result.GetValue<int>(reflector));
        }

        // ─── TryReadAt — dictionary integer key ──────────────────────────────────

        [Fact]
        public void TryReadAt_DictionaryIntKey()
        {
            var reflector = new Reflector();
            var container = new IntDictionaryContainer
            {
                lookup = new Dictionary<int, string> { [1] = "one", [2] = "two" }
            };
            object? obj = container;

            var ok = reflector.TryReadAt(obj, "lookup/[2]", out var result);

            Assert.True(ok);
            Assert.NotNull(result);
            Assert.Equal("two", result.GetValue<string>(reflector));
        }

        // ─── TryReadAt — invalid path returns false + logs ────────────────────────

        [Fact]
        public void TryReadAt_InvalidPath_ReturnsFalse()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;
            var logs = new Logs();

            var ok = reflector.TryReadAt(obj, "doesNotExist", out var result, logs: logs);

            Assert.False(ok);
            Assert.Null(result);
            var logsText = logs.ToString();
            _output.WriteLine(logsText);
            Assert.Contains("doesNotExist", logsText);
            Assert.Contains("not found", logsText);
        }

        // ─── TryReadAt — out-of-bounds index returns false + logs ────────────────

        [Fact]
        public void TryReadAt_OutOfBounds_ReturnsFalse()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 1f }
                }
            };
            object? obj = system;
            var logs = new Logs();

            var ok = reflector.TryReadAt(obj, "celestialBodies/[99]", out var result, logs: logs);

            Assert.False(ok);
            Assert.Null(result);
            var logsText = logs.ToString();
            _output.WriteLine(logsText);
            Assert.Contains("[99]", logsText);
            Assert.Contains("out of range", logsText);
        }

        // ─── Grep — simple pattern finds all matches ──────────────────────────────

        [Fact]
        public void Grep_SimplePattern_FindsAllMatches()
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

            var matches = reflector.Grep(obj, "^orbit");

            _output.WriteLine($"Found {matches.Count} matches:");
            foreach (var m in matches)
                _output.WriteLine($"  {m.Path}");

            Assert.True(matches.Count >= 4, "Expected at least orbitRadius and orbitSpeed for each of 2 bodies");
            Assert.Contains(matches, m => m.Path.Contains("orbitRadius"));
            Assert.Contains(matches, m => m.Path.Contains("orbitSpeed"));
        }

        // ─── Grep — maxDepth limits search ───────────────────────────────────────

        [Fact]
        public void Grep_MaxDepth_LimitsSearch()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                globalOrbitSpeedMultiplier = 1f,
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f }
                }
            };
            object? obj = system;

            // maxDepth=0 — only top-level fields of SolarSystem
            var matchesDepth0 = reflector.Grep(obj, ".*", maxDepth: 0);
            _output.WriteLine($"maxDepth=0 found {matchesDepth0.Count} matches");
            foreach (var m in matchesDepth0)
                _output.WriteLine($"  {m.Path}");

            // Should include globalOrbitSpeedMultiplier, globalSizeMultiplier, sun, celestialBodies
            // But NOT fields inside celestialBodies elements (those are at depth 2+)
            Assert.True(matchesDepth0.All(m => !m.Path.Contains("/")),
                "maxDepth=0 should return only root-level fields (no slashes in path)");
        }

        // ─── Grep — array elements found ─────────────────────────────────────────

        [Fact]
        public void Grep_ArrayElements_FindsMatches()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                celestialBodies = new[]
                {
                    new SolarSystem.CelestialBody { orbitRadius = 10f },
                    new SolarSystem.CelestialBody { orbitRadius = 20f },
                    new SolarSystem.CelestialBody { orbitRadius = 30f },
                }
            };
            object? obj = system;

            var matches = reflector.Grep(obj, "^orbitRadius$");

            _output.WriteLine($"Found {matches.Count} orbitRadius matches:");
            foreach (var m in matches)
                _output.WriteLine($"  {m.Path} = {m.Value.GetValue<float>(reflector)}");

            Assert.Equal(3, matches.Count);
            Assert.Contains(matches, m => m.Path.Contains("[0]") && m.Value.GetValue<float>(reflector) == 10f);
            Assert.Contains(matches, m => m.Path.Contains("[1]") && m.Value.GetValue<float>(reflector) == 20f);
            Assert.Contains(matches, m => m.Path.Contains("[2]") && m.Value.GetValue<float>(reflector) == 30f);
        }

        // ─── Grep — no matches returns empty list ─────────────────────────────────

        [Fact]
        public void Grep_NoMatches_ReturnsEmpty()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            var matches = reflector.Grep(obj, "thisFieldDoesNotExist_xyz");

            Assert.Empty(matches);
        }

        // ─── Grep — dictionary field found ───────────────────────────────────────

        [Fact]
        public void Grep_DictionaryField_Found()
        {
            var reflector = new Reflector();
            var container = new DictionaryContainer
            {
                config = new Dictionary<string, int> { ["timeout"] = 10 }
            };
            object? obj = container;

            var matches = reflector.Grep(obj, "^config$");

            _output.WriteLine($"Found {matches.Count} matches:");
            foreach (var m in matches)
                _output.WriteLine($"  {m.Path}");

            Assert.True(matches.Count >= 1);
            Assert.Contains(matches, m => m.Path == "config");
        }

        // ─── View — TypeFilter keeps only matching type branches ─────────────────

        [Fact]
        public void View_TypeFilter_KeepsMatchingBranches()
        {
            var reflector = new Reflector();
            var system = new SolarSystem
            {
                globalOrbitSpeedMultiplier = 2f,
                globalSizeMultiplier = 3f,
            };
            object? obj = system;

            // TypeFilter = float should keep only float fields and discard non-float ones
            var result = reflector.View(obj, new ViewQuery { TypeFilter = typeof(float) });

            Assert.NotNull(result);
            // All returned fields/props must resolve to float
            if (result.fields != null)
                Assert.True(result.fields.All(f => f.typeName != null && f.typeName.Contains("Single") || f.typeName == "float"),
                    "Only float fields should survive TypeFilter=float");
        }

        [Fact]
        public void View_TypeFilter_NoMatch_EmptyEnvelope()
        {
            var reflector = new Reflector();
            var system = new SolarSystem { globalOrbitSpeedMultiplier = 1f };
            object? obj = system;

            // Nothing in SolarSystem resolves to Guid
            var result = reflector.View(obj, new ViewQuery { TypeFilter = typeof(Guid) });

            Assert.NotNull(result);
            Assert.Contains("SolarSystem", result.typeName);
            Assert.True(result.fields == null || result.fields.Count == 0,
                "No matching type → empty envelope");
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

using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public class Issue109Tests : BaseTest
    {
        public Issue109Tests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("mat/wood", "[mat/wood]")]
        [InlineData("", "[]")]
        [InlineData("a]/b", @"[a\]/b]")]
        [InlineData(@"a\]/b", @"[a\\\]/b]")]
        [InlineData("a[b/c", "[a[b/c]")]
        [InlineData(@"a\b/c", @"[a\b/c]")]
        public void Path_DictionaryKey_ReadViewAndModify(string key, string segment)
        {
            var reflector = new Reflector();
            var catalog = new Catalog();
            catalog.Assets[key] = new Asset { Id = "old" };
            var path = "#/Assets/" + segment + "/Id";
            Assert.True(reflector.TryReadAt(catalog, path, out var read));
            Assert.Equal("old", read!.GetValue<string>(reflector));
            Assert.Equal("old", reflector.View(catalog, new ViewQuery { Path = path })!.GetValue<string>(reflector));
            object? target = catalog;
            Assert.True(reflector.TryModifyAt(ref target, path, "new"));
            Assert.Equal("new", catalog.Assets[key].Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void View_MaxDepth_DoesNotReadBoundaryGetters(int maxDepth)
        {
            var reflector = new Reflector();
            var boundary = new Node();
            Node root = boundary;
            for (int i = 0; i < maxDepth; i++) root = new Node { Child = root };
            var viewed = reflector.View(root, new ViewQuery { MaxDepth = maxDepth }, flags: BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(viewed);
            Assert.Equal(0, boundary.Reads);
            reflector.View(boundary, new ViewQuery { MaxDepth = 1 }, flags: BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, boundary.Reads);
        }

        [Fact]
        public void View_SharedReferences_VisitsObjectOncePerCall()
        {
            var reflector = new Reflector();
            var shared = new Node();
            var root = new[] { shared, shared, shared };
            var viewed = reflector.View(root, flags: BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(viewed);
            Assert.Equal(1, shared.Reads);
            var items = viewed!.valueJsonElement!.Value;
            Assert.Equal("#/[0]", items[1].GetProperty("value").GetProperty("$ref").GetString());
            reflector.View(root, flags: BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(2, shared.Reads);
        }

        [Fact]
        public void View_PathAndLoggingDepth_MaxDepthIsRelativeToTarget()
        {
            var child = new Node();
            var root = new Node { Child = child };
            var viewed = new Reflector().View(root, new ViewQuery { Path = "Child", MaxDepth = 0 }, depth: 12);
            Assert.NotNull(viewed);
            Assert.Equal(0, child.Reads);
        }

        [Fact]
        public void View_MaxDepth_CollectionBoundaryIsNotEnumerated()
        {
            var node = new Node();
            var reflector = new Reflector();
            var viewed = reflector.View(new[] { node }, new ViewQuery { MaxDepth = 0 });
            Assert.NotNull(viewed);
            Assert.Null(viewed!.valueJsonElement);
            reflector.View(new[] { node }, new ViewQuery { MaxDepth = 1 });
            Assert.Equal(0, node.Reads);
            reflector.View(new[] { node }, new ViewQuery { MaxDepth = 2 });
            Assert.Equal(1, node.Reads);
        }

        [Fact]
        public void Patch_DictionaryKeys_UsesLiteralJsonKeys()
        {
            var reflector = new Reflector();
            object? target = new Dictionary<string, string> { [@"a\]b"] = "old" };
            Assert.True(reflector.TryPatch(ref target, "{\"[a\\\\]b]\":\"new\"}"));
            Assert.Equal("new", ((Dictionary<string, string>)target!)[@"a\]b"]);
        }

        [Fact]
        public void Grep_EscapedDictionaryKey_PathCanBeRead()
        {
            var reflector = new Reflector();
            var root = new Dictionary<string, Asset> { [@"a\]/b"] = new Asset { Id = "found" } };
            var match = Assert.Single(reflector.Grep(root, "^Id$"));
            Assert.True(reflector.TryReadAt(root, match.Path, out var read));
            Assert.Equal("found", read!.GetValue<string>(reflector));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void View_Boundary_RetainsPrimitiveValue(int maxDepth)
        {
            var reflector = new Reflector();
            Assert.Equal(42, reflector.View(42, new ViewQuery { MaxDepth = maxDepth })!.GetValue<int>(reflector));
        }

        [Fact]
        public void Serialize_SharedReferences_StillExpandsEachOccurrence()
        {
            var shared = new Node();
            new Reflector().Serialize(new[] { shared, shared }, flags: BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(2, shared.Reads);
        }

        [Fact]
        public void Serialize_Cycle_DoesNotCorruptSiblingReferencePaths()
        {
            var root = new CyclicNode();
            root.Self = root;
            root.Child = new CyclicNode();
            root.Child.Self = root.Child;
            var serialized = new Reflector().Serialize(root, flags: BindingFlags.Public | BindingFlags.Instance);
            var reference = serialized.GetField("Child")!.GetField("Self")!;
            Assert.Equal("#/Child", reference.valueJsonElement!.Value.GetProperty("$ref").GetString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public void View_IssueGraph_TraversalStopsAtRequestedDepth(int maxDepth)
        {
            var world = new World();
            for (int i = 0; i < 300; i++) world.Npcs.Add(new Npc { World = world });
            foreach (var npc in world.Npcs)
                npc.Friends.AddRange(world.Npcs.GetRange(0, 20));
            var result = new Reflector().View(world.Npcs[0], new ViewQuery { MaxDepth = maxDepth });
            Assert.NotNull(result);
            Assert.InRange(world.Reads, 0, 1);
            foreach (var npc in world.Npcs) Assert.InRange(npc.Reads, 0, 1);
            if (maxDepth == 0) Assert.Equal(0, world.Npcs[0].Reads);
            else Assert.Equal(1, world.Npcs[0].Reads);
        }

        public class CyclicNode { public CyclicNode? Self; public CyclicNode? Child; }
        public class World
        {
            public List<Npc> Npcs = new();
            public int Reads;
            public int Value { get { Reads++; return 42; } }
        }
        public class Npc
        {
            public World? World;
            public List<Npc> Friends = new();
            public int Reads;
            public int Value { get { Reads++; return 42; } }
        }

        public class Asset { public string Id { get; set; } = ""; }
        public class Catalog { public Dictionary<string, Asset> Assets { get; set; } = new(); }
        public class Node
        {
            public int Reads;
            public Node? Child;
            public int Value { get { Reads++; return 42; } }
        }
    }
}

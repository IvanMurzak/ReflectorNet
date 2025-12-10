using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using Xunit;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public class RecursionTests
    {
        class RecursiveNode
        {
            public string? Name { get; set; }
            public RecursiveNode? Child { get; set; }
        }

        [Fact]
        public void Serialize_RecursiveObject_ShouldReturnReference()
        {
            var node1 = new RecursiveNode { Name = "Node1" };
            var node2 = new RecursiveNode { Name = "Node2" };
            node1.Child = node2;
            node2.Child = node1; // Cycle

            var reflector = new Reflector();
            var result = reflector.Serialize(node1);

            // Check structure
            // Node1 -> Child (Node2) -> Child (Reference to Node1)

            Assert.NotNull(result);
            Assert.NotNull(result.props);
            Assert.Equal("Node1", result.props.GetField("Name")?.valueJsonElement?.GetString());

            var child = result.props.GetField("Child");
            Assert.NotNull(child);
            Assert.NotNull(child.props);
            Assert.Equal("Node2", child.props.GetField("Name")?.valueJsonElement?.GetString());

            var grandChild = child.props.GetField("Child");
            Assert.NotNull(grandChild);
            Assert.Equal("Reference", grandChild.typeName);

            var refValue = grandChild.valueJsonElement?.GetProperty("$ref").GetString();
            Assert.Equal("#", refValue); // Should point to root
        }

        class SelfRecursive
        {
            public SelfRecursive? Self { get; set; }
        }

        [Fact]
        public void Serialize_SelfRecursive_ShouldReturnReference()
        {
            var obj = new SelfRecursive();
            obj.Self = obj;

            var reflector = new Reflector();
            var result = reflector.Serialize(obj);

            Assert.NotNull(result);
            Assert.NotNull(result.props);
            var self = result.props.GetField("Self");
            Assert.NotNull(self);
            Assert.Equal("Reference", self.typeName);
            Assert.Equal("#", self.valueJsonElement?.GetProperty("$ref").GetString());
        }
    }
}

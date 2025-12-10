using System.Collections.Generic;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using JsonSchema = com.IvanMurzak.ReflectorNet.Utils.JsonSchema;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public class RecursionTests
    {
        private readonly ITestOutputHelper _output;

        public RecursionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        class RecursiveNode
        {
            public string? Name { get; set; }
            public RecursiveNode? Child { get; set; }
        }

        class RecursiveWrapper
        {
            public RecursiveContainer? Container { get; set; }
        }
        class RecursiveContainer
        {
            public List<RecursiveWrapper> Items { get; set; } = new List<RecursiveWrapper>();
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

            _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

            // Check structure
            // Node1 -> Child (Node2) -> Child (Reference to Node1)

            Assert.NotNull(result);
            Assert.NotNull(result.props);
            Assert.Equal(node1.Name, result.props.GetField(nameof(RecursiveNode.Name))?.valueJsonElement?.GetString());

            var child = result.props.GetField(nameof(RecursiveNode.Child));
            Assert.NotNull(child);
            Assert.NotNull(child.props);
            Assert.Equal(node2.Name, child.props.GetField(nameof(RecursiveNode.Name))?.valueJsonElement?.GetString());

            var grandChild = child.props.GetField(nameof(RecursiveNode.Child));
            Assert.NotNull(grandChild);
            Assert.Equal(JsonSchema.Reference, grandChild.typeName);

            var refValue = grandChild.valueJsonElement?.GetProperty(JsonSchema.Ref).GetString();
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

            _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(result);
            Assert.NotNull(result.props);
            var self = result.props.GetField(nameof(SelfRecursive.Self));
            Assert.NotNull(self);
            Assert.Equal(JsonSchema.Reference, self.typeName);
            Assert.Equal("#", self.valueJsonElement?.GetProperty(JsonSchema.Ref).GetString());
        }

        [Fact]
        public void Serialize_RecursiveList_ShouldReturnReference()
        {
            var wrapper = new RecursiveWrapper();
            var container = new RecursiveContainer();

            wrapper.Container = container;
            container.Items.Add(wrapper);

            var reflector = new Reflector();
            var result = reflector.Serialize(wrapper);

            _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(result);
            Assert.NotNull(result.props);

            var containerProp = result.props.GetField(nameof(RecursiveWrapper.Container));
            Assert.NotNull(containerProp);

            var itemsProp = containerProp.props?.GetField(nameof(RecursiveContainer.Items));
            Assert.NotNull(itemsProp);

            var itemsArray = itemsProp.valueJsonElement;
            Assert.NotNull(itemsArray);
            Assert.Equal(JsonValueKind.Array, itemsArray.Value.ValueKind);

            var firstItem = itemsArray.Value[0];
            Assert.Equal(JsonSchema.Reference, firstItem.GetProperty(nameof(SerializedMember.typeName)).GetString());
            var refValue = firstItem.GetProperty(SerializedMember.ValueName).GetProperty(JsonSchema.Ref).GetString();
            Assert.Equal("#", refValue);
        }

        [Fact]
        public void Serialize_DeepRecursion_ShouldReturnCorrectReference()
        {
            var root = new RecursiveNode { Name = "root" };
            var child1 = new RecursiveNode { Name = "child1" };
            var child2 = new RecursiveNode { Name = "child2" };
            var child3 = new RecursiveNode { Name = "child3" };

            root.Child = child1;
            child1.Child = child2;
            child2.Child = child3;
            child3.Child = child2; // Cycle back to child2

            var reflector = new Reflector();
            var result = reflector.Serialize(root);

            _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(result);
            Assert.NotNull(result.props);

            // Root -> Child1
            var child1Prop = result.props.GetField(nameof(RecursiveNode.Child));
            Assert.NotNull(child1Prop);
            Assert.Equal(child1.Name, child1Prop.props!.GetField(nameof(RecursiveNode.Name))?.valueJsonElement?.GetString());

            // Child1 -> Child3
            var child3Prop = child1Prop.props.GetField(nameof(RecursiveNode.Child));
            Assert.NotNull(child3Prop);
            Assert.Equal(child2.Name, child3Prop.props!.GetField(nameof(RecursiveNode.Name))?.valueJsonElement?.GetString());

            // Child3 -> Child4
            var child4Prop = child3Prop.props.GetField(nameof(RecursiveNode.Child));
            Assert.NotNull(child4Prop);
            Assert.Equal(child3.Name, child4Prop.props!.GetField(nameof(RecursiveNode.Name))?.valueJsonElement?.GetString());

            // Child4 -> Reference to Child3
            var refProp = child4Prop.props.GetField(nameof(RecursiveNode.Child));
            Assert.NotNull(refProp);
            Assert.Equal(JsonSchema.Reference, refProp.typeName);

            var refValue = refProp.valueJsonElement?.GetProperty(JsonSchema.Ref).GetString();
            // Path to Child3: Root -> Child -> Child
            Assert.Equal($"#/{nameof(RecursiveNode.Child)}/{nameof(RecursiveNode.Child)}", refValue);
        }

        [Fact]
        public void Serialize_DeepRecursion_List_ShouldReturnCorrectReference()
        {
            var wrapper1 = new RecursiveWrapper(); // Root
            var container1 = new RecursiveContainer();
            wrapper1.Container = container1;

            var wrapper2 = new RecursiveWrapper(); // Child 1
            container1.Items.Add(wrapper2);
            var container2 = new RecursiveContainer();
            wrapper2.Container = container2;

            var wrapper3 = new RecursiveWrapper(); // Child 2
            container2.Items.Add(wrapper3);
            var container3 = new RecursiveContainer();
            wrapper3.Container = container3;

            // Cycle: wrapper3 -> container3 -> items[0] -> wrapper2
            container3.Items.Add(wrapper2);

            var reflector = new Reflector();
            var result = reflector.Serialize(wrapper1);

            _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

            Assert.NotNull(result);
            Assert.NotNull(result.props);

            // Root (Wrapper1) -> Container -> Items[0] (Wrapper2)
            var container1Prop = result.props.GetField(nameof(RecursiveWrapper.Container));
            Assert.NotNull(container1Prop);
            var items1Prop = container1Prop.props?.GetField(nameof(RecursiveContainer.Items));
            Assert.NotNull(items1Prop);
            // items1Prop is a SerializedMember. Its valueJsonElement IS the array of items.
            var wrapper2Prop = items1Prop.valueJsonElement?[0];
            Assert.NotNull(wrapper2Prop);

            // Wrapper2 -> Container -> Items[0] (Wrapper3)
            // Note: wrapper2Prop is a JsonElement, we need to navigate its structure which mimics SerializedMember
            // But wait, ArrayReflectionConvertor returns a SerializedMember with value as a list of SerializedMembers.
            // The "value" property of the SerializedMember for the list is the array.
            // The array elements are SerializedMembers.

            // Let's navigate using the SerializedMember structure if possible, but here we have JsonElement for the array items if we didn't deserialize back to SerializedMember.
            // Actually, Reflector.Serialize returns a SerializedMember.
            // For the list, the 'value' is a JsonElement which IS the array of SerializedMembers (serialized to JSON).
            // Wait, let's check ArrayReflectionConvertor.
            // It returns SerializedMember.FromValue(..., value: serializedList, ...).
            // serializedList is SerializedMemberList.
            // So the value IS a SerializedMemberList (which is a List<SerializedMember>).
            // But SerializedMember.valueJsonElement is a JsonElement.
            // When FromValue is called, it serializes the value to JsonElement.
            // So items1Prop.valueJsonElement is the JSON array of SerializedMembers.

            // wrapper2Prop is the first element of that array. It is a JSON object representing the SerializedMember for Wrapper2.

            // Wrapper2 (JsonElement) -> props -> Container -> props -> Items -> value -> [0] (Wrapper3)
            var wrapper2Props = wrapper2Prop?.GetProperty(nameof(SerializedMember.props));
            // We need to find the property with name "Container" in the props array
            var container2Prop = GetPropertyFromJsonArray(wrapper2Props, nameof(RecursiveWrapper.Container));
            var items2Prop = GetPropertyFromJsonArray(container2Prop.GetProperty(nameof(SerializedMember.props)), nameof(RecursiveContainer.Items));
            var wrapper3Prop = items2Prop.GetProperty(SerializedMember.ValueName)[0];

            // Wrapper3 -> Container -> Items -> [0] (Reference to Wrapper2)
            var wrapper3Props = wrapper3Prop.GetProperty(nameof(SerializedMember.props));
            var container3Prop = GetPropertyFromJsonArray(wrapper3Props, nameof(RecursiveWrapper.Container));
            var items3Prop = GetPropertyFromJsonArray(container3Prop.GetProperty(nameof(SerializedMember.props)), nameof(RecursiveContainer.Items));
            var refProp = items3Prop.GetProperty(SerializedMember.ValueName)[0];

            Assert.Equal(JsonSchema.Reference, refProp.GetProperty(nameof(SerializedMember.typeName)).GetString());
            var refValue = refProp.GetProperty(SerializedMember.ValueName).GetProperty(JsonSchema.Ref).GetString();

            // Path to Wrapper2: # -> Container -> Items -> [0]
            Assert.Equal($"#/{nameof(RecursiveWrapper.Container)}/{nameof(RecursiveContainer.Items)}/[0]", refValue);
        }

        private JsonElement GetPropertyFromJsonArray(JsonElement? array, string name)
        {
            if (array == null) throw new System.ArgumentNullException(nameof(array));
            foreach (var item in array.Value.EnumerateArray())
            {
                if (item.GetProperty(nameof(SerializedMember.name)).GetString() == name)
                {
                    return item;
                }
            }
            throw new System.Exception($"Property {name} not found");
        }
    }
}

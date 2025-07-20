using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace ReflectorNet.Tests.SchemaTests
{
    public partial class TestDescription : BaseTest
    {
        void TestClassMembersDescription(Type type, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            _output.WriteLine($"Testing members description for type: '{type.FullName}'");

            var schema = JsonUtils.Schema.GetSchema(type, justRef: false);
            Assert.NotNull(schema);

            var properties = default(JsonNode?);
            var members = default(List<MemberInfo>);

            var isArray = schema[JsonUtils.Schema.Type]?.ToString() == JsonUtils.Schema.Array;
            if (isArray)
            {
                _output.WriteLine($"Schema is an array");

                var items = schema[JsonUtils.Schema.Items];
                Assert.NotNull(items);

                properties = items[JsonUtils.Schema.Properties];

                var itemType = TypeUtils.GetEnumerableItemType(type);
                Assert.NotNull(itemType);

                members = Enumerable
                    .Concat(
                        itemType!.GetFields(bindingFlags) as IEnumerable<MemberInfo>,
                        itemType!.GetProperties(bindingFlags) as IEnumerable<MemberInfo>
                    )
                    .Where(member => member.GetCustomAttribute<ObsoleteAttribute>() == null)
                    .Where(member => member.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToList();
            }
            else
            {
                _output.WriteLine($"Schema is an object");
                properties = schema[JsonUtils.Schema.Properties];

                members = Enumerable
                    .Concat(
                        type.GetFields(bindingFlags) as IEnumerable<MemberInfo>,
                        type.GetProperties(bindingFlags) as IEnumerable<MemberInfo>
                    )
                    .Where(member => member.GetCustomAttribute<ObsoleteAttribute>() == null)
                    .Where(member => member.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToList();
            }

            Assert.NotNull(properties);

            _output.WriteLine($"Properties[{members.Count}]: {properties}");

            foreach (var kvp in properties.AsObject())
            {
                var name = kvp.Key;
                var propertySchema = kvp.Value;

                Assert.NotNull(name);
                Assert.NotNull(propertySchema);

                var member = members.FirstOrDefault(m => m.Name == name);
                Assert.NotNull(member);

                var description = TypeUtils.GetDescription(member);
                var schemaDescription = propertySchema[JsonUtils.Schema.Description]?.ToString();
                Assert.Equal(description, schemaDescription);
            }
        }
    }
}

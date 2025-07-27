using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public partial class TestDescription : BaseTest
    {
        void TestClassMembersDescription(Type type, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            _output.WriteLine($"Testing members description for type: '{type.GetTypeName(pretty: false)}'");

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

            // Some schemas (like arrays without item properties or enums) may not have properties
            if (properties == null)
            {
                _output.WriteLine("No properties found in schema - skipping property validation");
                return;
            }

            _output.WriteLine($"Properties[{members.Count}]: {properties}");

            foreach (var kvp in properties.AsObject())
            {
                var name = kvp.Key;
                var propertySchema = kvp.Value;

                Assert.NotNull(name);
                Assert.NotNull(propertySchema);

                // Handle camelCase to PascalCase conversion for member lookup
                var member = members.FirstOrDefault(m => m.Name == name) ??
                            members.FirstOrDefault(m => m.Name == ToPascalCase(name)) ??
                            members.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(member);

                var description = TypeUtils.GetDescription(member);
                var schemaDescription = propertySchema[JsonUtils.Schema.Description]?.ToString();
                Assert.Equal(description, schemaDescription);
            }
        }

        private static string ToPascalCase(string camelCase)
        {
            if (string.IsNullOrEmpty(camelCase))
                return camelCase;

            return char.ToUpperInvariant(camelCase[0]) + camelCase.Substring(1);
        }
    }
}

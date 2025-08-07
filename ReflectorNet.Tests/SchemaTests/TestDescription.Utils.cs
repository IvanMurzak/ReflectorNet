using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public partial class TestDescription : BaseTest
    {
        void TestClassMembersDescription(Type type, Reflector? reflector = null, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance)
        {
            reflector ??= new Reflector();

            _output.WriteLine($"Testing members description for type: '{type.GetTypeShortName()}'");

            var schema = reflector.GetSchema(type, justRef: false);
            Assert.NotNull(schema);

            var properties = default(JsonNode?);
            var members = default(List<MemberInfo>);

            var isArray = schema[JsonSchema.Type]?.ToString() == JsonSchema.Array;
            if (isArray)
            {
                _output.WriteLine($"Schema is an array");

                var items = schema[JsonSchema.Items];
                Assert.NotNull(items);

                properties = items[JsonSchema.Properties];

                var itemType = TypeUtils.GetEnumerableItemType(type);
                Assert.NotNull(itemType);

                members = Enumerable
                    .Concat(
                        itemType!.GetFields(bindingFlags) as IEnumerable<MemberInfo>,
                        itemType!.GetProperties(bindingFlags) as IEnumerable<MemberInfo>
                    )
                    .Where(member => member.GetCustomAttribute<ObsoleteAttribute>() == null)
                    //.Where(member => member.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToList();
            }
            else
            {
                _output.WriteLine($"Schema is an object");
                properties = schema[JsonSchema.Properties];

                members = Enumerable
                    .Concat(
                        type.GetFields(bindingFlags) as IEnumerable<MemberInfo>,
                        type.GetProperties(bindingFlags) as IEnumerable<MemberInfo>
                    )
                    .Where(member => member.GetCustomAttribute<ObsoleteAttribute>() == null)
                    //.Where(member => member.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    .ToList();
            }

            // Some schemas (like arrays without item properties or enums) may not have properties
            if (properties == null)
            {
                _output.WriteLine("No properties found in schema - skipping property validation.\n");
                return;
            }

            _output.WriteLine($"Properties[{members.Count}]: {properties}\n");

            foreach (var kvp in properties.AsObject())
            {
                var name = kvp.Key;
                var propertySchema = kvp.Value;

                Assert.NotNull(name);
                Assert.NotNull(propertySchema);

                // Handle camelCase to PascalCase conversion for member lookup
                var memberInfo = members.FirstOrDefault(m => m.Name == name) ??
                                 members.FirstOrDefault(m => m.Name == ToPascalCase(name)) ??
                                 members.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

                Assert.False(memberInfo == null, $"Schema property '{name}' not found in members of type '{type.GetTypeShortName()}'");

                var description = TypeUtils.GetDescription(memberInfo);

                if (propertySchema is not JsonObject)
                {
                    _output.WriteLine($"{memberInfo.MemberType} '{name}' is not an object schema. It is {propertySchema.GetType().GetTypeShortName()}, kind={propertySchema.GetValueKind()}.\n");
                    _output.WriteLine($"Description: {description}\n");
                    _output.WriteLine($"Schema: {propertySchema}\n");
                }

                var schemaDescription = propertySchema[JsonSchema.Description]?.ToString();

                var memberType = memberInfo.MemberType switch
                {
                    MemberTypes.Field => ((FieldInfo)memberInfo).FieldType.GetTypeShortName(),
                    MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType.GetTypeShortName(),
                    _ => "UNKNOWN"
                };

                _output.WriteLine($"{memberInfo.MemberType} '{type.GetTypeShortName()}.{name}', type='{memberType}' compare description\n---------");

                _output.WriteLine($"Json Schema: {schemaDescription}");
                _output.WriteLine($"Reflection:  {description}\n");

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

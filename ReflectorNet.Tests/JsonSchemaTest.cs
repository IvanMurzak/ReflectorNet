using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;

namespace ReflectorNet.Tests
{
    public class JsonSchemaTest : BaseTest
    {
        public JsonSchemaTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SerializedMemberList()
        {
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.SerializedMemberList_ReturnString))!;
            var schema = JsonUtils.Schema.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonUtils.Schema.Defs]);

            var defines = schema[JsonUtils.Schema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var targetSchema = defines[typeof(SerializedMemberList).FullName!];
            Assert.NotNull(targetSchema);
        }
        [Fact]
        public void Method_GameObject_Find()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Find))!;
            var schema = JsonUtils.Schema.GetArgumentsSchema(methodInfo, justRef: true)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonUtils.Schema.Defs]);

            var defines = schema[JsonUtils.Schema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var targetSchema = defines[typeof(GameObjectRef).FullName!];
            Assert.NotNull(targetSchema);
        }
        [Fact]
        public void GenericTypes_SerializedMemberList()
        {
            var type = typeof(SerializedMemberList);
            var genericTypes = TypeUtils.GetGenericTypes(type).ToList();

            _output.WriteLine($"Generic types for {type.FullName}: {string.Join(", ", genericTypes.Select(t => t.FullName))}");

            Assert.NotEmpty(genericTypes);

            Assert.Contains(typeof(SerializedMember), genericTypes);

            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMember)));
        }
        [Fact]
        public void GenericTypes_List_SerializedMemberList()
        {
            var type = typeof(List<SerializedMemberList>);
            var genericTypes = TypeUtils.GetGenericTypes(type).ToList();

            _output.WriteLine($"Generic types for {type.FullName}: {string.Join(", ", genericTypes.Select(t => t.FullName))}");

            Assert.NotEmpty(genericTypes);

            Assert.Contains(typeof(SerializedMemberList), genericTypes);
            Assert.Contains(typeof(SerializedMember), genericTypes);

            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMemberList)));
            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMember)));
        }

        [Fact]
        public void GenericTypes_List_SerializedMember()
        {
            var type = typeof(List<SerializedMember>);
            var genericTypes = TypeUtils.GetGenericTypes(type).ToList();

            _output.WriteLine($"Generic types for {type.FullName}: {string.Join(", ", genericTypes.Select(t => t.FullName))}");

            Assert.NotEmpty(genericTypes);

            Assert.Contains(typeof(SerializedMember), genericTypes);

            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMember)));
        }

        [Fact]
        public void PropertyDescriptionOfCustomType()
        {
            TestClassMembersDescription(typeof(GameObjectRef));
            TestClassMembersDescription(typeof(GameObjectRefList));
            TestClassMembersDescription(typeof(List<GameObjectRef>));
            TestClassMembersDescription(typeof(GameObjectRef[]));
        }
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
        // [Fact]
        // public void Method_GameObject_Find_Call()
        // {
        //     var json = """
        //     {
        //         "gameObjectDiffs": [
        //             {
        //                 "props": [
        //                     {
        //                         "name": "color",
        //                         "typeName": "UnityEngine.Color",
        //                         "value": {
        //                             "r": 1,
        //                             "g": 0,
        //                             "b": 0,
        //                             "a": 1
        //                         }
        //                     }
        //                 ]
        //             }
        //         ],
        //         "gameObjectRefs": [
        //             {
        //                 "instanceID": 22926
        //             }
        //         ]
        //     }
        //     """;

        //     Reflector.Instance.
        //     var gameObjectDiffs =

        //     Reflector.Instance.Populate()
        //     var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Find))!;
        //     Reflector.Instance.MethodCall(methodInfo, json);

        //     _output.WriteLine(schema.ToString());

        //     Assert.NotNull(schema);
        //     Assert.NotNull(schema[JsonUtils.Schema.Defs]);

        //     var defines = schema[JsonUtils.Schema.Defs]?.AsObject();
        //     Assert.NotNull(defines);

        //     var targetSchema = defines[typeof(GameObjectRef).FullName!];
        //     Assert.NotNull(targetSchema);
        // }
    }
}

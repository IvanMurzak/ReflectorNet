using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Utils;
using System.Text.Json;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Tests.SimpleTests
{
    public class StringTests : BaseTest
    {
        public StringTests(ITestOutputHelper output) : base(output) { }

        //         [Fact]
        //         public void TryUnstringify_JsonString_Stringified()
        //         {
        //             // Arrange
        //             var instanceID = 123;
        //             var stringifiedJson = $@"
        // {{
        //     ""gameObjectRef"": ""{{ \""instanceID\"": {instanceID} }}"",
        //     ""briefData"": false
        // }}";

        //             if (JsonUtils.TryUnstringifyJson(stringifiedJson, out var fixedJsonElement))
        //             {
        //                 _output.WriteLine($"Unstringified JSON: {fixedJsonElement}");

        //                 Assert.NotNull(fixedJsonElement);
        //                 Assert.NotNull(fixedJsonElement?.GetProperty("gameObjectRef"));
        //                 Assert.NotNull(fixedJsonElement?.GetProperty("gameObjectRef").GetProperty("instanceID"));

        //                 Assert.Equal(instanceID, fixedJsonElement!.Value.GetProperty("gameObjectRef").GetProperty("instanceID").GetInt32());
        //             }
        //             else
        //             {
        //                 Assert.Fail($"Failed to unstringify {nameof(stringifiedJson)}.");
        //                 Assert.Null(fixedJsonElement);
        //             }
        //         }

        //         [Fact]
        //         public void TryUnstringify_JsonString_Normal()
        //         {
        //             // Arrange
        //             var instanceID = 123;
        //             var normalJson = $@"
        // {{
        //     ""gameObjectRef"": {{ ""instanceID"": {instanceID} }},
        //     ""briefData"": false
        // }}";
        //             if (JsonUtils.TryUnstringifyJson(normalJson, out var fixedJsonElement))
        //             {
        //                 Assert.Fail($"Expected to fail unstringify of {nameof(normalJson)}.");
        //             }
        //             else
        //             {
        //                 Assert.Null(fixedJsonElement);

        //                 using var document = JsonDocument.Parse(normalJson);
        //                 var rootElement = document.RootElement;

        //                 Assert.Equal(instanceID, rootElement.GetProperty("gameObjectRef").GetProperty("instanceID").GetInt32());
        //             }
        //         }

        [Fact]
        public void TryUnstringify_JsonElement_Object_Stringified()
        {
            // Arrange
            var instanceID = 123;
            var stringifiedJson = $@"""{{ \""instanceID\"": {instanceID} }}""";

            using var document = JsonDocument.Parse(stringifiedJson);
            var rootElement = document.RootElement;

            if (JsonUtils.TryUnstringifyJson(rootElement, out var fixedJsonElement))
            {
                _output.WriteLine($"Unstringified JSON: {fixedJsonElement}");

                Assert.NotNull(fixedJsonElement);
                Assert.Equal(instanceID, fixedJsonElement.Value.GetProperty("instanceID").GetInt32());
            }
            else
            {
                Assert.Fail($"Failed to unstringify {nameof(stringifiedJson)}.");
                Assert.Null(fixedJsonElement);
            }
        }
        [Fact]
        public void TryUnstringify_JsonElement_Object_Normal()
        {
            // Arrange
            var instanceID = 123;
            var normalJson = $@"{{ ""instanceID"": {instanceID} }}";

            using var document = JsonDocument.Parse(normalJson);
            var rootElement = document.RootElement;

            if (JsonUtils.TryUnstringifyJson(rootElement, out var fixedJsonElement))
            {
                Assert.Fail($"Expected to fail unstringify of {nameof(normalJson)}.");
                _output.WriteLine($"Unstringified JSON: {fixedJsonElement}");

                Assert.NotNull(fixedJsonElement);
                Assert.Equal(instanceID, fixedJsonElement.Value.GetProperty("instanceID").GetInt32());
            }
            else
            {
                Assert.Null(fixedJsonElement);
                Assert.Equal(instanceID, rootElement.GetProperty("instanceID").GetInt32());
            }
        }

        [Fact]
        public void TryUnstringify_JsonElement_Object_Object_Normal()
        {
            // Arrange
            var instanceID = 123;
            var normalJson = $@"
{{
    ""gameObjectRef"": {{ ""instanceID"": {instanceID} }},
    ""briefData"": false
}}";

            using var document = JsonDocument.Parse(normalJson);
            var rootElement = document.RootElement;

            if (JsonUtils.TryUnstringifyJson(rootElement, out var fixedJsonElement))
            {
                Assert.Fail($"Expected to fail unstringify of {nameof(normalJson)}.");
            }
            else
            {
                Assert.Null(fixedJsonElement);

                using var document2 = JsonDocument.Parse(normalJson);
                var rootElement2 = document2.RootElement;

                Assert.Equal(instanceID, rootElement2.GetProperty("gameObjectRef").GetProperty("instanceID").GetInt32());
            }
        }
        [Fact]
        public void TryUnstringify_JsonElement_Array_Object_Array_Object_Stringified()
        {
            // Arrange
            var instanceID = -32464;
            var stringifiedJson = $@"""[
    {{
        \""typeName\"": \""UnityEngine.GameObject\"",
        \""value\"": null,
        \""fields\"": [
            {{
                \""name\"": \""component_0\"",
                \""typeName\"": \""UnityEngine.Transform\"",
                \""value\"": {{ \""instanceID\"": {instanceID} }},
                \""props\"": [
                    {{
                        \""name\"": \""localScale\"",
                        \""typeName\"": \""UnityEngine.Vector3\"",
                        \""value\"": {{\""x\"": 4.0, \""y\"": 4.0, \""z\"": 4.0}}
                    }}
                ]
            }}
        ]
    }}
]""";
            stringifiedJson = stringifiedJson
                .Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "");

            _output.WriteLine($"Stringified JSON: {stringifiedJson}");

            using var document = JsonDocument.Parse(stringifiedJson);
            var rootElement = document.RootElement;

            if (JsonUtils.TryUnstringifyJson(rootElement, out var fixedJsonElement))
            {
                _output.WriteLine($"Unstringified JSON: {fixedJsonElement}");

                Assert.NotNull(fixedJsonElement);
                Assert.Equal(instanceID, fixedJsonElement.Value
                    .EnumerateArray().First()
                    // .GetProperty("gameObjectDiffs").EnumerateArray().First()
                    .GetProperty("fields").EnumerateArray().First()
                    .GetProperty("value")
                    .GetProperty("instanceID").GetInt32());
            }
            else
            {
                Assert.Fail($"Failed to unstringify {nameof(stringifiedJson)}.");
                Assert.Null(fixedJsonElement);
            }
        }
    }
}

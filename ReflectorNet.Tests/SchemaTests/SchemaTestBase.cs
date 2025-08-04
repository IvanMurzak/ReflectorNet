using System;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public abstract class SchemaTestBase : BaseTest
    {
        protected SchemaTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected void TestMethodInputs_PropertyRefs(MethodInfo methodInfo, params string[] parameterNames)
        {
            var schema = JsonUtils.Schema.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonUtils.Schema.Defs]);

            var defines = schema[JsonUtils.Schema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonUtils.Schema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var parameterName in parameterNames)
            {
                var methodParameter = methodInfo.GetParameters().FirstOrDefault(p => p.Name == parameterName);
                Assert.NotNull(methodParameter);

                var typeId = methodParameter.ParameterType.GetTypeId();
                var refString = $"{JsonUtils.Schema.RefValue}{typeId}";

                var targetDefine = defines[typeId];
                Assert.NotNull(targetDefine);

                var refStringValue = properties.FirstOrDefault(kvp
                        => kvp.Value!.AsObject().TryGetPropertyValue(JsonUtils.Schema.Ref, out var refValue)
                        && refString == refValue?.ToString())
                    .Value
                    ?.ToString();

                Assert.False(string.IsNullOrEmpty(refStringValue));
            }
        }

        protected void TestMethodInputs_Defines(MethodInfo methodInfo, params Type[] expectedTypes)
        {
            var schema = JsonUtils.Schema.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonUtils.Schema.Defs]);

            var defines = schema[JsonUtils.Schema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonUtils.Schema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var expectedType in expectedTypes)
            {
                var typeId = expectedType.GetTypeId();
                var targetDefine = defines[typeId];
                Assert.NotNull(targetDefine);
            }
        }
    }
}

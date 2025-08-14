using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public abstract class SchemaTestBase : BaseTest
    {
        protected SchemaTestBase(ITestOutputHelper output) : base(output)
        {
        }

        protected JsonNode? JsonSchemaValidation(Type type, Reflector? reflector = null)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetSchema(type, justRef: false);

            _output.WriteLine($"Schema for {type.GetTypeShortName()}");
            _output.WriteLine($"{schema}");

            Assert.NotNull(schema);
            if (schema.AsObject().TryGetPropertyValue(JsonSchema.Error, out var errorValue))
            {
                Assert.Fail(errorValue!.ToString());
            }
            Assert.NotNull(schema.AsObject());
            return schema;
        }

        protected void TestMethodInputs_PropertyRefs(Reflector? reflector, MethodInfo methodInfo, params string[] parameterNames)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonSchema.Defs]);

            var defines = schema[JsonSchema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonSchema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var parameterName in parameterNames)
            {
                var methodParameter = methodInfo.GetParameters().FirstOrDefault(p => p.Name == parameterName);
                Assert.NotNull(methodParameter);

                var typeId = methodParameter.ParameterType.GetTypeId();
                var refString = $"{JsonSchema.RefValue}{typeId}";

                var targetDefine = defines[typeId];
                Assert.NotNull(targetDefine);

                var refStringValue = properties.FirstOrDefault(kvp
                        => kvp.Value!.AsObject().TryGetPropertyValue(JsonSchema.Ref, out var refValue)
                        && refString == refValue?.ToString())
                    .Value
                    ?.ToString();

                Assert.False(string.IsNullOrEmpty(refStringValue));
            }
        }

        protected JsonNode? TestMethodInputs_Defines(Reflector? reflector, MethodInfo methodInfo, params Type[] expectedTypes)
        {
            reflector ??= new Reflector();

            var schema = reflector.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonSchema.Defs]);

            var defines = schema[JsonSchema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var properties = schema[JsonSchema.Properties]?.AsObject();
            Assert.NotNull(properties);

            foreach (var expectedType in expectedTypes)
            {
                var typeId = expectedType.GetTypeId();
                var targetDefine = defines[typeId];
                Assert.NotNull(targetDefine);
            }

            return schema;
        }
    }
}

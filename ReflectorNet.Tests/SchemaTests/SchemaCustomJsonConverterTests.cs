using System;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class SchemaCustomJsonConverterTests : SchemaTestBase
    {
        public SchemaCustomJsonConverterTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        void GameObjectRef()
        {
            JsonUtils.AddConverter(new GameObjectRefConverter());
            JsonUtils.AddConverter(new ObjectRefConverter());

            JsonSchemaValidation(typeof(GameObjectRef));

            // JsonUtils.RemoveConverter<GameObjectRefConverter>();
            // JsonUtils.RemoveConverter<ObjectRefConverter>();
        }
    }
}

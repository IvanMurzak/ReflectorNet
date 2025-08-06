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
            var reflector = new Reflector();
            reflector.JsonSerializer.AddConverter(new GameObjectRefConverter());
            reflector.JsonSerializer.AddConverter(new ObjectRefConverter());

            JsonSchemaValidation(typeof(GameObjectRef), reflector);

            // JsonUtils.RemoveConverter<GameObjectRefConverter>();
            // JsonUtils.RemoveConverter<ObjectRefConverter>();
        }
    }
}

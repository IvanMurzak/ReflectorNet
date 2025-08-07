using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    using JsonSerializer = com.IvanMurzak.ReflectorNet.Utils.JsonSerializer;

    public partial class Reflector
    {
        readonly JsonSerializer jsonSerializer;
        readonly JsonSchema jsonSchema = new();

        public JsonSerializerOptions JsonSerializerOptions => jsonSerializer.JsonSerializerOptions;
        public JsonSerializer JsonSerializer => jsonSerializer;
        public JsonSchema JsonSchema => jsonSchema;

        public JsonNode GetSchema<T>(bool justRef = false)
            => jsonSchema.GetSchema<T>(this, justRef);

        public JsonNode GetSchema(Type type, bool justRef = false)
            => jsonSchema.GetSchema(this, type, justRef);

        public JsonNode GetArgumentsSchema(MethodInfo methodInfo, bool justRef = false)
            => jsonSchema.GetArgumentsSchema(this, methodInfo, justRef);
    }
}

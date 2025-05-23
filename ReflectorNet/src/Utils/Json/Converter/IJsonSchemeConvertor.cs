#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet.Common.Json
{
    public interface IJsonSchemaConverter
    {
        string Id { get; }
        JsonNode GetScheme();
        JsonNode GetSchemeRef();
    }
}
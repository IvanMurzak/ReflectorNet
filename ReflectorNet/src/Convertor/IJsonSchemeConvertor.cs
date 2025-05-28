using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public interface IJsonSchemaConverter
    {
        string Id { get; }
        JsonNode GetScheme();
        JsonNode GetSchemeRef();
    }
}
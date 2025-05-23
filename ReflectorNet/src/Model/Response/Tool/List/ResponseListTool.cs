#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public class ResponseListTool : IResponseListTool
    {
        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public JsonElement InputSchema { get; set; }

        public ResponseListTool() { }
    }
}
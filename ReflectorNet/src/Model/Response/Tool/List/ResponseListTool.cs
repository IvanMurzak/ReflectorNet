using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Model
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
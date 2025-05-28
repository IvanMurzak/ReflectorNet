using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IResponseListTool
    {
        string Name { get; set; }
        string? Title { get; set; }
        string? Description { get; set; }
        JsonElement InputSchema { get; set; }
    }
}
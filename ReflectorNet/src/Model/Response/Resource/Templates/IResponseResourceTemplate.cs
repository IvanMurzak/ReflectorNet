#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IResponseResourceTemplate
    {
        string uriTemplate { get; set; }
        string name { get; set; }
        string? mimeType { get; set; }
        string? description { get; set; }
    }
}
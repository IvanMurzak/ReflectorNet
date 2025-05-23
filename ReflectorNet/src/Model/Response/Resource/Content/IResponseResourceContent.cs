#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
namespace com.IvanMurzak.ReflectorNet.Common.Data
{
    public interface IResponseResourceContent
    {
        string uri { get; set; }
        string? mimeType { get; set; }
        string? text { get; set; }
        string? blob { get; set; }
    }
}
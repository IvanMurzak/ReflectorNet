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
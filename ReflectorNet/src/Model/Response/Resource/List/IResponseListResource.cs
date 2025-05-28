namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IResponseListResource
    {
        string uri { get; set; }
        string name { get; set; }
        string? mimeType { get; set; }
        string? description { get; set; }
        long? size { get; set; }
    }
}
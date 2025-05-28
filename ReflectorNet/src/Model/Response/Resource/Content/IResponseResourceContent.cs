namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IResponseResourceContent
    {
        string uri { get; set; }
        string? mimeType { get; set; }
        string? text { get; set; }
        string? blob { get; set; }
    }
}
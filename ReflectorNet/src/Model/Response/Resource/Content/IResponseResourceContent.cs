namespace com.IvanMurzak.ReflectorNet.Model
{
    public interface IResponseResourceContent
    {
        string uri { get; set; }
        string? mimeType { get; set; }
        string? text { get; set; }
        string? blob { get; set; }
    }
}
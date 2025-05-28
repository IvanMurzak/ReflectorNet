namespace com.IvanMurzak.ReflectorNet.Data
{
    public class ResponseResourceTemplate : IResponseResourceTemplate
    {
        public string uriTemplate { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? mimeType { get; set; }
        public string? description { get; set; }

        public ResponseResourceTemplate() { }
        public ResponseResourceTemplate(string uri, string name, string? mimeType = null, string? description = null)
        {
            this.uriTemplate = uri;
            this.name = name;
            this.mimeType = mimeType;
            this.description = description;
        }
    }
}
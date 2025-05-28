namespace com.IvanMurzak.ReflectorNet.Model
{
    public class ResponseListResource : IResponseListResource
    {
        public string uri { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string? mimeType { get; set; }
        public string? description { get; set; }
        public long? size { get; set; }

        public ResponseListResource() { }
        public
        ResponseListResource(string uri, string name, string? mimeType = null, string? description = null, long? size = null)
        {
            this.uri = uri;
            this.name = name;
            this.mimeType = mimeType;
            this.description = description;
            this.size = size;
        }
    }
}
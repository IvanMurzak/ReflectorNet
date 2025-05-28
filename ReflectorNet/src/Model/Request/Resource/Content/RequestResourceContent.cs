
using System;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public class RequestResourceContent : IRequestResourceContent
    {
        public string RequestID { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;

        public RequestResourceContent() { }
        public RequestResourceContent(string uri)
            : this(Guid.NewGuid().ToString(), uri) { }
        public RequestResourceContent(string requestId, string uri)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public virtual void Dispose()
        {

        }
        ~RequestResourceContent() => Dispose();
    }
}
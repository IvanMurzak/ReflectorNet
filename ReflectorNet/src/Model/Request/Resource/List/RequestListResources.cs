
using System;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public class RequestListResources : IRequestListResources
    {
        public string RequestID { get; set; } = Guid.NewGuid().ToString();
        public string? Cursor { get; set; }

        public RequestListResources() { }
        public RequestListResources(string? cursor = null)
            : this(Guid.NewGuid().ToString(), cursor) { }
        public RequestListResources(string requestId, string? cursor = null)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            Cursor = cursor;
        }

        public virtual void Dispose()
        {

        }
        ~RequestListResources() => Dispose();
    }
}
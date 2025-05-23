#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

using System;

namespace com.IvanMurzak.ReflectorNet.Common.Data
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
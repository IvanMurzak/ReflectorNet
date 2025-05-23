#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public class RequestListTool : IRequestListTool
    {
        public string RequestID { get; set; } = Guid.NewGuid().ToString();

        // Empty constructor for JSON deserialization
        public RequestListTool() { }

        // Overloaded constructor to set RequestID
        public RequestListTool(string requestId)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
        }

        public virtual void Dispose()
        {
        }
        ~RequestListTool() => Dispose();
    }
}

using System;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public class RequestListResourceTemplates : IRequestListResourceTemplates
    {
        public string RequestID { get; set; } = string.Empty;
        public RequestListResourceTemplates() { }
        public RequestListResourceTemplates(string requestID)
        {
            RequestID = requestID ?? throw new ArgumentNullException(nameof(requestID));
        }

        public virtual void Dispose()
        {

        }
        ~RequestListResourceTemplates() => Dispose();
    }
}
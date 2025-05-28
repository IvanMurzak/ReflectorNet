using System;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IRequestResourceContent : IRequestID, IDisposable
    {
        public string Uri { get; set; }
    }
}
using System;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public interface IRequestResourceContent : IRequestID, IDisposable
    {
        public string Uri { get; set; }
    }
}
using System;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IRequestListResources : IRequestID, IDisposable
    {
        public string? Cursor { get; set; }
    }
}
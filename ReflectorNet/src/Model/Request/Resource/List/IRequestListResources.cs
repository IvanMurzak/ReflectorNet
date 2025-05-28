using System;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public interface IRequestListResources : IRequestID, IDisposable
    {
        public string? Cursor { get; set; }
    }
}
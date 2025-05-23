#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IRequestNotification : IRequestID, IDisposable
    {
        string? Path { get; set; }
        string? Name { get; set; }
        IDictionary<string, object?>? Parameters { get; set; }
    }
}
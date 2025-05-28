using System;
using System.Collections.Generic;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IRequestCallTool : IRequestID, IDisposable
    {
        string Name { get; set; }
        IReadOnlyDictionary<string, JsonElement> Arguments { get; set; }
    }
}
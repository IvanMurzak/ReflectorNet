#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Common.Data
{
    public interface IRequestCallTool : IRequestID, IDisposable
    {
        string Name { get; set; }
        IReadOnlyDictionary<string, JsonElement> Arguments { get; set; }
    }
}
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Common.Data
{
    public interface IResponseCallTool
    {
        bool IsError { get; set; }
        List<ResponseCallToolContent> Content { get; set; }
    }
}
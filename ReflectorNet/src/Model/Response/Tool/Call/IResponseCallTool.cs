using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IResponseCallTool
    {
        bool IsError { get; set; }
        List<ResponseCallToolContent> Content { get; set; }
    }
}
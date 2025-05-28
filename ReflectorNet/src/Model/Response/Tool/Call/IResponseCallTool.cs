using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public interface IResponseCallTool
    {
        bool IsError { get; set; }
        List<ResponseCallToolContent> Content { get; set; }
    }
}
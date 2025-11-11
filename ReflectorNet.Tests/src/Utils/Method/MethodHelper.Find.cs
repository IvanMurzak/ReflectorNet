using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Tests.Model;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public static partial class MethodHelper
    {
        public static string Object_Int_Bool
        (
            [Description("GameObject reference.")]
            GameObjectRef obj,
            [Description("Integer parameter.")]
            int integer = 0,
            [Description("Boolean parameter.")]
            bool boolean = false
        )
        {
            return string.Empty;
        }
    }
}
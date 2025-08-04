using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public static partial class MethodHelper
    {
        public static string ListObject_ListObject
        (
            [Description("List of objects 1.")]
            GameObjectRefList obj1,
            [Description("List of objects 2.")]
            SerializedMemberList obj2
        )
        {
            return string.Empty;
        }
    }
}
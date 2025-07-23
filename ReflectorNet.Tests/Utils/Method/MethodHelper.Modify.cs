using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;
using ReflectorNet.Tests.Schema.Model;

namespace ReflectorNet.Tests
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
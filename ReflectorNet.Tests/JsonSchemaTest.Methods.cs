using System.ComponentModel;
using ReflectorNet.Tests.Schema.Model;

namespace ReflectorNet.Tests
{
    public static partial class MethodHelper
    {
        public static string Find
        (
            GameObjectRef gameObjectRef,
            [Description("Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below.")]
            int includeChildrenDepth = 0,
            [Description("If true, it will print only brief data of the target GameObject.")]
            bool briefData = false
        )
        {
            return string.Empty;
        }
    }
}
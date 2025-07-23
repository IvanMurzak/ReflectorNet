using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;

namespace ReflectorNet.Tests
{
    public class TestClass
    {
        public const string CustomDescriptionA = "CustomDescriptionA";
        public const string CustomDescriptionB = "CustomDescriptionB";

        public void NoParameters_ReturnVoid()
        {
            // This method does nothing
        }

        public bool NoParameters_ReturnBool()
        {
            return true;
        }

        public string SerializedMemberList_ReturnString
        (
            [Description(CustomDescriptionA)]
            SerializedMemberList gameObjectDiffs
        )
        {
            return "SerializedMemberList";
        }
    }
}
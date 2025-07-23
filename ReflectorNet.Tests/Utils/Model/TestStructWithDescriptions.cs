using System;
using System.ComponentModel;

namespace ReflectorNet.Tests.Schema.Model
{
    /// <summary>
    /// Test struct with Description attributes for testing schema generation.
    /// </summary>
    [Description("A test struct with various member types and descriptions.")]
    public struct TestStructWithDescriptions
    {
        [Description("X coordinate of the point.")]
        public float X;

        [Description("Y coordinate of the point.")]
        public float Y;

        [Description("Whether this point is valid.")]
        public bool IsValid { get; set; }

        // Constructor
        public TestStructWithDescriptions(float x, float y)
        {
            X = x;
            Y = y;
            IsValid = true;
        }
    }
}

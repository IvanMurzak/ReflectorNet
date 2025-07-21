using System.Collections.Generic;
using System.ComponentModel;

namespace ReflectorNet.Tests.Schema.Model
{
    [Description("A collection class that inherits from List for description testing.")]
    public class TestCollectionWithDescription : List<string>
    {
        [Description("Additional metadata field for this collection.")]
        public string CollectionName { get; set; } = "TestCollection";

        [Description("Maximum capacity for this collection.")]
        public int MaxCapacity = 100;
    }
}

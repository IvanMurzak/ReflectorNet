using System.Collections.Generic;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;

namespace ReflectorNet.Tests.SchemaTests
{
    public partial class TestDescription : BaseTest
    {
        public TestDescription(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void PropertyDescriptionOfCustomType()
        {
            TestClassMembersDescription(typeof(GameObjectRef));
            TestClassMembersDescription(typeof(GameObjectRefList));
            TestClassMembersDescription(typeof(List<GameObjectRef>));
            TestClassMembersDescription(typeof(GameObjectRef[]));
        }
    }
}

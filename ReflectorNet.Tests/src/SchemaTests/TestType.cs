using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public partial class TestType : BaseTest
    {
        public TestType(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Generic_SerializedMemberList()
        {
            var type = typeof(SerializedMemberList);
            var genericTypes = TypeUtils.GetGenericTypes(type).ToList();

            _output.WriteLine($"Generic types for {type.GetTypeId()}: {string.Join(", ", genericTypes.Select(t => t.GetTypeId()))}");

            Assert.NotEmpty(genericTypes);

            Assert.Contains(typeof(SerializedMember), genericTypes);

            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMember)));
        }
        [Fact]
        public void Generic_List_SerializedMemberList()
        {
            var type = typeof(List<SerializedMemberList>);
            var genericTypes = TypeUtils.GetGenericTypes(type).ToList();

            _output.WriteLine($"Generic types for {type.GetTypeId()}: {string.Join(", ", genericTypes.Select(t => t.GetTypeId()))}");

            Assert.NotEmpty(genericTypes);

            Assert.Contains(typeof(SerializedMemberList), genericTypes);
            Assert.Contains(typeof(SerializedMember), genericTypes);

            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMemberList)));
            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMember)));
        }

        [Fact]
        public void Generic_List_SerializedMember()
        {
            var type = typeof(List<SerializedMember>);
            var genericTypes = TypeUtils.GetGenericTypes(type).ToList();

            _output.WriteLine($"Generic types for {type.GetTypeId()}: {string.Join(", ", genericTypes.Select(t => t.GetTypeId()))}");

            Assert.NotEmpty(genericTypes);

            Assert.Contains(typeof(SerializedMember), genericTypes);

            Assert.Equal(1, genericTypes.Count(x => x == typeof(SerializedMember)));
        }

        void TestTypeName(Type type)
        {
            var typeName = TypeUtils.GetTypeName(type, pretty: false);
            Assert.Equal(type, TypeUtils.GetType(typeName));

            typeName = TypeUtils.GetTypeName(type, pretty: true);
            Assert.Equal(type, TypeUtils.GetType(typeName));
        }



        [Fact]
        public void GetTypeName_OuterAssembly()
        {
            TestTypeName(typeof(OuterAssembly.Model.ParentClass));
            TestTypeName(typeof(OuterAssembly.Model.ParentClass.NestedClass));
        }
    }
}

using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class CollectionsTests : BaseTest
    {
        public CollectionsTests(ITestOutputHelper output) : base(output) { }

        static readonly List<Type> _collectionTypes = new List<Type>
        {
            typeof(int[]),
            typeof(string[]),
            typeof(List<int>),
            typeof(IList<int>),
            typeof(IReadOnlyList<int>),
            typeof(Dictionary<string, int>),
            typeof(IEnumerable<int>),
            typeof(IList<string>),
            typeof(ICollection<double>),
            typeof(SerializedMemberList),
            typeof(ListType),
            typeof(ListTypeGeneric<int>),
            typeof(ListTypeGeneric<string>),
            typeof(ListTypeGeneric<int[]>),
            typeof(ListTypeGeneric<ListType>),
            typeof(ListTypeGeneric<ListTypeGeneric<int>>),
            typeof(ListTypeGeneric<ListTypeGeneric<int[]>>)
        };

        class ListType : List<int>
        {
            public int? shouldBeIgnored;
            public int ShouldBeIgnored { get; set; }

            public ListType() { shouldBeIgnored = default; }
        }
        class ListTypeGeneric<T> : List<T>
        {
            public T? shouldBeIgnored = default;
            public T? ShouldBeIgnored { get; set; }

            public ListTypeGeneric() { shouldBeIgnored = default; }
        }

        [Fact]
        public void GetTypeId_SimpleArray_ShouldAppendArray()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            foreach (var type in _collectionTypes)
            {
                var result = reflector.GetSchema(type);

                _output.WriteLine($"Type: {type.GetTypeShortName()}\n{result.ToJsonString()}\n");

                // Assert
                Assert.NotNull(result);
                Assert.NotNull(result[JsonSchema.Type]);
                Assert.Equal(JsonSchema.Array, result[JsonSchema.Type]!.ToString());

                Assert.NotNull(result[JsonSchema.Items]);

                Assert.Null(result[nameof(ListType.shouldBeIgnored)]);
                Assert.Null(result[nameof(ListType.ShouldBeIgnored)]);
            }
        }

        [Fact]
        public void CheckIfCollectionTypesAreEnumerable()
        {
            // Act
            foreach (var type in _collectionTypes)
            {
                var isEnumerable = TypeUtils.IsIEnumerable(type);
                _output.WriteLine($"Checking type: {type.GetTypeShortName()}, IsEnumerable: {isEnumerable}");

                Assert.True(isEnumerable);
            }
        }
    }
}

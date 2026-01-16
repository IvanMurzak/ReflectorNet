using System;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for JSON serialization behavior with [Obsolete] attributed members.
    /// </summary>
    public class ObsoletePropertyTests : BaseTest
    {
        private readonly Reflector _reflector;

        public ObsoletePropertyTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region Test Model Classes

        public class ClassWithObsoleteProperty
        {
            public string? ActiveProperty { get; set; }

            [Obsolete("birth0 property is deprecated. Use AddSubEmitter, RemoveSubEmitter, SetSubEmitterSystem and GetSubEmitterSystem instead.", false)]
            public string? ObsoleteProperty
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class ClassWithObsoleteField
        {
            public string? ActiveField;

            [Obsolete]
            public string? ObsoleteField;
        }

        public class ClassWithMixedObsoleteMembers
        {
            public string? ActiveProperty { get; set; }
            public string? ActiveField;

            [Obsolete]
            public string? ObsoleteProperty { get; set; }

            [Obsolete]
            public string? ObsoleteField;
        }

        public class ClassWithObsoleteMessage
        {
            public string? ActiveProperty { get; set; }

            [Obsolete("This property is deprecated, use ActiveProperty instead.")]
            public string? ObsoleteWithMessage { get; set; }
        }

        public class ParentWithObsolete
        {
            public string? Name { get; set; }
            public NestedWithObsolete? Nested { get; set; }
        }

        public class NestedWithObsolete
        {
            public string? ActiveValue { get; set; }

            [Obsolete]
            public string? ObsoleteValue { get; set; }
        }

        #endregion

        #region Test Model Structs

        public struct StructWithObsoleteProperty
        {
            public string? ActiveProperty { get; set; }

            [Obsolete]
            public string? ObsoleteProperty { get; set; }
        }

        public struct StructWithObsoleteField
        {
            public string? ActiveField;

            [Obsolete]
            public string? ObsoleteField;
        }

        public struct StructWithMixedObsoleteMembers
        {
            public string? ActiveProperty { get; set; }
            public string? ActiveField;

            [Obsolete]
            public string? ObsoleteProperty { get; set; }

            [Obsolete]
            public string? ObsoleteField;
        }

        #endregion

        #region Tests
#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete

        [Fact]
        public void Serialize_ExcludesObsoleteProperty()
        {
            // Arrange - note: ObsoleteProperty throws on get/set, so we don't set it
            // This test verifies that the serializer never accesses the obsolete property
            var instance = new ClassWithObsoleteProperty
            {
                ActiveProperty = "active"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveProperty", json);
            Assert.DoesNotContain("ObsoleteProperty", json);
        }

        [Fact]
        public void Serialize_ExcludesObsoleteField()
        {
            // Arrange
            var instance = new ClassWithObsoleteField
            {
                ActiveField = "active",
                ObsoleteField = "obsolete"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveField", json);
            Assert.DoesNotContain("ObsoleteField", json);
        }

        [Fact]
        public void Serialize_ExcludesMixedObsoleteMembers()
        {
            // Arrange
            var instance = new ClassWithMixedObsoleteMembers
            {
                ActiveProperty = "active prop",
                ActiveField = "active field",
                ObsoleteProperty = "obsolete prop",
                ObsoleteField = "obsolete field"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveProperty", json);
            Assert.Contains("ActiveField", json);
            Assert.DoesNotContain("ObsoleteProperty", json);
            Assert.DoesNotContain("ObsoleteField", json);
        }

        [Fact]
        public void Serialize_ObsoleteWithMessage_StillExcluded()
        {
            // Arrange
            var instance = new ClassWithObsoleteMessage
            {
                ActiveProperty = "active",
                ObsoleteWithMessage = "obsolete"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveProperty", json);
            Assert.DoesNotContain("ObsoleteWithMessage", json);
        }

        [Fact]
        public void Serialize_NestedClassWithObsolete_ExcludesObsoleteMembers()
        {
            // Arrange
            var instance = new ParentWithObsolete
            {
                Name = "Parent",
                Nested = new NestedWithObsolete
                {
                    ActiveValue = "nested active",
                    ObsoleteValue = "nested obsolete"
                }
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("Name", json);
            Assert.Contains("Nested", json);
            Assert.Contains("ActiveValue", json);
            Assert.DoesNotContain("ObsoleteValue", json);
        }

        [Fact]
        public void Serialize_Struct_ExcludesObsoleteProperty()
        {
            // Arrange
            var instance = new StructWithObsoleteProperty
            {
                ActiveProperty = "active",
                ObsoleteProperty = "obsolete"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveProperty", json);
            Assert.DoesNotContain("ObsoleteProperty", json);
        }

        [Fact]
        public void Serialize_Struct_ExcludesObsoleteField()
        {
            // Arrange
            var instance = new StructWithObsoleteField
            {
                ActiveField = "active",
                ObsoleteField = "obsolete"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveField", json);
            Assert.DoesNotContain("ObsoleteField", json);
        }

        [Fact]
        public void Serialize_Struct_ExcludesMixedObsoleteMembers()
        {
            // Arrange
            var instance = new StructWithMixedObsoleteMembers
            {
                ActiveProperty = "active prop",
                ActiveField = "active field",
                ObsoleteProperty = "obsolete prop",
                ObsoleteField = "obsolete field"
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(instance);
            _output.WriteLine($"Serialized JSON: {json}");

            // Assert
            Assert.Contains("ActiveProperty", json);
            Assert.Contains("ActiveField", json);
            Assert.DoesNotContain("ObsoleteProperty", json);
            Assert.DoesNotContain("ObsoleteField", json);
        }

        #endregion
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0612 // Type or member is obsolete
    }
}

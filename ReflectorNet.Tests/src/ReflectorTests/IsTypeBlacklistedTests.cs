using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public class IsTypeBlacklistedTests : BaseTest
    {
        public IsTypeBlacklistedTests(ITestOutputHelper output) : base(output) { }

        #region Test Types for Blacklist Testing

        // Base classes and interfaces for inheritance testing
        public class BlacklistedBaseClass { }
        public class DerivedFromBlacklisted : BlacklistedBaseClass { }
        public class DeeplyDerivedFromBlacklisted : DerivedFromBlacklisted { }

        public interface IBlacklistedInterface { }
        public class ImplementsBlacklistedInterface : IBlacklistedInterface { }

        public interface IBlacklistedGenericInterface<T> { }
        public class ImplementsBlacklistedGenericInterface : IBlacklistedGenericInterface<string> { }

        // Non-blacklisted types for negative tests
        public class NonBlacklistedClass { }
        public class AnotherNonBlacklistedClass { }

        // For nested generic testing
        public class GenericWrapper<T> { public T? Value { get; set; } }
        public class MultiGenericWrapper<T1, T2> { public T1? First { get; set; } public T2? Second { get; set; } }
        public class TripleGenericWrapper<T1, T2, T3> { public T1? First { get; set; } public T2? Second { get; set; } public T3? Third { get; set; } }

        #endregion

        #region Null Type Tests

        [Fact]
        public void IsTypeBlacklisted_NullType_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(null!);

            // Assert
            Assert.False(result);
            _output.WriteLine("Null type correctly returns false");
        }

        #endregion

        #region Exact Type Match Tests

        [Fact]
        public void IsTypeBlacklisted_ExactTypeMatch_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));

            // Assert
            Assert.True(result);
            _output.WriteLine("Exact type match correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_NonBlacklistedType_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));

            // Assert
            Assert.False(result);
            _output.WriteLine("Non-blacklisted type correctly returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_PrimitiveType_NotBlacklisted_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(int));

            // Assert
            Assert.False(result);
            _output.WriteLine("Primitive type (int) not blacklisted returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_PrimitiveType_Blacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(int));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(int));

            // Assert
            Assert.True(result);
            _output.WriteLine("Blacklisted primitive type (int) returns true");
        }

        #endregion

        #region Inheritance Chain Tests

        [Fact]
        public void IsTypeBlacklisted_DerivedFromBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted));

            // Assert
            Assert.True(result);
            _output.WriteLine("Type derived from blacklisted base correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_DeeplyDerivedFromBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(DeeplyDerivedFromBlacklisted));

            // Assert
            Assert.True(result);
            _output.WriteLine("Deeply derived type (2 levels) from blacklisted base correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_BaseClassNotBlacklisted_OnlyDerivedBlacklisted_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(DerivedFromBlacklisted));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));

            // Assert
            Assert.False(result);
            _output.WriteLine("Base class is not blacklisted when only derived is blacklisted");
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void IsTypeBlacklisted_ImplementsBlacklistedInterface_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(IBlacklistedInterface));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsBlacklistedInterface));

            // Assert
            Assert.True(result);
            _output.WriteLine("Type implementing blacklisted interface correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ImplementsBlacklistedGenericInterface_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(IBlacklistedGenericInterface<string>));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsBlacklistedGenericInterface));

            // Assert
            Assert.True(result);
            _output.WriteLine("Type implementing blacklisted generic interface correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ImplementsIDisposable_WhenBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(IDisposable));

            // Act - System.IO.MemoryStream implements IDisposable
            var result = reflector.Converters.IsTypeBlacklisted(typeof(System.IO.MemoryStream));

            // Assert
            Assert.True(result);
            _output.WriteLine("MemoryStream (implements IDisposable) correctly detected as blacklisted");
        }

        #endregion

        #region Array Tests

        [Fact]
        public void IsTypeBlacklisted_ArrayOfBlacklistedType_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass[]));

            // Assert
            Assert.True(result);
            _output.WriteLine("Array of blacklisted type correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ArrayOfDerivedFromBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted[]));

            // Assert
            Assert.True(result);
            _output.WriteLine("Array of type derived from blacklisted correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ArrayOfNonBlacklistedType_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass[]));

            // Assert
            Assert.False(result);
            _output.WriteLine("Array of non-blacklisted type correctly returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_JaggedArrayOfBlacklistedType_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass[][]));

            // Assert
            Assert.True(result);
            _output.WriteLine("Jagged array (BlacklistedBaseClass[][]) correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_MultiDimensionalArrayOfBlacklistedType_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass[,]));

            // Assert
            Assert.True(result);
            _output.WriteLine("Multi-dimensional array (BlacklistedBaseClass[,]) correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ArrayOfInterfaceImplementor_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(IBlacklistedInterface));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsBlacklistedInterface[]));

            // Assert
            Assert.True(result);
            _output.WriteLine("Array of type implementing blacklisted interface correctly returns true");
        }

        #endregion

        #region Generic Type Tests

        [Fact]
        public void IsTypeBlacklisted_GenericWithBlacklistedTypeArgument_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<BlacklistedBaseClass>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Generic List<BlacklistedBaseClass> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericWithDerivedTypeArgument_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<DerivedFromBlacklisted>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Generic List<DerivedFromBlacklisted> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericWithNonBlacklistedTypeArgument_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<NonBlacklistedClass>));

            // Assert
            Assert.False(result);
            _output.WriteLine("Generic List<NonBlacklistedClass> correctly returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_DictionaryWithBlacklistedKey_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<BlacklistedBaseClass, string>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Dictionary<BlacklistedBaseClass, string> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_DictionaryWithBlacklistedValue_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<string, BlacklistedBaseClass>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Dictionary<string, BlacklistedBaseClass> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_DictionaryWithNonBlacklistedTypes_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<string, int>));

            // Assert
            Assert.False(result);
            _output.WriteLine("Dictionary<string, int> correctly returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_MultipleGenericArguments_OneBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(MultiGenericWrapper<string, BlacklistedBaseClass>));

            // Assert
            Assert.True(result);
            _output.WriteLine("MultiGenericWrapper<string, BlacklistedBaseClass> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_MultipleGenericArguments_NoneBlacklisted_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(MultiGenericWrapper<string, int>));

            // Assert
            Assert.False(result);
            _output.WriteLine("MultiGenericWrapper<string, int> correctly returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_TripleGenericArguments_LastBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(TripleGenericWrapper<string, int, BlacklistedBaseClass>));

            // Assert
            Assert.True(result);
            _output.WriteLine("TripleGenericWrapper<string, int, BlacklistedBaseClass> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericWithInterfaceImplementor_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(IBlacklistedInterface));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<ImplementsBlacklistedInterface>));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<ImplementsBlacklistedInterface> correctly returns true");
        }

        #endregion

        #region Nested Generic and Array Combinations

        [Fact]
        public void IsTypeBlacklisted_ListOfArrayOfBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<BlacklistedBaseClass[]>));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<BlacklistedBaseClass[]> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ArrayOfListOfBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<BlacklistedBaseClass>[]));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<BlacklistedBaseClass>[] correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_NestedGeneric_InnerBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - List<GenericWrapper<BlacklistedBaseClass>>
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<GenericWrapper<BlacklistedBaseClass>>));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<GenericWrapper<BlacklistedBaseClass>> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_DeeplyNestedGeneric_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - Dictionary<string, List<GenericWrapper<BlacklistedBaseClass>>>
            var result = reflector.Converters.IsTypeBlacklisted(
                typeof(Dictionary<string, List<GenericWrapper<BlacklistedBaseClass>>>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Dictionary<string, List<GenericWrapper<BlacklistedBaseClass>>> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_DeeplyNestedGenericWithDerived_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - Dictionary<string, List<DerivedFromBlacklisted>>
            var result = reflector.Converters.IsTypeBlacklisted(
                typeof(Dictionary<string, List<DerivedFromBlacklisted>>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Dictionary<string, List<DerivedFromBlacklisted>> correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ArrayOfArrayOfGenericWithBlacklisted_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - List<BlacklistedBaseClass>[][]
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<BlacklistedBaseClass>[][]));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<BlacklistedBaseClass>[][] correctly returns true");
        }

        #endregion

        #region Nullable Type Tests

        [Fact]
        public void IsTypeBlacklisted_NullableOfBlacklistedStruct_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(DateTime));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(DateTime?));

            // Assert
            Assert.True(result);
            _output.WriteLine("Nullable<DateTime> correctly returns true when DateTime is blacklisted");
        }

        [Fact]
        public void IsTypeBlacklisted_NullableOfNonBlacklistedStruct_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(int?));

            // Assert
            Assert.False(result);
            _output.WriteLine("Nullable<int> correctly returns false");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void IsTypeBlacklisted_EmptyBlacklist_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            // Don't blacklist anything

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));

            // Assert
            Assert.False(result);
            _output.WriteLine("With empty blacklist, any type returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_ObjectType_WhenBlacklisted_AllDerivedAreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(object));

            // Act - Everything derives from object
            var result1 = reflector.Converters.IsTypeBlacklisted(typeof(string));
            var result2 = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));
            var result3 = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            _output.WriteLine("When object is blacklisted, all reference types are blacklisted");
        }

        [Fact]
        public void IsTypeBlacklisted_ValueType_WhenBlacklisted_AllStructsAreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(ValueType));

            // Act
            var result1 = reflector.Converters.IsTypeBlacklisted(typeof(int));
            var result2 = reflector.Converters.IsTypeBlacklisted(typeof(DateTime));
            var result3 = reflector.Converters.IsTypeBlacklisted(typeof(Guid));

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            _output.WriteLine("When ValueType is blacklisted, all structs are blacklisted");
        }

        [Fact]
        public void IsTypeBlacklisted_RemovedFromBlacklist_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Verify it's blacklisted first
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass)));

            // Act - Remove from blacklist
            reflector.Converters.RemoveBlacklistedType(typeof(BlacklistedBaseClass));
            var result = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));

            // Assert
            Assert.False(result);
            _output.WriteLine("After removal from blacklist, type returns false");
        }

        [Fact]
        public void IsTypeBlacklisted_MultipleTypesBlacklisted_AllDetected()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));
            reflector.Converters.BlacklistType(typeof(NonBlacklistedClass));
            reflector.Converters.BlacklistType(typeof(IBlacklistedInterface));

            // Act
            var result1 = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));
            var result2 = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));
            var result3 = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsBlacklistedInterface));
            var result4 = reflector.Converters.IsTypeBlacklisted(typeof(AnotherNonBlacklistedClass));

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.True(result3);
            Assert.False(result4);
            _output.WriteLine("Multiple blacklisted types are all correctly detected");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericTypeDefinition_WhenClosed_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - Check open generic with closed blacklisted type argument
            var closedGenericType = typeof(GenericWrapper<>).MakeGenericType(typeof(BlacklistedBaseClass));
            var result = reflector.Converters.IsTypeBlacklisted(closedGenericType);

            // Assert
            Assert.True(result);
            _output.WriteLine("Dynamically constructed generic with blacklisted type argument correctly detected");
        }

        [Fact]
        public void IsTypeBlacklisted_SystemTypes_CanBeBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(System.Threading.Thread));

            // Act
            var result = reflector.Converters.IsTypeBlacklisted(typeof(System.Threading.Thread));

            // Assert
            Assert.True(result);
            _output.WriteLine("System types (Thread) can be blacklisted");
        }

        [Fact]
        public void IsTypeBlacklisted_IEnumerableBlacklisted_ListIsBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(System.Collections.IEnumerable));

            // Act - List<T> implements IEnumerable
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<int>));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<int> is blacklisted when IEnumerable is blacklisted");
        }

        #endregion

        #region Separate Reflector Instance Tests

        [Fact]
        public void IsTypeBlacklisted_SeparateReflectorInstances_IndependentBlacklists()
        {
            // Arrange
            var reflector1 = new Reflector();
            var reflector2 = new Reflector();
            reflector1.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act
            var result1 = reflector1.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));
            var result2 = reflector2.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));

            // Assert
            Assert.True(result1);
            Assert.False(result2);
            _output.WriteLine("Separate Reflector instances have independent blacklists");
        }

        #endregion
    }
}

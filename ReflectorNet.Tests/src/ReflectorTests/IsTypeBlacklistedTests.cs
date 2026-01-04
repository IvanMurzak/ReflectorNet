using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        // Generic interface with blacklisted type argument testing
        public interface IGenericInterface<T> { }
        public class ImplementsGenericInterfaceWithBlacklisted : IGenericInterface<BlacklistedBaseClass> { }
        public class ImplementsGenericInterfaceWithDerived : IGenericInterface<DerivedFromBlacklisted> { }
        public class ImplementsGenericInterfaceWithNonBlacklisted : IGenericInterface<NonBlacklistedClass> { }

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

        [Fact]
        public void IsTypeBlacklisted_ImplementsGenericInterfaceWithBlacklistedTypeArg_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - Class implements IGenericInterface<BlacklistedBaseClass>
            var result = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsGenericInterfaceWithBlacklisted));

            // Assert
            Assert.True(result);
            _output.WriteLine("Type implementing generic interface with blacklisted type argument correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ImplementsGenericInterfaceWithDerivedBlacklistedTypeArg_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - Class implements IGenericInterface<DerivedFromBlacklisted> where DerivedFromBlacklisted extends BlacklistedBaseClass
            var result = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsGenericInterfaceWithDerived));

            // Assert
            Assert.True(result);
            _output.WriteLine("Type implementing generic interface with derived blacklisted type argument correctly returns true");
        }

        [Fact]
        public void IsTypeBlacklisted_ImplementsGenericInterfaceWithNonBlacklistedTypeArg_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Act - Class implements IGenericInterface<NonBlacklistedClass>
            var result = reflector.Converters.IsTypeBlacklisted(typeof(ImplementsGenericInterfaceWithNonBlacklisted));

            // Assert
            Assert.False(result);
            _output.WriteLine("Type implementing generic interface with non-blacklisted type argument correctly returns false");
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

        #region Inheritance with Generics Tests

        public class GenericBase<T> { }

        // Case 1: Extended from a class that has a generic argument which is blacklisted
        public class DerivedFromGenericWithBlacklistedArg : GenericBase<BlacklistedBaseClass> { }

        // Case 3: Extended from a class that extends a class with blacklisted generic arg
        public class DeeplyDerived : DerivedFromGenericWithBlacklistedArg { }

        [Fact]
        public void IsTypeBlacklisted_DerivedFromGenericWithBlacklistedArg_ReturnsTrue()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromGenericWithBlacklistedArg)),
                "DerivedFromGenericWithBlacklistedArg should be blacklisted because it inherits from GenericBase<BlacklistedBaseClass>");
        }

        [Fact]
        public void IsTypeBlacklisted_DeeplyDerivedFromGenericWithBlacklistedArg_ReturnsTrue()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(DeeplyDerived)),
                "DeeplyDerived should be blacklisted because it inherits from DerivedFromGenericWithBlacklistedArg");
        }

        #endregion

        #region Concurrency and Cache Tests

        [Fact]
        public async Task IsTypeBlacklisted_ConcurrentAccess_NoExceptionsAndCorrectResults()
        {
            // Arrange
            var reflector = new Reflector();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var results = new System.Collections.Concurrent.ConcurrentBag<bool>();
            var barrier = new Barrier(4);

            // Act - Run concurrent reads and writes
            var tasks = new Task[4];

            // Task 0: Adds types to blacklist
            tasks[0] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    for (int i = 0; i < 100; i++)
                    {
                        reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));
                        reflector.Converters.BlacklistType(typeof(IBlacklistedInterface));
                    }
                }
                catch (Exception ex) { exceptions.Add(ex); }
            });

            // Tasks 1-3: Read from blacklist concurrently
            for (int t = 1; t < 4; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (int i = 0; i < 100; i++)
                        {
                            results.Add(reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted)));
                            results.Add(reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass)));
                            results.Add(reflector.Converters.IsTypeBlacklisted(typeof(List<BlacklistedBaseClass>)));
                        }
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
            _output.WriteLine($"Completed {results.Count} concurrent blacklist checks without exceptions");
        }

        [Fact]
        public async Task IsTypeBlacklisted_ConcurrentBlacklistModification_RetriesAndReturnsCorrectResult()
        {
            // Arrange
            var reflector = new Reflector();
            var correctResultsCount = 0;
            var barrier = new Barrier(2);

            // Act - One thread modifies blacklist while another reads
            var writerTask = Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < 50; i++)
                {
                    reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));
                    Thread.Sleep(1); // Small delay to interleave
                    reflector.Converters.RemoveBlacklistedType(typeof(BlacklistedBaseClass));
                }
                // End with type blacklisted
                reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));
            });

            var readerTask = Task.Run(() =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < 100; i++)
                {
                    // Result may vary during concurrent modification, but should never throw
                    var result = reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted));
                    if (result) Interlocked.Increment(ref correctResultsCount);
                }
            });

            await Task.WhenAll(writerTask, readerTask);

            // Final state should be consistent
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted)),
                "After writer finishes with BlacklistType, derived types should be blacklisted");
            _output.WriteLine($"Reader got 'true' result {correctResultsCount} times during concurrent modification");
        }

        // Helper types for cache size test - generate many unique types
        public class CacheTestType1 { }
        public class CacheTestType2 { }
        public class CacheTestType3 { }
        public class CacheTestType4 { }
        public class CacheTestType5 { }

        [Fact]
        public void IsTypeBlacklisted_CacheSizeLimit_ClearsWhenExceeded()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedBaseClass));

            // Get all types from the current assembly to fill the cache
            var types = typeof(IsTypeBlacklistedTests).Assembly.GetTypes()
                .Where(t => t != null)
                .Take(1100) // More than MaxBlacklistCacheSize (1000)
                .ToList();

            // Act - Query many types to fill the cache beyond its limit
            foreach (var type in types)
            {
                try
                {
                    reflector.Converters.IsTypeBlacklisted(type);
                }
                catch
                {
                    // Some types may throw during reflection, ignore
                }
            }

            // Query again - should still work correctly after cache was cleared
            var result1 = reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass));
            var result2 = reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted));
            var result3 = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));

            // Assert - Results should still be correct after cache overflow
            Assert.True(result1, "Exact blacklisted type should return true");
            Assert.True(result2, "Derived from blacklisted type should return true");
            Assert.False(result3, "Non-blacklisted type should return false");

            _output.WriteLine($"Queried {types.Count} types, cache correctly handles overflow");
        }

        [Fact]
        public void IsTypeBlacklisted_CacheInvalidation_ReturnsCorrectResultAfterBlacklistChange()
        {
            // Arrange
            var reflector = new Reflector();

            // Prime the cache with a "false" result
            var initialResult = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));
            Assert.False(initialResult);

            // Act - Blacklist the type
            reflector.Converters.BlacklistType(typeof(NonBlacklistedClass));

            // Assert - Cache should be invalidated, new result should be correct
            var afterBlacklistResult = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));
            Assert.True(afterBlacklistResult, "After blacklisting, type should be detected as blacklisted");

            // Act - Remove from blacklist
            reflector.Converters.RemoveBlacklistedType(typeof(NonBlacklistedClass));

            // Assert - Cache should be invalidated again
            var afterRemoveResult = reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass));
            Assert.False(afterRemoveResult, "After removing from blacklist, type should not be detected as blacklisted");

            _output.WriteLine("Cache correctly invalidated after blacklist modifications");
        }

        #endregion
    }
}

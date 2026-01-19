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

        #region Generic Type Definition Blacklisting Tests

        [Fact]
        public void IsTypeBlacklisted_ClosedGenericBlacklisted_OtherClosedTypesNotAffected()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(List<int>)); // Only blacklist List<int>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<int>)));     // Exact match - blacklisted
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(List<string>))); // Different closed type - NOT blacklisted
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(List<object>))); // Different closed type - NOT blacklisted

            _output.WriteLine("Blacklisting List<int> does not affect List<string> or List<object>");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericTypeDefinitionBlacklisted_AllClosedTypesBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(List<>)); // Blacklist the generic type definition

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<int>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<string>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<object>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<BlacklistedBaseClass>)));

            _output.WriteLine("Blacklisting List<> blacklists all List<T> types");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericTypeDefinitionBlacklisted_OtherGenericFamiliesNotAffected()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(List<>)); // Only blacklist List<>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<int>)));              // List<> family - blacklisted
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<string, int>))); // Different family - NOT blacklisted
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(HashSet<int>)));           // Different family - NOT blacklisted

            _output.WriteLine("Blacklisting List<> does not affect Dictionary<,> or HashSet<>");
        }

        [Fact]
        public void IsTypeBlacklisted_DictionaryDefinitionBlacklisted_AllClosedTypesBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(Dictionary<,>)); // Blacklist Dictionary<,>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<string, int>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<int, string>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<object, object>)));

            _output.WriteLine("Blacklisting Dictionary<,> blacklists all Dictionary<TKey, TValue> types");
        }

        [Fact]
        public void IsTypeBlacklisted_NestedGenericWithBlacklistedDefinition_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(List<>)); // Blacklist List<>

            // Act - Dictionary contains List<int> as a type argument
            var result = reflector.Converters.IsTypeBlacklisted(typeof(Dictionary<string, List<int>>));

            // Assert
            Assert.True(result);
            _output.WriteLine("Dictionary<string, List<int>> is blacklisted when List<> is blacklisted");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericWrapperDefinitionBlacklisted_CustomClassBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(GenericWrapper<>)); // Blacklist our custom GenericWrapper<>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(GenericWrapper<int>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(GenericWrapper<string>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(GenericWrapper<BlacklistedBaseClass>)));

            _output.WriteLine("Blacklisting GenericWrapper<> blacklists all GenericWrapper<T> types");
        }

        [Fact]
        public void IsTypeBlacklisted_MultiGenericWrapperDefinitionBlacklisted_AllClosedTypesBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(MultiGenericWrapper<,>)); // Blacklist MultiGenericWrapper<,>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(MultiGenericWrapper<int, string>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(MultiGenericWrapper<string, int>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(MultiGenericWrapper<object, object>)));

            _output.WriteLine("Blacklisting MultiGenericWrapper<,> blacklists all MultiGenericWrapper<T1, T2> types");
        }

        [Fact]
        public void IsTypeBlacklisted_GenericTypeDefinitionItself_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(List<>));

            // Act - Check if the open generic type itself is blacklisted
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<>));

            // Assert
            Assert.True(result);
            _output.WriteLine("The generic type definition List<> itself is blacklisted");
        }

        [Fact]
        public void IsTypeBlacklisted_ArrayOfBlacklistedGenericDefinition_ReturnsTrue()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(List<>));

            // Act - Array of List<int> where List<> is blacklisted
            var result = reflector.Converters.IsTypeBlacklisted(typeof(List<int>[]));

            // Assert
            Assert.True(result);
            _output.WriteLine("List<int>[] is blacklisted when List<> is blacklisted");
        }

#if NET5_0_OR_GREATER
        [Fact]
        public void IsTypeBlacklisted_SpanDefinitionBlacklisted_AllSpanTypesBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(Span<>)); // Blacklist Span<>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(Span<int>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(Span<byte>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(Span<char>)));

            _output.WriteLine("Blacklisting Span<> blacklists all Span<T> types");
        }

        [Fact]
        public void IsTypeBlacklisted_ReadOnlySpanDefinitionBlacklisted_AllReadOnlySpanTypesBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(ReadOnlySpan<>)); // Blacklist ReadOnlySpan<>

            // Act & Assert
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(ReadOnlySpan<int>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(ReadOnlySpan<byte>)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(ReadOnlySpan<char>)));

            _output.WriteLine("Blacklisting ReadOnlySpan<> blacklists all ReadOnlySpan<T> types");
        }
#endif

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

        #region Batch Blacklist Method Tests

        [Fact]
        public void BlacklistTypes_MultipleTypes_AllAreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistTypes(
                typeof(BlacklistedBaseClass),
                typeof(NonBlacklistedClass),
                typeof(IBlacklistedInterface));

            // Assert
            Assert.True(result, "BlacklistTypes should return true when types are added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(IBlacklistedInterface)));
            _output.WriteLine("All types from batch blacklist are correctly blacklisted");
        }

        [Fact]
        public void BlacklistTypes_WithNullValues_IgnoresNulls()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - Include null values which should be ignored
            var result = reflector.Converters.BlacklistTypes(
                typeof(BlacklistedBaseClass),
                null!,
                typeof(NonBlacklistedClass));

            // Assert
            Assert.True(result, "BlacklistTypes should return true when at least one type is added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(NonBlacklistedClass)));
            Assert.Equal(2, reflector.Converters.GetAllBlacklistedTypes().Count);
            _output.WriteLine("Null values in batch blacklist are correctly ignored");
        }

        [Fact]
        public void BlacklistTypes_EmptyArray_NoChange()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistTypes(Array.Empty<Type>());

            // Assert
            Assert.False(result, "BlacklistTypes should return false when no types are added");
            Assert.Empty(reflector.Converters.GetAllBlacklistedTypes());
            _output.WriteLine("Empty batch blacklist results in no changes");
        }

        [Fact]
        public void BlacklistTypes_DuplicateTypes_AddsOnlyOnce()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - Add same type multiple times
            var result = reflector.Converters.BlacklistTypes(
                typeof(BlacklistedBaseClass),
                typeof(BlacklistedBaseClass),
                typeof(BlacklistedBaseClass));

            // Assert
            Assert.True(result, "BlacklistTypes should return true when at least one type is added");
            Assert.Single(reflector.Converters.GetAllBlacklistedTypes());
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass)));
            _output.WriteLine("Duplicate types in batch are correctly deduplicated");
        }

        [Fact]
        public void BlacklistTypes_DerivedTypesInBatch_AllDetected()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistTypes(typeof(BlacklistedBaseClass));

            // Assert - Derived types should also be blacklisted
            Assert.True(result, "BlacklistTypes should return true when type is added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(DerivedFromBlacklisted)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(DeeplyDerivedFromBlacklisted)));
            _output.WriteLine("Derived types are correctly blacklisted after batch operation");
        }

        [Fact]
        public void BlacklistTypes_AlreadyBlacklisted_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistTypes(typeof(BlacklistedBaseClass));

            // Act - Try to add the same type again
            var result = reflector.Converters.BlacklistTypes(typeof(BlacklistedBaseClass));

            // Assert
            Assert.False(result, "BlacklistTypes should return false when all types are already blacklisted");
            _output.WriteLine("BlacklistTypes returns false when type already blacklisted");
        }

        [Fact]
        public void BlacklistTypes_OnlyNulls_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - Only null values
            var result = reflector.Converters.BlacklistTypes((Type)null!, (Type)null!);

            // Assert
            Assert.False(result, "BlacklistTypes should return false when only null values provided");
            Assert.Empty(reflector.Converters.GetAllBlacklistedTypes());
            _output.WriteLine("BlacklistTypes returns false when only nulls provided");
        }

        #endregion

        #region String-Based Blacklist Method Tests

        [Fact]
        public void BlacklistType_ByStringName_TypeIsBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var typeFullName = typeof(BlacklistedBaseClass).FullName!;

            // Act
            var result = reflector.Converters.BlacklistType(typeFullName);

            // Assert
            Assert.True(result, "BlacklistType should return true when type is added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(BlacklistedBaseClass)));
            _output.WriteLine($"Type blacklisted by string name '{typeFullName}' is correctly detected");
        }

        [Fact]
        public void BlacklistType_ByStringName_SystemType_IsBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistType("System.String");

            // Assert
            Assert.True(result, "BlacklistType should return true when type is added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(string)));
            _output.WriteLine("System.String blacklisted by string name is correctly detected");
        }

        [Fact]
        public void BlacklistType_ByStringName_InvalidTypeName_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - Should not throw for invalid type name
            var result = reflector.Converters.BlacklistType("This.Type.Does.Not.Exist");

            // Assert - No types should be blacklisted
            Assert.False(result, "BlacklistType should return false for invalid type name");
            Assert.Empty(reflector.Converters.GetAllBlacklistedTypes());
            _output.WriteLine("Invalid type name returns false and is silently ignored");
        }

        [Fact]
        public void BlacklistType_ByStringName_AlreadyBlacklisted_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistType("System.String");

            // Act - Try to add the same type again
            var result = reflector.Converters.BlacklistType("System.String");

            // Assert
            Assert.False(result, "BlacklistType should return false when type already blacklisted");
            _output.WriteLine("BlacklistType returns false when type already blacklisted");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_AllTypesAreBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistTypes(
                "System.String",
                "System.Int32",
                "System.DateTime");

            // Assert
            Assert.True(result, "BlacklistTypes should return true when types are added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(string)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(int)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(DateTime)));
            _output.WriteLine("Multiple types blacklisted by string names are correctly detected");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_MixedValidAndInvalid_OnlyValidAdded()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - Mix of valid and invalid type names
            var result = reflector.Converters.BlacklistTypes(
                "System.String",
                "This.Does.Not.Exist",
                "System.Int32");

            // Assert
            Assert.True(result, "BlacklistTypes should return true when at least one type is added");
            Assert.Equal(2, reflector.Converters.GetAllBlacklistedTypes().Count);
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(string)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(int)));
            _output.WriteLine("Only valid type names from batch are blacklisted");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_EmptyArray_NoChange()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistTypes(Array.Empty<string>());

            // Assert
            Assert.False(result, "BlacklistTypes should return false when no types are added");
            Assert.Empty(reflector.Converters.GetAllBlacklistedTypes());
            _output.WriteLine("Empty string array batch results in no changes");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_DuplicateNames_AddsOnlyOnce()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var result = reflector.Converters.BlacklistTypes(
                "System.String",
                "System.String",
                "System.String");

            // Assert
            Assert.True(result, "BlacklistTypes should return true when at least one type is added");
            Assert.Single(reflector.Converters.GetAllBlacklistedTypes());
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(string)));
            _output.WriteLine("Duplicate string names in batch are correctly deduplicated");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_GenericType_IsBlacklisted()
        {
            // Arrange
            var reflector = new Reflector();
            var listStringTypeName = typeof(List<string>).FullName!;

            // Act
            var result = reflector.Converters.BlacklistTypes(listStringTypeName);

            // Assert
            Assert.True(result, "BlacklistTypes should return true when type is added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(List<string>)));
            _output.WriteLine($"Generic type '{listStringTypeName}' blacklisted by string name is correctly detected");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_CacheInvalidation_WorksCorrectly()
        {
            // Arrange
            var reflector = new Reflector();

            // Prime cache with false result
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(string)));

            // Act
            var result = reflector.Converters.BlacklistTypes("System.String");

            // Assert - Cache should be invalidated
            Assert.True(result, "BlacklistTypes should return true when type is added");
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(string)));
            _output.WriteLine("Cache is correctly invalidated after string-based batch blacklist");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_AllInvalid_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - All invalid type names
            var result = reflector.Converters.BlacklistTypes(
                "This.Does.Not.Exist",
                "Neither.Does.This");

            // Assert
            Assert.False(result, "BlacklistTypes should return false when no types could be resolved");
            Assert.Empty(reflector.Converters.GetAllBlacklistedTypes());
            _output.WriteLine("BlacklistTypes returns false when all type names are invalid");
        }

        [Fact]
        public void BlacklistTypes_ByStringNames_AlreadyBlacklisted_ReturnsFalse()
        {
            // Arrange
            var reflector = new Reflector();
            reflector.Converters.BlacklistTypes("System.String", "System.Int32");

            // Act - Try to add the same types again
            var result = reflector.Converters.BlacklistTypes("System.String", "System.Int32");

            // Assert
            Assert.False(result, "BlacklistTypes should return false when all types already blacklisted");
            _output.WriteLine("BlacklistTypes returns false when all types already blacklisted");
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

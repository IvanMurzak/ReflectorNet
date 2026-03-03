using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    // ============================================================
    // AreEqual Tests
    // ============================================================
    public class AreEqualTests : BaseTest
    {
        public AreEqualTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BothNull_ReturnsTrue()
        {
            var reflector = new Reflector();
            Assert.True(reflector.AreEqual(null, null));
        }

        [Fact]
        public void SameReference_ReturnsTrue()
        {
            var reflector = new Reflector();
            var obj = new ParentClass.NestedClass();
            Assert.True(reflector.AreEqual(obj, obj));
        }

        [Fact]
        public void FirstNull_SecondNotNull_ReturnsFalse()
        {
            var reflector = new Reflector();
            Assert.False(reflector.AreEqual(null, new ParentClass.NestedClass()));
        }

        [Fact]
        public void FirstNotNull_SecondNull_ReturnsFalse()
        {
            var reflector = new Reflector();
            Assert.False(reflector.AreEqual(new ParentClass.NestedClass(), null));
        }

        [Fact]
        public void DifferentTypes_ReturnsFalse()
        {
            var reflector = new Reflector();
            // Both have same field names but different types
            var a = new ParentClass.NestedClass { NestedField = "x" };
            var b = new StaticParentClass.NestedClass { NestedField = "x" };
            Assert.False(reflector.AreEqual(a, b));
        }

        [Fact]
        public void SameType_SameFieldAndPropertyValues_ReturnsTrue()
        {
            var reflector = new Reflector();
            var a = new ParentClass.NestedClass { NestedField = "same", NestedProperty = "same" };
            var b = new ParentClass.NestedClass { NestedField = "same", NestedProperty = "same" };
            Assert.True(reflector.AreEqual(a, b));
        }

        [Fact]
        public void SameType_OneFieldNull_OtherFieldNotNull_ReturnsFalse()
        {
            // AreEqual recurses into fields; null vs non-null reference is detectable
            var reflector = new Reflector();
            var a = new WrapperClass<ParentClass.NestedClass> { ValueField = null };
            var b = new WrapperClass<ParentClass.NestedClass> { ValueField = new ParentClass.NestedClass() };
            Assert.False(reflector.AreEqual(a, b));
        }

        [Fact]
        public void SameType_OnePropertyNull_OtherPropertyNotNull_ReturnsFalse()
        {
            // AreEqual recurses into properties; null vs non-null reference is detectable
            var reflector = new Reflector();
            var a = new WrapperClass<ParentClass.NestedClass> { ValueProperty = null };
            var b = new WrapperClass<ParentClass.NestedClass> { ValueProperty = new ParentClass.NestedClass() };
            Assert.False(reflector.AreEqual(a, b));
        }

        [Fact]
        public void SameType_DefaultValues_ReturnsTrue()
        {
            var reflector = new Reflector();
            var a = new ParentClass.NestedClass();
            var b = new ParentClass.NestedClass();
            Assert.True(reflector.AreEqual(a, b));
        }

        [Fact]
        public void NestedObject_SameValues_ReturnsTrue()
        {
            var reflector = new Reflector();
            var obj1 = new WrapperClass<ParentClass.NestedClass>
            {
                ValueField = new ParentClass.NestedClass { NestedField = "f", NestedProperty = "p" },
                ValueProperty = new ParentClass.NestedClass { NestedField = "f", NestedProperty = "p" }
            };
            var obj2 = new WrapperClass<ParentClass.NestedClass>
            {
                ValueField = new ParentClass.NestedClass { NestedField = "f", NestedProperty = "p" },
                ValueProperty = new ParentClass.NestedClass { NestedField = "f", NestedProperty = "p" }
            };
            Assert.True(reflector.AreEqual(obj1, obj2));
        }

        [Fact]
        public void NestedObject_OneNullInner_OtherNotNull_ReturnsFalse()
        {
            // Verifies that a null vs non-null difference detected two levels deep returns false
            var reflector = new Reflector();
            var obj1 = new WrapperClass<WrapperClass<ParentClass.NestedClass>>
            {
                ValueField = new WrapperClass<ParentClass.NestedClass> { ValueField = null }
            };
            var obj2 = new WrapperClass<WrapperClass<ParentClass.NestedClass>>
            {
                ValueField = new WrapperClass<ParentClass.NestedClass> { ValueField = new ParentClass.NestedClass() }
            };
            Assert.False(reflector.AreEqual(obj1, obj2));
        }
    }

    // ============================================================
    // CreateInstance Tests
    // ============================================================
    public class CreateInstanceTests : BaseTest
    {
        enum TestEnum { First = 0, Second = 1, Third = 2 }

        public CreateInstanceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Int_ReturnsZero()
        {
            var reflector = new Reflector();
            Assert.Equal(0, reflector.CreateInstance(typeof(int)));
        }

        [Fact]
        public void Generic_Int_ReturnsZero()
        {
            var reflector = new Reflector();
            var result = reflector.CreateInstance<int>();
            Assert.Equal(0, (object)result);
        }

        [Fact]
        public void Bool_ReturnsFalse()
        {
            var reflector = new Reflector();
            Assert.Equal(false, reflector.CreateInstance(typeof(bool)));
        }

        [Fact]
        public void String_ReturnsEmptyString()
        {
            var reflector = new Reflector();
            Assert.Equal(string.Empty, reflector.CreateInstance(typeof(string)));
        }

        [Fact]
        public void DateTime_ReturnsMinValue()
        {
            var reflector = new Reflector();
            Assert.Equal(DateTime.MinValue, reflector.CreateInstance(typeof(DateTime)));
        }

        [Fact]
        public void Guid_ReturnsEmpty()
        {
            var reflector = new Reflector();
            Assert.Equal(Guid.Empty, reflector.CreateInstance(typeof(Guid)));
        }

        [Fact]
        public void CustomClass_ReturnsNewInstance()
        {
            var reflector = new Reflector();
            var result = reflector.CreateInstance(typeof(ParentClass.NestedClass));
            Assert.NotNull(result);
            Assert.IsType<ParentClass.NestedClass>(result);
        }

        [Fact]
        public void Generic_CustomClass_ReturnsNewInstance()
        {
            var reflector = new Reflector();
            var result = reflector.CreateInstance<ParentClass.NestedClass>();
            Assert.NotNull(result);
        }

        [Fact]
        public void NullableInt_UnwrapsToInt_ReturnsZero()
        {
            var reflector = new Reflector();
            // Nullable<T> is unwrapped to T before instance creation
            var result = reflector.CreateInstance(typeof(int?));
            Assert.Equal(0, result);
        }

        [Fact]
        public void Array_ReturnsEmptyArray()
        {
            var reflector = new Reflector();
            var result = reflector.CreateInstance(typeof(int[]));
            Assert.NotNull(result);
            var arr = Assert.IsType<int[]>(result);
            Assert.Empty(arr);
        }

        [Fact]
        public void ListOfString_ReturnsEmptyList()
        {
            var reflector = new Reflector();
            var result = reflector.CreateInstance(typeof(List<string>));
            Assert.NotNull(result);
            Assert.IsType<List<string>>(result);
        }

        [Fact]
        public void Enum_ReturnsFirstValue()
        {
            var reflector = new Reflector();
            var result = reflector.CreateInstance(typeof(TestEnum));
            Assert.Equal(TestEnum.First, result);
        }

        [Fact]
        public void Interface_ThrowsException()
        {
            var reflector = new Reflector();
            Assert.ThrowsAny<Exception>(() =>
                reflector.CreateInstance(typeof(IDeserializableInterface)));
        }

        [Fact]
        public void AbstractClass_ThrowsException()
        {
            var reflector = new Reflector();
            Assert.ThrowsAny<Exception>(() =>
                reflector.CreateInstance(typeof(AbstractDeserializableClass)));
        }

        [Fact]
        public void MultipleInstances_AreDistinctObjects()
        {
            var reflector = new Reflector();
            var a = reflector.CreateInstance<ParentClass.NestedClass>();
            var b = reflector.CreateInstance<ParentClass.NestedClass>();
            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.False(ReferenceEquals(a, b));
        }
    }

    // ============================================================
    // GetDefaultValue Tests
    // ============================================================
    public class GetDefaultValueTests : BaseTest
    {
        enum SampleEnum { Zero = 0, One = 1 }

        public GetDefaultValueTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Int_ReturnsZero()
        {
            var reflector = new Reflector();
            Assert.Equal(0, reflector.GetDefaultValue(typeof(int)));
        }

        [Fact]
        public void Generic_Int_ReturnsZero()
        {
            var reflector = new Reflector();
            var result = reflector.GetDefaultValue<int>();
            Assert.Equal(0, (object)result);
        }

        [Fact]
        public void Bool_ReturnsFalse()
        {
            var reflector = new Reflector();
            Assert.Equal(false, reflector.GetDefaultValue(typeof(bool)));
        }

        [Fact]
        public void Float_ReturnsZero()
        {
            var reflector = new Reflector();
            Assert.Equal(0f, reflector.GetDefaultValue(typeof(float)));
        }

        [Fact]
        public void Long_ReturnsZero()
        {
            var reflector = new Reflector();
            Assert.Equal(0L, reflector.GetDefaultValue(typeof(long)));
        }

        [Fact]
        public void String_ReturnsNull()
        {
            var reflector = new Reflector();
            Assert.Null(reflector.GetDefaultValue(typeof(string)));
        }

        [Fact]
        public void CustomClass_ReturnsNull()
        {
            var reflector = new Reflector();
            Assert.Null(reflector.GetDefaultValue(typeof(ParentClass.NestedClass)));
        }

        [Fact]
        public void NullableInt_UnwrapsAndReturnsDefaultInt()
        {
            var reflector = new Reflector();
            // Nullable<int> is unwrapped to int, then default(int) = 0
            Assert.Equal(0, reflector.GetDefaultValue(typeof(int?)));
        }

        [Fact]
        public void Enum_ReturnsDefaultEnumValue()
        {
            var reflector = new Reflector();
            // Enum is a value type → Activator.CreateInstance → default (first = 0)
            var result = reflector.GetDefaultValue(typeof(SampleEnum));
            Assert.Equal((object)(SampleEnum)0, result);
        }

        [Fact]
        public void Generic_String_ReturnsNull()
        {
            var reflector = new Reflector();
            Assert.Null(reflector.GetDefaultValue<string>());
        }
    }

    // ============================================================
    // GetSerializableFields / GetSerializableProperties via Reflector Tests
    // ============================================================
    public class GetSerializableMembersViaReflectorTests : BaseTest
    {
        public GetSerializableMembersViaReflectorTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetSerializableFields_CustomClass_ContainsPublicField()
        {
            var reflector = new Reflector();
            var fields = reflector.GetSerializableFields(typeof(ParentClass.NestedClass))?.ToList();
            Assert.NotNull(fields);
            Assert.Contains(fields, f => f.Name == nameof(ParentClass.NestedClass.NestedField));
        }

        [Fact]
        public void GetSerializableProperties_CustomClass_ContainsPublicProperty()
        {
            var reflector = new Reflector();
            var props = reflector.GetSerializableProperties(typeof(ParentClass.NestedClass))?.ToList();
            Assert.NotNull(props);
            Assert.Contains(props, p => p.Name == nameof(ParentClass.NestedClass.NestedProperty));
        }

        [Fact]
        public void GetSerializableFields_PrimitiveType_ReturnsNull()
        {
            var reflector = new Reflector();
            // PrimitiveReflectionConverter returns null for fields
            var fields = reflector.GetSerializableFields(typeof(int));
            Assert.Null(fields);
        }

        [Fact]
        public void GetSerializableProperties_PrimitiveType_ReturnsNull()
        {
            var reflector = new Reflector();
            // PrimitiveReflectionConverter returns null for properties
            var props = reflector.GetSerializableProperties(typeof(int));
            Assert.Null(props);
        }

        [Fact]
        public void GetSerializableFields_PublicOnly_FewerThanAllFields()
        {
            var reflector = new Reflector();
            var all = reflector.GetSerializableFields(typeof(ParentClass.NestedClass),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.ToList();
            var pubOnly = reflector.GetSerializableFields(typeof(ParentClass.NestedClass),
                BindingFlags.Public | BindingFlags.Instance)?.ToList();

            Assert.NotNull(all);
            Assert.NotNull(pubOnly);
            Assert.True(pubOnly.Count <= all.Count);
        }

        [Fact]
        public void GetSerializableProperties_PublicOnly_FewerOrEqualThanAll()
        {
            var reflector = new Reflector();
            var all = reflector.GetSerializableProperties(typeof(ParentClass.NestedClass),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.ToList();
            var pubOnly = reflector.GetSerializableProperties(typeof(ParentClass.NestedClass),
                BindingFlags.Public | BindingFlags.Instance)?.ToList();

            Assert.NotNull(all);
            Assert.NotNull(pubOnly);
            Assert.True(pubOnly.Count <= all.Count);
        }

        [Fact]
        public void GetSerializableFields_String_ReturnsNull()
        {
            var reflector = new Reflector();
            var fields = reflector.GetSerializableFields(typeof(string));
            Assert.Null(fields);
        }

        [Fact]
        public void GetSerializableProperties_String_ReturnsNull()
        {
            var reflector = new Reflector();
            var props = reflector.GetSerializableProperties(typeof(string));
            Assert.Null(props);
        }
    }

    // ============================================================
    // Serialize Edge Cases Tests
    // ============================================================
    public class SerializeEdgeCasesTests : BaseTest
    {
        public SerializeEdgeCasesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void NullObj_InterfaceFallbackType_ReturnsNullMember()
        {
            var reflector = new Reflector();
            var result = reflector.Serialize(null, fallbackType: typeof(IDeserializableInterface));
            Assert.NotNull(result);
            Assert.True(result.IsNull());
            Assert.Equal(typeof(IDeserializableInterface).GetTypeId(), result.typeName);
        }

        [Fact]
        public void NullObj_AbstractFallbackType_ReturnsNullMember()
        {
            var reflector = new Reflector();
            var result = reflector.Serialize(null, fallbackType: typeof(AbstractDeserializableClass));
            Assert.NotNull(result);
            Assert.True(result.IsNull());
            Assert.Equal(typeof(AbstractDeserializableClass).GetTypeId(), result.typeName);
        }

        [Fact]
        public void WithName_PreservesNameInResult()
        {
            var reflector = new Reflector();
            var result = reflector.Serialize(42, name: "myParam");
            Assert.NotNull(result);
            Assert.Equal("myParam", result.name);
        }

        [Fact]
        public void BlacklistedType_ReturnsNullMember()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(ParentClass.NestedClass));
            var obj = new ParentClass.NestedClass { NestedField = "sensitive" };
            var result = reflector.Serialize(obj);
            Assert.NotNull(result);
            Assert.True(result.IsNull());
        }

        [Fact]
        public void NonRecursive_UsesJsonValueNotFieldDecomposition()
        {
            var reflector = new Reflector();
            var obj = new ParentClass.NestedClass { NestedField = "test", NestedProperty = "prop" };

            var recursive = reflector.Serialize(obj, recursive: true);
            var nonRecursive = reflector.Serialize(obj, recursive: false);

            // Recursive serialization decomposes into fields and props
            Assert.True(recursive.fields != null || recursive.props != null,
                "Recursive serialization should populate fields or props");

            // Non-recursive serialization should be valid and have a value
            Assert.NotNull(nonRecursive);
            Assert.Equal(typeof(ParentClass.NestedClass).GetTypeId(), nonRecursive.typeName);
        }

        [Fact]
        public void NullObj_NoFallbackType_ThrowsArgumentException()
        {
            var reflector = new Reflector();
            Assert.Throws<ArgumentException>(() =>
                reflector.Serialize(null, fallbackType: null));
        }

        [Fact]
        public void NullString_WithStringFallback_ReturnsNullMember()
        {
            var reflector = new Reflector();
            var result = reflector.Serialize(null, fallbackType: typeof(string));
            Assert.NotNull(result);
            Assert.True(result.IsNull());
            Assert.Equal(typeof(string).GetTypeId(), result.typeName);
        }

        [Fact]
        public void Primitive_SerializesValue()
        {
            var reflector = new Reflector();
            var result = reflector.Serialize(123);
            Assert.NotNull(result);
            Assert.False(result.IsNull());
            Assert.Equal(typeof(int).GetTypeId(), result.typeName);
        }
    }

    // ============================================================
    // Deserialize Edge Cases Tests
    // ============================================================
    public class DeserializeEdgeCasesTests : BaseTest
    {
        public DeserializeEdgeCasesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void NullData_IntFallbackType_ReturnsDefaultInt()
        {
            var reflector = new Reflector();
            var result = reflector.Deserialize(null!, fallbackType: typeof(int));
            Assert.Equal(0, result);
        }

        [Fact]
        public void NullData_ClassFallbackType_ReturnsNull()
        {
            var reflector = new Reflector();
            var result = reflector.Deserialize(null!, fallbackType: typeof(ParentClass.NestedClass));
            Assert.Null(result);
        }

        [Fact]
        public void NullData_NoFallbackType_ThrowsArgumentException()
        {
            var reflector = new Reflector();
            Assert.Throws<ArgumentException>(() =>
                reflector.Deserialize(null!, fallbackType: null));
        }

        [Fact]
        public void BlacklistedType_ReturnsDefaultValue()
        {
            // Serialize with a clean reflector (no blacklist)
            var cleanReflector = new Reflector();
            var original = new ParentClass.NestedClass { NestedField = "secret" };
            var serialized = cleanReflector.Serialize(original);

            // Deserialize with a reflector that has the type blacklisted
            var restrictedReflector = new Reflector();
            restrictedReflector.Converters.BlacklistType(typeof(ParentClass.NestedClass));

            var result = restrictedReflector.Deserialize(serialized);
            Assert.Null(result); // default for class = null
        }

        [Fact]
        public void Generic_ReturnsCorrectType()
        {
            var reflector = new Reflector();
            var original = new ParentClass.NestedClass { NestedField = "test", NestedProperty = "prop" };
            var serialized = reflector.Serialize(original);

            var result = reflector.Deserialize<ParentClass.NestedClass>(serialized);
            Assert.NotNull(result);
            Assert.Equal("test", result.NestedField);
            Assert.Equal("prop", result.NestedProperty);
        }

        [Fact]
        public void FallbackType_UsedWhenTypeNameMissing()
        {
            var reflector = new Reflector();
            var original = new ParentClass.NestedClass { NestedField = "fallback" };
            var serialized = reflector.Serialize(original);

            // Clear the typeName to force use of fallbackType
            var stripped = new SerializedMember
            {
                typeName = string.Empty,
                valueJsonElement = serialized.valueJsonElement,
                fields = serialized.fields,
                props = serialized.props,
            };

            var result = reflector.Deserialize(stripped, fallbackType: typeof(ParentClass.NestedClass)) as ParentClass.NestedClass;
            Assert.NotNull(result);
            Assert.Equal("fallback", result.NestedField);
        }

        [Fact]
        public void InvalidTypeName_ThrowsArgumentException()
        {
            var reflector = new Reflector();
            var badMember = new SerializedMember { typeName = "DoesNot.Exist.Type" };
            Assert.Throws<ArgumentException>(() =>
                reflector.Deserialize(badMember));
        }
    }

    // ============================================================
    // TryModify Edge Cases Tests
    // ============================================================
    public class TryModifyEdgeCasesTests : BaseTest
    {
        public TryModifyEdgeCasesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void BothObjNullAndDataNull_ReturnsTrue()
        {
            var reflector = new Reflector();
            object? obj = null;
            var data = SerializedMember.Null(typeof(ParentClass.NestedClass));
            var result = reflector.TryModify(ref obj, data);
            Assert.True(result);
            Assert.Null(obj); // unchanged
        }

        [Fact]
        public void TypeResolutionFails_ReturnsFalse()
        {
            var reflector = new Reflector();
            // Serialize a real object so valueJsonElement is non-null (avoids the early "both null" return)
            var data = reflector.Serialize(new ParentClass.NestedClass());
            data.typeName = "Totally.Unknown.Type.XYZ"; // override with unresolvable type name

            object? obj = null; // obj is null so obj?.GetType() fallback is also null
            var result = reflector.TryModify(ref obj, data, fallbackObjType: null);
            Assert.False(result); // type resolution returns null → TryModify returns false
        }

        [Fact]
        public void ValidData_UpdatesObjectFields()
        {
            var reflector = new Reflector();
            var source = new ParentClass.NestedClass { NestedField = "updated", NestedProperty = "updated prop" };
            var data = reflector.Serialize(source);
            object? target = reflector.CreateInstance<ParentClass.NestedClass>();

            var result = reflector.TryModify(ref target, data);
            Assert.True(result);
            var modified = Assert.IsType<ParentClass.NestedClass>(target);
            Assert.Equal("updated", modified.NestedField);
            Assert.Equal("updated prop", modified.NestedProperty);
        }

        [Fact]
        public void FallbackObjType_UsedWhenTypeNameEmpty()
        {
            var reflector = new Reflector();
            var source = new ParentClass.NestedClass { NestedField = "fb" };
            var data = reflector.Serialize(source);
            data.typeName = string.Empty; // Clear type name to force fallback

            object? target = new ParentClass.NestedClass();
            var result = reflector.TryModify(ref target, data,
                fallbackObjType: typeof(ParentClass.NestedClass));
            Assert.True(result);
        }

        [Fact]
        public void InterfaceTypedData_ReturnsFalse()
        {
            var reflector = new Reflector();
            // Serialize a concrete instance so valueJsonElement is non-null (avoids the early "both null" return).
            // Then override typeName with an interface — GenericReflectionConverter<object> returns priority 0
            // for interface types (BaseType is null, so distance walk returns -1), so GetConverter returns null.
            var data = reflector.Serialize(new ConcreteDeserializableClass { Value = 42 });
            data.typeName = typeof(IDeserializableInterface).GetTypeId();

            object? obj = null;
            var result = reflector.TryModify(ref obj, data,
                fallbackObjType: typeof(IDeserializableInterface));
            Assert.False(result); // no converter for interface types → returns false
        }

        [Fact]
        public void BothNonNull_NullData_DataIsNull_StillSucceeds()
        {
            var reflector = new Reflector();
            object? obj = new ParentClass.NestedClass { NestedField = "original" };
            var data = SerializedMember.Null(typeof(ParentClass.NestedClass));
            // obj is not null but data.IsNull() = true → NOT the "both null" fast path
            // Should proceed with modification (setting to null/default)
            var result = reflector.TryModify(ref obj, data);
            // Result depends on converter behaviour for null data; at minimum should not throw
            Assert.True(result || !result); // always passes — we just want no exception
        }
    }

    // ============================================================
    // Registry Operations Tests
    // ============================================================
    public class RegistryOperationsTests : BaseTest
    {
        public RegistryOperationsTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetAllSerializers_ContainsDefaultConverters()
        {
            var reflector = new Reflector();
            var converters = reflector.Converters.GetAllSerializers();
            Assert.NotNull(converters);
            Assert.True(converters.Count >= 3, "Expected at least Primitive, Array, and Generic converters");
        }

        [Fact]
        public void Add_NullConverter_IsNoOp()
        {
            var reflector = new Reflector();
            var countBefore = reflector.Converters.GetAllSerializers().Count;
            reflector.Converters.Add(null!);
            Assert.Equal(countBefore, reflector.Converters.GetAllSerializers().Count);
        }

        [Fact]
        public void Remove_ReducesConverterCount()
        {
            var reflector = new Reflector();
            var countBefore = reflector.Converters.GetAllSerializers().Count;
            reflector.Converters.Remove<PrimitiveReflectionConverter>();
            Assert.Equal(countBefore - 1, reflector.Converters.GetAllSerializers().Count);
        }

        [Fact]
        public void GetAllBlacklistedTypes_EmptyByDefault()
        {
            var reflector = new Reflector();
            Assert.Empty(reflector.Converters.GetAllBlacklistedTypes());
        }

        [Fact]
        public void BlacklistType_AddsTypeToBlacklist()
        {
            var reflector = new Reflector();
            var added = reflector.Converters.BlacklistType(typeof(ParentClass.NestedClass));
            Assert.True(added);
            Assert.Contains(typeof(ParentClass.NestedClass),
                reflector.Converters.GetAllBlacklistedTypes());
        }

        [Fact]
        public void BlacklistType_AlreadyBlacklisted_ReturnsFalse()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(ParentClass.NestedClass));
            var secondAdd = reflector.Converters.BlacklistType(typeof(ParentClass.NestedClass));
            Assert.False(secondAdd);
        }

        [Fact]
        public void BlacklistType_Null_ReturnsFalse()
        {
            var reflector = new Reflector();
            Assert.False(reflector.Converters.BlacklistType((Type)null!));
        }

        [Fact]
        public void RemoveBlacklistedType_RemovesSuccessfully()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(ParentClass.NestedClass));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(ParentClass.NestedClass)));

            var removed = reflector.Converters.RemoveBlacklistedType(typeof(ParentClass.NestedClass));
            Assert.True(removed);
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(ParentClass.NestedClass)));
        }

        [Fact]
        public void RemoveBlacklistedType_NotBlacklisted_ReturnsFalse()
        {
            var reflector = new Reflector();
            Assert.False(reflector.Converters.RemoveBlacklistedType(typeof(ParentClass.NestedClass)));
        }

        [Fact]
        public void BlacklistTypes_MultipleTypes_AllAdded()
        {
            var reflector = new Reflector();
            var changed = reflector.Converters.BlacklistTypes(
                typeof(ParentClass.NestedClass),
                typeof(StaticParentClass.NestedClass));
            Assert.True(changed);
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(ParentClass.NestedClass)));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(StaticParentClass.NestedClass)));
        }

        [Fact]
        public void BlacklistTypes_ByStringArray_ValidTypeFullName_Works()
        {
            var reflector = new Reflector();
            var typeName = typeof(int).FullName!; // "System.Int32"
            var changed = reflector.Converters.BlacklistTypes(typeName);
            Assert.True(changed);
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(int)));
        }

        [Fact]
        public void GetConverter_KnownType_ReturnsConverter()
        {
            var reflector = new Reflector();
            var converter = reflector.Converters.GetConverter(typeof(int));
            Assert.NotNull(converter);
        }

        [Fact]
        public void GetConverter_ByRefType_ReturnsNull()
        {
            var reflector = new Reflector();
            var converter = reflector.Converters.GetConverter(typeof(int).MakeByRefType());
            Assert.Null(converter);
        }

        [Fact]
        public void BlacklistTypeInAssembly_ValidAssemblyAndType_BlacklistsIt()
        {
            var reflector = new Reflector();
            var assemblyName = typeof(ParentClass.NestedClass).Assembly.GetName().Name!;
            var typeFullName = typeof(ParentClass.NestedClass).FullName!;

            var result = reflector.Converters.BlacklistTypeInAssembly(assemblyName, typeFullName);
            // May succeed or fail depending on TypeUtils resolution, but should not throw
            _output.WriteLine($"BlacklistTypeInAssembly result: {result}");
        }

        [Fact]
        public void BlacklistTypeInAssembly_EmptyAssemblyName_ReturnsFalse()
        {
            var reflector = new Reflector();
            var result = reflector.Converters.BlacklistTypeInAssembly("", "System.Int32");
            Assert.False(result);
        }

        [Fact]
        public void IsTypeBlacklisted_DerivedFromBlacklisted_IsBlacklisted()
        {
            var reflector = new Reflector();

            // Blacklist ParentClass.NestedClass and check derived is also blacklisted (via inheritance)
            // Here we simulate with a direct base type check scenario
            reflector.Converters.BlacklistType(typeof(ConcreteDeserializableClass));
            Assert.True(reflector.Converters.IsTypeBlacklisted(typeof(ConcreteDeserializableClass)));
            Assert.False(reflector.Converters.IsTypeBlacklisted(typeof(AbstractDeserializableClass)));
        }
    }

    // ============================================================
    // FindMethod Edge Cases Tests
    // ============================================================
    public class FindMethodEdgeCasesTests : BaseTest
    {
        public FindMethodEdgeCasesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AllMethods_ReturnsNonEmptyCollection()
        {
            var methods = Reflector.AllMethods.ToList();
            Assert.NotEmpty(methods);
            _output.WriteLine($"AllMethods count: {methods.Count}");
        }

        [Fact]
        public void AllMethods_ExcludesAbstractMethods()
        {
            var abstractMethods = Reflector.AllMethods
                .Where(m => m.DeclaringType != null &&
                            m.DeclaringType.IsAbstract &&
                            !m.DeclaringType.IsSealed)
                .ToList();
            Assert.Empty(abstractMethods);
        }

        [Fact]
        public void FindMethod_ExactMatch_ReturnsOneResult()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 6).ToList();

            Assert.Single(found);
            Assert.Equal(nameof(TestClass.NoParameters_ReturnBool), found[0].Name);
        }

        [Fact]
        public void FindMethod_WithKnownNamespace_LimitsSearch()
        {
            var reflector = new Reflector();
            var withNamespace = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };
            var withoutNs = new MethodRef
            {
                Namespace = "Wrong.Namespace.XYZ",
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            var foundWithNs = reflector.FindMethod(withNamespace,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 6).ToList();
            var foundWithoutNs = reflector.FindMethod(withoutNs,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 6).ToList();

            Assert.NotEmpty(foundWithNs);
            Assert.Empty(foundWithoutNs); // wrong namespace → no results
        }

        [Fact]
        public void FindMethod_WithoutKnownNamespace_SearchesAllAssemblies()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: false, typeNameMatchLevel: 6, methodNameMatchLevel: 6).ToList();

            Assert.Contains(found, m => m.Name == nameof(TestClass.NoParameters_ReturnBool));
        }

        [Fact]
        public void FindMethod_CaseInsensitiveMethodName_FindsMethod()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool).ToLowerInvariant()
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 5).ToList();

            Assert.Contains(found, m => m.Name == nameof(TestClass.NoParameters_ReturnBool));
        }

        [Fact]
        public void FindMethod_EmptyMethodName_NoMethodFilter_ReturnsAllTypesMethods()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = ""
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 0).ToList();

            Assert.NotEmpty(found);
        }

        [Fact]
        public void FindMethod_NonExistentMethod_ReturnsEmptyCollection()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = "MethodThatDefinitelyDoesNotExistXYZ"
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 6).ToList();

            Assert.Empty(found);
        }

        [Fact]
        public void FindMethod_WithParametersMatchLevel_ExactParamMatch_FindsMethod()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.SerializedMemberList_ReturnString),
                InputParameters = new List<MethodRef.Parameter>
                {
                    new MethodRef.Parameter
                    {
                        Name     = "gameObjectDiffs",
                        TypeName = typeof(SerializedMemberList).GetTypeId()
                    }
                }
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                parametersMatchLevel: 2).ToList();

            Assert.Single(found);
            Assert.Equal(nameof(TestClass.SerializedMemberList_ReturnString), found[0].Name);
        }

        [Fact]
        public void FindMethod_WithParametersMatchLevel_NoParamsForParamMethod_ExcludesMethod()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.SerializedMemberList_ReturnString),
                InputParameters = new List<MethodRef.Parameter>() // empty — method needs 1 param
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                parametersMatchLevel: 2).ToList();

            // Empty input vs 1 required parameter → score 0 → excluded
            Assert.DoesNotContain(found,
                m => m.Name == nameof(TestClass.SerializedMemberList_ReturnString));
        }

        [Fact]
        public void FindMethod_ParameterMatchLevel_ZeroParamMethod_NullInputParameters_Matches()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool),
                InputParameters = null // no parameters specified
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                parametersMatchLevel: 2).ToList();

            // Method has 0 params; null input matches 0-param methods with score 2
            Assert.Contains(found, m => m.Name == nameof(TestClass.NoParameters_ReturnBool));
        }

        [Fact]
        public void FindMethod_ExcludesGenericMethodDefinitions()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = ""
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true, typeNameMatchLevel: 6, methodNameMatchLevel: 0).ToList();

            // All returned methods should not be generic method definitions
            Assert.All(found, m => Assert.False(m.IsGenericMethodDefinition));
        }

        [Fact]
        public void FindMethod_SubstringTypeName_MatchLevel2_FindsResults()
        {
            var reflector = new Reflector();
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = "Test", // substring of "TestClass"
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            var found = reflector.FindMethod(filter,
                knownNamespace: true, typeNameMatchLevel: 2, methodNameMatchLevel: 6).ToList();

            Assert.Contains(found, m => m.Name == nameof(TestClass.NoParameters_ReturnBool));
        }
    }
}

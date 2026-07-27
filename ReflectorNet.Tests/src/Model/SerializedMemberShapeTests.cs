/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Linq;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    /// <summary>
    /// The classifier is the crux of the converter tri-state: it is the single place that decides
    /// whether a payload is "not my shape" (<c>NotApplicable</c>, quiet) or "my shape, but broken"
    /// (<c>Failed</c>, loud). Getting the boundary wrong in either direction reintroduces one of the
    /// two historic defects, so the boundary is pinned here directly.
    ///
    /// <para>
    /// The discriminator is a STRUCTURAL key (<c>value</c> / <c>fields</c> / <c>props</c>), not any
    /// recognised key. <c>name</c> and <c>typeName</c> are ordinary English words that foreign
    /// payloads use as freely as ReflectorNet does - Unity-MCP's own object-reference converters
    /// emit both - so they carry no evidence about intent. Requiring only "≥1 recognised key" is the
    /// 5.3.3 defect: it rejected legitimate consumer references such as
    /// <c>{"instanceID":"7","typeName":"UnityEngine.Rigidbody"}</c>.
    /// </para>
    /// </summary>
    public class SerializedMemberShapeTests : BaseTest
    {
        public SerializedMemberShapeTests(ITestOutputHelper output) : base(output) { }

        static SerializedMemberShape Classify(string json)
            => SerializedMemberShape.Classify(JsonDocument.Parse(json).RootElement);

        [Theory]
        // A consumer object reference: keys, but not one of them is ours -> decline quietly.
        [InlineData("{\"instanceID\":\"12345\"}")]
        [InlineData("{\"assetPath\":\"Assets/A.mat\",\"assetGuid\":\"abc\"}")]
        // Raw JSON of a plain struct - also simply not a SerializedMember.
        [InlineData("{\"X\":1,\"Y\":2}")]
        public void ObjectWithNoRecognisedKey_IsForeign(string json)
        {
            var shape = Classify(json);

            _output.WriteLine($"{json} -> {shape.Kind} (unknown: {string.Join(",", shape.UnknownKeys)})");

            Assert.Equal(SerializedMemberShapeKind.Foreign, shape.Kind);
            Assert.True(shape.IsForeign);
            Assert.Equal(0, shape.RecognisedKeyCount);
            Assert.Equal(0, shape.StructuralKeyCount);
            Assert.NotEmpty(shape.UnknownKeys);
        }

        [Theory]
        // ⚠ THE REAL UNITY WIRE SHAPES. 'name' and 'typeName' are ordinary English words: they are
        // SerializedMember keys, but they are also emitted by consumer object-reference converters
        // (Unity-MCP's GameObjectRefConverter writes "name", ComponentRefConverter writes "typeName")
        // and by countless other foreign payloads. Carrying one of them is NOT evidence that the
        // payload was trying to be a SerializedMember, so these must decline QUIETLY and let the
        // consumer's converter resolve the reference.
        //
        // Treating them as evidence is the shipped 5.3.3 defect: a live Unity Editor probe through
        // Tool_GameObject.ModifyComponent showed {"instanceID":"7"} resolving fine while
        // {"instanceID":"7","typeName":"UnityEngine.Rigidbody"} threw
        // "Unexpected property name: 'instanceID'" before Unity could resolve the ref.
        [InlineData("{\"instanceID\":\"12345\",\"typeName\":\"UnityEngine.Rigidbody\"}")]     // ComponentRef
        [InlineData("{\"instanceID\":12345,\"name\":\"Player\"}")]                            // GameObjectRef
        [InlineData("{\"instanceID\":\"12345\",\"index\":0,\"typeName\":\"UnityEngine.Transform\"}")]
        [InlineData("{\"instanceID\":\"12345\",\"name\":\"Player\",\"path\":\"/Player\"}")]
        [InlineData("{\"name\":\"a\",\"typeName\":\"System.Int32\",\"X\":1}")]
        public void ObjectWithDescriptiveKeysOnly_PlusUnknownKeys_IsForeign(string json)
        {
            var shape = Classify(json);

            _output.WriteLine($"{json} -> {shape.Kind} (unknown: {string.Join(",", shape.UnknownKeys)})");

            Assert.Equal(SerializedMemberShapeKind.Foreign, shape.Kind);
            Assert.True(shape.IsForeign);
            Assert.Equal(0, shape.StructuralKeyCount);
            Assert.True(shape.RecognisedKeyCount > 0, "'name'/'typeName' ARE recognised - they are just not structural.");
            Assert.NotEmpty(shape.UnknownKeys);
        }

        [Theory]
        // A STRUCTURAL key ('value'/'fields'/'props') is unique to the SerializedMember shape, so
        // carrying one AND an unknown key means the payload was trying to be a SerializedMember and
        // got it wrong -> loud failure, not a decline.
        [InlineData("{\"typeName\":\"T\",\"value\":42,\"bogus\":1}")]
        [InlineData("{\"value\":1,\"unknown\":2}")]
        [InlineData("{\"fields\":[],\"nope\":true}")]
        [InlineData("{\"value\":{},\"bogus\":true}")]
        [InlineData("{\"name\":\"a\",\"props\":[],\"instanceID\":\"12345\"}")]
        public void ObjectMixingStructuralAndUnknownKeys_IsMalformed(string json)
        {
            var shape = Classify(json);

            _output.WriteLine($"{json} -> {shape.Kind} (unknown: {string.Join(",", shape.UnknownKeys)})");

            Assert.Equal(SerializedMemberShapeKind.Malformed, shape.Kind);
            Assert.False(shape.IsForeign);
            Assert.True(shape.StructuralKeyCount > 0);
            Assert.True(shape.RecognisedKeyCount > 0);
            Assert.NotEmpty(shape.UnknownKeys);
        }

        [Theory]
        // `{}` is the exact shape ReflectorNet emits for a cascade member whose data travels in
        // 'fields'/'props'. Classifying it as foreign would break the library's own round trip.
        [InlineData("{}")]
        [InlineData("{\"typeName\":\"System.Int32\"}")]
        [InlineData("{\"name\":\"a\",\"typeName\":\"System.Int32\",\"value\":{},\"fields\":[],\"props\":[]}")]
        public void ObjectWithOnlyRecognisedKeys_IsASerializedMember(string json)
        {
            var shape = Classify(json);

            _output.WriteLine($"{json} -> {shape.Kind}");

            Assert.Equal(SerializedMemberShapeKind.SerializedMember, shape.Kind);
            Assert.False(shape.IsForeign);
            Assert.Empty(shape.UnknownKeys);
        }

        [Theory]
        [InlineData("5")]
        [InlineData("\"text\"")]
        [InlineData("[1,2,3]")]
        [InlineData("true")]
        public void NonObjectPayload_IsNotAnObject(string json)
        {
            var shape = Classify(json);

            Assert.Equal(SerializedMemberShapeKind.NotAnObject, shape.Kind);
            Assert.False(shape.IsForeign);
        }

        [Fact]
        public void NullOrAbsentPayload_IsAbsent()
        {
            Assert.Equal(SerializedMemberShapeKind.Absent, SerializedMemberShape.Classify(null).Kind);
            Assert.Equal(SerializedMemberShapeKind.Absent, Classify("null").Kind);
        }

        [Fact]
        public void EveryAdvertisedKnownKey_IsAccepted()
        {
            // Guards the one duplication in the classifier: the KnownKeys list used to phrase the
            // diagnostic message and the IsStructuralKey/IsDescriptiveKey predicates used to decide
            // must not drift apart.
            foreach (var key in SerializedMemberShape.KnownKeys)
            {
                var shape = Classify($"{{\"{key}\":null}}");
                Assert.Equal(SerializedMemberShapeKind.SerializedMember, shape.Kind);
            }

            // And the advertised list matches the real SerializedMember schema.
            Assert.Equal(
                new[]
                {
                    nameof(SerializedMember.name),
                    nameof(SerializedMember.typeName),
                    SerializedMember.ValueName,
                    nameof(SerializedMember.fields),
                    nameof(SerializedMember.props)
                }.OrderBy(k => k),
                SerializedMemberShape.KnownKeys.OrderBy(k => k));
        }

        [Fact]
        public void StructuralAndDescriptiveKeys_PartitionTheKnownKeys()
        {
            // The whole fix rests on this split, so pin it: 'value'/'fields'/'props' are unique to
            // the SerializedMember shape and therefore discriminate; 'name'/'typeName' are ordinary
            // English words that foreign payloads use too, and therefore do not.
            Assert.Equal(
                new[] { SerializedMember.ValueName, nameof(SerializedMember.fields), nameof(SerializedMember.props) }.OrderBy(k => k),
                SerializedMemberShape.StructuralKeys.OrderBy(k => k));

            Assert.Equal(
                new[] { nameof(SerializedMember.name), nameof(SerializedMember.typeName) }.OrderBy(k => k),
                SerializedMemberShape.DescriptiveKeys.OrderBy(k => k));

            // Together they must be exactly the known keys - no key may be in both, none left out.
            Assert.Empty(SerializedMemberShape.StructuralKeys.Intersect(SerializedMemberShape.DescriptiveKeys));
            Assert.Equal(
                SerializedMemberShape.KnownKeys.OrderBy(k => k),
                SerializedMemberShape.StructuralKeys.Concat(SerializedMemberShape.DescriptiveKeys).OrderBy(k => k));
        }

        [Fact]
        public void StructuralKeyCount_CountsOnlyStructuralKeys()
        {
            Assert.Equal(0, Classify("{\"name\":\"a\",\"typeName\":\"T\"}").StructuralKeyCount);
            Assert.Equal(2, Classify("{\"name\":\"a\",\"typeName\":\"T\"}").RecognisedKeyCount);
            Assert.Equal(3, Classify("{\"value\":1,\"fields\":[],\"props\":[]}").StructuralKeyCount);
            Assert.Equal(1, Classify("{\"typeName\":\"T\",\"value\":1}").StructuralKeyCount);
        }

        [Fact]
        public void DefaultInstance_HasNoNullCollections()
        {
            var shape = default(SerializedMemberShape);

            Assert.NotNull(shape.UnknownKeys);
            Assert.Empty(shape.UnknownKeys);
            Assert.Null(shape.FirstUnknownKey);
            Assert.Equal(string.Empty, shape.DescribeUnknownKeys());
        }

        [Fact]
        public void DescribeUnknownKeys_NamesTheOffenderAndTheValidKeys()
        {
            var description = Classify("{\"instanceID\":\"12345\"}").DescribeUnknownKeys();

            _output.WriteLine(description);

            Assert.Contains("instanceID", description);
            foreach (var key in SerializedMemberShape.KnownKeys)
                Assert.Contains(key, description);
        }
    }
}

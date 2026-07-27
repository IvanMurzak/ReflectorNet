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
            Assert.NotEmpty(shape.UnknownKeys);
        }

        [Theory]
        // Trying to be a SerializedMember and getting it wrong -> loud failure, not a decline.
        [InlineData("{\"typeName\":\"System.Int32\",\"instanceID\":\"12345\"}")]
        [InlineData("{\"name\":\"a\",\"typeName\":\"System.Int32\",\"X\":1}")]
        [InlineData("{\"value\":{},\"bogus\":true}")]
        public void ObjectMixingRecognisedAndUnknownKeys_IsMalformed(string json)
        {
            var shape = Classify(json);

            _output.WriteLine($"{json} -> {shape.Kind} (unknown: {string.Join(",", shape.UnknownKeys)})");

            Assert.Equal(SerializedMemberShapeKind.Malformed, shape.Kind);
            Assert.False(shape.IsForeign);
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
            // diagnostic message and the IsKnownKey predicate used to decide must not drift apart.
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

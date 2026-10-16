using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Model;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// A value whose typeName names a different numeric type than the member (e.g. 'System.Double' for a float)
    /// used to be parsed as the sent type; writing it back threw, and TryModifyAt sent that only to the ILogger
    /// while returning true — a silently dropped write. The declared type now wins, and a write-back that still
    /// fails is reported and returns false.
    /// </summary>
    public class ModifyAtWriteBackTests : BaseTest
    {
        public ModifyAtWriteBackTests(ITestOutputHelper output) : base(output) { }

        enum Mode { Off, On }

        class Inner
        {
            public string Name { get; set; } = "a";
        }

        [Flags]
        enum Layers { None = 0, A = 1, B = 2 }

        class Target
        {
            public float Ratio { get; set; } = 1f;
            public float? Optional { get; set; }
            public int Count { get; set; } = 1;
            public byte Small = 0;
            public Mode Mode { get; set; }
            public Layers Layers { get; set; }
            public string Title { get; set; } = "t";
            public List<float> Weights { get; set; } = new List<float> { 1f };
            public int[] Slots { get; set; } = { 1 };
            public Dictionary<string, int> Map { get; set; } = new Dictionary<string, int> { ["k"] = 1 };

            readonly Inner child = new Inner();
            public Inner Child
            {
                get => child;
                set => throw new InvalidOperationException("child is fixed");
            }

            public int Guarded
            {
                get => 0;
                set => throw new InvalidOperationException("guarded is read-only at runtime");
            }
        }

        static SerializedMember Value<T>(string json) => SerializedMember.FromJson(typeof(T), json);

        static (bool ok, Target target, Logs logs) Modify(string path, SerializedMember value)
        {
            object? obj = new Target();
            var logs = new Logs();
            var ok = new Reflector().TryModifyAt(ref obj, path, value, logs: logs);
            return (ok, (Target)obj!, logs);
        }

        static bool HasError(Logs logs, string fragment)
            => logs.Any(e => e.Type == LogType.Error && e.Message.Contains(fragment));

        [Fact]
        public void DoubleIntoFloatProperty_IsConvertedAndLands()
        {
            var (ok, target, logs) = Modify("Ratio", Value<double>("0.5"));

            Assert.True(ok);
            Assert.Equal(0.5f, target.Ratio);
            Assert.DoesNotContain(logs, e => e.Type == LogType.Error);
        }

        [Fact]
        public void WholeDoubleIntoIntProperty_Lands()
        {
            var (ok, target, _) = Modify("Count", Value<double>("3"));

            Assert.True(ok);
            Assert.Equal(3, target.Count);
        }

        [Fact]
        public void FractionalDoubleIntoIntProperty_FailsLoudly()
        {
            var (ok, target, logs) = Modify("Count", Value<double>("3.7"));

            Assert.False(ok);
            Assert.Equal(1, target.Count);
            Assert.True(HasError(logs, "out of range"));
        }

        [Fact]
        public void OverflowIntoByteField_FailsLoudly()
        {
            var (ok, target, logs) = Modify("Small", Value<int>("300"));

            Assert.False(ok);
            Assert.Equal(0, target.Small);
            Assert.True(HasError(logs, "out of range"));
        }

        [Fact]
        public void HugeDoubleIntoFloat_FailsInsteadOfStoringInfinity()
        {
            var (ok, target, _) = Modify("Ratio", Value<double>("1e300"));

            Assert.False(ok);
            Assert.Equal(1f, target.Ratio);
        }

        [Fact]
        public void IntIntoEnumProperty_Lands()
        {
            var (ok, target, _) = Modify("Mode", Value<int>("1"));

            Assert.True(ok);
            Assert.Equal(Mode.On, target.Mode);
        }

        [Fact]
        public void DoubleIntoNullableFloat_Lands()
        {
            var (ok, target, _) = Modify("Optional", Value<double>("0.5"));

            Assert.True(ok);
            Assert.Equal(0.5f, target.Optional);
        }

        [Fact]
        public void TryModify_WithoutPath_DoubleIntoFloatMember_Lands()
        {
            object? obj = new Target();
            var data = new SerializedMember { typeName = typeof(Target).FullName! };
            data.props = new SerializedMemberList { Value<double>("0.75").SetName("Ratio") };

            Assert.True(new Reflector().TryModify(ref obj, data, logs: new Logs()));
            Assert.Equal(0.75f, ((Target)obj!).Ratio);
        }

        [Fact]
        public void IncompatibleType_FailsLoudly()
        {
            var (ok, target, logs) = Modify("Title", Value<int>("5"));

            Assert.False(ok);
            Assert.Equal("t", target.Title);
            Assert.True(HasError(logs, "Type mismatch"));
        }

        [Fact]
        public void ThrowingSetter_FailsAndReportsTheSetterMessage()
        {
            var (ok, _, logs) = Modify("Guarded", Value<int>("2"));

            Assert.False(ok);
            Assert.True(HasError(logs, "guarded is read-only at runtime"));
        }

        [Fact]
        public void DoubleIntoListOfFloatElement_Lands()
        {
            var (ok, target, _) = Modify("Weights/[0]", Value<double>("0.25"));

            Assert.True(ok);
            Assert.Equal(0.25f, target.Weights[0]);
        }

        [Fact]
        public void IncompatibleArrayElement_FailsInsteadOfThrowing()
        {
            var (ok, target, logs) = Modify("Slots/[0]", Value<string>("\"x\""));

            Assert.False(ok);
            Assert.Equal(1, target.Slots[0]);
            Assert.True(HasError(logs, "Type mismatch"));
        }

        [Fact]
        public void DoubleIntoDictionaryOfIntEntry_Lands()
        {
            var (ok, target, _) = Modify("Map/[k]", Value<double>("2"));

            Assert.True(ok);
            Assert.Equal(2, target.Map["k"]);
        }

        [Fact]
        public void IntIntoFlagsEnumProperty_CombinationLands()
        {
            var (ok, target, _) = Modify("Layers", Value<int>("3"));

            Assert.True(ok);
            Assert.Equal(Layers.A | Layers.B, target.Layers);
        }

        [Fact]
        public void Deserialize_EnumTypeNameWithIntFallback_KeepsTheEnum()
        {
            var result = new Reflector().Deserialize(Value<Mode>("\"On\""), typeof(int));

            Assert.Equal(Mode.On, result);
        }

        [Fact]
        public void Patch_ThrowingSetter_ReportsTheSetterMessage()
        {
            object? obj = new Target();
            var logs = new Logs();

            Assert.False(new Reflector().TryPatch(ref obj, "{\"Guarded\":2}", logs: logs));
            Assert.True(HasError(logs, "guarded is read-only at runtime"));
        }

        [Fact]
        public void NestedChangeOfClassBehindThrowingSetter_LandsAndSucceeds()
        {
            // The child is a reference type modified in place; writing the same instance back is skipped, so the
            // container's throwing setter cannot turn an applied change into a reported failure.
            var (ok, target, logs) = Modify("Child/Name", Value<string>("\"b\""));

            Assert.True(ok);
            Assert.Equal("b", target.Child.Name);
            Assert.DoesNotContain(logs, e => e.Type == LogType.Error);
        }

        [Fact]
        public void TryModify_WithoutPath_ThrowingSetter_ReportsTheSetterMessage()
        {
            object? obj = new Target();
            var data = new SerializedMember { typeName = typeof(Target).FullName! };
            data.props = new SerializedMemberList { Value<int>("2").SetName("Guarded") };
            var logs = new Logs();

            Assert.False(new Reflector().TryModify(ref obj, data, logs: logs));
            Assert.True(HasError(logs, "guarded is read-only at runtime"));
        }
    }
}

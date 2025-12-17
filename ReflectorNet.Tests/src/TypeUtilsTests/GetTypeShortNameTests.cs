using System;
using System.Collections.Generic;
using System.Text;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.TypeUtilsTests
{
    public class GetTypeShortNameTests : BaseTest
    {
        public GetTypeShortNameTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetTypeShortName_BuiltInTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.BuiltInTypes);
        }

        [Fact]
        public void GetTypeShortName_BuiltInArrayTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.BuiltInArrayTypes);
        }

        [Fact]
        public void GetTypeShortName_BuiltInGenericTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.BuiltInGenericTypes);
        }

        [Fact]
        public void GetTypeShortName_NestedGenericTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.NestedGenericTypes);
        }

        [Fact]
        public void GetTypeShortName_ThisAssemblyTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.ThisAssemblyTypes);
        }

        [Fact]
        public void GetTypeShortName_ReflectorNetTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.ReflectorNetTypes);
        }

        [Fact]
        public void GetTypeShortName_OuterAssemblyTypes()
        {
            ValidateTypeShortName(GetTypeIdTests.OuterAssemblyTypes);
        }

        private void ValidateTypeShortName(Dictionary<string, Type> typeMap)
        {
            var failed = new List<string>();
            foreach (var kvp in typeMap)
            {
                var typeId = kvp.Key;
                var type = kvp.Value;

                var expectedShortName = StripNamespace(typeId);
                var actualShortName = TypeUtils.GetTypeShortName(type);

                // Normalize spaces in expectedShortName just in case
                // expectedShortName = expectedShortName.Replace(", ", ",");
                // No, GetTypeShortName outputs ", ". My StripNamespace outputs ", ".

                if (expectedShortName != actualShortName)
                {
                    _output.WriteLine($"FAIL: {typeId}");
                    _output.WriteLine($"  Expected: {expectedShortName}");
                    _output.WriteLine($"  Actual:   {actualShortName}");
                    failed.Add(typeId);
                }
                else
                {
                    _output.WriteLine($"PASS: {typeId} -> {actualShortName}");
                }
            }

            if (failed.Count > 0)
            {
                Assert.Fail($"Failed {failed.Count} types. See output for details.");
            }
        }

        private string StripNamespace(string typeId)
        {
            if (string.IsNullOrEmpty(typeId)) return typeId;

            var sb = new StringBuilder();
            var currentToken = new StringBuilder();

            for (int i = 0; i < typeId.Length; i++)
            {
                char c = typeId[i];
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '`')
                {
                    currentToken.Append(c);
                }
                else
                {
                    // Flush current token
                    if (currentToken.Length > 0)
                    {
                        var token = currentToken.ToString();
                        var shortToken = GetShortToken(token);
                        sb.Append(shortToken);
                        currentToken.Clear();
                    }

                    sb.Append(c);
                    if (c == ',')
                    {
                        sb.Append(" ");
                    }
                }
            }

            // Flush last token
            if (currentToken.Length > 0)
            {
                var token = currentToken.ToString();
                var shortToken = GetShortToken(token);
                sb.Append(shortToken);
            }

            return sb.ToString();
        }

        private string GetShortToken(string token)
        {
            var lastDot = token.LastIndexOf('.');
            if (lastDot >= 0)
            {
                return token.Substring(lastDot + 1);
            }
            return token;
        }
    }
}

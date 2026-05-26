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

                // The shared GetTypeIdTests dictionary keys are firewall-safe schema type-ids
                // (issue #80): generic '()', nested-class '-', array '-rank'. GetTypeShortName is a
                // human-readable display name that is intentionally NOT firewall-safe — it still
                // uses '<>', '+', '[]'/'[, ]'. Convert the safe-form key to that display form before
                // stripping namespaces so the two representations line up.
                var expectedShortName = StripNamespace(ToDisplayForm(typeId));
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

        /// <summary>
        /// Converts a firewall-safe schema type-id (issue #80) to the human-readable display form
        /// that <see cref="TypeUtils.GetTypeShortName"/> emits: generic '(' ')' -> '&lt;' '&gt;',
        /// nested-class '-&lt;ident&gt;' -> '+', array '-&lt;rank&gt;' -> '[' + (rank-1 commas) + ']'.
        /// Comma spacing is left to <see cref="StripNamespace"/>, which already inserts a space
        /// after every comma. Hyphens past a top-level comma (assembly name) are left literal.
        /// </summary>
        private string ToDisplayForm(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
                return typeId;

            var sb = new StringBuilder(typeId.Length + 4);
            var depth = 0;
            var inAssemblyName = false;

            for (int i = 0; i < typeId.Length; i++)
            {
                var c = typeId[i];

                if (inAssemblyName)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '(':
                        depth++;
                        sb.Append('<');
                        break;
                    case ')':
                        depth--;
                        sb.Append('>');
                        break;
                    case '[':
                        depth++;
                        sb.Append('[');
                        break;
                    case ']':
                        depth--;
                        sb.Append(']');
                        break;
                    case ',':
                        if (depth == 0)
                            inAssemblyName = true;
                        sb.Append(',');
                        break;
                    case '-':
                        {
                            var next = i + 1 < typeId.Length ? typeId[i + 1] : '\0';
                            if (next >= '0' && next <= '9')
                            {
                                int j = i + 1;
                                while (j < typeId.Length && typeId[j] >= '0' && typeId[j] <= '9')
                                    j++;
                                var rankText = typeId.Substring(i + 1, j - (i + 1));
                                if (int.TryParse(rankText, out var rank) && rank >= 1)
                                {
                                    sb.Append('[');
                                    if (rank > 1)
                                        sb.Append(new string(',', rank - 1));
                                    sb.Append(']');
                                    i = j - 1;
                                }
                                else
                                {
                                    sb.Append('-');
                                }
                            }
                            else if (next == '_' || char.IsLetter(next))
                            {
                                sb.Append('+');
                            }
                            else
                            {
                                sb.Append('-');
                            }
                            break;
                        }
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
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

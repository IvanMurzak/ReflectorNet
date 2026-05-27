using System;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Converts a firewall-safe schema type-id (issue #80) into the C#-canonical form that the
        /// <c>TryResolveArrayType</c> / <c>TryResolveCSharpGenericType</c> / <c>TryResolveClassicGenericType</c>
        /// parsers expect. The safe form uses delimiters that are illegal in C# identifiers, legal
        /// unescaped in a URI fragment, and absent from every LLM provider's content-firewall blocklist:
        /// <list type="bullet">
        /// <item><c>(</c> -> <c>&lt;</c> and <c>)</c> -> <c>&gt;</c> (generic args)</item>
        /// <item><c>-&lt;digits&gt;</c> -> <c>[</c> + (rank-1 commas) + <c>]</c> (array rank: <c>-1</c> -> <c>[]</c>, <c>-2</c> -> <c>[,]</c>, jagged <c>-1-1</c> -> <c>[][]</c>)</item>
        /// <item><c>-&lt;letter|underscore&gt;</c> -> <c>+</c> (nested class)</item>
        /// </list>
        /// Disambiguation is unambiguous because C# identifiers cannot start with a digit: a digit
        /// after <c>-</c> is always an array rank, a letter/underscore is always a nested class.
        /// <para>
        /// Callers may submit either the safe form (e.g. <c>System.Int32-1</c>, what a JSON Schema
        /// <c>$ref</c> consumer round-trips) or the already-canonical raw form (e.g. <c>System.Int32[]</c>);
        /// both normalize to canonical here so every resolution path sees a single form.
        /// </para>
        /// <para>
        /// Assembly-qualified-name edge: an assembly name can legitimately contain a hyphen
        /// (e.g. <c>Some-Assembly</c>). Only hyphens inside the TYPE-NAME portion are structural;
        /// once the scan crosses the first top-level (bracket-depth 0) comma — which introduces the
        /// assembly name — hyphens are left literal. Hyphens inside an existing <c>[ ]</c> classic-AQN
        /// region (depth &gt; 0) are likewise left untouched.
        /// </para>
        /// </summary>
        static string DecodeSchemaRefChars(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // Fast path: nothing structural to convert.
            if (typeName.IndexOf('(') < 0 && typeName.IndexOf(')') < 0 && typeName.IndexOf('-') < 0)
                return typeName;

            var sb = new System.Text.StringBuilder(typeName.Length + 4);
            var depth = 0;            // nesting depth across generic '(' and classic-AQN '['.
            var inAssemblyName = false; // set once a top-level (depth-0) comma is crossed.

            for (int i = 0; i < typeName.Length; i++)
            {
                var c = typeName[i];

                if (inAssemblyName)
                {
                    // Past the type-name portion (a top-level comma introduced the assembly name):
                    // safe-form ids never carry structural delimiters here, so emit every remaining
                    // character verbatim without decoding.
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
                        // Already-canonical bracket (raw input or classic AQN). Track depth so a
                        // depth>0 comma is not mistaken for the assembly separator.
                        depth++;
                        sb.Append('[');
                        break;
                    case ']':
                        depth--;
                        sb.Append(']');
                        break;
                    case ',':
                        if (depth == 0)
                            inAssemblyName = true; // assembly name begins; stop structural decoding.
                        sb.Append(',');
                        break;
                    case '-':
                        {
                            var next = i + 1 < typeName.Length ? typeName[i + 1] : '\0';
                            if (next >= '0' && next <= '9')
                            {
                                // Array rank: read all consecutive digits.
                                int j = i + 1;
                                while (j < typeName.Length && typeName[j] >= '0' && typeName[j] <= '9')
                                    j++;
                                var rankText = typeName.Substring(i + 1, j - (i + 1));
                                // Defensive: int.Parse on a bounded digit run.
                                if (int.TryParse(rankText, out var rank) && rank >= 1)
                                {
                                    sb.Append('[');
                                    if (rank > 1)
                                        sb.Append(new string(',', rank - 1));
                                    sb.Append(']');
                                    i = j - 1; // skip the consumed digits.
                                }
                                else
                                {
                                    sb.Append('-'); // not a valid rank; leave literal.
                                }
                            }
                            else if (next == '_' || char.IsLetter(next))
                            {
                                // Nested class.
                                sb.Append('+');
                            }
                            else
                            {
                                // Hyphen not introducing a rank or nested class (e.g. a stray
                                // hyphen). Leave literal.
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

        /// <summary>
        /// Retrieves a <see cref="Type"/> by its name.
        /// </summary>
        /// <param name="typeName">The name of the type to retrieve. Can be a full name, assembly qualified name, or a custom identifier. Firewall-safe schema type-ids (issue #80; e.g. <c>System.Int32-1</c>, <c>IList(System.Int32)</c>, <c>Outer-Nested</c>) are converted to their C#-canonical form before resolution, so $ref-style input is accepted alongside raw type ids.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name, or <see langword="null"/> if the type cannot be found.</returns>
        public static Type? GetType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            typeName = DecodeSchemaRefChars(typeName!);

            if (_typeCache.TryGetValue(typeName, out var cachedType))
                return cachedType;

            Type? type = null;
            try
            {
                type = Type.GetType(typeName, throwOnError: false);
            }
            catch
            {
                // Ignore exceptions from Type.GetType
            }

            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = TryResolveArrayType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = TryResolveCSharpGenericType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = TryResolveClassicGenericType(
                assemblyPrefix: null,
                typeName: typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = AssemblyUtils.AllTypes.FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                // typeName has already been normalized to canonical form by DecodeSchemaRefChars;
                // GetTypeId() now emits the firewall-safe form (issue #80), so decode it too before
                // comparing (covers open generics / nested types where no parser path matched).
                typeName == DecodeSchemaRefChars(t.GetTypeId()));

            _typeCache[typeName] = type;

            return type;
        }

        /// <summary>
        /// Retrieves a <see cref="Type"/> by its name and (optionally) an assembly name prefix.
        /// </summary>
        /// <param name="assemblyName">
        /// The name, or prefix of the name, of the assembly containing the type. The value is used as a prefix,
        /// and the method will match any assembly whose name starts with this value.
        /// </param>
        /// <param name="typeName">The name of the type to retrieve.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name and assembly name prefix, or <see langword="null"/> if the type cannot be found.</returns>
        /// <remarks>
        /// Note: For generic types, only the generic type definition is required to be in an assembly whose name matches the
        /// specified prefix. Generic type arguments are resolved from all currently loaded assemblies, not only from assemblies
        /// whose names start with the provided prefix.
        /// </remarks>
        public static Type? GetType(string? assemblyName, string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            typeName = DecodeSchemaRefChars(typeName!);

            if (string.IsNullOrEmpty(assemblyName))
                return GetType(typeName);

            var cacheKey = $"{assemblyName}|{typeName}";
            if (_assemblyTypeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            Type? type = null;
            try
            {
                type = Type.GetType(typeName, throwOnError: false);
                if (type != null && !IsTypeInMatchingAssembly(type, assemblyName))
                    type = null;
            }
            catch
            {
                // Ignore exceptions from Type.GetType
            }

            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveArrayType(assemblyName, typeName);
            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveCSharpGenericType(assemblyName, typeName);
            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveClassicGenericType(assemblyName, typeName);
            if (type != null)
            {
                _assemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = AssemblyUtils.GetTypesStartingWith(assemblyName).FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                typeName == DecodeSchemaRefChars(t.GetTypeId()));

            _assemblyTypeCache[cacheKey] = type;

            return type;
        }

        /// <summary>
        /// Retrieves a <see cref="Type"/> by its name within a specific assembly.
        /// </summary>
        /// <param name="assembly">The assembly to search in.</param>
        /// <param name="typeName">The name of the type to retrieve.</param>
        /// <returns>The <see cref="Type"/> corresponding to the specified name in the specified assembly, or <see langword="null"/> if the type cannot be found.</returns>
        public static Type? GetType(Assembly assembly, string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName) || assembly == null)
                return null;

            typeName = DecodeSchemaRefChars(typeName!);

            var cacheKey = $"{assembly.GetName().Name}|{typeName}";
            if (_exactAssemblyTypeCache.TryGetValue(cacheKey, out var cachedType))
                return cachedType;

            Type? type = null;
            try
            {
                type = assembly.GetType(typeName, throwOnError: false);
            }
            catch
            {
                // Ignore exceptions from Assembly.GetType
            }

            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveArrayType(assembly, typeName);
            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveCSharpGenericType(assembly, typeName);
            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = TryResolveClassicGenericType(assembly, typeName);
            if (type != null)
            {
                _exactAssemblyTypeCache[cacheKey] = type;
                return type;
            }

            type = AssemblyUtils.GetAssemblyTypes(assembly).FirstOrDefault(t =>
                typeName == t.FullName ||
                typeName == t.AssemblyQualifiedName ||
                typeName == DecodeSchemaRefChars(t.GetTypeId()));

            _exactAssemblyTypeCache[cacheKey] = type;

            return type;
        }
    }
}

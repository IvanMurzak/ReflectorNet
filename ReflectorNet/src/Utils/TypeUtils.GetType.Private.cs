using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        private static bool IsTypeInMatchingAssembly(Type type, string assemblyPrefix)
        {
            var assemblyName = type.Assembly.GetName().Name;
            return assemblyName != null && assemblyName.StartsWith(assemblyPrefix, StringComparison.Ordinal);
        }

        private static Type? ResolveSimpleType(string name)
        {
            var type = Type.GetType(name, throwOnError: false);
            if (type != null)
                return type;

            return AssemblyUtils.AllTypes.FirstOrDefault(t =>
                name == t.AssemblyQualifiedName ||
                name == t.FullName ||
                name == t.Name);
        }

        private static Type? ResolveSimpleType(string assemblyPrefix, string name)
        {
            var type = Type.GetType(name, throwOnError: false);
            if (type != null && IsTypeInMatchingAssembly(type, assemblyPrefix))
                return type;

            return AssemblyUtils.GetTypesStartingWith(assemblyPrefix).FirstOrDefault(t =>
                name == t.AssemblyQualifiedName ||
                name == t.FullName ||
                name == t.Name);
        }

        private static Type? TryResolveArrayType(string typeName)
        {
            if (!typeName.EndsWith("]"))
                return null;

            var lastOpenBracket = typeName.LastIndexOf('[');
            if (lastOpenBracket < 0)
                return null;

            var suffix = typeName.Substring(lastOpenBracket);
            var content = suffix.Substring(1, suffix.Length - 2);
            if (content.Length > 0 && content.Any(c => c != ','))
                return null;

            var commas = content.Length;
            var elementTypeName = typeName.Substring(0, lastOpenBracket);
            var elementType = GetType(elementTypeName);

            if (elementType == null) return null;

            return commas == 0
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(commas + 1);
        }

        private static Type? TryResolveArrayType(string assemblyPrefix, string typeName)
        {
            if (!typeName.EndsWith("]"))
                return null;

            var lastOpenBracket = typeName.LastIndexOf('[');
            if (lastOpenBracket < 0)
                return null;

            var suffix = typeName.Substring(lastOpenBracket);
            var content = suffix.Substring(1, suffix.Length - 2);
            if (content.Length > 0 && content.Any(c => c != ','))
                return null;

            var commas = content.Length;
            var elementTypeName = typeName.Substring(0, lastOpenBracket);
            var elementType = GetType(assemblyPrefix, elementTypeName);

            if (elementType == null) return null;

            return commas == 0
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(commas + 1);
        }

        private static Type? TryResolveCSharpGenericType(string typeName)
        {
            var openBracketIndex = typeName.IndexOf('<');
            if (openBracketIndex < 0)
                return null;

            var closeBracketIndex = FindMatchingCloseBracket(typeName, openBracketIndex);
            if (closeBracketIndex < 0)
                return null;

            var baseTypeName = typeName.Substring(0, openBracketIndex);
            if (string.IsNullOrWhiteSpace(baseTypeName))
                return null;

            var typeArgsString = typeName.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
            var typeArgNames = ParseCSharpGenericArguments(typeArgsString);
            if (typeArgNames == null || typeArgNames.Length == 0)
                return null;

            var genericDefName = $"{baseTypeName}`{typeArgNames.Length}";
            var genericDef = ResolveSimpleType(genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            var typeArgs = new Type[typeArgNames.Length];
            for (int i = 0; i < typeArgNames.Length; i++)
            {
                var argType = GetType(typeArgNames[i].Trim());
                if (argType == null)
                    return null;
                typeArgs[i] = argType;
            }

            Type? currentType;
            try
            {
                currentType = genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }

            var remaining = typeName.Substring(closeBracketIndex + 1);
            while (!string.IsNullOrEmpty(remaining))
            {
                if (!remaining.StartsWith("+") && !remaining.StartsWith("."))
                    return null;

                remaining = remaining.Substring(1);

                var open = remaining.IndexOf('<');
                string nestedName;
                Type[]? nestedArgs = null;
                int nextRemainingIndex;

                if (open > 0)
                {
                    var close = FindMatchingCloseBracket(remaining, open);
                    if (close < 0) return null;

                    nestedName = remaining.Substring(0, open);
                    var argsStr = remaining.Substring(open + 1, close - open - 1);
                    var argNames = ParseCSharpGenericArguments(argsStr);
                    if (argNames == null) return null;

                    nestedArgs = new Type[argNames.Length];
                    for (int i = 0; i < argNames.Length; i++)
                    {
                        var tempType = GetType(argNames[i]?.Trim());
                        if (tempType == null) return null;
                        nestedArgs[i] = tempType;
                    }

                    nextRemainingIndex = close + 1;
                }
                else
                {
                    var nextSep = remaining.IndexOfAny(NestedTypeSeparators);
                    if (nextSep > 0)
                    {
                        nestedName = remaining.Substring(0, nextSep);
                        nextRemainingIndex = nextSep;
                    }
                    else
                    {
                        nestedName = remaining;
                        nextRemainingIndex = remaining.Length;
                    }
                }

                Type? nestedType;
                Type[] allArgs;

                if (nestedArgs != null)
                {
                    nestedType = currentType.GetNestedType($"{nestedName}`{nestedArgs.Length}");
                    if (nestedType == null) return null;

                    if (currentType.IsGenericType && !currentType.IsGenericTypeDefinition)
                    {
                        var parentArgs = currentType.GetGenericArguments();
                        allArgs = new Type[parentArgs.Length + nestedArgs.Length];
                        Array.Copy(parentArgs, allArgs, parentArgs.Length);
                        Array.Copy(nestedArgs, 0, allArgs, parentArgs.Length, nestedArgs.Length);
                    }
                    else
                    {
                        allArgs = nestedArgs;
                    }
                }
                else
                {
                    nestedType = currentType.GetNestedType(nestedName);
                    if (nestedType == null) return null;

                    allArgs = currentType.IsGenericType && !currentType.IsGenericTypeDefinition
                        ? currentType.GetGenericArguments()
                        : Type.EmptyTypes;
                }

                if (nestedType.IsGenericTypeDefinition)
                {
                    try
                    {
                        currentType = nestedType.GetGenericArguments().Length == allArgs.Length
                            ? nestedType.MakeGenericType(allArgs)
                            : nestedType;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    currentType = nestedType;
                }

                remaining = remaining.Substring(nextRemainingIndex);
            }

            return currentType;
        }

        private static Type? TryResolveCSharpGenericType(string assemblyPrefix, string typeName)
        {
            var openBracketIndex = typeName.IndexOf('<');
            if (openBracketIndex < 0)
                return null;

            var closeBracketIndex = FindMatchingCloseBracket(typeName, openBracketIndex);
            if (closeBracketIndex < 0)
                return null;

            var baseTypeName = typeName.Substring(0, openBracketIndex);
            if (string.IsNullOrWhiteSpace(baseTypeName))
                return null;

            var typeArgsString = typeName.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
            var typeArgNames = ParseCSharpGenericArguments(typeArgsString);
            if (typeArgNames == null || typeArgNames.Length == 0)
                return null;

            var genericDefName = $"{baseTypeName}`{typeArgNames.Length}";
            var genericDef = ResolveSimpleType(assemblyPrefix, genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            var typeArgs = new Type[typeArgNames.Length];
            for (int i = 0; i < typeArgNames.Length; i++)
            {
                var argType = GetType(typeArgNames[i].Trim());
                if (argType == null)
                    return null;
                typeArgs[i] = argType;
            }

            Type? currentType;
            try
            {
                currentType = genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }

            var remaining = typeName.Substring(closeBracketIndex + 1);
            while (!string.IsNullOrEmpty(remaining))
            {
                if (!remaining.StartsWith("+") && !remaining.StartsWith("."))
                    return null;

                remaining = remaining.Substring(1);

                var open = remaining.IndexOf('<');
                string nestedName;
                Type[]? nestedArgs = null;
                int nextRemainingIndex;

                if (open > 0)
                {
                    var close = FindMatchingCloseBracket(remaining, open);
                    if (close < 0) return null;

                    nestedName = remaining.Substring(0, open);
                    var argsStr = remaining.Substring(open + 1, close - open - 1);
                    var argNames = ParseCSharpGenericArguments(argsStr);
                    if (argNames == null) return null;

                    nestedArgs = new Type[argNames.Length];
                    for (int i = 0; i < argNames.Length; i++)
                    {
                        var tempType = GetType(argNames[i]?.Trim());
                        if (tempType == null) return null;
                        nestedArgs[i] = tempType;
                    }

                    nextRemainingIndex = close + 1;
                }
                else
                {
                    var nextSep = remaining.IndexOfAny(NestedTypeSeparators);
                    if (nextSep > 0)
                    {
                        nestedName = remaining.Substring(0, nextSep);
                        nextRemainingIndex = nextSep;
                    }
                    else
                    {
                        nestedName = remaining;
                        nextRemainingIndex = remaining.Length;
                    }
                }

                Type? nestedType;
                Type[] allArgs;

                if (nestedArgs != null)
                {
                    nestedType = currentType.GetNestedType($"{nestedName}`{nestedArgs.Length}");
                    if (nestedType == null) return null;

                    if (currentType.IsGenericType && !currentType.IsGenericTypeDefinition)
                    {
                        var parentArgs = currentType.GetGenericArguments();
                        allArgs = new Type[parentArgs.Length + nestedArgs.Length];
                        Array.Copy(parentArgs, allArgs, parentArgs.Length);
                        Array.Copy(nestedArgs, 0, allArgs, parentArgs.Length, nestedArgs.Length);
                    }
                    else
                    {
                        allArgs = nestedArgs;
                    }
                }
                else
                {
                    nestedType = currentType.GetNestedType(nestedName);
                    if (nestedType == null) return null;

                    allArgs = currentType.IsGenericType && !currentType.IsGenericTypeDefinition
                        ? currentType.GetGenericArguments()
                        : Type.EmptyTypes;
                }

                if (nestedType.IsGenericTypeDefinition)
                {
                    try
                    {
                        currentType = nestedType.GetGenericArguments().Length == allArgs.Length
                            ? nestedType.MakeGenericType(allArgs)
                            : nestedType;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    currentType = nestedType;
                }

                remaining = remaining.Substring(nextRemainingIndex);
            }

            return currentType;
        }

        private static int FindMatchingCloseBracket(string typeName, int openIndex)
        {
            var depth = 0;
            for (int i = openIndex; i < typeName.Length; i++)
            {
                if (typeName[i] == '<')
                    depth++;
                else if (typeName[i] == '>')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }
            return -1;
        }

        private static string[]? ParseCSharpGenericArguments(string argsString)
        {
            if (string.IsNullOrWhiteSpace(argsString))
                return null;

            var args = new System.Collections.Generic.List<string>();
            var depth = 0;
            var currentArg = new System.Text.StringBuilder();

            for (int i = 0; i < argsString.Length; i++)
            {
                var c = argsString[i];

                if (c == '<')
                {
                    depth++;
                    currentArg.Append(c);
                }
                else if (c == '>')
                {
                    depth--;
                    currentArg.Append(c);
                }
                else if (c == ',' && depth == 0)
                {
                    var arg = currentArg.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg))
                        args.Add(arg);
                    currentArg.Clear();
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            var lastArg = currentArg.ToString().Trim();
            if (!string.IsNullOrEmpty(lastArg))
                args.Add(lastArg);

            return args.Count > 0 ? args.ToArray() : null;
        }

        private static Type? TryResolveClassicGenericType(string typeName)
        {
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex < 0)
                return null;

            var argsStart = typeName.IndexOf("[[", backtickIndex);
            if (argsStart < 0)
                return null;

            var genericDefName = typeName.Substring(0, argsStart);
            var genericDef = ResolveSimpleType(genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            var typeArgs = ParseGenericArguments(typeName, argsStart);
            if (typeArgs == null || typeArgs.Length != genericDef.GetGenericArguments().Length)
                return null;

            try
            {
                return genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }
        }

        private static Type? TryResolveClassicGenericType(string assemblyPrefix, string typeName)
        {
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex < 0)
                return null;

            var argsStart = typeName.IndexOf("[[", backtickIndex);
            if (argsStart < 0)
                return null;

            var genericDefName = typeName.Substring(0, argsStart);
            var genericDef = ResolveSimpleType(assemblyPrefix, genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            var typeArgs = ParseGenericArguments(assemblyPrefix, typeName, argsStart);
            if (typeArgs == null || typeArgs.Length != genericDef.GetGenericArguments().Length)
                return null;

            try
            {
                return genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }
        }

        private static Type[]? ParseGenericArguments(string typeName, int startIndex)
        {
            var args = new System.Collections.Generic.List<Type>();
            var depth = 0;
            var currentArg = new System.Text.StringBuilder();

            for (int i = startIndex; i < typeName.Length; i++)
            {
                var c = typeName[i];

                if (c == '[')
                {
                    depth++;
                    if (depth > 2)
                        currentArg.Append(c);
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 1)
                    {
                        var argTypeName = currentArg.ToString().Trim();
                        if (!string.IsNullOrEmpty(argTypeName))
                        {
                            var argType = GetType(argTypeName);
                            if (argType == null)
                                return null;
                            args.Add(argType);
                        }
                        currentArg.Clear();
                    }
                    else if (depth > 1)
                    {
                        currentArg.Append(c);
                    }
                    else if (depth == 0)
                    {
                        break;
                    }
                }
                else if (c == ',' && depth == 1)
                {
                }
                else if (depth > 1)
                {
                    currentArg.Append(c);
                }
            }

            return args.Count > 0 ? args.ToArray() : null;
        }

        private static Type[]? ParseGenericArguments(string assemblyPrefix, string typeName, int startIndex)
        {
            var args = new System.Collections.Generic.List<Type>();
            var depth = 0;
            var currentArg = new System.Text.StringBuilder();

            for (int i = startIndex; i < typeName.Length; i++)
            {
                var c = typeName[i];

                if (c == '[')
                {
                    depth++;
                    if (depth > 2)
                        currentArg.Append(c);
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 1)
                    {
                        var argTypeName = currentArg.ToString().Trim();
                        if (!string.IsNullOrEmpty(argTypeName))
                        {
                            var argType = GetType(argTypeName);
                            if (argType == null)
                                return null;
                            args.Add(argType);
                        }
                        currentArg.Clear();
                    }
                    else if (depth > 1)
                    {
                        currentArg.Append(c);
                    }
                    else if (depth == 0)
                    {
                        break;
                    }
                }
                else if (c == ',' && depth == 1)
                {
                }
                else if (depth > 1)
                {
                    currentArg.Append(c);
                }
            }

            return args.Count > 0 ? args.ToArray() : null;
        }

        private static Type? ResolveSimpleType(Assembly assembly, string name)
        {
            var type = assembly.GetType(name, throwOnError: false);
            if (type != null)
                return type;

            try
            {
                return AssemblyUtils.GetAssemblyTypes(assembly).FirstOrDefault(t =>
                    name == t.AssemblyQualifiedName ||
                    name == t.FullName ||
                    name == t.Name);
            }
            catch
            {
                return null;
            }
        }

        private static Type? TryResolveArrayType(Assembly assembly, string typeName)
        {
            if (!typeName.EndsWith("]"))
                return null;

            var lastOpenBracket = typeName.LastIndexOf('[');
            if (lastOpenBracket < 0)
                return null;

            var suffix = typeName.Substring(lastOpenBracket);
            var content = suffix.Substring(1, suffix.Length - 2);
            if (content.Length > 0 && content.Any(c => c != ','))
                return null;

            var commas = content.Length;
            var elementTypeName = typeName.Substring(0, lastOpenBracket);
            var elementType = GetType(assembly, elementTypeName);

            if (elementType == null) return null;

            return commas == 0
                ? elementType.MakeArrayType()
                : elementType.MakeArrayType(commas + 1);
        }

        private static Type? TryResolveCSharpGenericType(Assembly assembly, string typeName)
        {
            var openBracketIndex = typeName.IndexOf('<');
            if (openBracketIndex < 0)
                return null;

            var closeBracketIndex = FindMatchingCloseBracket(typeName, openBracketIndex);
            if (closeBracketIndex < 0)
                return null;

            var baseTypeName = typeName.Substring(0, openBracketIndex);
            if (string.IsNullOrWhiteSpace(baseTypeName))
                return null;

            var typeArgsString = typeName.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
            var typeArgNames = ParseCSharpGenericArguments(typeArgsString);
            if (typeArgNames == null || typeArgNames.Length == 0)
                return null;

            var genericDefName = $"{baseTypeName}`{typeArgNames.Length}";
            var genericDef = ResolveSimpleType(assembly, genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            var typeArgs = new Type[typeArgNames.Length];
            for (int i = 0; i < typeArgNames.Length; i++)
            {
                var argType = GetType(typeArgNames[i].Trim());
                if (argType == null)
                    return null;
                typeArgs[i] = argType;
            }

            Type? currentType;
            try
            {
                currentType = genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }

            var remaining = typeName.Substring(closeBracketIndex + 1);
            while (!string.IsNullOrEmpty(remaining))
            {
                if (!remaining.StartsWith("+") && !remaining.StartsWith("."))
                    return null;

                remaining = remaining.Substring(1);

                var open = remaining.IndexOf('<');
                string nestedName;
                Type[]? nestedArgs = null;
                int nextRemainingIndex;

                if (open > 0)
                {
                    var close = FindMatchingCloseBracket(remaining, open);
                    if (close < 0) return null;

                    nestedName = remaining.Substring(0, open);
                    var argsStr = remaining.Substring(open + 1, close - open - 1);
                    var argNames = ParseCSharpGenericArguments(argsStr);
                    if (argNames == null) return null;

                    nestedArgs = new Type[argNames.Length];
                    for (int i = 0; i < argNames.Length; i++)
                    {
                        var tempType = GetType(argNames[i]?.Trim());
                        if (tempType == null) return null;
                        nestedArgs[i] = tempType;
                    }

                    nextRemainingIndex = close + 1;
                }
                else
                {
                    var nextSep = remaining.IndexOfAny(NestedTypeSeparators);
                    if (nextSep > 0)
                    {
                        nestedName = remaining.Substring(0, nextSep);
                        nextRemainingIndex = nextSep;
                    }
                    else
                    {
                        nestedName = remaining;
                        nextRemainingIndex = remaining.Length;
                    }
                }

                Type? nestedType;
                Type[] allArgs;

                if (nestedArgs != null)
                {
                    nestedType = currentType.GetNestedType($"{nestedName}`{nestedArgs.Length}");
                    if (nestedType == null) return null;

                    if (currentType.IsGenericType && !currentType.IsGenericTypeDefinition)
                    {
                        var parentArgs = currentType.GetGenericArguments();
                        allArgs = new Type[parentArgs.Length + nestedArgs.Length];
                        Array.Copy(parentArgs, allArgs, parentArgs.Length);
                        Array.Copy(nestedArgs, 0, allArgs, parentArgs.Length, nestedArgs.Length);
                    }
                    else
                    {
                        allArgs = nestedArgs;
                    }
                }
                else
                {
                    nestedType = currentType.GetNestedType(nestedName);
                    if (nestedType == null) return null;

                    allArgs = currentType.IsGenericType && !currentType.IsGenericTypeDefinition
                        ? currentType.GetGenericArguments()
                        : Type.EmptyTypes;
                }

                if (nestedType.IsGenericTypeDefinition)
                {
                    try
                    {
                        currentType = nestedType.GetGenericArguments().Length == allArgs.Length
                            ? nestedType.MakeGenericType(allArgs)
                            : nestedType;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    currentType = nestedType;
                }

                remaining = remaining.Substring(nextRemainingIndex);
            }

            return currentType;
        }

        private static Type? TryResolveClassicGenericType(Assembly assembly, string typeName)
        {
            var backtickIndex = typeName.IndexOf('`');
            if (backtickIndex < 0)
                return null;

            var argsStart = typeName.IndexOf("[[", backtickIndex);
            if (argsStart < 0)
                return null;

            var genericDefName = typeName.Substring(0, argsStart);
            var genericDef = ResolveSimpleType(assembly, genericDefName);
            if (genericDef == null || !genericDef.IsGenericTypeDefinition)
                return null;

            var typeArgs = ParseGenericArguments(typeName, argsStart);
            if (typeArgs == null || typeArgs.Length != genericDef.GetGenericArguments().Length)
                return null;

            try
            {
                return genericDef.MakeGenericType(typeArgs);
            }
            catch
            {
                return null;
            }
        }
    }
}

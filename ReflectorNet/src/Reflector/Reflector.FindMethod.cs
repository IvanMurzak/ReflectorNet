using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        public static IEnumerable<MethodInfo> AllMethods => TypeUtils.AllTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.DeclaringType != null && !method.DeclaringType.IsAbstract);

        /// <summary>
        /// Compares two strings and returns a match score based on similarity.
        /// </summary>
        /// <param name="original">The original string to compare against.</param>
        /// <param name="value">The value string to compare with the original.</param>
        /// <returns>
        /// A match score:
        /// 6 for exact case-sensitive match,
        /// 5 for case-insensitive match,
        /// 4 for case-sensitive prefix match,
        /// 3 for case-insensitive prefix match,
        /// 2 for case-sensitive substring match,
        /// 1 for case-insensitive substring match,
        /// 0 for no match.
        /// </returns>
        static int Compare(string original, string value)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(value))
                return 0;

            if (original.Equals(value, StringComparison.OrdinalIgnoreCase))
                return original.Equals(value, StringComparison.Ordinal)
                    ? 6
                    : 5;

            if (original.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                return original.StartsWith(value)
                    ? 4
                    : 3;

            if (original.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                return original.Contains(value)
                    ? 2
                    : 1;

            return 0;
        }

        /// <summary>
        /// Compares method parameters with a list of parameter references and returns a match score.
        /// </summary>
        /// <param name="original">The original method parameters to compare against.</param>
        /// <param name="value">The list of parameter references to compare with the original parameters.</param>
        /// <returns>
        /// A match score:
        /// 2 for perfect match (including optional parameters handling),
        /// 1 for partial match (name or type mismatch),
        /// 0 for no match or incompatible parameters.
        /// </returns>
        static int Compare(ParameterInfo[] original, List<MethodRef.Parameter>? value)
        {
            if (original == null && value == null)
                return 2;

            if (original == null)
                return 0;

            if (value == null)
            {
                // If the method has no parameters and no parameters are specified, it's a match
                return original.Length == 0 ? 2 : 0;
            }

            // Check if we have fewer input parameters than method parameters
            if (value.Count < original.Length)
            {
                // Allow if the remaining parameters are optional (have default values)
                for (int i = value.Count; i < original.Length; i++)
                {
                    if (!original[i].IsOptional)
                        return 0; // Required parameter missing
                }
            }
            else if (original.Length != value.Count)
            {
                return 0; // Too many parameters provided
            }

            // Check the provided parameters
            for (int i = 0; i < value.Count && i < original.Length; i++)
            {
                var parameter = original[i];
                var methodRefParameter = value[i];

                if (parameter.Name != methodRefParameter.Name)
                    return 1;

                if (parameter.ParameterType.GetTypeName(pretty: false) != methodRefParameter.TypeName)
                    return 1;
            }

            return 2;
        }

        public IEnumerable<MethodInfo> FindMethod(
            MethodRef filter,
            bool knownNamespace = false,
            int typeNameMatchLevel = 1,
            int methodNameMatchLevel = 1,
            int parametersMatchLevel = 0,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        {
            // Prepare Namespace
            filter.Namespace = filter.Namespace?.Trim()?.Replace("null", string.Empty);

            var typesEnumerable = TypeUtils.AllTypes
                .Where(type => type.IsVisible)
                .Where(type => !type.IsInterface)
                .Where(type => !type.IsAbstract || type.IsSealed)
                .Where(type => !type.IsGenericTypeDefinition); // ignore generic types (class Foo<T>)

            if (knownNamespace)
                typesEnumerable = typesEnumerable.Where(type => type.Namespace == filter.Namespace);

            if (typeNameMatchLevel > 0 && !string.IsNullOrEmpty(filter.TypeName))
                typesEnumerable = typesEnumerable
                    .Select(type => new
                    {
                        Type = type,
                        MatchLevel = Compare(type.Name, filter.TypeName)
                    })
                    .Where(entry => entry.MatchLevel >= typeNameMatchLevel)
                    .OrderByDescending(entry => entry.MatchLevel)
                    .Select(entry => entry.Type);

            var types = typesEnumerable.ToList();

            var methodEnumerable = types
                .SelectMany(type => type.GetMethods(bindingFlags)
                    // Is declared in the class
                    .Where(method => method.DeclaringType == type))
                .Where(method => method.DeclaringType != null)
                .Where(method => !method.DeclaringType!.IsAbstract || method.DeclaringType.IsSealed) // ignore abstract non static classes
                .Where(method => !method.IsGenericMethodDefinition); // ignore generic methods (void Foo<T>)

            if (methodNameMatchLevel > 0 && !string.IsNullOrEmpty(filter.MethodName))
                methodEnumerable = methodEnumerable
                    .Select(method => new
                    {
                        Method = method,
                        MatchLevel = Compare(method.Name, filter.MethodName)
                    })
                    .Where(entry => entry.MatchLevel >= methodNameMatchLevel)
                    .OrderByDescending(entry => entry.MatchLevel)
                    .Select(entry => entry.Method);
            if (parametersMatchLevel > 0)
                methodEnumerable = methodEnumerable
                    .Select(method => new
                    {
                        Method = method,
                        MatchLevel = Compare(method.GetParameters(), filter.InputParameters)
                    })
                    .Where(entry => entry.MatchLevel >= parametersMatchLevel)
                    .OrderByDescending(entry => entry.MatchLevel)
                    .Select(entry => entry.Method);

            return methodEnumerable;
        }
    }
}

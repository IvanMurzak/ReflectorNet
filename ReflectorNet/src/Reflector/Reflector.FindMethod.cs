using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        static IEnumerable<Type> AllTypes => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes());

        static IEnumerable<MethodInfo> AllMethods => AllTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Where(method => method.DeclaringType != null && !method.DeclaringType.IsAbstract);

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

        static int Compare(ParameterInfo[] original, List<MethodPointerRef.Parameter>? value)
        {
            if (original == null && value == null)
                return 2;

            if (original == null || value == null)
                return 0;

            if (original.Length != value.Count)
                return 0;

            for (int i = 0; i < original.Length; i++)
            {
                var parameter = original[i];
                var methodRefParameter = value[i];

                if (parameter.Name != methodRefParameter.Name)
                    return 1;

                if (parameter.ParameterType.FullName != methodRefParameter.TypeName)
                    return 1;
            }

            return 2;
        }

        public IEnumerable<MethodInfo> FindMethod(
            MethodPointerRef filter,
            bool knownNamespace = false,
            int typeNameMatchLevel = 1,
            int methodNameMatchLevel = 1,
            int parametersMatchLevel = 0,
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
        {
            // Prepare Namespace
            filter.Namespace = filter.Namespace?.Trim()?.Replace("null", string.Empty);

            var typesEnumerable = AllTypes
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
                .Where(method => !method.DeclaringType.IsAbstract || method.DeclaringType.IsSealed) // ignore abstract non static classes
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

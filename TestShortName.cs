using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Utils;

namespace TestNamespace
{
    public class Outer<T>
    {
        public class Nested { }
    }
}

public class Program
{
    public static void Main()
    {
        var type = typeof(TestNamespace.Outer<int>.Nested);
        Console.WriteLine($"Type: {type.FullName}");
        Console.WriteLine($"Type.Name: {type.Name}");
        Console.WriteLine($"IsGenericType: {type.IsGenericType}");
        Console.WriteLine($"GenericArgs: {string.Join(", ", type.GetGenericArguments().Select(t => t.Name))}");

        // Simulate GetTypeShortName logic
        Console.WriteLine($"ShortName: {TypeUtils.GetTypeShortName(type)}");
    }
}

using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    class TestTypeId
    {
        static void Main(string[] args)
        {
            // Test simple array
            var intArrayType = typeof(int[]);
            Console.WriteLine($"int[] -> {JsonUtils.Schema.GetTypeId(intArrayType)}");

            // Test nested array
            var intDoubleArrayType = typeof(int[][]);
            Console.WriteLine($"int[][] -> {JsonUtils.Schema.GetTypeId(intDoubleArrayType)}");

            // Test triple nested array
            var intTripleArrayType = typeof(int[][][]);
            Console.WriteLine($"int[][][] -> {JsonUtils.Schema.GetTypeId(intTripleArrayType)}");

            // Test List<T>
            var listIntType = typeof(List<int>);
            Console.WriteLine($"List<int> -> {JsonUtils.Schema.GetTypeId(listIntType)}");

            // Test List<int[]>
            var listIntArrayType = typeof(List<int[]>);
            Console.WriteLine($"List<int[]> -> {JsonUtils.Schema.GetTypeId(listIntArrayType)}");

            // Test int[] (List<T> implements IEnumerable<T>, but arrays are handled separately)
            var intArrayInListType = typeof(List<int[]>);
            Console.WriteLine($"List<int[]> -> {JsonUtils.Schema.GetTypeId(intArrayInListType)}");

            // Test List<List<int>>
            var listListIntType = typeof(List<List<int>>);
            Console.WriteLine($"List<List<int>> -> {JsonUtils.Schema.GetTypeId(listListIntType)}");

            // Test complex case: List<int[][]>
            var listIntDoubleArrayType = typeof(List<int[][]>);
            Console.WriteLine($"List<int[][]> -> {JsonUtils.Schema.GetTypeId(listIntDoubleArrayType)}");

            // Test string array (should work for any type)
            var stringArrayType = typeof(string[]);
            Console.WriteLine($"string[] -> {JsonUtils.Schema.GetTypeId(stringArrayType)}");

            // Test List<string[]>
            var listStringArrayType = typeof(List<string[]>);
            Console.WriteLine($"List<string[]> -> {JsonUtils.Schema.GetTypeId(listStringArrayType)}");
        }
    }
}

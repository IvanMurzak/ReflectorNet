/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class DictionaryConverterTests : BaseTest
    {
        public DictionaryConverterTests(ITestOutputHelper output) : base(output) { }

        void BackAndForthTest<T>(T sourceInstance)
        {
            // Arrange
            var reflector = new Reflector();
            var sourceType = typeof(T);

            // Act
            var sourceJson = reflector.JsonSerializer.Serialize(sourceInstance);
            _output.WriteLine($"Source {sourceType.GetTypeShortName()}: {sourceJson}");
            _output.WriteLine("------------------------------------------------------");

            var parsedInstance = reflector.JsonSerializer.Deserialize<T>(sourceJson);
            var parsedJson = reflector.JsonSerializer.Serialize(parsedInstance);
            _output.WriteLine($"Parsed {sourceType.GetTypeShortName()}: {parsedJson}");

            // Assert
            Assert.Equal(sourceJson, parsedJson);
        }

        [Fact]
        public void DictionaryConverter_StringInt_Dictionary()
        {
            BackAndForthTest(new Dictionary<string, int>
            {
                { "one", 1 },
                { "two", 2 },
                { "three", 3 }
            });
        }

        [Fact]
        public void DictionaryConverter_StringString_Dictionary()
        {
            BackAndForthTest(new Dictionary<string, string>
            {
                { "name", "John" },
                { "city", "New York" },
                { "country", "USA" }
            });
        }

        [Fact]
        public void DictionaryConverter_IntString_Dictionary()
        {
            BackAndForthTest(new Dictionary<int, string>
            {
                { 1, "one" },
                { 2, "two" },
                { 3, "three" }
            });
        }

        [Fact]
        public void DictionaryConverter_StringObject_Dictionary()
        {
            BackAndForthTest(new Dictionary<string, object>
            {
                { "number", 42 },
                { "text", "hello" },
                { "flag", true }
            });
        }

        [Fact]
        public void DictionaryConverter_StringListInt_Dictionary()
        {
            BackAndForthTest(new Dictionary<string, List<int>>
            {
                { "primes", new List<int> { 2, 3, 5, 7, 11 } },
                { "evens", new List<int> { 2, 4, 6, 8, 10 } },
                { "odds", new List<int> { 1, 3, 5, 7, 9 } }
            });
        }

        [Fact]
        public void DictionaryConverter_Empty_Dictionary()
        {
            BackAndForthTest(new Dictionary<string, int>());
        }

        [Fact]
        public void DictionaryConverter_Null_Dictionary()
        {
            // Arrange
            var reflector = new Reflector();
            Dictionary<string, int>? sourceInstance = null;

            // Act
            var sourceJson = reflector.JsonSerializer.Serialize(sourceInstance);
            _output.WriteLine($"Source: {sourceJson}");

            var parsedInstance = reflector.JsonSerializer.Deserialize<Dictionary<string, int>?>(sourceJson);
            _output.WriteLine($"Parsed: {parsedInstance}");

            // Assert
            Assert.Null(parsedInstance);
        }

        [Fact]
        public void DictionaryConverter_NestedDictionary()
        {
            BackAndForthTest(new Dictionary<string, Dictionary<string, int>>
            {
                {
                    "first",
                    new Dictionary<string, int>
                    {
                        { "a", 1 },
                        { "b", 2 }
                    }
                },
                {
                    "second",
                    new Dictionary<string, int>
                    {
                        { "x", 10 },
                        { "y", 20 }
                    }
                }
            });
        }
    }
}

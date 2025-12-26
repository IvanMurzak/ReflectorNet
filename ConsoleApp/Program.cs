/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using System.ComponentModel;
using System.Reflection;

Console.WriteLine($"Do nothing");

var reflector = new Reflector();
var methodInfo = typeof(Sample).GetMethod(nameof(Sample.Command));

var argumentsSchema = reflector.GetArgumentsSchema(methodInfo!);
var outputSchema = reflector.GetReturnSchema(methodInfo!);

Console.WriteLine("Arguments Schema:");
Console.WriteLine(argumentsSchema.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Output Schema:");
Console.WriteLine(outputSchema?.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

var schemaJson = reflector.GetSchema<GameState<PlayerProfile>>();

Console.WriteLine("Generic Schema:");
Console.WriteLine(schemaJson.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

public static class Sample
{
    // [McpServerTool]
    public static Result Command(List<Data> dataList, EnumFlag flag, Data? optionalData = null)
    {
        return new Result();
    }
}


public class Data
{
    public int? optionalInt;
    [Description("THIS IS THE DEMO DESCRIPTION.")]
    public string address = null!;
    public List<Data> list = null!; // recursive
}

public struct Result
{
    public bool isDone;
    public string? errorMessage;
}

[Description("DEMO ENUM DESCRIPTION.")]
public enum EnumFlag { red, green, yellow }







// A simple value type
public struct GeoPoint
{
    public double Latitude;
    public double Longitude;
}

// A complex entity
public class PlayerProfile
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string[] Badges { get; set; } = null!; // Array
    public Dictionary<string, int> Stats { get; set; } = null!; // Dictionary
}

// A generic wrapper (common in Game State or API responses)
public class GameState<T>
{
    public long Timestamp { get; set; }
    public T Player { get; set; } = default!;
    public List<GeoPoint> Checkpoints { get; set; } = null!; // List of Structs
}
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using ReflectorNet.Tests;
using ReflectorNet.Tests.Schema.Model;
using System.Reflection;

Console.WriteLine("=== TESTING ENHANCE INPUT PARAMETERS ===");

var reflector = new Reflector();

// Create the same test data as in the failing test
var gameObjectRef = new GameObjectRef { instanceID = 123, name = "TestObject" };
var inputParameters = new SerializedMemberList
{
    reflector.Serialize(gameObjectRef, name: "obj"),
    reflector.Serialize(0, name: "integer"),
    reflector.Serialize(false, name: "boolean")
};

Console.WriteLine($"Input parameters created: {inputParameters.Count}");
foreach (var param in inputParameters)
{
    Console.WriteLine($"  {param.name}: {param.typeName}");
}

// Create the filter as in the failing test
var filter = new MethodPointerRef
{
    Namespace = typeof(MethodHelper).Namespace,
    TypeName = nameof(MethodHelper),
    MethodName = nameof(MethodHelper.Object_Int_Bool)
};

Console.WriteLine($"\nFilter before enhancement:");
Console.WriteLine($"  InputParameters: {filter.InputParameters?.Count ?? 0}");

// Test the enhancement as done in MethodCall
if ((filter.InputParameters?.Count ?? 0) == 0 && (inputParameters?.Count ?? 0) > 0)
{
    Console.WriteLine($"Enhancing filter with input parameters...");
    filter.EnhanceInputParameters(inputParameters);
}

Console.WriteLine($"\nFilter after enhancement:");
Console.WriteLine($"  InputParameters: {filter.InputParameters?.Count ?? 0}");
if (filter.InputParameters != null)
{
    foreach (var param in filter.InputParameters)
    {
        Console.WriteLine($"    {param.Name}: {param.TypeName}");
    }
}

// Now test parameter comparison with enhanced filter
var methodHelperType = typeof(MethodHelper);
var actualMethod = methodHelperType.GetMethod("Object_Int_Bool");

if (actualMethod != null)
{
    var reflectorType = typeof(Reflector);
    var paramCompareMethod = reflectorType.GetMethod("Compare", BindingFlags.Static | BindingFlags.NonPublic, null,
        new[] { typeof(ParameterInfo[]), typeof(List<MethodPointerRef.Parameter>) }, null);

    if (paramCompareMethod != null)
    {
        var paramResult = (int)paramCompareMethod.Invoke(null, new object[] { actualMethod.GetParameters(), filter.InputParameters });
        Console.WriteLine($"\nParameter comparison after enhancement: {paramResult}");
        Console.WriteLine($"Should pass parametersMatchLevel 2: {paramResult >= 2}");
    }
}

// Test FindMethod with enhanced filter
Console.WriteLine($"\n=== TESTING FINDMETHOD WITH ENHANCED FILTER ===");
var methods = reflector.FindMethod(
    filter: filter,
    knownNamespace: true,
    typeNameMatchLevel: 6,
    methodNameMatchLevel: 6,
    parametersMatchLevel: 2
).ToList();

Console.WriteLine($"Found {methods.Count} methods with enhanced filter");

// Test the full MethodCall as in the failing test
Console.WriteLine($"\n=== TESTING FULL METHOD CALL ===");
var result = reflector.MethodCall(
    reflector: reflector,
    filter: new MethodPointerRef
    {
        Namespace = typeof(MethodHelper).Namespace,
        TypeName = nameof(MethodHelper),
        MethodName = nameof(MethodHelper.Object_Int_Bool)
    },
    knownNamespace: true,
    typeNameMatchLevel: 6,
    methodNameMatchLevel: 6,
    inputParameters: inputParameters,
    executeInMainThread: false
);

Console.WriteLine($"MethodCall result: {result}");
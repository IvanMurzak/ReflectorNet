using System.Reflection;
using com.IvanMurzak.ReflectorNet.Common.MCP;
using com.IvanMurzak.ReflectorNet.Common.Reflection;
using Microsoft.Extensions.Logging;

public static class Tester
{
    static async Task MethodCall(Reflector reflector, ILogger? logger, MethodInfo methodInfo)
    {
        var methodWrapper = MethodWrapper.Create(reflector, logger, methodInfo);

        logger?.LogInformation($"Method: {methodInfo.Name}");
        logger?.LogInformation($"Description: {methodWrapper.Description}");

        var inputSchema = methodWrapper.InputSchema;

        if (inputSchema != null)
            logger?.LogInformation($"Input schema: {inputSchema}");

        var result = await methodWrapper.Invoke();
        logger?.LogInformation($"Result: {result}");
        logger?.LogInformation("----------------------------------------");
    }
    public static async Task Test(ILogger? logger = null)
    {
        var reflector = new Reflector();

        await MethodCall(reflector, logger, typeof(TestMethod).GetMethod(nameof(TestMethod.NoParameters_ReturnVoid))!);
        await MethodCall(reflector, logger, typeof(TestMethod).GetMethod(nameof(TestMethod.NoParameters_ReturnBool))!);
    }
}
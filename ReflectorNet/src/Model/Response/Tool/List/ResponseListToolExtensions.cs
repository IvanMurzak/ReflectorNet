using System;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public static class ResponseListToolExtensions
    {
        public static ResponseListTool[] Log(this ResponseListTool[] response, ILogger logger, Exception? ex = null)
        {
            if (!logger.IsEnabled(LogLevel.Information))
                return response;

            foreach (var item in response)
                logger.LogInformation(ex, $"{item.Name}\n{JsonUtils.ToJson(item)}");

            return response;
        }

        public static IResponseData<ResponseListTool[]> Pack(this ResponseListTool[] response, string requestId, string? message = null)
            => ResponseData<ResponseListTool[]>.Success(requestId, message ?? "List Tool execution completed.")
                .SetData(response);
    }
}
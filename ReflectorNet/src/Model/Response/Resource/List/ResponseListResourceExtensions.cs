using System;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public static class ResponseListResourceExtensions
    {
        public static IResponseListResource[] Log(this ResponseListResource[] target, ILogger logger, Exception? ex = null)
        {
            if (!logger.IsEnabled(LogLevel.Information))
                return target;

            foreach (var item in target)
                logger.LogInformation(ex, "Resource: {0}", item.uri);

            return target;
        }

        public static IResponseData<ResponseListResource[]> Pack(this ResponseListResource[] target, string requestId, string? message = null)
            => ResponseData<ResponseListResource[]>.Success(requestId, message ?? "List Tool execution completed.")
                .SetData(target);
    }
}
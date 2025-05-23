#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Common.Data
{
    public static class IResponseResourceTemplateExtensions
    {
        public static ResponseResourceTemplate[] Log(this ResponseResourceTemplate[] target, ILogger logger, Exception? ex = null)
        {
            if (!logger.IsEnabled(LogLevel.Information))
                return target;

            foreach (var item in target)
                logger.LogInformation(ex, "Resource Template: {0}", item.uriTemplate);

            return target;
        }

        public static IResponseData<ResponseResourceTemplate[]> Pack(this ResponseResourceTemplate[] target, string requestId, string? message = null)
            => ResponseData<ResponseResourceTemplate[]>.Success(requestId, message ?? "List Tool execution completed.")
                .SetData(target);
    }
}
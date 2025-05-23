#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Data
{
    public static class ResponseDataExtension
    {
        public static IResponseData<T> Log<T>(this IResponseData<T> response, ILogger logger, Exception? ex = null)
        {
            if (response.IsError)
                logger.LogError(ex, response.Message ?? "Execution failed.");
            else
                logger.LogInformation(ex, response.Message ?? "Executed successfully.");

            return response;
        }
        public static IResponseData<T> SetData<T>(this IResponseData<T> response, T? data)
        {
            response.Value = data;
            return response;
        }
        public static IResponseData<T> SetError<T>(this IResponseData<T> response, string? message = null)
        {
            response.IsError = true;
            response.Message = message ?? "Execution failed.";
            return response;
        }
        public static IResponseData<T> SetSuccess<T>(this IResponseData<T> response, string? message = null)
        {
            response.IsError = false;
            response.Message = message ?? "Executed successfully.";
            return response;
        }

        public static Task<IResponseData<T>> TaskFromResult<T>(this IResponseData<T> response)
            => Task.FromResult(response);
    }
}
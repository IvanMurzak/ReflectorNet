
using System;

namespace com.IvanMurzak.ReflectorNet.Model
{
    public class ResponseData<T> : IResponseData<T>
    {
        public string RequestID { get; set; } = string.Empty;
        public bool IsError { get; set; }
        public string? Message { get; set; }
        public T? Value { get; set; }

        public ResponseData() { }
        public ResponseData(string requestId, bool isError)
        {
            RequestID = requestId ?? throw new ArgumentNullException(nameof(requestId));
            IsError = isError;
        }

        public static ResponseData<T> Success(string requestId, string? message = null) => new ResponseData<T>(requestId, isError: false)
        {
            Message = message
        };
        public static ResponseData<T> Error(string requestId, string? message = null) => new ResponseData<T>(requestId, isError: true)
        {
            Message = message
        };
    }
}
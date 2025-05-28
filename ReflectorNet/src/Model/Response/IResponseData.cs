namespace com.IvanMurzak.ReflectorNet.Data
{
    public interface IResponseData<T> : IRequestID
    {
        bool IsError { get; set; }
        string? Message { get; set; }
        T? Value { get; set; }
    }
}
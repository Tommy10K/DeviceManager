namespace DeviceManager.Application.Exceptions;

public sealed class ExternalServiceException : Exception
{
    public int StatusCode { get; }

    public string ErrorTitle { get; }

    public ExternalServiceException(int statusCode, string errorTitle, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorTitle = errorTitle;
    }
}

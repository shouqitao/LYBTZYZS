using System;

namespace LYBT.Desktop.Models.Exceptions
{
    /// <summary>
    /// API 调用异常
    /// </summary>
    public class ApiCallException : Exception
    {
        public string? ErrorCode { get; }
        public int? StatusCode { get; set; }
        public string? OperationName { get; set; }
        public int? AttemptNumber { get; set; }

        public ApiCallException() : base()
        {
        }

        public ApiCallException(string message) : base(message)
        {
        }

        public ApiCallException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ApiCallException(string message, string? errorCode, int? statusCode = null) : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}
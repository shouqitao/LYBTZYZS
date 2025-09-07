using System.Net;

namespace LYBT.Desktop.Core.Exceptions
{

    /// <summary>
    /// API调用异常
    /// </summary>
    public class ApiCallException : Exception
    {

        /// <summary>
        /// 操作名称
        /// </summary>
        public string OperationName { get; set; } = string.Empty;

        /// <summary>
        /// 尝试次数
        /// </summary>
        public int AttemptNumber { get; set; } = 1;

        /// <summary>
        /// HTTP状态码
        /// </summary>
        public HttpStatusCode? StatusCode { get; set; }

        /// <summary>
        /// 请求URL
        /// </summary>
        public string? RequestUrl { get; set; }

        /// <summary>
        /// 响应内容
        /// </summary>
        public string? ResponseContent { get; set; }

        public ApiCallException()
        {
        }

        public ApiCallException(string message) : base(message)
        {
        }

        public ApiCallException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ApiCallException(string operationName, string message) : base(message)
        {
            OperationName = operationName;
        }

        public ApiCallException(string operationName, HttpStatusCode statusCode, string message) : base(message)
        {
            OperationName = operationName;
            StatusCode = statusCode;
        }

        public ApiCallException(string operationName, HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            OperationName = operationName;
            StatusCode = statusCode;
        }
    }
}

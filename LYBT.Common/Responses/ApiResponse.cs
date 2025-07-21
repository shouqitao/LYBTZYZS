// LYBT.Common/Responses/ApiResponse.cs
namespace LYBT.Common.Responses {

    /// <summary>
    /// 接口统一响应体
    /// </summary>
    public class ApiResponse<T> {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Success(T data, string message = "Success")
            => new() { IsSuccess = true, Data = data, Message = message, StatusCode = 200 };

        public static ApiResponse<T> Fail(string message, int statusCode = 400)
            => new() { IsSuccess = false, Message = message, StatusCode = statusCode };
    }
}
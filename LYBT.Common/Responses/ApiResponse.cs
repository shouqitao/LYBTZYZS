// LYBT.Common/Responses/ApiResponse.cs
namespace LYBT.Common.Responses {

    /// <summary>
    /// 接口统一响应体
    /// </summary>
    public class ApiResponse<T> {
/// <summary>
/// IsSuccess 属性。
/// </summary>
        public bool IsSuccess { get; set; }
/// <summary>
/// Message 属性。
/// </summary>
        public string Message { get; set; } = string.Empty;
/// <summary>
/// Data 属性。
/// </summary>
        public T? Data { get; set; }
/// <summary>
/// StatusCode 属性。
/// </summary>
        public int StatusCode { get; set; }
/// <summary>
/// Timestamp 属性。
/// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

/// <summary>
/// 执行Success操作。
/// </summary>
/// <param name="data">参数data</param>
/// <param name=""Success"">参数"Success"</param>
/// <returns>返回值</returns>
        public static ApiResponse<T> Success(T data, string message = "Success")
            => new() { IsSuccess = true, Data = data, Message = message, StatusCode = 200 };

/// <summary>
/// 执行Fail操作。
/// </summary>
/// <param name="message">参数message</param>
/// <param name="400">参数400</param>
/// <returns>返回值</returns>
        public static ApiResponse<T> Fail(string message, int statusCode = 400)
            => new() { IsSuccess = false, Message = message, StatusCode = statusCode };
    }
}

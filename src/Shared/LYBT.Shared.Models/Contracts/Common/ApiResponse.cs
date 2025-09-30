using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Contracts.Common
{

    /// <summary>
    /// 统一API响应格式 - Shared层版本
    /// </summary>
    public class ApiResponse<T>
    {

        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 返回消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 返回数据
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>
        /// 错误详情
        /// </summary>
        [JsonPropertyName("errors")]
        public object? Errors { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// 请求ID（用于链路追踪）
        /// </summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse<T> CreateSuccess(T? data = default, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static ApiResponse<T> CreateFail(string message = "操作失败", object? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// 非泛型版本的ApiResponse
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static new ApiResponse CreateSuccess(object? data = null, string message = "操作成功")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                Data = data
            };
        }
    }
}

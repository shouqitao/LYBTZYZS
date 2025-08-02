using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Common {

    /// <summary>
    /// API统一返回格式 - 前后端共享
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class ApiResponse<T> {

        /// <summary>是否成功</summary>
        [DisplayName("是否成功")]
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        /// <summary>状态码</summary>
        [DisplayName("状态码")]
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>消息</summary>
        [DisplayName("消息")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>数据</summary>
        [DisplayName("数据")]
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>时间戳</summary>
        [DisplayName("时间戳")]
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>错误代码（可选）</summary>
        [DisplayName("错误代码")]
        [JsonPropertyName("errorCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>追踪ID（用于日志关联）</summary>
        [DisplayName("追踪ID")]
        [JsonPropertyName("traceId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; set; }

        /// <summary>创建成功响应</summary>
        public static ApiResponse<T> Success(T data, string message = "操作成功") {
            return new ApiResponse<T> {
                IsSuccess = true,
                StatusCode = 200,
                Message = message,
                Data = data
            };
        }

        /// <summary>创建成功响应（无数据）</summary>
        public static ApiResponse<object> Success(string message = "操作成功") {
            return new ApiResponse<object> {
                IsSuccess = true,
                StatusCode = 200,
                Message = message
            };
        }

        /// <summary>创建失败响应</summary>
        public static ApiResponse<T> Fail(string message, int statusCode = 400, string? errorCode = null) {
            return new ApiResponse<T> {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = message,
                Data = default(T),
                ErrorCode = errorCode
            };
        }

        /// <summary>创建验证失败响应</summary>
        public static ApiResponse<T> ValidationError(string message, Dictionary<string, string[]>? validationErrors = null) {
            return new ApiResponse<T> {
                IsSuccess = false,
                StatusCode = 422,
                Message = message,
                ErrorCode = "VALIDATION_ERROR",
                Data = validationErrors != null ? (T)(object)validationErrors : default
            };
        }

        /// <summary>创建未授权响应</summary>
        public static ApiResponse<T> Unauthorized(string message = "未授权访问") {
            return new ApiResponse<T> {
                IsSuccess = false,
                StatusCode = 401,
                Message = message,
                ErrorCode = "UNAUTHORIZED"
            };
        }

        /// <summary>创建禁止访问响应</summary>
        public static ApiResponse<T> Forbidden(string message = "禁止访问") {
            return new ApiResponse<T> {
                IsSuccess = false,
                StatusCode = 403,
                Message = message,
                ErrorCode = "FORBIDDEN"
            };
        }

        /// <summary>创建资源未找到响应</summary>
        public static ApiResponse<T> NotFound(string message = "资源未找到") {
            return new ApiResponse<T> {
                IsSuccess = false,
                StatusCode = 404,
                Message = message,
                ErrorCode = "NOT_FOUND"
            };
        }

        /// <summary>创建服务器错误响应</summary>
        public static ApiResponse<T> ServerError(string message = "服务器内部错误") {
            return new ApiResponse<T> {
                IsSuccess = false,
                StatusCode = 500,
                Message = message,
                ErrorCode = "INTERNAL_ERROR"
            };
        }

        /// <summary>设置追踪ID</summary>
        public ApiResponse<T> WithTraceId(string traceId) {
            TraceId = traceId;
            return this;
        }
    }

    /// <summary>
    /// 非泛型版本的API响应
    /// </summary>
    public class ApiResponse : ApiResponse<object> {

        /// <summary>创建成功响应</summary>
        public new static ApiResponse Success(string message = "操作成功") {
            return new ApiResponse {
                IsSuccess = true,
                StatusCode = 200,
                Message = message
            };
        }

        /// <summary>创建失败响应</summary>
        public new static ApiResponse Fail(string message, int statusCode = 400, string? errorCode = null) {
            return new ApiResponse {
                IsSuccess = false,
                StatusCode = statusCode,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }
}
using System.Text.Json.Serialization;

namespace LYBT.Common.Responses {

    /// <summary>
    /// 接口统一响应体 - 优化版本
    /// </summary>
    public class ApiResponse<T> {

        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 响应数据
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 错误代码（可选）
        /// </summary>
        [JsonPropertyName("errorCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 追踪ID（用于日志关联）
        /// </summary>
        [JsonPropertyName("traceId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse<T> Success(T data, string message = "操作成功") {
            return new ApiResponse<T> {
                IsSuccess = true,
                Data = data,
                Message = message,
                StatusCode = 200
            };
        }

        /// <summary>
        /// 创建成功响应（无数据）
        /// </summary>
        public static ApiResponse<object> Success(string message = "操作成功") {
            return new ApiResponse<object> {
                IsSuccess = true,
                Message = message,
                StatusCode = 200
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static ApiResponse<T> Fail(string message, int statusCode = 400, string? errorCode = null) {
            return new ApiResponse<T> {
                IsSuccess = false,
                Message = message,
                StatusCode = statusCode,
                ErrorCode = errorCode
            };
        }

        /// <summary>
        /// 创建验证失败响应
        /// </summary>
        public static ApiResponse<T> ValidationError(string message, Dictionary<string, string[]>? validationErrors = null) {
            return new ApiResponse<T> {
                IsSuccess = false,
                Message = message,
                StatusCode = 422,
                ErrorCode = "VALIDATION_ERROR",
                Data = validationErrors != null ? (T)(object)validationErrors : default
            };
        }

        /// <summary>
        /// 创建未授权响应
        /// </summary>
        public static ApiResponse<T> Unauthorized(string message = "未授权访问") {
            return new ApiResponse<T> {
                IsSuccess = false,
                Message = message,
                StatusCode = 401,
                ErrorCode = "UNAUTHORIZED"
            };
        }

        /// <summary>
        /// 创建禁止访问响应
        /// </summary>
        public static ApiResponse<T> Forbidden(string message = "禁止访问") {
            return new ApiResponse<T> {
                IsSuccess = false,
                Message = message,
                StatusCode = 403,
                ErrorCode = "FORBIDDEN"
            };
        }

        /// <summary>
        /// 创建资源未找到响应
        /// </summary>
        public static ApiResponse<T> NotFound(string message = "资源未找到") {
            return new ApiResponse<T> {
                IsSuccess = false,
                Message = message,
                StatusCode = 404,
                ErrorCode = "NOT_FOUND"
            };
        }

        /// <summary>
        /// 创建服务器错误响应
        /// </summary>
        public static ApiResponse<T> ServerError(string message = "服务器内部错误") {
            return new ApiResponse<T> {
                IsSuccess = false,
                Message = message,
                StatusCode = 500,
                ErrorCode = "INTERNAL_ERROR"
            };
        }

        /// <summary>
        /// 设置追踪ID
        /// </summary>
        public ApiResponse<T> WithTraceId(string traceId) {
            TraceId = traceId;
            return this;
        }
    }

    /// <summary>
    /// 非泛型版本的API响应
    /// </summary>
    public class ApiResponse : ApiResponse<object> {

        public new static ApiResponse Success(string message = "操作成功") {
            return new ApiResponse {
                IsSuccess = true,
                Message = message,
                StatusCode = 200
            };
        }

        public new static ApiResponse Fail(string message, int statusCode = 400, string? errorCode = null) {
            return new ApiResponse {
                IsSuccess = false,
                Message = message,
                StatusCode = statusCode,
                ErrorCode = errorCode
            };
        }
    }
}
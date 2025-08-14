using System.Text.Json.Serialization;

namespace LYBT.Infrastructure.Web {

    /// <summary>
    /// 统一API响应格式 - 前后端契约标准化
    /// 所有API都应使用此格式包装响应数据
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    public class ApiResponse<T> {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误代码（可选）
        /// </summary>
        [JsonPropertyName("errorCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <summary>
        /// 请求ID（用于链路追踪）
        /// </summary>
        [JsonPropertyName("requestId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RequestId { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        public static ApiResponse<T> Ok(T data, string message = "操作成功") {
            return new ApiResponse<T> {
                Success = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        public static ApiResponse<T> Fail(string message, string? errorCode = null) {
            return new ApiResponse<T> {
                Success = false,
                Data = default,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// 无数据的API响应格式
    /// </summary>
    public class ApiResponse : ApiResponse<object> {
        /// <summary>
        /// 创建成功响应（无数据）
        /// </summary>
        public static ApiResponse Ok(string message = "操作成功") {
            return new ApiResponse {
                Success = true,
                Data = null,
                Message = message
            };
        }

        /// <summary>
        /// 创建失败响应（无数据）
        /// </summary>
        public static new ApiResponse Fail(string message, string? errorCode = null) {
            return new ApiResponse {
                Success = false,
                Data = null,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// 分页响应格式
    /// </summary>
    public class PagedApiResponse<T> : ApiResponse<PagedData<T>> {
        /// <summary>
        /// 创建分页成功响应
        /// </summary>
        public static PagedApiResponse<T> Ok(IList<T> items, long totalCount, int currentPage, int pageSize, string message = "查询成功") {
            return new PagedApiResponse<T> {
                Success = true,
                Data = new PagedData<T> {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = currentPage,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                },
                Message = message
            };
        }
    }

    /// <summary>
    /// 分页数据格式
    /// </summary>
    public class PagedData<T> {
        /// <summary>
        /// 数据项
        /// </summary>
        [JsonPropertyName("items")]
        public IList<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// 总记录数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public long TotalCount { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        /// <summary>
        /// 页大小
        /// </summary>
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        /// <summary>
        /// 是否有下一页
        /// </summary>
        [JsonPropertyName("hasNext")]
        public bool HasNext => CurrentPage < TotalPages;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious => CurrentPage > 1;
    }
}
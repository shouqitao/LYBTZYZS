using System.Text.Json.Serialization;
using LYBT.Shared.Models.Common;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 分页API响应模型 - 统一分页响应格式
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public class PagedApiResponse<T>
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 分页数据
        /// </summary>
        [JsonPropertyName("data")]
        public PagedData<T> Data { get; set; } = new PagedData<T>();

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonPropertyName("errors")]
        public object? Errors { get; set; }

        /// <summary>
        /// 时间戳（Unix时间戳）
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// 请求ID（用于链路追踪）
        /// </summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// 错误代码
        /// </summary>
        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 创建成功的分页响应
        /// </summary>
        /// <param name="items">数据项列表</param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="message">响应消息</param>
        /// <returns>成功的分页响应</returns>
        public static PagedApiResponse<T> Ok(
            IList<T> items, 
            int totalCount, 
            int currentPage, 
            int pageSize, 
            string message = "操作成功")
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            return new PagedApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = new PagedData<T>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = currentPage,
                    PageSize = pageSize,
                    TotalPages = totalPages
                },
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 创建成功的分页响应（从PaginatedResult转换）
        /// </summary>
        /// <param name="pagedResult">分页结果</param>
        /// <param name="message">响应消息</param>
        /// <returns>成功的分页响应</returns>
        public static PagedApiResponse<T> Ok(PaginatedResult<T> pagedResult, string message = "操作成功")
        {
            return Ok(
                pagedResult.Items,
                pagedResult.TotalCount,
                pagedResult.CurrentPage,
                pagedResult.PageSize,
                message
            );
        }

        /// <summary>
        /// 创建失败的分页响应
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="errors">详细错误信息</param>
        /// <returns>失败的分页响应</returns>
        public static PagedApiResponse<T> Fail(
            string message = "操作失败", 
            string? errorCode = null, 
            object? errors = null)
        {
            return new PagedApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Errors = errors,
                Data = new PagedData<T>
                {
                    Items = new List<T>(),
                    TotalCount = 0,
                    CurrentPage = 1,
                    PageSize = 10,
                    TotalPages = 0
                },
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// 创建空数据的成功响应
        /// </summary>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="message">响应消息</param>
        /// <returns>空数据的成功响应</returns>
        public static PagedApiResponse<T> Empty(
            int currentPage = 1, 
            int pageSize = 10, 
            string message = "查询成功，暂无数据")
        {
            return Ok(new List<T>(), 0, currentPage, pageSize, message);
        }
    }
}
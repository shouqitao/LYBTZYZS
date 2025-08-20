using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 分页数据模型 - 统一分页数据格式
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public class PagedData<T>
    {
        /// <summary>
        /// 数据项列表
        /// </summary>
        [JsonPropertyName("items")]
        public IList<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// 数据项列表别名（兼容性）
        /// </summary>
        [JsonPropertyName("data")]
        public IList<T> Data 
        { 
            get => Items; 
            set => Items = value; 
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每页大小
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

        /// <summary>
        /// 是否为第一页
        /// </summary>
        [JsonPropertyName("isFirst")]
        public bool IsFirst => CurrentPage <= 1;

        /// <summary>
        /// 是否为最后一页
        /// </summary>
        [JsonPropertyName("isLast")]
        public bool IsLast => CurrentPage >= TotalPages;

        /// <summary>
        /// 当前页开始记录索引（从0开始）
        /// </summary>
        [JsonPropertyName("startIndex")]
        public int StartIndex => (CurrentPage - 1) * PageSize;

        /// <summary>
        /// 当前页结束记录索引
        /// </summary>
        [JsonPropertyName("endIndex")]
        public int EndIndex => Math.Min(StartIndex + PageSize - 1, (int)TotalCount - 1);
    }
}
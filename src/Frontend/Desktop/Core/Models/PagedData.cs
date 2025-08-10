using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LYBT.WPF.Client.Core.Models
{
    /// <summary>
    /// 分页数据格式 - 匹配后端PagedData结构
    /// </summary>
    public class PagedData<T>
    {
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
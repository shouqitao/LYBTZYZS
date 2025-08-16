using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Contracts.Common
{
    /// <summary>
    /// 统一分页结果模型 - UltraThink架构统一
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// UltraThink统一构造函数
        /// </summary>
        public PagedResult()
        {
        }

        /// <summary>
        /// UltraThink统一构造函数 - 4参数版本
        /// </summary>
        public PagedResult(List<T> items, int totalCount, int currentPage, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
            CurrentPage = currentPage;
            PageSize = pageSize;
        }
        /// <summary>数据列表</summary>
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>总记录数</summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        /// <summary>当前页码</summary>
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        /// <summary>每页条数</summary>
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        /// <summary>总页数</summary>
        [JsonPropertyName("totalPages")]
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>是否有上一页</summary>
        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>是否有下一页</summary>
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>错误信息（用于传递API错误）</summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        // UltraThink兼容性别名 - 确保架构统一
        /// <summary>数据兼容性别名</summary>
        [JsonIgnore]
        public List<T> Data { get => Items; set => Items = value; }
    }
}
using System;
using System.Collections.Generic;

namespace LYBT.WPF.Client.Core.Models.Common
{
    /// <summary>
    /// 分页结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PaginatedResult<T>
    {
        /// <summary>数据列表</summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>总记录数</summary>
        public int TotalCount { get; set; }

        /// <summary>当前页码</summary>
        public int CurrentPage { get; set; }

        /// <summary>每页条数</summary>
        public int PageSize { get; set; }

        /// <summary>总页数</summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

        /// <summary>是否有上一页</summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>是否有下一页</summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
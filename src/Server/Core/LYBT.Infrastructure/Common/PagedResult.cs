using System;
using System.Collections.Generic;

namespace LYBT.Infrastructure.Common
{
    /// <summary>
    /// 通用分页结果
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// 数据项
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// 无参构造函数
        /// </summary>
        public PagedResult()
        {
        }

        /// <summary>
        /// 带参数构造函数
        /// </summary>
        /// <param name="items">数据项</param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="pageIndex">页面索引（从0开始）</param>
        /// <param name="pageSize">每页大小</param>
        public PagedResult(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
            PageNumber = pageIndex + 1; // 转换为从1开始的页码
            PageSize = pageSize;
            TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        }

        /// <summary>
        /// 创建空分页结果
        /// </summary>
        public static PagedResult<T> Empty()
        {
            return new PagedResult<T>
            {
                Items = new List<T>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 20,
                TotalPages = 0
            };
        }
    }
}
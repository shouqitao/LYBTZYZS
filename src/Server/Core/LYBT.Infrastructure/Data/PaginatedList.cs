namespace LYBT.Infrastructure.Data
{
    /// <summary>
    /// 分页列表
    /// 用于封装分页查询结果和分页信息
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class PaginatedList<T>
    {
        /// <summary>
        /// 当前页的数据项
        /// </summary>
        public List<T> Items { get; }

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// 当前页的第一条记录索引（从1开始）
        /// </summary>
        public int FirstItemIndex => (PageIndex - 1) * PageSize + 1;

        /// <summary>
        /// 当前页的最后一条记录索引
        /// </summary>
        public int LastItemIndex => Math.Min(PageIndex * PageSize, TotalCount);

        /// <summary>
        /// 初始化分页列表
        /// </summary>
        /// <param name="items">当前页的数据项</param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        public PaginatedList(List<T> items, int totalCount, int pageIndex, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        }

        /// <summary>
        /// 创建空的分页列表
        /// </summary>
        public static PaginatedList<T> Empty(int pageSize = 10)
        {
            return new PaginatedList<T>(new List<T>(), 0, 1, pageSize);
        }

        /// <summary>
        /// 从IQueryable创建分页列表（同步版本）
        /// </summary>
        public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count();
            var items = source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        /// <summary>
        /// 转换为另一种类型的分页列表
        /// </summary>
        /// <typeparam name="TResult">目标类型</typeparam>
        /// <param name="converter">转换函数</param>
        /// <returns>转换后的分页列表</returns>
        public PaginatedList<TResult> ConvertTo<TResult>(Func<T, TResult> converter)
        {
            var convertedItems = Items.Select(converter).ToList();
            return new PaginatedList<TResult>(convertedItems, TotalCount, PageIndex, PageSize);
        }

        /// <summary>
        /// 获取分页信息摘要
        /// </summary>
        public PaginationInfo GetPaginationInfo()
        {
            return new PaginationInfo
            {
                PageIndex = PageIndex,
                PageSize = PageSize,
                TotalCount = TotalCount,
                TotalPages = TotalPages,
                HasPreviousPage = HasPreviousPage,
                HasNextPage = HasNextPage,
                FirstItemIndex = FirstItemIndex,
                LastItemIndex = LastItemIndex
            };
        }
    }

    /// <summary>
    /// 分页信息
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// 当前页的第一条记录索引
        /// </summary>
        public int FirstItemIndex { get; set; }

        /// <summary>
        /// 当前页的最后一条记录索引
        /// </summary>
        public int LastItemIndex { get; set; }
    }
}

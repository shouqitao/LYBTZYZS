namespace LYBT.Shared.Models.Common {

    /// <summary>
    /// 分页请求模型 - 前后端统一
    /// </summary>
    public class PaginationRequest {

        /// <summary>
        /// 当前页码（从1开始）
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// 是否升序排列
        /// </summary>
        public bool SortAscending { get; set; } = true;

        /// <summary>
        /// 计算跳过的记录数
        /// </summary>
        public int SkipCount => (CurrentPage - 1) * PageSize;
    }

    /// <summary>
    /// 分页结果模型 - 前后端统一
    /// </summary>
    /// <typeparam name="T">数据项类型</typeparam>
    public class PaginatedResult<T> {

        /// <summary>
        /// 数据项集合
        /// </summary>
        public IList<T> Items { get; set; } = [];

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPrevious => CurrentPage > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNext => CurrentPage < TotalPages;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PaginatedResult() {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="items">数据项</param>
        /// <param name="totalCount">总记录数</param>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        public PaginatedResult(IList<T> items, int totalCount, int currentPage, int pageSize) {
            Items = items;
            TotalCount = totalCount;
            CurrentPage = currentPage;
            PageSize = pageSize;
        }

        /// <summary>
        /// 创建空的分页结果
        /// </summary>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>空分页结果</returns>
        public static PaginatedResult<T> Empty(int currentPage = 1, int pageSize = 10) {
            return new PaginatedResult<T>([], 0, currentPage, pageSize);
        }
    }
}
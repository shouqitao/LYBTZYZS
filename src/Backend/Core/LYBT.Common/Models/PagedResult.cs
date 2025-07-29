namespace LYBT.Common.Models {

    /// <summary>
    /// 分页返回结构
    /// </summary>
    public class PagedResult<T> {

        /// <summary>
        /// 总记录数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 总记录数（别名，为了兼容性）
        /// </summary>
        public int TotalCount {
            get => Total;
            set => Total = value;
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// 数据项列表
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();
    }
}
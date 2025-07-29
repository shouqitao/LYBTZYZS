namespace LYBT.Common.Models {

    /// <summary>
    /// 分页请求参数
    /// </summary>
    public class PaginationRequest {

        /// <summary>
        /// PageIndex 属性。
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// PageSize 属性。
        /// </summary>
        public int PageSize { get; set; }
    }
}
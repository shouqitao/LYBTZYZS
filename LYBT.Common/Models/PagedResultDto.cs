using System.Collections.Generic;

namespace LYBT.Common.Models {
    /// <summary>
    /// 通用分页响应 DTO
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PagedResultDto<T> {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 当前页数据列表
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();
    }
}

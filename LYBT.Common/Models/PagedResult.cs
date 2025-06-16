using System.Collections.Generic;

namespace LYBT.Common.Models {
    /// <summary>
    /// 分页返回结构
    /// </summary>
    public class PagedResult<T> {
        public int Total { get; set; }
        public List<T> Items { get; set; } = new List<T>();
    }
}

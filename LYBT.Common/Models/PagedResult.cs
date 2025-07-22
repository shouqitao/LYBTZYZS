namespace LYBT.Common.Models {

    /// <summary>
    /// 分页返回结构
    /// </summary>
    public class PagedResult<T> {
/// <summary>
/// Total 属性。
/// </summary>
        public int Total { get; set; }
/// <summary>
/// Items 属性。
/// </summary>
        public List<T> Items { get; set; } = new List<T>();
    }
}

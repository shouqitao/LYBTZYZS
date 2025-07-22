using System.ComponentModel;

namespace LYBT.Module.Herbs.Dtos {
    /// <summary>
    /// 药材分页查询参数
    /// </summary>
    public class HerbPagedQueryDto {
        /// <summary>关键词（名称或拼音）</summary>
        [DisplayName("关键词")]
/// <summary>
/// Keyword 属性。
/// </summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>页码（从1开始）</summary>
        [DisplayName("页码")]
/// <summary>
/// Page 属性。
/// </summary>
        public int Page { get; set; } = 1;

        /// <summary>每页数量</summary>
        [DisplayName("每页数量")]
/// <summary>
/// PageSize 属性。
/// </summary>
        public int PageSize { get; set; } = 20;
    }
}

using System.Collections.Generic;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 扩展分页请求 - UltraThink重构：基于PagedQueryBaseDto的扩展版本
    /// </summary>
    public class ExtendedPaginationRequest : PagedQueryBaseDto
    {
        /// <summary>
        /// 扩展数据字典，用于传递额外的查询参数
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? SortField { get; set; }

        /// <summary>
        /// 是否降序排序
        /// </summary>
        public bool IsDescending { get; set; }
    }
}
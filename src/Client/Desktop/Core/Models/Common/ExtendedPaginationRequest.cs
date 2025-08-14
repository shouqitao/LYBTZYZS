using System.Collections.Generic;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Core.Models.Common
{
    /// <summary>
    /// 扩展的分页请求，支持额外的查询参数
    /// </summary>
    public class ExtendedPaginationRequest : PagedQueryBaseDto
    {
        /// <summary>
        /// 扩展数据字典，用于传递额外的查询参数
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}
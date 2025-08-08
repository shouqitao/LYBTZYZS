using System.Collections.Generic;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Core.Models.Common
{
    /// <summary>
    /// 扩展的分页请求，支持额外的查询参数
    /// </summary>
    public class ExtendedPaginationRequest : PaginationRequest
    {
        /// <summary>
        /// 扩展数据字典，用于传递额外的查询参数
        /// </summary>
        public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
    }
}
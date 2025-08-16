using LYBT.Shared.Models.Contracts.Common;
using System.Collections.Generic;

namespace LYBT.Desktop.Workbench.Admin.Helpers
{
    /// <summary>
    /// 分页大小选项
    /// </summary>
    public static class PageSizeOptions
    {
        /// <summary>
        /// 可用的分页大小选项
        /// </summary>
        public static readonly List<int> Options = new List<int>
        {
            10, 20, 50, 100, 200
        };
    }
}
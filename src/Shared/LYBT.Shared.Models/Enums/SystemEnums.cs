using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 通用状态枚举
    /// </summary>
    public enum CommonStatus
    {
        /// <summary>禁用</summary>
        [Description("禁用")]
        Disabled = 0,

        /// <summary>启用</summary>
        [Description("启用")]
        Enabled = 1
    }
}

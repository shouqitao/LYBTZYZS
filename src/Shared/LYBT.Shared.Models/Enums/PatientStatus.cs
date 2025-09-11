using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 患者状态枚举 - Record-Only模式简化版本（仅Active/Inactive）
    /// </summary>
    [Description("患者状态")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PatientStatus
    {

        /// <summary>停用</summary>
        [Description("停用")]
        Inactive = 0,

        /// <summary>活跃</summary>
        [Description("活跃")]
        Active = 1,

        /// <summary>正常（兼容别名，指向Active）</summary>
        [Description("正常")]
        [Obsolete("Use Active instead. Normal status removed in Record-Only mode.", false)]
        Normal = 1,

        /// <summary>已删除 - Record-Only模式已移除此状态</summary>
        [Description("已删除")]
        [Obsolete("Deleted status removed in Record-Only mode. Use Inactive instead.", false)]
        Deleted = -1,

        /// <summary>黑名单 - Record-Only模式已移除此状态</summary>
        [Description("黑名单")]
        [Obsolete("Blacklisted status removed in Record-Only mode. Use Inactive instead.", false)]
        Blacklisted = -2
    }
}

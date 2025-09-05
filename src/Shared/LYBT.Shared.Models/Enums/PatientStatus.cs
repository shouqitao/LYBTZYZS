using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 患者状态枚举 - 前后端共享
    /// </summary>
    [Description("患者状态")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PatientStatus
    {

        /// <summary>停用</summary>
        [Description("停用")]
        Inactive = 0,

        /// <summary>正常</summary>
        [Description("正常")]
        Normal = 1,

        /// <summary>活跃（别名）</summary>
        [Description("活跃")]
        Active = 1,

        /// <summary>已删除</summary>
        [Description("已删除")]
        Deleted = -1,

        /// <summary>黑名单</summary>
        [Description("黑名单")]
        Blacklisted = -2
    }
}

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
        Active = 1
    }
}

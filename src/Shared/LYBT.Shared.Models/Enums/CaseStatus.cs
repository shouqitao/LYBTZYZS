using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 案例状态枚举 - UltraThink简化版别名
    /// 为MedicalCaseStatus提供简化的别名访问
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CaseStatus
    {
        /// <summary>活跃状态</summary>
        [Description("活跃")]
        Active = MedicalCaseStatus.Active,

        /// <summary>已关闭</summary>
        [Description("已关闭")]
        Closed = MedicalCaseStatus.Closed
    }
}

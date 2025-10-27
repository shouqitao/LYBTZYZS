using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 案例状态枚举 - Epic #1612修正版别名
    /// 为MedicalCaseStatus提供简化的别名访问
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CaseStatus
    {
        /// <summary>暂存</summary>
        [Description("暂存")]
        Draft = MedicalCaseStatus.Draft,

        /// <summary>活跃/进行中</summary>
        [Description("进行中")]
        Active = MedicalCaseStatus.Active,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = MedicalCaseStatus.Completed,

        /// <summary>已取消</summary>
        [Description("已取消")]
        Cancelled = MedicalCaseStatus.Cancelled
    }
}

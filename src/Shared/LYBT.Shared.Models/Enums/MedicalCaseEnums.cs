using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 医疗案例状态枚举 - Issue #2242简化版
    /// 简化状态机：Draft ↔ Active → Completed，取消操作使用软删除（IsDeleted）
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MedicalCaseStatus
    {
        /// <summary>暂存（用户暂时保存，稍后继续）- Issue #1647</summary>
        [Description("暂存")]
        Draft = 0,

        /// <summary>活跃/进行中（正在诊疗）</summary>
        [Description("进行中")]
        Active = 1,

        /// <summary>已完成（三步流程全部完成）</summary>
        [Description("已完成")]
        Completed = 2
    }

    /// <summary>
    /// 待看诊类型（预留，用于未来挂号集成）
    /// Epic #1583 - Phase 5
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PendingType
    {
        /// <summary>
        /// 未完成医案
        /// </summary>
        [Description("未完成医案")]
        Incomplete = 1,

        /// <summary>
        /// 已挂号（预留）
        /// </summary>
        [Description("已挂号")]
        Appointment = 2
    }
}

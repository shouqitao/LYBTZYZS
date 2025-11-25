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
        Completed = 2,

        /// <summary>已取消 - Issue #2242: 已废弃，使用软删除（IsDeleted=true）代替</summary>
        [Description("已取消")]
        [Obsolete("Use soft delete (IsDeleted=true) instead of Cancelled status. Issue #2242", false)]
        Cancelled = 3,

        // ========== 废弃状态（兼容性保留） ==========

        /// <summary>已关闭 - 废弃，拆分为Completed和Cancelled</summary>
        [Description("已关闭")]
        [Obsolete("Use Completed or Cancelled instead. Closed status split into Completed/Cancelled.", true)]
        Closed = 20,

        /// <summary>挂号完成 - 废弃，合并到Draft或Active</summary>
        [Description("挂号完成")]
        [Obsolete("Use Draft or Active instead.", true)]
        Registered = 10,

        /// <summary>诊疗中 - 废弃，使用Active代替</summary>
        [Description("诊疗中")]
        [Obsolete("Use Active instead.", true)]
        InConsultation = 11,

        /// <summary>暂停 - 废弃，使用Draft代替</summary>
        [Description("暂停")]
        [Obsolete("Use Draft instead.", true)]
        Suspended = 12,

        /// <summary>已归档 - 废弃，使用Completed代替</summary>
        [Description("已归档")]
        [Obsolete("Use Completed instead.", true)]
        Archived = 13
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

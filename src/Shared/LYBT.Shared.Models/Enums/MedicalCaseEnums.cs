using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{

    /// <summary>
    /// 医疗案例状态枚举 - Record-Only模式简化版本（仅Active/Closed）
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MedicalCaseStatus
    {

        /// <summary>活跃状态（包含挂号、诊疗中、暂停等活跃状态）</summary>
        [Description("活跃")]
        Active = 10,

        /// <summary>已关闭（包含完成、取消、归档等结束状态）</summary>
        [Description("已关闭")]
        Closed = 20,

        // 兼容性映射：旧状态保留以避免序列化错误，但标记为过时

        /// <summary>挂号完成 - 已合并到Active状态</summary>
        [Description("挂号完成")]
        [Obsolete("Use Active instead. Registered status merged into Active in Record-Only mode.", false)]
        Registered = 0,

        /// <summary>诊疗中 - 已合并到Active状态</summary>
        [Description("诊疗中")]
        [Obsolete("Use Active instead. InConsultation status merged into Active in Record-Only mode.", false)]
        InConsultation = 1,

        /// <summary>已完成 - 已合并到Closed状态</summary>
        [Description("已完成")]
        [Obsolete("Use Closed instead. Completed status merged into Closed in Record-Only mode.", false)]
        Completed = 2,

        /// <summary>已取消 - 已合并到Closed状态</summary>
        [Description("已取消")]
        [Obsolete("Use Closed instead. Cancelled status merged into Closed in Record-Only mode.", false)]
        Cancelled = 3,

        /// <summary>暂停 - 已合并到Active状态</summary>
        [Description("暂停")]
        [Obsolete("Use Active instead. Suspended status merged into Active in Record-Only mode.", false)]
        Suspended = 4,

        /// <summary>已归档 - 已合并到Closed状态</summary>
        [Description("已归档")]
        [Obsolete("Use Closed instead. Archived status merged into Closed in Record-Only mode.", false)]
        Archived = 5
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

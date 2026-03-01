using System.ComponentModel;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 案例状态枚举 - Epic #1612修正版别名
    /// 为MedicalCaseStatus提供简化的别名访问
    /// OpenSpec: unify-enums-to-shared - 移除冗余JsonConverter（已全局配置）
    /// </summary>
    public enum CaseStatus
    {
        /// <summary>已挂起</summary>
        [Description("已挂起")]
        Suspended = MedicalCaseStatus.Suspended,

        /// <summary>活跃/进行中</summary>
        [Description("进行中")]
        Active = MedicalCaseStatus.Active,

        /// <summary>已完成</summary>
        [Description("已完成")]
        Completed = MedicalCaseStatus.Completed
    }
}

using LYBT.Shared.Models.Enums;

namespace LYBT.Infrastructure.Utilities
{
    /// <summary>
    /// 验证工具类
    /// Issue #1757: 从MedicalCaseService提取纯验证方法
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 验证病案状态流转是否合法
        /// Issue #2242：简化状态机 - 移除Cancelled状态，使用IsDeleted替代
        /// </summary>
        /// <param name="from">原状态</param>
        /// <param name="to">目标状态</param>
        /// <returns>是否允许流转</returns>
        public static bool IsValidMedicalCaseStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        {
            // 状态机规则（Issue #2242简化版：Draft ↔ Active → Completed）
            return (from, to) switch
            {
                // Draft <-> Active
                (MedicalCaseStatus.Draft, MedicalCaseStatus.Active) => true,   // 继续看诊
                (MedicalCaseStatus.Active, MedicalCaseStatus.Draft) => true,   // 暂存 (Issue #1647)

                // Active -> Completed (终态)
                (MedicalCaseStatus.Active, MedicalCaseStatus.Completed) => true,  // 完成三步流程

                _ => false // 其他流转禁止
            };
        }
    }
}

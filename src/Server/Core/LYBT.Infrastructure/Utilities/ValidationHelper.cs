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
        /// Epic #1612修正版：支持Draft/Active/Completed/Cancelled四状态流转
        /// </summary>
        /// <param name="from">原状态</param>
        /// <param name="to">目标状态</param>
        /// <returns>是否允许流转</returns>
        public static bool IsValidMedicalCaseStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        {
            // 状态机规则（Epic #1612修正版）
            return (from, to) switch
            {
                // Draft <-> Active
                (MedicalCaseStatus.Draft, MedicalCaseStatus.Active) => true,   // 继续看诊
                (MedicalCaseStatus.Active, MedicalCaseStatus.Draft) => true,   // 暂存 (Issue #1647)

                // Active -> 终态
                (MedicalCaseStatus.Active, MedicalCaseStatus.Completed) => true,  // 完成三步流程
                (MedicalCaseStatus.Active, MedicalCaseStatus.Cancelled) => true,  // 取消

                // Cancelled -> Active (允许重新激活)
                (MedicalCaseStatus.Cancelled, MedicalCaseStatus.Active) => true,

                _ => false // 其他流转禁止
            };
        }
    }
}

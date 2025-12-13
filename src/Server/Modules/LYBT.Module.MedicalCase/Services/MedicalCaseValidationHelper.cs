using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 病案验证工具类
    /// 从Infrastructure层迁移，遵循DDD原则 - 领域逻辑应位于领域模块中
    /// Issue #1757: 从MedicalCaseService提取纯验证方法
    /// </summary>
    public static class MedicalCaseValidationHelper
    {
        /// <summary>
        /// 验证病案状态流转是否合法
        /// Issue #2242：简化状态机 - 移除Cancelled状态，使用IsDeleted替代
        /// </summary>
        /// <param name="from">原状态</param>
        /// <param name="to">目标状态</param>
        /// <returns>是否允许流转</returns>
        public static bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        {
            // 状态机规则（Issue #2242简化版：Draft ↔ Active → Completed）
            // 补充：允许Draft直接到Completed（"完成看诊"按钮一步完成）
            return (from, to) switch
            {
                // Draft <-> Active
                (MedicalCaseStatus.Draft, MedicalCaseStatus.Active) => true,   // 继续看诊
                (MedicalCaseStatus.Active, MedicalCaseStatus.Draft) => true,   // 暂存 (Issue #1647)

                // Draft/Active -> Completed (终态)
                (MedicalCaseStatus.Draft, MedicalCaseStatus.Completed) => true,   // 一步完成（完成看诊按钮）
                (MedicalCaseStatus.Active, MedicalCaseStatus.Completed) => true,  // 完成三步流程

                _ => false // 其他流转禁止
            };
        }
    }
}

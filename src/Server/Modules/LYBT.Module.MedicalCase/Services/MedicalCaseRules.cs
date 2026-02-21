using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医疗案例规则 - 集中管理核心业务逻辑
    /// 简化版本，只保留最核心的业务规则
    /// </summary>
    public static class MedicalCaseRules
    {
        /// <summary>
        /// 核心规则1：患者同时只能有一个进行中或暂存的医案
        /// Issue #xxxx: 增加Draft（暂存/挂起）状态检查
        /// </summary>
        /// <param name="existingCases">患者现有的医案列表</param>
        /// <returns>是否可以创建新医案</returns>
        public static bool CanCreateNewCase(IEnumerable<MedicalCase> existingCases)
        {
            return !existingCases.Any(c => c.CaseStatus == MedicalCaseStatus.Active ||
                                            c.CaseStatus == MedicalCaseStatus.Draft);
        }

        /// <summary>
        /// 检查是否有Active状态的医案
        /// </summary>
        public static bool HasActiveCase(IEnumerable<MedicalCase> existingCases)
        {
            return existingCases.Any(c => c.CaseStatus == MedicalCaseStatus.Active);
        }

        /// <summary>
        /// 检查是否有Draft（暂存/挂起）状态的医案
        /// </summary>
        public static bool HasDraftCase(IEnumerable<MedicalCase> existingCases)
        {
            return existingCases.Any(c => c.CaseStatus == MedicalCaseStatus.Draft);
        }

        /// <summary>
        /// 验证病案状态流转是否合法
        /// 简化状态机: Draft <-> Active -> Completed (Completed 由 CompleteAsync 专门处理)
        /// </summary>
        /// <param name="from">原状态</param>
        /// <param name="to">目标状态</param>
        /// <returns>是否允许流转</returns>
        public static bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        {
            return (from, to) switch
            {
                (MedicalCaseStatus.Draft, MedicalCaseStatus.Active) => true,   // 继续看诊
                (MedicalCaseStatus.Active, MedicalCaseStatus.Draft) => true,   // 暂存
                _ => false // Completed 流转由 CompleteAsync 统一管理
            };
        }
    }
}

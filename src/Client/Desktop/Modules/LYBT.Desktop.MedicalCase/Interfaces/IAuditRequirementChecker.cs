using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010)
    /// 审计需求检查器接口 - 判断修改医案时是否需要填写修改原因
    /// </summary>
    public interface IAuditRequirementChecker
    {
        /// <summary>
        /// 判断是否需要审计（填写修改原因）
        /// </summary>
        /// <param name="medicalCase">医案数据</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>true: 需要填写修改原因; false: 无需审计</returns>
        /// <remarks>
        /// 审计规则:
        /// 1. 已完成的医案必须审计 (CaseStatus == Completed)
        /// 2. 非本人修改必须审计 (DoctorId != currentUserId)
        /// 3. 隔天修改必须审计 (CreatedAt.Date &lt; DateTime.Today)
        /// 其他情况: 当天本人修改进行中的医案，无需审计
        /// </remarks>
        bool IsAuditRequired(MedicalCaseDto medicalCase, Guid currentUserId);
    }
}

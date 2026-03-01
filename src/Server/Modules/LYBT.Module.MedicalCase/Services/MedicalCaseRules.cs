using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Validators.BusinessRules;

namespace LYBT.Module.MedicalCases.Services;

/// <summary>
/// 医案规则 - Server 端适配器, 将实体集合转换为状态集合后委托给 MedicalCaseBusinessRules (Shared)
/// OpenSpec: design-issues-solutions - 兼容设计，待 Server 端调用者直接使用 MedicalCaseBusinessRules 后移除
/// </summary>
public static class MedicalCaseRules
{
    public static bool CanCreateNewCase(IEnumerable<MedicalCase> existingCases)
        => MedicalCaseBusinessRules.CanCreateNewCase(existingCases.Select(c => c.CaseStatus));

    public static bool HasActiveCase(IEnumerable<MedicalCase> existingCases)
        => MedicalCaseBusinessRules.HasActiveCase(existingCases.Select(c => c.CaseStatus));

    public static bool HasSuspendedCase(IEnumerable<MedicalCase> existingCases)
        => MedicalCaseBusinessRules.HasSuspendedCase(existingCases.Select(c => c.CaseStatus));

    public static bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        => MedicalCaseBusinessRules.IsValidStatusTransition(from, to);
}

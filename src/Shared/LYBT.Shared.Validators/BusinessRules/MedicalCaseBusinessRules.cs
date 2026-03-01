using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Validators.BusinessRules;

/// <summary>
/// 医案核心业务规则 (纯函数, 无外部依赖)
/// Server 端 MedicalCaseRules 和 Client 端 LocalDataSource 共同引用
/// </summary>
public static class MedicalCaseBusinessRules
{
    /// <summary>患者同时只能有一个 Active 或 Suspended 状态的医案</summary>
    public static bool CanCreateNewCase(IEnumerable<MedicalCaseStatus> existingStatuses)
        => !existingStatuses.Any(s => s == MedicalCaseStatus.Active || s == MedicalCaseStatus.Suspended);

    /// <summary>状态流转: Suspended 和 Active 双向, Completed 由 CompleteAsync 专门处理</summary>
    public static bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        => (from, to) switch
        {
            (MedicalCaseStatus.Suspended, MedicalCaseStatus.Active) => true,
            (MedicalCaseStatus.Active, MedicalCaseStatus.Suspended) => true,
            _ => false
        };

    public static bool HasActiveCase(IEnumerable<MedicalCaseStatus> statuses)
        => statuses.Any(s => s == MedicalCaseStatus.Active);

    public static bool HasSuspendedCase(IEnumerable<MedicalCaseStatus> statuses)
        => statuses.Any(s => s == MedicalCaseStatus.Suspended);
}

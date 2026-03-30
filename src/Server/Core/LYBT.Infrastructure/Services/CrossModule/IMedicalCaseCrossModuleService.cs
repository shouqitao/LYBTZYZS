using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 医案域跨模块服务
/// 供 Patients 模块使用，替代直接依赖 IMedicalCaseReferenceService
/// Architecture Fix: 解决 Patients → MedicalCases 循环依赖问题
/// </summary>
public interface IMedicalCaseCrossModuleService
{
    /// <summary>统计患者未完成的医案数量（Active/Suspended状态）</summary>
    Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>统计患者的医案总数</summary>
    Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>获取患者最近的医案引用列表</summary>
    Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default);
}

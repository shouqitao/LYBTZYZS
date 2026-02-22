using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 医案生命周期服务接口 - 跨模块共享
/// OpenSpec: refactor-frontend-srp-patterns (ADR-1) - SRP职责分离，生命周期职责
/// 负责医案的初始化、状态流转（暂存、取消、完成、恢复）
/// </summary>
public interface IMedicalCaseLifecycleService
{
    /// <summary>
    /// 医案ID
    /// </summary>
    Guid MedicalCaseId { get; }

    /// <summary>
    /// 当前诊疗数据（来自聚合根导航属性）
    /// </summary>
    ConsultationDetailDto? CurrentConsultation { get; }

    /// <summary>
    /// 当前处方数据（来自聚合根导航属性）
    /// </summary>
    PrescriptionDetailDto? CurrentPrescription { get; }

    /// <summary>
    /// 初始化并加载医案数据
    /// </summary>
    /// <param name="entityId">医案ID</param>
    Task InitializeAsync(Guid entityId);

    /// <summary>
    /// 重新加载数据
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// 暂存医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>(是否成功, 错误信息)</returns>
    Task<(bool success, string? errorMessage)> SaveDraftAsync(Guid medicalCaseId);

    /// <summary>
    /// 取消医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="reason">取消原因</param>
    /// <returns>(是否成功, 错误信息)</returns>
    Task<(bool success, string? errorMessage)> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null);

    /// <summary>
    /// 完成医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>(是否成功, 错误信息)</returns>
    Task<(bool success, string? errorMessage)> CompleteMedicalCaseAsync(Guid medicalCaseId);

    /// <summary>
    /// 恢复暂存医案为Active状态
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>(是否成功, 错误信息)</returns>
    Task<(bool success, string? errorMessage)> ResumeDraftAsync(Guid medicalCaseId);
}

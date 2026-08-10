using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 医案生命周期服务接口 - 跨模块共享
/// 负责医案的初始化、状态流转（暂存、取消、完成、恢复）
/// </summary>
public interface IMedicalCaseLifecycleService
{
    /// <summary>
    /// 医案ID
    /// </summary>
    Guid MedicalCaseId { get; }

    /// <summary>
    /// 当前医案详情 DTO（D5: 会话唯一 DTO 快照，取代原 CachedMedicalCase 门面）
    /// </summary>
    MedicalCaseDetailDto? CurrentDetail { get; }

    /// <summary>
    /// 更新 DTO 快照并前移编辑会话基线（保存后同步）
    /// </summary>
    void UpdateSnapshot(MedicalCaseDetailDto? detail);

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
    Task InitializeAsync(Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// 重新加载数据
    /// </summary>
    Task ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// 挂起医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    Task<CommandResult<bool>> SuspendAsync(Guid medicalCaseId, CancellationToken ct = default);

    /// <summary>
    /// 取消医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="reason">取消原因</param>
    Task<CommandResult<bool>> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// 完成医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    Task<CommandResult<bool>> CompleteMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default);

    /// <summary>
    /// 恢复挂起医案为Active状态
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    Task<CommandResult<bool>> ResumeSuspendedAsync(Guid medicalCaseId, CancellationToken ct = default);

    /// <summary>
    /// 关闭医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>API响应，包含关闭后的医案详情</returns>
    Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId, CancellationToken ct = default);
}

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 挂号域跨模块服务 (ISP: D5-1)
/// 供 MedicalCase + Users 模块使用
/// </summary>
public interface IRegistrationCrossModuleService
{
    /// <summary>
    /// MedicalCase 完成时同步 Registration 状态
    /// </summary>
    Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default);

    /// <summary>
    /// MedicalCase 取消时回滚 Registration 状态
    /// </summary>
    Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default);

    /// <summary>
    /// 检查患者是否有等待中的挂号
    /// </summary>
    Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>
    /// 关联挂号到医案
    /// </summary>
    Task LinkRegistrationToMedicalCaseAsync(Guid registrationId, Guid medicalCaseId, CancellationToken ct = default);
}



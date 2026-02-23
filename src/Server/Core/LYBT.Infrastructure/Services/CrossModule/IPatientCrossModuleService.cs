using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 患者域跨模块服务 (ISP: D5-1)
/// 供 MedicalCase + Sync 模块使用
/// </summary>
public interface IPatientCrossModuleService
{
    /// <summary>获取患者基本信息</summary>
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);

    /// <summary>批量获取患者基本信息</summary>
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    /// <summary>检查患者是否存在 (未删除)</summary>
    Task<bool> PatientExistsAsync(Guid patientId);

    /// <summary>检查患者引用关系 (医案引用数)</summary>
    Task<ReferenceCheckResult> CheckPatientReferenceAsync(Guid patientId);
}

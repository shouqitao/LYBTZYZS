using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 患者域跨模块服务 (ISP: D5-1)
/// 供 MedicalCase + Sync 模块使用
/// </summary>
public interface IPatientCrossModuleService
{
    /// <summary>获取患者基本信息</summary>
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default);
}



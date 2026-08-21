using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;

namespace LYBT.Module.MedicalCases.Services;

/// <summary>
/// 医案运营时间服务（P1-10）：以诊所本地时区（默认 Asia/Shanghai）判定日界，
/// 供 CommandService 强锁与实体计算属性一致。
/// </summary>
public sealed class MedicalCaseTimeService : IMedicalCaseTimeService
{
    /// <inheritdoc/>
    public bool IsLocked(MedicalCase medicalCase, DateTimeOffset? now = null)
        => medicalCase.IsCompleted && medicalCase.CompletedAt.HasValue
           && MedicalCaseTime.ClinicLocalDate(medicalCase.CompletedAt.Value)
              < MedicalCaseTime.ClinicLocalDate((now ?? DateTimeOffset.UtcNow).UtcDateTime);
}

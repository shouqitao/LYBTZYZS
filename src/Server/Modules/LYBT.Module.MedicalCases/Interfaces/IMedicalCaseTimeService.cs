using LYBT.Entities.MedicalCases;

namespace LYBT.Module.MedicalCases.Interfaces;

/// <summary>
/// 医案运营时间服务（P1-10）：客户端本地时区日界判定
/// </summary>
public interface IMedicalCaseTimeService
{
    /// <summary>判定医案是否锁定（Completed 且非诊所本地当天）；now 可选注入便于测试</summary>
    bool IsLocked(MedicalCase medicalCase, DateTimeOffset? now = null);
}

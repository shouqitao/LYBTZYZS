using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.ViewModels.Handlers;

/// <summary>
/// 患者状态处理接口
/// </summary>
public interface IPatientStatusHandler
{
    /// <summary>
    /// 恢复已删除的患者
    /// </summary>
    /// <param name="patient">患者信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> RestoreAsync(PatientListDto patient);
}

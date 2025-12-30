using System.Collections.ObjectModel;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 待诊队列管理器接口
/// OpenSpec: refactor-medicalcase-workspace - 解耦MedicalCase和Patients模块
/// </summary>
public interface IPendingQueueManager
{
    /// <summary>
    /// 待诊队列（未完成医案的患者列表）
    /// </summary>
    ObservableCollection<PendingMedicalCaseDto> PendingQueue { get; }

    /// <summary>
    /// 加载待看诊队列
    /// </summary>
    Task LoadPendingCasesAsync();

    /// <summary>
    /// 为待看诊队列选中的患者加载完整信息
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>患者详情</returns>
    Task<PatientDetailDto?> LoadPatientForPendingCaseAsync(Guid patientId);

    /// <summary>
    /// 从待诊队列中移除患者
    /// </summary>
    /// <param name="patientId">患者ID</param>
    void RemoveFromQueue(Guid patientId);

    /// <summary>
    /// 清空待诊队列
    /// </summary>
    void ClearQueue();
}

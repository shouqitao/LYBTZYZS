using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using static LYBT.Desktop.Patients.ViewModels.Components.MedicalCaseStartCoordinator;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 医案启动协调器接口
    /// 处理患者开始看诊的完整流程
    /// </summary>
    public interface IMedicalCaseStartCoordinator
    {
        /// <summary>
        /// 检查患者是否有未完成医案
        /// </summary>
        Task<MedicalCaseDetailDto?> CheckUnfinishedCaseAsync(Guid patientId);

        /// <summary>
        /// 判断是否为其他医生的挂起医案
        /// </summary>
        bool IsOtherDoctorCase(MedicalCaseDetailDto? unfinishedCase);

        /// <summary>
        /// 获取其他医生名称
        /// </summary>
        string GetOtherDoctorName(MedicalCaseDetailDto unfinishedCase);

        /// <summary>
        /// 继续现有医案
        /// </summary>
        Task<StartResultData> ContinueExistingCaseAsync(PatientDetailDto patient, Guid medicalCaseId);

        /// <summary>
        /// 关闭旧医案并创建新医案
        /// </summary>
        Task<StartResultData> CloseAndCreateNewAsync(PatientDetailDto patient, Guid oldMedicalCaseId);

        /// <summary>
        /// 仅关闭旧医案（不创建新医案）
        /// </summary>
        Task<StartResultData> CloseOnlyAsync(PatientDetailDto patient, Guid oldMedicalCaseId);

        /// <summary>
        /// 处理用户对话框选择
        /// </summary>
        Task<StartResultData> HandleUserChoiceAsync(
            int choice,
            PatientDetailDto patient,
            Guid unfinishedCaseId,
            Func<Task>? refreshPendingQueueCallback = null);
    }
}

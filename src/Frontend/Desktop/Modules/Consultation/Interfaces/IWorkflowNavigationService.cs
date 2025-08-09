using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Modules.Consultation.Services;

namespace LYBT.WPF.Client.Modules.Consultation.Interfaces
{
    /// <summary>
    /// 工作流导航服务接口
    /// </summary>
    public interface IWorkflowNavigationService
    {
        /// <summary>导航到指定的工作流步骤</summary>
        Task<bool> NavigateToStepAsync(WorkflowStep step, object? parameters = null);

        /// <summary>导航到患者选择页面</summary>
        Task NavigateToPatientSelectionAsync(Guid? patientId = null);

        /// <summary>导航到四诊采集页面</summary>
        Task NavigateToFourDiagnosisAsync(Guid medicalCaseId, Guid patientId);

        /// <summary>导航到辨证分析页面</summary>
        Task NavigateToDifferentiationAsync(Guid medicalCaseId, Guid consultationId);

        /// <summary>导航到处方开具页面</summary>
        Task NavigateToPrescriptionAsync(Guid medicalCaseId, Guid consultationId);

        /// <summary>导航回主页</summary>
        Task NavigateToHomeAsync();

        /// <summary>导航到医疗案例列表</summary>
        Task NavigateToMedicalCaseListAsync();
    }
}
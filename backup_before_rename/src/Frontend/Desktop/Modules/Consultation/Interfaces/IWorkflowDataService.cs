using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Consultation.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// 工作流数据服务接口
    /// </summary>
    public interface IWorkflowDataService
    {
        /// <summary>加载医疗案例详情</summary>
        Task<MedicalCaseInfo?> LoadMedicalCaseAsync(Guid medicalCaseId);

        /// <summary>更新医疗案例状态</summary>
        Task<bool> UpdateMedicalCaseStatusAsync(Guid medicalCaseId, MedicalCaseStatus status);

        /// <summary>保存诊疗记录</summary>
        Task<ConsultationInfo?> SaveConsultationAsync(ConsultationCreateDto consultationDto);

        /// <summary>加载诊疗记录</summary>
        Task<ConsultationInfo?> LoadConsultationAsync(Guid consultationId);

        /// <summary>保存处方</summary>
        Task<PrescriptionInfo?> SavePrescriptionAsync(PrescriptionCreateDto prescriptionDto);

        /// <summary>加载患者历史处方</summary>
        Task<List<PrescriptionInfo>> LoadPatientPrescriptionsAsync(Guid patientId, int count = 10);

        /// <summary>验证工作流数据完整性</summary>
        Task<WorkflowValidationResult> ValidateWorkflowDataAsync(Guid medicalCaseId);
    }
}
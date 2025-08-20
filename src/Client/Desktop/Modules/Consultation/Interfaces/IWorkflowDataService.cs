using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// 工作流数据服务接口 - UltraThink v2.0: 直接使用DTO模型
    /// </summary>
    public interface IWorkflowDataService
    {
        /// <summary>加载医疗案例详情</summary>
        Task<MedicalCaseDto?> LoadMedicalCaseAsync(Guid medicalCaseId);

        /// <summary>更新医疗案例状态</summary>
        Task<bool> UpdateMedicalCaseStatusAsync(Guid medicalCaseId, MedicalCaseStatus status);

        /// <summary>保存诊疗记录</summary>
        Task<ConsultationDto?> SaveConsultationAsync(ConsultationCreateDto consultationDto);

        /// <summary>加载诊疗记录</summary>
        Task<ConsultationDto?> LoadConsultationAsync(Guid consultationId);

        /// <summary>保存处方</summary>
        Task<PrescriptionDto?> SavePrescriptionAsync(PrescriptionCreateDto prescriptionDto);

        /// <summary>加载患者历史处方</summary>
        Task<List<PrescriptionDto>> LoadPatientPrescriptionsAsync(Guid patientId, int count = 10);

        /// <summary>验证工作流数据完整性</summary>
        Task<WorkflowValidationResult> ValidateWorkflowDataAsync(Guid medicalCaseId);
    }

    /// <summary>
    /// 工作流数据验证结果
    /// </summary>
    public class WorkflowValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsMedicalCaseValid { get; set; }
        public bool IsPatientValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Validation;

namespace LYBT.Desktop.Consultation.Services.Interfaces
{
    /// <summary>
    /// 看诊验证器接口
    /// </summary>
    public interface IConsultationValidator
    {
        /// <summary>
        /// 验证患者是否可以开始看诊
        /// </summary>
        ValidationResult ValidatePatientForConsultation(PatientInfo patient);

        /// <summary>
        /// 验证看诊基本信息
        /// </summary>
        ValidationResult ValidateConsultationBasicInfo(ConsultationInfo consultation);

        /// <summary>
        /// 验证中医四诊信息
        /// </summary>
        ValidationResult ValidateTCMDiagnosis(TCMDiagnosisInfo diagnosis);

        /// <summary>
        /// 验证处方是否可以保存
        /// </summary>
        Task<ValidationResult> ValidatePrescriptionForSaveAsync(
            PrescriptionInfo prescription,
            ConsultationInfo consultation);

        /// <summary>
        /// 验证看诊是否可以完成
        /// </summary>
        ValidationResult ValidateConsultationCompletion(ConsultationInfo consultation);

        /// <summary>
        /// 验证症状描述
        /// </summary>
        ValidationResult ValidateSymptoms(string symptoms);

        /// <summary>
        /// 验证诊断结果
        /// </summary>
        ValidationResult ValidateDiagnosis(string diagnosis);

        /// <summary>
        /// 批量验证
        /// </summary>
        ValidationResult ValidateAll(params ValidationResult[] results);

        /// <summary>
        /// 获取所有验证规则描述
        /// </summary>
        Dictionary<string, string> GetValidationRules();
    }
}
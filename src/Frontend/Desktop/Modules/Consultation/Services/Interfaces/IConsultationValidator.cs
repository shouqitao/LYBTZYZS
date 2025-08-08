using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Prescriptions;

namespace LYBT.WPF.Client.Modules.Consultation.Services.Interfaces
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

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// 警告列表（不影响通过）
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 第一个错误消息
        /// </summary>
        public string? FirstError => Errors?.FirstOrDefault()?.Message;

        /// <summary>
        /// 所有错误消息
        /// </summary>
        public string AllErrors => string.Join("; ", Errors?.Select(e => e.Message) ?? Enumerable.Empty<string>());

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        public static ValidationResult Success() => new() { IsValid = true };

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        public static ValidationResult Failure(string field, string message)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError>
                {
                    new ValidationError { Field = field, Message = message }
                }
            };
        }

        /// <summary>
        /// 添加错误
        /// </summary>
        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationError { Field = field, Message = message });
            IsValid = false;
        }

        /// <summary>
        /// 添加警告
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误级别
        /// </summary>
        public ValidationErrorLevel Level { get; set; } = ValidationErrorLevel.Error;
    }

    /// <summary>
    /// 验证错误级别
    /// </summary>
    public enum ValidationErrorLevel
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 严重错误
        /// </summary>
        Critical
    }

    /// <summary>
    /// 中医四诊信息（简化版）
    /// </summary>
    public class TCMDiagnosisInfo
    {
        public string? Inspection { get; set; }  // 望诊
        public string? Auscultation { get; set; } // 闻诊
        public string? Inquiry { get; set; }      // 问诊
        public string? Palpation { get; set; }    // 切诊
        public string? Syndrome { get; set; }     // 辨证
        public string? Treatment { get; set; }    // 治法
    }
}
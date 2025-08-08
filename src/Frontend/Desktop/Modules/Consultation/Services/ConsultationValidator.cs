using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Core.Models.Validation;
using LYBT.WPF.Client.Modules.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.WPF.Client.Modules.Consultation.Services
{
    /// <summary>
    /// 看诊验证器 - 负责看诊流程中的所有验证逻辑
    /// </summary>
    public class ConsultationValidator : IConsultationValidator
    {
        #region 验证规则常量

        private const int MIN_SYMPTOM_LENGTH = 2;
        private const int MAX_SYMPTOM_LENGTH = 2000;
        private const int MIN_DIAGNOSIS_LENGTH = 2;
        private const int MAX_DIAGNOSIS_LENGTH = 1000;
        private const int MAX_CHIEF_COMPLAINT_LENGTH = 500;
        private const int MAX_TCM_FIELD_LENGTH = 1000;
        private const int MIN_PATIENT_AGE = 0;
        private const int MAX_PATIENT_AGE = 150;

        #endregion

        private readonly ILogger<ConsultationValidator> _logger;

        public ConsultationValidator(ILogger<ConsultationValidator> logger)
        {
            _logger = logger;
        }

        #region 患者验证

        /// <summary>
        /// 验证患者是否可以开始看诊
        /// </summary>
        public ValidationResult ValidatePatientForConsultation(PatientInfo patient)
        {
            var result = new ValidationResult { IsValid = true };

            if (patient == null)
            {
                return ValidationResult.Failure("Patient", "请选择患者");
            }

            // 验证患者ID
            if (patient.Id == Guid.Empty)
            {
                result.AddError("PatientId", "患者ID无效");
            }

            // 验证患者姓名
            if (string.IsNullOrWhiteSpace(patient.Name))
            {
                result.AddError("PatientName", "患者姓名不能为空");
            }

            // 验证患者年龄
            if (patient.Age < MIN_PATIENT_AGE || patient.Age > MAX_PATIENT_AGE)
            {
                result.AddWarning($"患者年龄异常: {patient.Age}岁");
            }

            // 验证患者状态
            if (!patient.IsActive)
            {
                result.AddError("PatientStatus", "患者状态为非活动，无法看诊");
            }

            // 验证手机号格式（如果有）
            if (!string.IsNullOrWhiteSpace(patient.PhoneNumber))
            {
                if (!IsValidPhoneNumber(patient.PhoneNumber))
                {
                    result.AddWarning("手机号格式可能不正确");
                }
            }

            return result;
        }

        #endregion

        #region 看诊信息验证

        /// <summary>
        /// 验证看诊基本信息
        /// </summary>
        public ValidationResult ValidateConsultationBasicInfo(ConsultationInfo consultation)
        {
            var result = new ValidationResult { IsValid = true };

            if (consultation == null)
            {
                return ValidationResult.Failure("Consultation", "看诊信息不能为空");
            }

            // 验证看诊ID
            if (consultation.Id == Guid.Empty)
            {
                result.AddError("ConsultationId", "看诊ID无效");
            }

            // 验证患者ID
            if (consultation.PatientId == Guid.Empty)
            {
                result.AddError("PatientId", "未关联患者");
            }

            // 验证主诉
            if (string.IsNullOrWhiteSpace(consultation.ChiefComplaint))
            {
                result.AddError("ChiefComplaint", "主诉不能为空");
            }
            else if (consultation.ChiefComplaint.Length > MAX_CHIEF_COMPLAINT_LENGTH)
            {
                result.AddError("ChiefComplaint", $"主诉长度不能超过{MAX_CHIEF_COMPLAINT_LENGTH}字");
            }

            // 验证症状
            var symptomsValidation = ValidateSymptoms(consultation.Symptoms);
            if (!symptomsValidation.IsValid)
            {
                foreach (var error in symptomsValidation.Errors)
                {
                    result.AddError(error.Field, error.Message);
                }
            }

            // 验证看诊时间
            if (consultation.ConsultationTime == default)
            {
                result.AddWarning("看诊时间未设置");
            }
            else if (consultation.ConsultationTime > DateTime.Now)
            {
                result.AddError("ConsultationTime", "看诊时间不能晚于当前时间");
            }

            return result;
        }

        /// <summary>
        /// 验证中医四诊信息
        /// </summary>
        public ValidationResult ValidateTCMDiagnosis(TCMDiagnosisInfo diagnosis)
        {
            var result = new ValidationResult { IsValid = true };

            if (diagnosis == null)
            {
                result.AddWarning("中医四诊信息为空");
                return result;
            }

            // 验证望诊
            if (!string.IsNullOrWhiteSpace(diagnosis.Inspection))
            {
                if (diagnosis.Inspection.Length > MAX_TCM_FIELD_LENGTH)
                {
                    result.AddError("Inspection", $"望诊内容不能超过{MAX_TCM_FIELD_LENGTH}字");
                }
                if (ContainsInvalidCharacters(diagnosis.Inspection))
                {
                    result.AddWarning("望诊内容包含特殊字符");
                }
            }

            // 验证闻诊
            if (!string.IsNullOrWhiteSpace(diagnosis.Auscultation))
            {
                if (diagnosis.Auscultation.Length > MAX_TCM_FIELD_LENGTH)
                {
                    result.AddError("Auscultation", $"闻诊内容不能超过{MAX_TCM_FIELD_LENGTH}字");
                }
            }

            // 验证问诊
            if (!string.IsNullOrWhiteSpace(diagnosis.Inquiry))
            {
                if (diagnosis.Inquiry.Length > MAX_TCM_FIELD_LENGTH)
                {
                    result.AddError("Inquiry", $"问诊内容不能超过{MAX_TCM_FIELD_LENGTH}字");
                }
            }

            // 验证切诊
            if (!string.IsNullOrWhiteSpace(diagnosis.Palpation))
            {
                if (diagnosis.Palpation.Length > MAX_TCM_FIELD_LENGTH)
                {
                    result.AddError("Palpation", $"切诊内容不能超过{MAX_TCM_FIELD_LENGTH}字");
                }
            }

            // 验证辨证
            if (string.IsNullOrWhiteSpace(diagnosis.Syndrome))
            {
                result.AddWarning("辨证结果为空，建议填写");
            }
            else if (diagnosis.Syndrome.Length > MAX_TCM_FIELD_LENGTH)
            {
                result.AddError("Syndrome", $"辨证内容不能超过{MAX_TCM_FIELD_LENGTH}字");
            }

            // 验证治法
            if (string.IsNullOrWhiteSpace(diagnosis.Treatment))
            {
                result.AddWarning("治法为空，建议填写");
            }
            else if (diagnosis.Treatment.Length > MAX_TCM_FIELD_LENGTH)
            {
                result.AddError("Treatment", $"治法内容不能超过{MAX_TCM_FIELD_LENGTH}字");
            }

            // 至少需要一项四诊内容
            if (string.IsNullOrWhiteSpace(diagnosis.Inspection) &&
                string.IsNullOrWhiteSpace(diagnosis.Auscultation) &&
                string.IsNullOrWhiteSpace(diagnosis.Inquiry) &&
                string.IsNullOrWhiteSpace(diagnosis.Palpation))
            {
                result.AddError("TCMDiagnosis", "中医四诊至少需要填写一项");
            }

            return result;
        }

        #endregion

        #region 处方验证

        /// <summary>
        /// 验证处方是否可以保存
        /// </summary>
        public async Task<ValidationResult> ValidatePrescriptionForSaveAsync(
            PrescriptionInfo prescription,
            ConsultationInfo consultation)
        {
            var result = new ValidationResult { IsValid = true };

            // 验证处方基本信息
            if (prescription == null)
            {
                return ValidationResult.Failure("Prescription", "处方信息不能为空");
            }

            // 验证关联的看诊
            if (consultation == null || consultation.Id == Guid.Empty)
            {
                result.AddError("Consultation", "处方必须关联有效的看诊记录");
            }

            // 验证处方项目
            if (prescription.Items == null || !prescription.Items.Any())
            {
                result.AddError("PrescriptionItems", "处方至少需要一味药材");
            }
            else
            {
                // 验证每个处方项目
                foreach (var item in prescription.Items)
                {
                    var itemValidation = ValidatePrescriptionItem(item);
                    if (!itemValidation.IsValid)
                    {
                        foreach (var error in itemValidation.Errors)
                        {
                            result.AddError($"Item_{item.HerbName}", error.Message);
                        }
                    }
                }

                // 检查重复药材
                var duplicates = prescription.Items
                    .GroupBy(x => x.HerbId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.First().HerbName);

                if (duplicates.Any())
                {
                    result.AddError("DuplicateHerbs", $"处方中存在重复药材: {string.Join(", ", duplicates)}");
                }

                // 验证处方药材数量
                if (prescription.Items.Count > 50)
                {
                    result.AddWarning($"处方包含{prescription.Items.Count}味药材，数量较多");
                }
            }

            // 验证诊断
            if (string.IsNullOrWhiteSpace(prescription.Diagnosis))
            {
                result.AddError("Diagnosis", "诊断结果不能为空");
            }
            else
            {
                var diagnosisValidation = ValidateDiagnosis(prescription.Diagnosis);
                if (!diagnosisValidation.IsValid)
                {
                    foreach (var error in diagnosisValidation.Errors)
                    {
                        result.AddError(error.Field, error.Message);
                    }
                }
            }

            // 验证用法用量
            if (string.IsNullOrWhiteSpace(prescription.Usage))
            {
                result.AddError("Usage", "用法用量不能为空");
            }

            // 验证剂型
            if (string.IsNullOrWhiteSpace(prescription.DosageForm))
            {
                result.AddWarning("剂型未指定");
            }

            // 验证总价
            if (prescription.TotalAmount <= 0)
            {
                result.AddError("TotalAmount", "处方总价无效");
            }

            // 异步验证（可以添加更多异步验证逻辑）
            await Task.CompletedTask;

            return result;
        }

        /// <summary>
        /// 验证处方项目
        /// </summary>
        private ValidationResult ValidatePrescriptionItem(PrescriptionItemInfo item)
        {
            var result = new ValidationResult { IsValid = true };

            if (item == null)
            {
                return ValidationResult.Failure("Item", "处方项目不能为空");
            }

            // 验证药材ID
            if (item.HerbId == Guid.Empty)
            {
                result.AddError("HerbId", "药材ID无效");
            }

            // 验证药材名称
            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                result.AddError("HerbName", "药材名称不能为空");
            }

            // 验证数量
            if (item.Quantity <= 0)
            {
                result.AddError("Quantity", $"药材{item.HerbName}的数量必须大于0");
            }
            else if (item.Quantity > 1000)
            {
                result.AddError("Quantity", $"药材{item.HerbName}的数量过大（>1000）");
            }

            // 验证单价
            if (item.UnitPrice <= 0)
            {
                result.AddError("UnitPrice", $"药材{item.HerbName}的单价无效");
            }

            // 验证小计
            var expectedSubtotal = item.Quantity * item.UnitPrice;
            if (Math.Abs(item.Subtotal - expectedSubtotal) > 0.01m)
            {
                result.AddWarning($"药材{item.HerbName}的小计金额可能不正确");
            }

            return result;
        }

        #endregion

        #region 完成验证

        /// <summary>
        /// 验证看诊是否可以完成
        /// </summary>
        public ValidationResult ValidateConsultationCompletion(ConsultationInfo consultation)
        {
            var result = new ValidationResult { IsValid = true };

            // 验证基本信息
            var basicValidation = ValidateConsultationBasicInfo(consultation);
            if (!basicValidation.IsValid)
            {
                return basicValidation;
            }

            // 验证是否有诊断
            if (string.IsNullOrWhiteSpace(consultation.Diagnosis))
            {
                result.AddError("Diagnosis", "完成看诊前必须填写诊断");
            }

            // 验证是否有治疗原则
            if (string.IsNullOrWhiteSpace(consultation.TreatmentPrinciple))
            {
                result.AddWarning("未填写治疗原则");
            }

            // 验证状态 - 检查记录是否处于活跃状态
            if (consultation.Status == CommonStatus.Disabled)
            {
                result.AddError("Status", "该看诊记录已被禁用，无法完成");
            }

            return result;
        }

        #endregion

        #region 具体字段验证

        /// <summary>
        /// 验证症状描述
        /// </summary>
        public ValidationResult ValidateSymptoms(string symptoms)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(symptoms))
            {
                result.AddError("Symptoms", "症状描述不能为空");
                return result;
            }

            if (symptoms.Length < MIN_SYMPTOM_LENGTH)
            {
                result.AddError("Symptoms", $"症状描述至少需要{MIN_SYMPTOM_LENGTH}个字");
            }
            else if (symptoms.Length > MAX_SYMPTOM_LENGTH)
            {
                result.AddError("Symptoms", $"症状描述不能超过{MAX_SYMPTOM_LENGTH}字");
            }

            // 检查是否包含敏感词或无效内容
            if (ContainsInvalidContent(symptoms))
            {
                result.AddWarning("症状描述可能包含无效内容");
            }

            return result;
        }

        /// <summary>
        /// 验证诊断结果
        /// </summary>
        public ValidationResult ValidateDiagnosis(string diagnosis)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                result.AddError("Diagnosis", "诊断结果不能为空");
                return result;
            }

            if (diagnosis.Length < MIN_DIAGNOSIS_LENGTH)
            {
                result.AddError("Diagnosis", $"诊断结果至少需要{MIN_DIAGNOSIS_LENGTH}个字");
            }
            else if (diagnosis.Length > MAX_DIAGNOSIS_LENGTH)
            {
                result.AddError("Diagnosis", $"诊断结果不能超过{MAX_DIAGNOSIS_LENGTH}字");
            }

            // 检查是否包含中医诊断术语
            if (!ContainsTCMTerms(diagnosis))
            {
                result.AddWarning("诊断结果建议包含中医证型或病名");
            }

            return result;
        }

        #endregion

        #region 批量验证

        /// <summary>
        /// 批量验证
        /// </summary>
        public ValidationResult ValidateAll(params ValidationResult[] results)
        {
            var combinedResult = new ValidationResult { IsValid = true };

            foreach (var result in results)
            {
                if (!result.IsValid)
                {
                    combinedResult.IsValid = false;
                    combinedResult.Errors.AddRange(result.Errors);
                }
                combinedResult.Warnings.AddRange(result.Warnings);
            }

            return combinedResult;
        }

        #endregion

        #region 验证规则说明

        /// <summary>
        /// 获取所有验证规则描述
        /// </summary>
        public Dictionary<string, string> GetValidationRules()
        {
            return new Dictionary<string, string>
            {
                ["患者姓名"] = "必填，不能为空",
                ["患者年龄"] = $"必须在{MIN_PATIENT_AGE}-{MAX_PATIENT_AGE}岁之间",
                ["主诉"] = $"必填，不超过{MAX_CHIEF_COMPLAINT_LENGTH}字",
                ["症状描述"] = $"必填，{MIN_SYMPTOM_LENGTH}-{MAX_SYMPTOM_LENGTH}字",
                ["诊断结果"] = $"必填，{MIN_DIAGNOSIS_LENGTH}-{MAX_DIAGNOSIS_LENGTH}字，建议包含中医证型",
                ["中医四诊"] = $"至少填写一项，每项不超过{MAX_TCM_FIELD_LENGTH}字",
                ["处方药材"] = "至少一味，最多50味，无重复",
                ["药材数量"] = "大于0，不超过1000",
                ["用法用量"] = "必填",
                ["手机号"] = "11位数字，可选"
            };
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证手机号格式
        /// </summary>
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // 中国手机号正则
            var regex = new Regex(@"^1[3-9]\d{9}$");
            return regex.IsMatch(phoneNumber);
        }

        /// <summary>
        /// 检查是否包含无效字符
        /// </summary>
        private bool ContainsInvalidCharacters(string text)
        {
            // 检查是否包含SQL注入常见字符
            var invalidPatterns = new[] { "--", "/*", "*/", "xp_", "sp_", "drop ", "truncate " };
            var lowerText = text.ToLower();
            return invalidPatterns.Any(pattern => lowerText.Contains(pattern));
        }

        /// <summary>
        /// 检查是否包含无效内容
        /// </summary>
        private bool ContainsInvalidContent(string text)
        {
            // 检查是否全是重复字符
            if (text.Length > 10 && text.Distinct().Count() < 3)
            {
                return true;
            }

            // 检查是否包含过多特殊字符
            var specialCharCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
            if (specialCharCount > text.Length * 0.5)
            {
                return true;
            }

            return ContainsInvalidCharacters(text);
        }

        /// <summary>
        /// 检查是否包含中医术语
        /// </summary>
        private bool ContainsTCMTerms(string text)
        {
            var tcmTerms = new[]
            {
                "证", "型", "虚", "实", "寒", "热", "湿", "燥", 
                "气", "血", "阴", "阳", "脾", "肾", "肝", "心", "肺",
                "风", "火", "痰", "瘀", "郁", "滞"
            };

            return tcmTerms.Any(term => text.Contains(term));
        }

        #endregion
    }

    /// <summary>
    /// 看诊状态枚举
    /// </summary>
    public enum ConsultationStatus
    {
        /// <summary>
        /// 进行中
        /// </summary>
        InProgress,

        /// <summary>
        /// 已完成
        /// </summary>
        Completed,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }
}
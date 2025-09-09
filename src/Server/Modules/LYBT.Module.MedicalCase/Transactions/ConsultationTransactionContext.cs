using System;
using System.Collections.Generic;
using LYBT.Infrastructure.Transactions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Transactions
{
    /// <summary>
    /// 看诊流程事务上下文
    /// 包含诊疗流程中的所有必要数据传递
    /// </summary>
    public class ConsultationTransactionContext : TransactionContext
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名（用于显示和验证）
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }

        /// <summary>
        /// 医生姓名（用于显示和验证）
        /// </summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>
        /// 创建的医疗案例ID
        /// </summary>
        public Guid? MedicalCaseId { get; set; }

        /// <summary>
        /// 创建的看诊记录ID
        /// </summary>
        public Guid? ConsultationId { get; set; }

        /// <summary>
        /// 处方ID（可选）
        /// </summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>
        /// 看诊时间
        /// </summary>
        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 医疗案例状态
        /// </summary>
        public MedicalCaseStatus? MedicalCaseStatus { get; set; }

        /// <summary>
        /// 患者原始状态（用于回滚）
        /// </summary>
        public string? OriginalPatientStatus { get; set; }

        /// <summary>
        /// 业务验证结果
        /// </summary>
        public Dictionary<string, object> ValidationResults { get; set; } = new();

        /// <summary>
        /// 诊疗流程元数据
        /// </summary>
        public Dictionary<string, object> ConsultationMetadata { get; set; } = new();

        /// <summary>
        /// 备注信息
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 主诉（来自前端输入）
        /// </summary>
        public string? ChiefComplaint { get; set; }

        /// <summary>
        /// 现病史（来自前端输入）
        /// </summary>
        public string? PresentIllness { get; set; }

        /// <summary>
        /// 是否需要创建处方
        /// </summary>
        public bool RequiresPrescription { get; set; }

        /// <summary>
        /// 是否为急诊
        /// </summary>
        public bool IsEmergency { get; set; }

        /// <summary>
        /// 优先级（1-5，1最高）
        /// </summary>
        public int Priority { get; set; } = 3;

        /// <summary>
        /// 获取上下文摘要信息
        /// </summary>
        /// <returns>上下文摘要</returns>
        public ConsultationTransactionSummary GetSummary()
        {
            return new ConsultationTransactionSummary
            {
                PatientId = PatientId,
                PatientName = PatientName,
                DoctorId = DoctorId,
                DoctorName = DoctorName,
                ConsultationDate = ConsultationDate,
                MedicalCaseId = MedicalCaseId,
                ConsultationId = ConsultationId,
                Status = MedicalCaseStatus?.ToString() ?? "Unknown",
                IsEmergency = IsEmergency,
                Priority = Priority
            };
        }

        /// <summary>
        /// 验证上下文数据完整性
        /// </summary>
        /// <returns>验证结果和错误信息</returns>
        public (bool IsValid, List<string> Errors) ValidateContext()
        {
            var errors = new List<string>();

            if (PatientId == Guid.Empty)
                errors.Add("患者ID不能为空");

            if (string.IsNullOrEmpty(PatientName))
                errors.Add("患者姓名不能为空");

            if (DoctorId == Guid.Empty)
                errors.Add("医生ID不能为空");

            if (string.IsNullOrEmpty(DoctorName))
                errors.Add("医生姓名不能为空");

            if (ConsultationDate > DateTime.Now.AddHours(1))
                errors.Add("看诊时间不能超过当前时间1小时");

            if (ConsultationDate < DateTime.Now.AddDays(-1))
                errors.Add("看诊时间不能早于昨天");

            if (Priority < 1 || Priority > 5)
                errors.Add("优先级必须在1-5之间");

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 设置验证结果
        /// </summary>
        /// <param name="key">验证项键</param>
        /// <param name="result">验证结果</param>
        public void SetValidationResult(string key, object result)
        {
            ValidationResults[key] = result;
        }

        /// <summary>
        /// 获取验证结果
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="key">验证项键</param>
        /// <returns>验证结果</returns>
        public T? GetValidationResult<T>(string key)
        {
            if (ValidationResults.TryGetValue(key, out var result) && result is T typedResult)
            {
                return typedResult;
            }

            return default(T);
        }
    }

    /// <summary>
    /// 看诊事务摘要信息
    /// </summary>
    public class ConsultationTransactionSummary
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime ConsultationDate { get; set; }
        public Guid? MedicalCaseId { get; set; }
        public Guid? ConsultationId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public int Priority { get; set; }
    }
}

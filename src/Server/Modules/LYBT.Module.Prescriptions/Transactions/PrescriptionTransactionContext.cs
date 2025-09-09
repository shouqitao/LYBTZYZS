using System;
using System.Collections.Generic;
using LYBT.Infrastructure.Transactions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Transactions
{
    /// <summary>
    /// 处方创建事务上下文
    /// 包含处方创建流程中的所有必要数据传递
    /// </summary>
    public class PrescriptionTransactionContext : TransactionContext
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
        /// 医疗案例ID（必须关联现有医疗案例）
        /// </summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 诊断记录ID（可选，如果提供则验证关联性）
        /// </summary>
        public Guid? ConsultationId { get; set; }

        /// <summary>
        /// 创建的处方ID（事务过程中生成）
        /// </summary>
        public Guid? PrescriptionId { get; set; }

        /// <summary>
        /// 处方状态
        /// </summary>
        public PrescriptionStatus PrescriptionStatus { get; set; } = PrescriptionStatus.Draft;

        /// <summary>
        /// 主治（适应症/主要症状描述）
        /// </summary>
        public string Indication { get; set; } = string.Empty;

        /// <summary>
        /// 处方帖数
        /// </summary>
        public int DosageCount { get; set; } = 7;

        /// <summary>
        /// 折扣（0-1之间，0.8表示8折）
        /// </summary>
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>
        /// 医嘱
        /// </summary>
        public string? Advice { get; set; }

        /// <summary>
        /// 验方来源（如果基于验方创建）
        /// </summary>
        public string? FormulaSource { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 处方药材项目列表
        /// </summary>
        public List<PrescriptionItemContext> Items { get; set; } = new();

        /// <summary>
        /// 是否需要验证配伍安全性
        /// </summary>
        public bool RequireCompatibilityCheck { get; set; } = true;

        /// <summary>
        /// 是否自动计算价格
        /// </summary>
        public bool AutoCalculatePrice { get; set; } = true;

        /// <summary>
        /// 处方总价格（计算后存储）
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// 业务验证结果
        /// </summary>
        public Dictionary<string, object> ValidationResults { get; set; } = new();

        /// <summary>
        /// 处方创建元数据
        /// </summary>
        public Dictionary<string, object> PrescriptionMetadata { get; set; } = new();

        /// <summary>
        /// 获取上下文摘要信息
        /// </summary>
        /// <returns>上下文摘要</returns>
        public PrescriptionTransactionSummary GetSummary()
        {
            return new PrescriptionTransactionSummary
            {
                PatientId = PatientId,
                PatientName = PatientName,
                DoctorId = DoctorId,
                DoctorName = DoctorName,
                MedicalCaseId = MedicalCaseId,
                ConsultationId = ConsultationId,
                PrescriptionId = PrescriptionId,
                Status = PrescriptionStatus.ToString(),
                Indication = Indication,
                ItemCount = Items.Count,
                TotalPrice = TotalPrice,
                RequireCompatibilityCheck = RequireCompatibilityCheck
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

            if (MedicalCaseId == Guid.Empty)
                errors.Add("医疗案例ID不能为空");

            if (string.IsNullOrEmpty(Indication))
                errors.Add("主治不能为空");

            if (DosageCount <= 0 || DosageCount > 100)
                errors.Add("处方帖数必须在1-100之间");

            if (Discount < 0 || Discount > 1)
                errors.Add("折扣必须在0-1之间");

            if (Items == null || Items.Count == 0)
                errors.Add("处方必须包含至少一味药材");

            // 验证药材项目
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (item.HerbId == Guid.Empty)
                    errors.Add($"第{i + 1}项药材ID不能为空");

                if (string.IsNullOrEmpty(item.HerbName))
                    errors.Add($"第{i + 1}项药材名称不能为空");

                if (item.Quantity <= 0)
                    errors.Add($"第{i + 1}项药材用量必须大于0");

                if (item.UnitPrice < 0)
                    errors.Add($"第{i + 1}项药材单价不能为负数");
            }

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

        /// <summary>
        /// 计算处方总价
        /// </summary>
        public void CalculateTotalPrice()
        {
            if (Items == null || Items.Count == 0)
            {
                TotalPrice = 0m;
                return;
            }

            var subtotal = Items.Sum(item => item.UnitPrice * item.Quantity);
            var singleDosePrice = subtotal * Discount;
            TotalPrice = singleDosePrice * DosageCount;
        }
    }

    /// <summary>
    /// 处方药材项目上下文
    /// </summary>
    public class PrescriptionItemContext
    {
        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 用量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = "g";

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 用法说明
        /// </summary>
        public string? Usage { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 小计金额
        /// </summary>
        public decimal Amount => UnitPrice * Quantity;
    }

    /// <summary>
    /// 处方事务摘要信息
    /// </summary>
    public class PrescriptionTransactionSummary
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public Guid MedicalCaseId { get; set; }
        public Guid? ConsultationId { get; set; }
        public Guid? PrescriptionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Indication { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalPrice { get; set; }
        public bool RequireCompatibilityCheck { get; set; }
    }
}

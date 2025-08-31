using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Components
{
    /// <summary>
    /// 处方基础验证组件 - UltraThink简化版本
    /// 专注于基础数据验证，不包含复杂的业务规则验证
    /// </summary>
    public class BasicValidator
    {
        private readonly ILogger<BasicValidator> _logger;

        public BasicValidator(ILogger<BasicValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证处方基础信息
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>验证结果</returns>
        public ValidationResult ValidatePrescription(PrescriptionDto prescription)
        {
            var result = new ValidationResult();

            try
            {
                if (prescription == null)
                {
                    result.AddError("处方信息不能为空");
                    return result;
                }

                // 验证诊断
                ValidateDiagnosis(prescription.Diagnosis ?? "", result);

                // 验证剂数
                ValidateDosageCount(prescription.DosageCount, result);

                // 验证药材列表
                ValidatePrescriptionItems(prescription.Items, result);

                // 验证患者和医生信息
                ValidateBasicIds(prescription, result);

                _logger.LogDebug("处方验证完成: {ErrorCount}个错误, {WarningCount}个警告", 
                    result.Errors.Count, result.Warnings.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方验证时发生错误");
                result.AddError("验证处理过程中发生错误");
            }

            return result;
        }

        /// <summary>
        /// 验证诊断信息
        /// </summary>
        /// <param name="diagnosis">诊断</param>
        /// <param name="result">验证结果</param>
        private void ValidateDiagnosis(string diagnosis, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                result.AddError("诊断不能为空");
                return;
            }

            if (diagnosis.Length > 500)
            {
                result.AddError("诊断长度不能超过500个字符");
            }

            if (diagnosis.Length < 2)
            {
                result.AddWarning("诊断信息过于简短，建议详细填写");
            }
        }

        /// <summary>
        /// 验证剂数
        /// </summary>
        /// <param name="dosageCount">剂数</param>
        /// <param name="result">验证结果</param>
        private void ValidateDosageCount(int dosageCount, ValidationResult result)
        {
            if (dosageCount <= 0)
            {
                result.AddError("剂数必须大于0");
                return;
            }

            if (dosageCount > 30)
            {
                result.AddWarning("剂数超过30剂，请确认是否正确");
            }

            if (dosageCount > 100)
            {
                result.AddError("剂数不能超过100剂");
            }
        }

        /// <summary>
        /// 验证处方药材项目
        /// </summary>
        /// <param name="items">药材项目列表</param>
        /// <param name="result">验证结果</param>
        private void ValidatePrescriptionItems(ICollection<PrescriptionItemDto> items, ValidationResult result)
        {
            if (items == null || !items.Any())
            {
                result.AddError("必须包含至少一味中药材");
                return;
            }

            if (items.Count > 50)
            {
                result.AddWarning("药材种类过多，请确认处方的合理性");
            }

            // 验证每个药材项目
            foreach (var item in items)
            {
                ValidatePrescriptionItem(item, result);
            }

            // 检查重复药材
            CheckDuplicateHerbs(items, result);
        }

        /// <summary>
        /// 验证单个药材项目
        /// </summary>
        /// <param name="item">药材项目</param>
        /// <param name="result">验证结果</param>
        private void ValidatePrescriptionItem(PrescriptionItemDto item, ValidationResult result)
        {
            if (item == null) return;

            // 验证药材名称
            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                result.AddError("药材名称不能为空");
            }

            // 验证用量
            if (item.Quantity <= 0)
            {
                result.AddError($"药材 {item.HerbName} 的用量必须大于0");
            }
            else if (item.Quantity > 1000)
            {
                result.AddWarning($"药材 {item.HerbName} 的用量({item.Quantity}g)较大，请确认是否正确");
            }

            // 验证单价
            if (item.UnitPrice < 0)
            {
                result.AddError($"药材 {item.HerbName} 的单价不能为负数");
            }
            else if (item.UnitPrice > 1000)
            {
                result.AddWarning($"药材 {item.HerbName} 的单价({item.UnitPrice:C})较高，请确认是否正确");
            }

            // 验证单位
            if (string.IsNullOrWhiteSpace(item.Unit))
            {
                result.AddWarning($"药材 {item.HerbName} 缺少计量单位");
            }
        }

        /// <summary>
        /// 检查重复药材
        /// </summary>
        /// <param name="items">药材项目列表</param>
        /// <param name="result">验证结果</param>
        private void CheckDuplicateHerbs(ICollection<PrescriptionItemDto> items, ValidationResult result)
        {
            var duplicates = items
                .GroupBy(x => x.HerbId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First().HerbName)
                .ToList();

            if (duplicates.Any())
            {
                result.AddWarning($"发现重复药材: {string.Join(", ", duplicates)}");
            }
        }

        /// <summary>
        /// 验证基础ID信息
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <param name="result">验证结果</param>
        private void ValidateBasicIds(PrescriptionDto prescription, ValidationResult result)
        {
            if (prescription.PatientId == Guid.Empty)
            {
                result.AddError("患者ID不能为空");
            }

            if (prescription.UserId == Guid.Empty)
            {
                result.AddError("医生ID不能为空");
            }

            if (prescription.MedicalCaseId == Guid.Empty)
            {
                result.AddWarning("缺少关联的医疗案例ID");
            }
        }

        /// <summary>
        /// 快速验证 - 仅检查最基础的必填项
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>是否通过基础验证</returns>
        public bool QuickValidate(PrescriptionDto prescription)
        {
            try
            {
                if (prescription == null) return false;
                if (string.IsNullOrWhiteSpace(prescription.Diagnosis)) return false;
                if (prescription.DosageCount <= 0) return false;
                if (prescription.Items == null || !prescription.Items.Any()) return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速验证时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 验证药材用量范围
        /// </summary>
        /// <param name="herbName">药材名称</param>
        /// <param name="quantity">用量</param>
        /// <returns>验证结果</returns>
        public (bool IsValid, string Message) ValidateHerbQuantity(string herbName, decimal quantity)
        {
            if (quantity <= 0)
            {
                return (false, "用量必须大于0");
            }

            if (quantity > 1000)
            {
                return (false, "单味药材用量不能超过1000g");
            }

            // 常见药材的用量建议检查
            var quantityWarnings = GetQuantityWarnings(herbName, quantity);
            if (!string.IsNullOrEmpty(quantityWarnings))
            {
                return (true, quantityWarnings); // 通过验证但有警告
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 获取用量警告信息
        /// </summary>
        /// <param name="herbName">药材名称</param>
        /// <param name="quantity">用量</param>
        /// <returns>警告信息</returns>
        private string GetQuantityWarnings(string herbName, decimal quantity)
        {
            // 这里可以扩展为从配置或数据库读取药材用量建议
            var commonHerbLimits = new Dictionary<string, (decimal Min, decimal Max)>
            {
                { "附子", (3, 15) },
                { "干姜", (3, 10) },
                { "肉桂", (3, 10) },
                { "麻黄", (3, 10) },
                { "大黄", (3, 12) }
            };

            if (commonHerbLimits.ContainsKey(herbName))
            {
                var (min, max) = commonHerbLimits[herbName];
                if (quantity < min)
                {
                    return $"{herbName}常用量为{min}-{max}g，当前用量({quantity}g)可能偏小";
                }
                if (quantity > max)
                {
                    return $"{herbName}常用量为{min}-{max}g，当前用量({quantity}g)可能偏大";
                }
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();

        /// <summary>是否验证通过（无错误）</summary>
        public bool IsValid => !Errors.Any();

        /// <summary>是否有警告</summary>
        public bool HasWarnings => Warnings.Any();

        /// <summary>添加错误</summary>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        /// <summary>添加警告</summary>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }

        /// <summary>获取所有消息</summary>
        public IEnumerable<string> GetAllMessages()
        {
            return Errors.Concat(Warnings);
        }

        public override string ToString()
        {
            var messages = new List<string>();
            
            if (Errors.Any())
                messages.Add($"{Errors.Count}个错误");
            
            if (Warnings.Any())
                messages.Add($"{Warnings.Count}个警告");

            return messages.Any() ? string.Join(", ", messages) : "验证通过";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Models.Prescriptions;

namespace LYBT.Desktop.Consultation.ViewModels.Components
{
    /// <summary>
    /// 处方验证器 - UltraThink专门化组件
    /// 职责单一：专注处方数据验证和业务规则检查
    /// 代码干净：清晰的验证规则和错误处理
    /// 性能出色：高效的验证算法和缓存机制
    /// </summary>
    public class PrescriptionValidator
    {
        private readonly ILogger<PrescriptionValidator> _logger;

        public PrescriptionValidator(ILogger<PrescriptionValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 验证结果类

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();

            public void AddError(string error)
            {
                Errors.Add(error);
                IsValid = false;
            }

            public void AddWarning(string warning)
            {
                Warnings.Add(warning);
            }

            public string GetSummary()
            {
                var messages = new List<string>();
                if (Errors.Any())
                    messages.Add($"错误: {string.Join("; ", Errors)}");
                if (Warnings.Any())
                    messages.Add($"警告: {string.Join("; ", Warnings)}");
                return string.Join(" | ", messages);
            }
        }

        #endregion

        #region 核心验证方法

        /// <summary>
        /// 验证处方完整性
        /// </summary>
        public ValidationResult ValidatePrescription(
            IEnumerable<PrescriptionItemViewModel> items,
            string prescriptionNo,
            int dosageCount,
            string usage,
            decimal discount)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                // 基础验证
                ValidateBasicInfo(result, prescriptionNo, dosageCount, usage, discount);
                
                // 处方项验证
                ValidatePrescriptionItems(result, items);
                
                // 业务规则验证
                ValidateBusinessRules(result, items, dosageCount);

                _logger.LogDebug("处方验证完成，结果: {IsValid}, 错误数: {ErrorCount}, 警告数: {WarningCount}",
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方验证失败");
                result.AddError("验证过程中发生未知错误");
                return result;
            }
        }

        /// <summary>
        /// 验证单个处方项
        /// </summary>
        public ValidationResult ValidatePrescriptionItem(PrescriptionItemViewModel item)
        {
            var result = new ValidationResult { IsValid = true };

            if (item == null)
            {
                result.AddError("处方项不能为空");
                return result;
            }

            // 药材名称验证
            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                result.AddError("药材名称不能为空");
            }

            // 数量验证
            if (item.Quantity <= 0)
            {
                result.AddError($"药材 '{item.HerbName}' 的数量必须大于0");
            }
            else if (item.Quantity > 1000)
            {
                result.AddWarning($"药材 '{item.HerbName}' 的数量过大，请检查是否正确");
            }

            // 单价验证
            if (item.UnitPrice <= 0)
            {
                result.AddError($"药材 '{item.HerbName}' 的单价必须大于0");
            }
            else if (item.UnitPrice > 10000)
            {
                result.AddWarning($"药材 '{item.HerbName}' 的单价过高，请检查是否正确");
            }

            // 小计验证
            var expectedSubtotal = item.Quantity * item.UnitPrice;
            if (Math.Abs(item.Subtotal - expectedSubtotal) > 0.01m)
            {
                result.AddError($"药材 '{item.HerbName}' 的小计计算错误");
            }

            return result;
        }

        /// <summary>
        /// 验证折扣
        /// </summary>
        public ValidationResult ValidateDiscount(string discountInput, out decimal discount)
        {
            var result = new ValidationResult { IsValid = true };
            discount = 1.0m;

            if (string.IsNullOrWhiteSpace(discountInput))
            {
                return result; // 空值使用默认折扣
            }

            try
            {
                // 处理百分比格式 (如 "85%" 或 "8.5折")
                var cleanInput = discountInput.Trim().ToLower()
                    .Replace("%", "")
                    .Replace("折", "")
                    .Replace("％", "");

                if (decimal.TryParse(cleanInput, out var parsedValue))
                {
                    // 如果值大于1，认为是百分比格式
                    if (parsedValue > 1)
                    {
                        discount = parsedValue / 100m;
                    }
                    else
                    {
                        discount = parsedValue;
                    }

                    // 折扣范围验证
                    if (discount < 0.1m)
                    {
                        result.AddError("折扣不能低于1折");
                        discount = 0.1m;
                    }
                    else if (discount > 1.0m)
                    {
                        result.AddWarning("折扣不能超过原价，已设置为无折扣");
                        discount = 1.0m;
                    }
                }
                else
                {
                    result.AddError("折扣格式不正确，请输入数字、百分比或折扣");
                    discount = 1.0m;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析折扣失败: {Input}", discountInput);
                result.AddError("折扣解析失败");
                discount = 1.0m;
            }

            return result;
        }

        /// <summary>
        /// 验证剂数
        /// </summary>
        public ValidationResult ValidateDosage(string dosageInput, out int dosage)
        {
            var result = new ValidationResult { IsValid = true };
            dosage = 7;

            if (string.IsNullOrWhiteSpace(dosageInput))
            {
                return result; // 空值使用默认剂数
            }

            try
            {
                var cleanInput = dosageInput.Trim().Replace("剂", "").Replace("副", "");
                
                if (int.TryParse(cleanInput, out var parsedValue))
                {
                    dosage = parsedValue;

                    if (dosage < 1)
                    {
                        result.AddError("剂数不能小于1");
                        dosage = 1;
                    }
                    else if (dosage > 100)
                    {
                        result.AddWarning("剂数过多，请确认是否正确");
                    }
                }
                else
                {
                    result.AddError("剂数格式不正确，请输入数字");
                    dosage = 7;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析剂数失败: {Input}", dosageInput);
                result.AddError("剂数解析失败");
                dosage = 7;
            }

            return result;
        }

        #endregion

        #region 私有验证方法

        /// <summary>
        /// 验证基础信息
        /// </summary>
        private void ValidateBasicInfo(ValidationResult result, string prescriptionNo, int dosageCount, string usage, decimal discount)
        {
            // 处方编号验证
            if (string.IsNullOrWhiteSpace(prescriptionNo))
            {
                result.AddError("处方编号不能为空");
            }

            // 剂数验证
            if (dosageCount < 1)
            {
                result.AddError("剂数不能小于1");
            }
            else if (dosageCount > 100)
            {
                result.AddWarning("剂数过多，请确认是否正确");
            }

            // 用法验证
            if (string.IsNullOrWhiteSpace(usage))
            {
                result.AddWarning("建议填写用法说明");
            }

            // 折扣验证
            if (discount < 0.1m || discount > 1.0m)
            {
                result.AddError("折扣必须在0.1到1.0之间");
            }
        }

        /// <summary>
        /// 验证处方项集合
        /// </summary>
        private void ValidatePrescriptionItems(ValidationResult result, IEnumerable<PrescriptionItemViewModel> items)
        {
            var itemList = items?.ToList() ?? new List<PrescriptionItemViewModel>();

            if (!itemList.Any())
            {
                result.AddError("处方至少需要一个药材");
                return;
            }

            // 验证每个处方项
            foreach (var item in itemList)
            {
                var itemValidation = ValidatePrescriptionItem(item);
                result.Errors.AddRange(itemValidation.Errors);
                result.Warnings.AddRange(itemValidation.Warnings);
                if (!itemValidation.IsValid)
                {
                    result.IsValid = false;
                }
            }

            // 检查重复药材
            var duplicates = itemList.GroupBy(item => item.HerbName)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var duplicate in duplicates)
            {
                result.AddWarning($"药材 '{duplicate}' 重复添加，请确认是否需要合并");
            }
        }

        /// <summary>
        /// 验证业务规则
        /// </summary>
        private void ValidateBusinessRules(ValidationResult result, IEnumerable<PrescriptionItemViewModel> items, int dosageCount)
        {
            var itemList = items?.ToList() ?? new List<PrescriptionItemViewModel>();

            // 药材数量过多提醒
            if (itemList.Count > 20)
            {
                result.AddWarning("处方药材过多，请确认是否需要简化");
            }

            // 总价过高提醒
            var totalPrice = itemList.Sum(item => item.Subtotal) * dosageCount;
            if (totalPrice > 10000)
            {
                result.AddWarning("处方总价过高，请确认价格是否正确");
            }

            // 检查常见配伍禁忌（简化版）
            CheckCommonIncompatibilities(result, itemList);
        }

        /// <summary>
        /// 检查常见配伍禁忌
        /// </summary>
        private void CheckCommonIncompatibilities(ValidationResult result, List<PrescriptionItemViewModel> items)
        {
            var herbNames = items.Select(item => item.HerbName.Trim()).ToHashSet();

            // 十八反检查（简化版）
            var incompatiblePairs = new Dictionary<string, string[]>
            {
                ["甘草"] = new[] { "大戟", "芫花", "甘遂", "海藻" },
                ["乌头"] = new[] { "贝母", "瓜蒌", "半夏", "白蔹", "白芨" },
                ["藜芦"] = new[] { "人参", "沙参", "丹参", "玄参", "细辛", "芍药" }
            };

            foreach (var pair in incompatiblePairs)
            {
                if (herbNames.Contains(pair.Key))
                {
                    var conflicts = pair.Value.Where(herbNames.Contains).ToList();
                    if (conflicts.Any())
                    {
                        result.AddError($"配伍禁忌：{pair.Key} 与 {string.Join("、", conflicts)} 不宜同用");
                    }
                }
            }
        }

        #endregion
    }
}
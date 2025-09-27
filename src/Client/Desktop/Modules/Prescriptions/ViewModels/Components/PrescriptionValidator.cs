using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.ViewModels.Components
{

    /// <summary>
    /// 处方验证器 - 简化版，只保留基本验证
    /// 去除过度设计，保持简单实用
    /// </summary>
    public class PrescriptionValidator
    {
        private readonly ILogger<PrescriptionValidator> _logger;

        public PrescriptionValidator(ILogger<PrescriptionValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 验证结果类 - 简化版

        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
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

            public string GetErrorSummary()
            {
                return Errors.Any() ? string.Join("; ", Errors) : string.Empty;
            }

            public string GetSummary()
            {
                var parts = new List<string>();
                if (Errors.Any())
                    parts.Add($"错误: {string.Join("; ", Errors)}");
                if (Warnings.Any())
                    parts.Add($"警告: {string.Join("; ", Warnings)}");
                return parts.Any() ? string.Join(" | ", parts) : "验证通过";
            }
        }

        #endregion 验证结果类

        #region 核心验证方法 - 简化版

        /// <summary>
        /// 验证处方完整性 - 只保留基本验证
        /// </summary>
        public ValidationResult ValidatePrescription(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount)
        {
            var result = new ValidationResult();

            try
            {
                // 简化验证，只检查最基本的规则
                var itemList = items?.ToList() ?? new List<PrescriptionItemViewModel>();

                if (!itemList.Any())
                {
                    result.AddError("处方至少需要一个药材");
                    return result;
                }

                if (dosageCount < 1 || dosageCount > 100)
                {
                    result.AddError("剂数必须在1-100之间");
                }

                // 验证各个处方项
                foreach (var item in itemList)
                {
                    ValidatePrescriptionItem(result, item);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方验证失败");
                result.AddError("验证过程中发生错误");
                return result;
            }
        }

        /// <summary>
        /// 验证处方完整性 - 5参数重载版本
        /// </summary>
        public ValidationResult ValidatePrescription(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount,
            decimal discount,
            string notes,
            bool checkWarnings)
        {
            var result = ValidatePrescription(items, dosageCount);

            // 添加额外验证
            if (!ValidateDiscount(discount))
            {
                result.AddError("折扣必须在0.1-1.0之间");
            }

            if (checkWarnings)
            {
                // 添加一些警告检查
                if (items?.Count() > 20)
                {
                    result.AddWarning("处方药材超过20种，请确认是否合理");
                }

                if (dosageCount > 30)
                {
                    result.AddWarning("剂数超过30剂，请确认患者是否需要长期服药");
                }
            }

            return result;
        }

        /// <summary>
        /// 验证单个处方项 - 简化版
        /// </summary>
        private void ValidatePrescriptionItem(ValidationResult result, PrescriptionItemViewModel item)
        {
            if (item == null)
            {
                result.AddError("处方项不能为空");
                return;
            }

            // 只验证基本必要字段
            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                result.AddError("药材名称不能为空");
            }

            if (item.Quantity <= 0)
            {
                result.AddError($"药材 '{item.HerbName}' 的数量必须大于0");
            }

            if (item.UnitPrice <= 0)
            {
                result.AddError($"药材 '{item.HerbName}' 的单价必须大于0");
            }
        }

        /// <summary>
        /// 验证折扣 - 简化版
        /// </summary>
        public bool ValidateDiscount(decimal discount)
        {
            return discount >= 0.1m && discount <= 1.0m;
        }

        /// <summary>
        /// 验证剂数 - 简化版
        /// </summary>
        public bool ValidateDosageCount(int dosageCount)
        {
            return dosageCount >= 1 && dosageCount <= 100;
        }

        /// <summary>
        /// 验证剂数字符串并转换为数值 - 支持PrescriptionCommandHandler
        /// </summary>
        public bool ValidateDosage(string dosageStr, out int dosage)
        {
            dosage = 7; // 默认值

            if (string.IsNullOrWhiteSpace(dosageStr))
            {
                return true; // 使用默认值
            }

            if (int.TryParse(dosageStr, out var parsed))
            {
                if (parsed >= 1 && parsed <= 100)
                {
                    dosage = parsed;
                    return true;
                }
            }

            return false;
        }

        #endregion 核心验证方法
    }
}
namespace LYBT.Shared.Components
{
    /// <summary>
    /// 药材验证器基类 - 提供共享的验证逻辑
    /// Issue #1153: 提取Prescription和Formula模块的共享验证逻辑
    /// </summary>
    /// <typeparam name="TItem">药材项目类型，必须实现IHerbItem接口</typeparam>
    public abstract class HerbValidatorBase<TItem> where TItem : IHerbItem
    {
        #region 重复检测

        /// <summary>
        /// 检测重复药材
        /// </summary>
        protected List<string> GetDuplicateHerbs(IEnumerable<TItem> items)
        {
            if (items == null) return new List<string>();

            return items
                .GroupBy(i => i.HerbId)
                .Where(g => g.Count() > 1)
                .Select(g => g.First().HerbName)
                .ToList();
        }

        /// <summary>
        /// 验证是否存在重复药材
        /// </summary>
        protected bool HasDuplicateHerbs(IEnumerable<TItem> items)
        {
            return GetDuplicateHerbs(items).Any();
        }

        #endregion

        #region 剂量验证

        /// <summary>
        /// 验证剂量是否在合理范围内
        /// </summary>
        protected bool IsValidDosage(decimal dosage, decimal minDosage = 0.1m, decimal maxDosage = 500m)
        {
            return dosage >= minDosage && dosage <= maxDosage;
        }

        /// <summary>
        /// 获取剂量验证警告
        /// </summary>
        protected string? GetDosageWarning(TItem item, decimal minDosage = 0.1m, decimal maxDosage = 100m)
        {
            if (item == null) return null;

            if (item.Dosage > maxDosage)
            {
                return $"{item.HerbName} 用量较大（{item.Dosage}{item.Unit}），请确认是否正确";
            }

            if (item.Dosage < minDosage)
            {
                return $"{item.HerbName} 用量较小（{item.Dosage}{item.Unit}），请确认是否正确";
            }

            return null;
        }

        #endregion

        #region 必填项验证

        /// <summary>
        /// 验证药材项目的必填字段
        /// </summary>
        protected ValidationResult ValidateRequiredFields(TItem item)
        {
            var result = new ValidationResult();

            if (item == null)
            {
                result.AddError("药材项目不能为空");
                return result;
            }

            if (item.HerbId == Guid.Empty)
            {
                result.AddError("药材不能为空");
            }

            if (string.IsNullOrWhiteSpace(item.HerbName))
            {
                result.AddError("药材名称不能为空");
            }

            if (item.Dosage <= 0)
            {
                result.AddError($"{item.HerbName} 用量必须大于0");
            }

            if (string.IsNullOrWhiteSpace(item.Unit))
            {
                result.AddError($"{item.HerbName} 单位不能为空");
            }

            return result;
        }

        /// <summary>
        /// 验证药材列表不为空
        /// </summary>
        protected ValidationResult ValidateHerbListNotEmpty(IEnumerable<TItem> items, string entityName = "配方")
        {
            var result = new ValidationResult();

            if (items == null || !items.Any())
            {
                result.AddError($"{entityName}至少需要包含一味药材");
            }

            return result;
        }

        #endregion

        #region 组合验证

        /// <summary>
        /// 验证药材列表的基础规则
        /// </summary>
        protected ValidationResult ValidateHerbList(IEnumerable<TItem> items, string entityName = "配方")
        {
            var result = new ValidationResult();

            // 验证列表不为空
            var emptyValidation = ValidateHerbListNotEmpty(items, entityName);
            result.Merge(emptyValidation);

            if (!result.IsValid) return result;

            // 检查重复药材
            var duplicateHerbs = GetDuplicateHerbs(items);
            if (duplicateHerbs.Any())
            {
                result.AddError($"{entityName}中存在重复药材：{string.Join("、", duplicateHerbs)}");
            }

            // 验证每个项目
            foreach (var item in items)
            {
                var itemValidation = ValidateRequiredFields(item);
                result.Merge(itemValidation);

                // 添加剂量警告
                var dosageWarning = GetDosageWarning(item);
                if (!string.IsNullOrWhiteSpace(dosageWarning))
                {
                    result.AddWarning(dosageWarning);
                }
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// 验证结果 - 共享的验证结果类
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public bool IsValid => !Errors.Any();
        public bool HasWarnings => Warnings.Any();

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }

        public void Merge(ValidationResult other)
        {
            if (other != null)
            {
                Errors.AddRange(other.Errors);
                Warnings.AddRange(other.Warnings);
            }
        }

        public string GetErrorSummary()
        {
            return string.Join("; ", Errors);
        }

        public string GetWarningSummary()
        {
            return string.Join("; ", Warnings);
        }
    }
}

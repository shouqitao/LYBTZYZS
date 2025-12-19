using LYBT.Desktop.Formula.ViewModels;
using LYBT.Shared.Components;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// 配方验证器 - 组件化架构实现
    /// Issue #1153: 继承HerbValidatorBase共享基类，提供配方特有验证逻辑
    /// </summary>
    public class FormulaValidator : HerbValidatorBase<FormulaHerbItemViewModel>
    {
        #region 配方基本验证

        /// <summary>
        /// 验证配方基本信息
        /// </summary>
        public ValidationResult ValidateFormulaInfo(string formulaName, string effect, string usage)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(formulaName))
            {
                result.AddError("配方名称不能为空");
            }
            else if (formulaName.Length > 100)
            {
                result.AddError("配方名称长度不能超过100个字符");
            }

            if (string.IsNullOrWhiteSpace(effect))
            {
                result.AddError("功效主治不能为空");
            }
            else if (effect.Length > 500)
            {
                result.AddError("功效主治长度不能超过500个字符");
            }

            if (string.IsNullOrWhiteSpace(usage))
            {
                result.AddError("用法用量不能为空");
            }
            else if (usage.Length > 500)
            {
                result.AddError("用法用量长度不能超过500个字符");
            }

            return result;
        }

        /// <summary>
        /// 验证配方药材列表
        /// </summary>
        public ValidationResult ValidateFormulaHerbs(IEnumerable<FormulaHerbItemViewModel> items)
        {
            // 使用基类的ValidateHerbList方法
            var result = ValidateHerbList(items, "配方");

            // 添加配方特有验证
            if (result.IsValid && items != null)
            {
                var itemList = items.ToList();

                // 检查配方药材数量
                if (itemList.Count < 2)
                {
                    result.AddWarning("配方药材较少，建议至少包含2味药材");
                }

                if (itemList.Count > 20)
                {
                    result.AddWarning($"配方药材较多（{itemList.Count}味），建议控制在20味以内");
                }

                // 检查是否有主药（剂量占比较大的药材）
                var totalDosage = itemList.Sum(i => i.Dosage);
                var maxDosage = itemList.Max(i => i.Dosage);
                if (totalDosage > 0 && (maxDosage / totalDosage) < 0.15m)
                {
                    result.AddWarning("配方缺少明显的主药，建议突出主要药材");
                }
            }

            return result;
        }

        /// <summary>
        /// 验证配方名称唯一性
        /// </summary>
        public async Task<bool> IsUniqueFormulaNameAsync(string formulaName, Guid? excludeId = null)
        {
            // 这里需要调用Repository检查
            // 实际实现需要注入IFormulaRepository
            await Task.Delay(1); // 避免编译警告
            return true; // 暂时返回true
        }

        #endregion

        #region 配方完整性验证

        /// <summary>
        /// 验证配方完整性
        /// </summary>
        public ValidationResult ValidateFormulaCompleteness(
            string formulaName,
            string effect,
            string usage,
            IEnumerable<FormulaHerbItemViewModel> herbs)
        {
            var result = new ValidationResult();

            // 基本信息验证
            var infoValidation = ValidateFormulaInfo(formulaName, effect, usage);
            result.Merge(infoValidation);

            // 药材列表验证
            var herbValidation = ValidateFormulaHerbs(herbs);
            result.Merge(herbValidation);

            return result;
        }

        #endregion
    }
}

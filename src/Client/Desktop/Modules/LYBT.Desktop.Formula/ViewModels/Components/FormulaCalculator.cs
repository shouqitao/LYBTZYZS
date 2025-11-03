using LYBT.Shared.Components;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 配方计算器 - 组件化架构实现
    /// Issue #1153: 继承HerbCalculatorBase共享基类，提供配方特有计算逻辑
    /// </summary>
    public class FormulaCalculator : HerbCalculatorBase<FormulaHerbItemViewModel>
    {
        #region 配方特有计算

        /// <summary>
        /// 计算配方比例分布
        /// </summary>
        public FormulaRatioAnalysis CalculateRatioDistribution(IEnumerable<FormulaHerbItemViewModel> items)
        {
            if (items == null || !items.Any())
            {
                return new FormulaRatioAnalysis();
            }

            var itemList = items.ToList();
            var totalDosage = CalculateTotalDosage(itemList);

            var ratios = new List<HerbRatio>();
            foreach (var item in itemList)
            {
                ratios.Add(new HerbRatio
                {
                    HerbName = item.HerbName,
                    Dosage = item.Dosage,
                    Ratio = CalculateItemRatio(item, itemList),
                    Category = ClassifyHerbCategory(item.Dosage, totalDosage)
                });
            }

            return new FormulaRatioAnalysis
            {
                TotalItems = itemList.Count,
                TotalDosage = totalDosage,
                HerbRatios = ratios,
                IsBalanced = CheckFormulaBalance(ratios)
            };
        }

        /// <summary>
        /// 分类药材类别（君臣佐使）
        /// </summary>
        private string ClassifyHerbCategory(decimal herbDosage, decimal totalDosage)
        {
            if (totalDosage == 0) return "未知";

            var ratio = (herbDosage / totalDosage) * 100;

            if (ratio >= 30) return "君药";
            if (ratio >= 20) return "臣药";
            if (ratio >= 10) return "佐药";
            return "使药";
        }

        /// <summary>
        /// 检查配方平衡性
        /// </summary>
        private bool CheckFormulaBalance(List<HerbRatio> ratios)
        {
            if (!ratios.Any()) return false;

            // 检查是否有君药（主药）
            var hasMonarch = ratios.Any(r => r.Category == "君药");

            // 检查配比是否合理（标准差不能太大）
            var dosages = ratios.Select(r => r.Dosage).ToList();
            var stdDev = CalculateStandardDeviation(dosages);

            return hasMonarch && stdDev < 50;
        }

        /// <summary>
        /// 计算配方预估成本
        /// </summary>
        public decimal CalculateFormulaCost(IEnumerable<FormulaHerbItemViewModel> items, int servings = 1)
        {
            if (items == null || !items.Any()) return 0;

            var singleCost = CalculateTotalPrice(items);
            return singleCost * servings;
        }

        /// <summary>
        /// 分析配方用量合理性
        /// </summary>
        public FormulaAnalysisResult AnalyzeFormula(IEnumerable<FormulaHerbItemViewModel> items)
        {
            var result = new FormulaAnalysisResult();

            if (items == null || !items.Any())
            {
                result.AddWarning("配方为空");
                return result;
            }

            var itemList = items.ToList();

            // 基础用量验证
            var dosageWarnings = ValidateDosageReasonableness(itemList);
            foreach (var warning in dosageWarnings)
            {
                result.AddWarning(warning);
            }

            // 配方特有验证
            var ratioAnalysis = CalculateRatioDistribution(itemList);

            if (!ratioAnalysis.IsBalanced)
            {
                result.AddWarning("配方配比可能不够平衡，请检查君臣佐使配置");
            }

            if (itemList.Count > 15)
            {
                result.AddWarning($"配方药味较多（{itemList.Count}味），建议精简");
            }

            result.TotalDosage = ratioAnalysis.TotalDosage;
            result.HerbCount = itemList.Count;
            result.IsValid = !result.Warnings.Any();

            return result;
        }

        #endregion
    }

    #region 辅助类

    /// <summary>
    /// 配方比例分析结果
    /// </summary>
    public class FormulaRatioAnalysis
    {
        public int TotalItems { get; set; }
        public decimal TotalDosage { get; set; }
        public List<HerbRatio> HerbRatios { get; set; } = new();
        public bool IsBalanced { get; set; }
    }

    /// <summary>
    /// 药材比例信息
    /// </summary>
    public class HerbRatio
    {
        public string HerbName { get; set; } = string.Empty;
        public decimal Dosage { get; set; }
        public decimal Ratio { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// 配方分析结果
    /// </summary>
    public class FormulaAnalysisResult
    {
        public List<string> Warnings { get; set; } = new();
        public decimal TotalDosage { get; set; }
        public int HerbCount { get; set; }
        public bool IsValid { get; set; }

        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
            }
        }
    }

    #endregion
}

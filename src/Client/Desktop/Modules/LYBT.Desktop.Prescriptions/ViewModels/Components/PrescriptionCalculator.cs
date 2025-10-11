using LYBT.Shared.Components;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方计算器 - UltraThink架构实现
    /// Issue #1153: 继承HerbCalculatorBase共享基类
    /// </summary>
    public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemViewModel>
    {
        #region 剂量计算 (继承自基类)

        // CalculateTotalDosage - 已由基类提供
        // CalculateTotalWeight - 已由基类提供
        // CalculateItemRatio - 已由基类提供
        // CalculateEstimatedTotalPrice - 已由基类提供
        // ValidateDosageReasonableness - 已由基类提供
        // CalculateStandardDeviation - 已由基类提供

        #endregion

        #region 价格计算（处方特有）

        /// <summary>
        /// 计算处方价格详情
        /// </summary>
        public CalculationResult CalculatePrescriptionPrice(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount = 1,
            decimal discount = 1.0m)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return new CalculationResult
                    {
                        IsValid = false,
                        ErrorMessage = "处方项目为空"
                    };
                }

                var itemList = items.ToList();
                var singleDosagePrice = itemList.Sum(item => item.Quantity * item.UnitPrice);
                var totalPrice = singleDosagePrice * dosageCount;
                var discountedPrice = totalPrice * discount;
                var totalSaved = totalPrice - discountedPrice;

                return new CalculationResult
                {
                    SingleDosagePrice = singleDosagePrice,
                    TotalPrice = totalPrice,
                    DiscountedPrice = discountedPrice,
                    TotalSaved = totalSaved,
                    ItemCount = itemList.Count,
                    IsValid = true,
                    ErrorMessage = string.Empty,
                    CalculatedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new CalculationResult
                {
                    IsValid = false,
                    ErrorMessage = $"计算价格时发生错误: {ex.Message}"
                };
            }
        }

        #endregion

        #region 用量分析

        /// <summary>
        /// 分析处方用量分布
        /// </summary>
        public PrescriptionDosageAnalysis AnalyzeDosageDistribution(IEnumerable<PrescriptionItemViewModel> items)
        {
            if (items == null || !items.Any())
            {
                return new PrescriptionDosageAnalysis();
            }

            var dosages = items.Select(i => i.Dosage).ToList();

            return new PrescriptionDosageAnalysis
            {
                TotalItems = dosages.Count,
                MinDosage = dosages.Min(),
                MaxDosage = dosages.Max(),
                AverageDosage = dosages.Average(),
                TotalDosage = dosages.Sum(),
                StandardDeviation = CalculateStandardDeviation(dosages) // 调用基类的protected方法
            };
        }

        #endregion

        /// <summary>
        /// 处方计算结果
        /// </summary>
        public class CalculationResult
        {
            public decimal SingleDosagePrice { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal DiscountedPrice { get; set; }
            public decimal TotalSaved { get; set; }
            public int ItemCount { get; set; }
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public DateTime CalculatedAt { get; set; } = DateTime.Now;
        }
    }

    /// <summary>
    /// 处方用量分析结果
    /// </summary>
    public class PrescriptionDosageAnalysis
    {
        public int TotalItems { get; set; }
        public decimal MinDosage { get; set; }
        public decimal MaxDosage { get; set; }
        public decimal AverageDosage { get; set; }
        public decimal TotalDosage { get; set; }
        public decimal StandardDeviation { get; set; }
    }
}

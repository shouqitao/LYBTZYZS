namespace LYBT.Desktop.Modules.Prescriptions.ViewModels.Components
{
    /// <summary>
    /// 处方计算器 - UltraThink架构实现
    /// 负责处方的各种计算逻辑
    /// </summary>
    public class PrescriptionCalculator
    {
        #region 剂量计算

        /// <summary>
        /// 计算处方总剂量
        /// </summary>
        public decimal CalculateTotalDosage(IEnumerable<PrescriptionItemViewModel> items)
        {
            if (items == null) return 0;

            return items.Sum(item => item.Dosage);
        }

        /// <summary>
        /// 计算处方总重量（按克计算）
        /// </summary>
        public decimal CalculateTotalWeight(IEnumerable<PrescriptionItemViewModel> items)
        {
            if (items == null) return 0;

            return items.Sum(item => ConvertToGrams(item.Dosage, item.Unit));
        }

        /// <summary>
        /// 计算单项药材在处方中的比例
        /// </summary>
        public decimal CalculateItemRatio(PrescriptionItemViewModel item, IEnumerable<PrescriptionItemViewModel> allItems)
        {
            if (item == null || allItems == null) return 0;

            var totalDosage = CalculateTotalDosage(allItems);
            if (totalDosage == 0) return 0;

            return (item.Dosage / totalDosage) * 100;
        }

        #endregion

        #region 价格计算

        /// <summary>
        /// 计算处方预估总价
        /// </summary>
        public decimal CalculateEstimatedTotalPrice(IEnumerable<PrescriptionItemViewModel> items, Dictionary<Guid, decimal> herbPrices)
        {
            if (items == null || herbPrices == null) return 0;

            return items.Sum(item =>
            {
                if (herbPrices.TryGetValue(item.HerbId, out var unitPrice))
                {
                    var weightInGrams = ConvertToGrams(item.Dosage, item.Unit);
                    return weightInGrams * unitPrice;
                }
                return 0;
            });
        }

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
                StandardDeviation = CalculateStandardDeviation(dosages)
            };
        }

        /// <summary>
        /// 检查处方用量是否合理
        /// </summary>
        public List<string> ValidateDosageReasonableness(IEnumerable<PrescriptionItemViewModel> items)
        {
            var warnings = new List<string>();

            if (items == null) return warnings;

            foreach (var item in items)
            {
                // 检查单味药用量是否过大
                if (item.Dosage > 100)
                {
                    warnings.Add($"{item.HerbName} 用量过大（{item.Dosage}{item.Unit}），请检查是否正确");
                }

                // 检查单味药用量是否过小
                if (item.Dosage < 0.1m)
                {
                    warnings.Add($"{item.HerbName} 用量过小（{item.Dosage}{item.Unit}），请检查是否正确");
                }
            }

            // 检查总用量
            var totalWeight = CalculateTotalWeight(items);
            if (totalWeight > 500)
            {
                warnings.Add($"处方总重量过大（{totalWeight:F1}g），请检查是否合理");
            }
            else if (totalWeight < 10)
            {
                warnings.Add($"处方总重量过小（{totalWeight:F1}g），请检查是否合理");
            }

            return warnings;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将不同单位转换为克
        /// </summary>
        private decimal ConvertToGrams(decimal dosage, string unit)
        {
            return unit?.ToLower() switch
            {
                "kg" => dosage * 1000,
                "g" => dosage,
                "mg" => dosage / 1000,
                "钱" => dosage * 3.125m, // 1钱 = 3.125克
                "两" => dosage * 31.25m, // 1两 = 31.25克
                _ => dosage // 默认按克处理
            };
        }

        /// <summary>
        /// 计算标准差
        /// </summary>
        private decimal CalculateStandardDeviation(List<decimal> values)
        {
            if (values.Count <= 1) return 0;

            var average = values.Average();
            var sumOfSquaresOfDifferences = values.Sum(val => (decimal)Math.Pow((double)(val - average), 2));
            var standardDeviation = (decimal)Math.Sqrt((double)(sumOfSquaresOfDifferences / values.Count));

            return standardDeviation;
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

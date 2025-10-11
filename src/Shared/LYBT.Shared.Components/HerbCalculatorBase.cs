namespace LYBT.Shared.Components
{
    /// <summary>
    /// 药材计算器基类 - 提供共享的计算逻辑
    /// Issue #1153: 提取Prescription和Formula模块的共享计算逻辑
    /// </summary>
    /// <typeparam name="TItem">药材项目类型，必须实现IHerbItem接口</typeparam>
    public abstract class HerbCalculatorBase<TItem> where TItem : IHerbItem
    {
        #region 剂量计算

        /// <summary>
        /// 计算总剂量
        /// </summary>
        protected decimal CalculateTotalDosage(IEnumerable<TItem> items)
        {
            if (items == null) return 0;
            return items.Sum(item => item.Dosage);
        }

        /// <summary>
        /// 计算总重量（按克计算）
        /// </summary>
        protected decimal CalculateTotalWeight(IEnumerable<TItem> items)
        {
            if (items == null) return 0;
            return items.Sum(item => ConvertToGrams(item.Dosage, item.Unit));
        }

        /// <summary>
        /// 计算单项药材在配方中的比例
        /// </summary>
        protected decimal CalculateItemRatio(TItem item, IEnumerable<TItem> allItems)
        {
            if (item == null || allItems == null) return 0;

            var totalDosage = CalculateTotalDosage(allItems);
            if (totalDosage == 0) return 0;

            return (item.Dosage / totalDosage) * 100;
        }

        #endregion

        #region 价格计算

        /// <summary>
        /// 计算总价
        /// </summary>
        protected decimal CalculateTotalPrice(IEnumerable<TItem> items)
        {
            if (items == null) return 0;
            return items.Sum(item => item.Quantity * item.UnitPrice);
        }

        /// <summary>
        /// 计算预估总价（基于药材价格字典）
        /// </summary>
        protected decimal CalculateEstimatedTotalPrice(IEnumerable<TItem> items, Dictionary<Guid, decimal> herbPrices)
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

        #endregion

        #region 用量分析

        /// <summary>
        /// 验证剂量合理性
        /// </summary>
        public List<string> ValidateDosageReasonableness(IEnumerable<TItem> items)
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
                warnings.Add($"总重量过大（{totalWeight:F1}g），请检查是否合理");
            }
            else if (totalWeight < 10)
            {
                warnings.Add($"总重量过小（{totalWeight:F1}g），请检查是否合理");
            }

            return warnings;
        }

        /// <summary>
        /// 计算标准差
        /// </summary>
        protected decimal CalculateStandardDeviation(IEnumerable<decimal> values)
        {
            var valueList = values?.ToList();
            if (valueList == null || valueList.Count <= 1) return 0;

            var average = valueList.Average();
            var sumOfSquaresOfDifferences = valueList.Sum(val => (decimal)Math.Pow((double)(val - average), 2));
            var standardDeviation = (decimal)Math.Sqrt((double)(sumOfSquaresOfDifferences / valueList.Count));

            return standardDeviation;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将不同单位转换为克
        /// </summary>
        protected decimal ConvertToGrams(decimal dosage, string unit)
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

        #endregion
    }
}

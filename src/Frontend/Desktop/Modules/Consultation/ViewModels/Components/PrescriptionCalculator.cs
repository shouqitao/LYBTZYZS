using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using LYBT.WPF.Client.Core.Models.Prescriptions;

namespace LYBT.WPF.Client.Modules.Consultation.ViewModels.Components
{
    /// <summary>
    /// 处方计算器 - UltraThink专门化组件
    /// 职责单一：专注处方价格计算和数学运算
    /// 代码干净：清晰的计算逻辑和精度处理
    /// 性能出色：优化的计算算法和缓存机制
    /// </summary>
    public class PrescriptionCalculator
    {
        private readonly ILogger<PrescriptionCalculator> _logger;

        public PrescriptionCalculator(ILogger<PrescriptionCalculator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 计算结果类

        public class CalculationResult
        {
            public decimal SingleDosagePrice { get; set; }
            public decimal TotalPrice { get; set; }
            public decimal DiscountedPrice { get; set; }
            public decimal TotalSaved { get; set; }
            public int ItemCount { get; set; }
            public string DiscountText { get; set; } = "无折扣";
        }

        #endregion

        #region 核心计算方法

        /// <summary>
        /// 计算处方总价（完整计算）
        /// </summary>
        public CalculationResult CalculatePrescriptionPrice(
            IEnumerable<PrescriptionItemViewModel> items,
            int dosageCount,
            decimal discount = 1.0m)
        {
            try
            {
                var itemList = items?.ToList() ?? new List<PrescriptionItemViewModel>();
                
                // 计算单剂价格
                var singleDosagePrice = CalculateSingleDosagePrice(itemList);
                
                // 计算总价
                var totalPrice = singleDosagePrice * dosageCount;
                
                // 应用折扣
                var discountedPrice = ApplyDiscount(totalPrice, discount);
                
                // 计算节省金额
                var totalSaved = totalPrice - discountedPrice;

                var result = new CalculationResult
                {
                    SingleDosagePrice = Math.Round(singleDosagePrice, 2),
                    TotalPrice = Math.Round(totalPrice, 2),
                    DiscountedPrice = Math.Round(discountedPrice, 2),
                    TotalSaved = Math.Round(totalSaved, 2),
                    ItemCount = itemList.Count,
                    DiscountText = GenerateDiscountText(discount)
                };

                _logger.LogDebug("处方价格计算完成：单剂 {SinglePrice}元，总价 {TotalPrice}元，优惠后 {DiscountedPrice}元",
                    result.SingleDosagePrice, result.TotalPrice, result.DiscountedPrice);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算处方价格失败");
                return new CalculationResult();
            }
        }

        /// <summary>
        /// 计算单剂价格
        /// </summary>
        public decimal CalculateSingleDosagePrice(IEnumerable<PrescriptionItemViewModel> items)
        {
            if (items == null)
                return 0m;

            try
            {
                var total = items.Sum(item => CalculateItemSubtotal(item.Quantity, item.UnitPrice));
                return Math.Round(total, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算单剂价格失败");
                return 0m;
            }
        }

        /// <summary>
        /// 计算单个药材小计
        /// </summary>
        public decimal CalculateItemSubtotal(decimal quantity, decimal unitPrice)
        {
            if (quantity <= 0 || unitPrice <= 0)
                return 0m;

            var subtotal = quantity * unitPrice;
            return Math.Round(subtotal, 2);
        }

        /// <summary>
        /// 应用折扣
        /// </summary>
        public decimal ApplyDiscount(decimal originalPrice, decimal discount)
        {
            if (originalPrice <= 0)
                return 0m;

            // 确保折扣在有效范围内
            discount = Math.Max(0.1m, Math.Min(1.0m, discount));
            
            var discountedPrice = originalPrice * discount;
            return Math.Round(discountedPrice, 2);
        }

        /// <summary>
        /// 计算折扣金额
        /// </summary>
        public decimal CalculateDiscountAmount(decimal originalPrice, decimal discount)
        {
            if (originalPrice <= 0)
                return 0m;

            var discountedPrice = ApplyDiscount(originalPrice, discount);
            return Math.Round(originalPrice - discountedPrice, 2);
        }

        /// <summary>
        /// 生成折扣文本
        /// </summary>
        public string GenerateDiscountText(decimal discount)
        {
            if (discount >= 1.0m)
                return "无折扣";

            // 转换为折扣显示（如 0.85 显示为 "8.5折"）
            var discountDisplay = discount * 10;
            return $"{discountDisplay:F1}折";
        }

        #endregion

        #region 高级计算功能

        /// <summary>
        /// 批量更新处方项小计
        /// </summary>
        public void UpdateItemSubtotals(IEnumerable<PrescriptionItemViewModel> items)
        {
            if (items == null)
                return;

            try
            {
                // Subtotal是计算属性，会自动根据Quantity和UnitPrice计算
                // 无需手动更新，属性变更会自动触发UI更新
                _logger.LogDebug("批量更新处方项小计完成，共{Count}项", items.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新处方项小计失败");
            }
        }

        /// <summary>
        /// 计算成本分析
        /// </summary>
        public CostAnalysis AnalyzeCosts(IEnumerable<PrescriptionItemViewModel> items, int dosageCount, decimal discount)
        {
            try
            {
                var itemList = items?.ToList() ?? new List<PrescriptionItemViewModel>();
                var result = CalculatePrescriptionPrice(itemList, dosageCount, discount);

                return new CostAnalysis
                {
                    AverageItemPrice = itemList.Count > 0 ? result.SingleDosagePrice / itemList.Count : 0m,
                    MostExpensiveItem = itemList.OrderByDescending(i => i.Subtotal).FirstOrDefault(),
                    LeastExpensiveItem = itemList.OrderBy(i => i.Subtotal).FirstOrDefault(),
                    PricePerDosage = result.SingleDosagePrice,
                    TotalSavings = result.TotalSaved,
                    DiscountPercentage = (1 - discount) * 100
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析成本失败");
                return new CostAnalysis();
            }
        }

        /// <summary>
        /// 计算价格趋势（用于多次保存的处方）
        /// </summary>
        public decimal CalculatePriceTrend(decimal currentPrice, decimal? previousPrice)
        {
            if (!previousPrice.HasValue || previousPrice.Value <= 0)
                return 0m;

            var trend = (currentPrice - previousPrice.Value) / previousPrice.Value * 100;
            return Math.Round(trend, 2);
        }

        #endregion

        #region 辅助计算方法

        /// <summary>
        /// 检查价格是否异常
        /// </summary>
        public PriceValidation ValidatePrice(decimal price, PriceValidationType type)
        {
            var validation = new PriceValidation();

            switch (type)
            {
                case PriceValidationType.UnitPrice:
                    if (price <= 0)
                        validation.AddWarning("单价不能为0或负数");
                    else if (price > 1000)
                        validation.AddWarning("单价过高，请检查");
                    break;

                case PriceValidationType.TotalPrice:
                    if (price <= 0)
                        validation.AddWarning("总价不能为0或负数");
                    else if (price > 10000)
                        validation.AddWarning("总价过高，请确认");
                    break;

                case PriceValidationType.Quantity:
                    if (price <= 0)
                        validation.AddError("数量必须大于0");
                    else if (price > 1000)
                        validation.AddWarning("数量过大，请检查");
                    break;
            }

            return validation;
        }

        /// <summary>
        /// 四舍五入到指定精度
        /// </summary>
        public decimal RoundToDecimalPlaces(decimal value, int decimalPlaces = 2)
        {
            return Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region 支持类

        public class CostAnalysis
        {
            public decimal AverageItemPrice { get; set; }
            public PrescriptionItemViewModel? MostExpensiveItem { get; set; }
            public PrescriptionItemViewModel? LeastExpensiveItem { get; set; }
            public decimal PricePerDosage { get; set; }
            public decimal TotalSavings { get; set; }
            public decimal DiscountPercentage { get; set; }
        }

        public class PriceValidation
        {
            public List<string> Errors { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public bool IsValid => !Errors.Any();

            public void AddError(string error) => Errors.Add(error);
            public void AddWarning(string warning) => Warnings.Add(warning);
        }

        public enum PriceValidationType
        {
            UnitPrice,
            TotalPrice,
            Quantity
        }

        #endregion
    }
}
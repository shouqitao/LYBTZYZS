using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Components
{

    /// <summary>
    /// 处方价格计算组件 - UltraThink简化版本
    /// 专注于价格计算逻辑，不涉及复杂的业务规则
    /// </summary>
    public class PriceCalculator
    {
        private readonly ILogger<PriceCalculator> _logger;

        public PriceCalculator(ILogger<PriceCalculator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 计算单剂价格
        /// </summary>
        /// <param name="items">处方项目列表</param>
        /// <param name="discount">折扣率（默认1.0无折扣）</param>
        /// <returns>单剂价格</returns>
        public decimal CalculateSingleDosePrice(IEnumerable<PrescriptionItemDto> items, decimal discount = 1.0m)
        {
            try
            {
                if (items == null || !items.Any())
                {
                    return 0m;
                }

                // 计算小计：Σ(药材单价 × 用量)
                var subtotal = items.Sum(item => item.UnitPrice * item.Quantity);

                // 应用折扣
                var discountedPrice = subtotal * Math.Max(0m, Math.Min(1m, discount));

                _logger.LogDebug(
                    "计算单剂价格: 小计={Subtotal}, 折扣={Discount}, 单剂价格={Price}",
                    subtotal, discount, discountedPrice);

                return Math.Round(discountedPrice, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算单剂价格时发生错误");
                return 0m;
            }
        }

        /// <summary>
        /// 计算总价格
        /// </summary>
        /// <param name="singleDosePrice">单剂价格</param>
        /// <param name="dosageCount">剂数</param>
        /// <returns>总价格</returns>
        public decimal CalculateTotalPrice(decimal singleDosePrice, int dosageCount)
        {
            try
            {
                if (dosageCount <= 0)
                {
                    return 0m;
                }

                var totalPrice = singleDosePrice * dosageCount;

                _logger.LogDebug(
                    "计算总价格: 单剂价格={SinglePrice}, 剂数={DosageCount}, 总价格={TotalPrice}",
                    singleDosePrice, dosageCount, totalPrice);

                return Math.Round(totalPrice, 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算总价格时发生错误");
                return 0m;
            }
        }

        /// <summary>
        /// 计算处方完整价格信息
        /// </summary>
        /// <param name="prescription">处方信息</param>
        /// <returns>价格计算结果</returns>
        public PriceCalculationResult CalculatePrescriptionPrice(PrescriptionDto prescription)
        {
            try
            {
                if (prescription == null)
                {
                    return new PriceCalculationResult();
                }

                var singleDosePrice = CalculateSingleDosePrice(prescription.Items, prescription.Discount);
                var totalPrice = CalculateTotalPrice(singleDosePrice, prescription.DosageCount);
                var totalWeight = CalculateTotalWeight(prescription.Items, prescription.DosageCount);

                var result = new PriceCalculationResult
                {
                    SingleDosePrice = singleDosePrice,
                    TotalPrice = totalPrice,
                    TotalWeight = totalWeight,
                    ItemCount = prescription.Items?.Count ?? 0,
                    DosageCount = prescription.DosageCount,
                    Discount = prescription.Discount
                };

                _logger.LogInformation("处方价格计算完成: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算处方价格时发生错误");
                return new PriceCalculationResult();
            }
        }

        /// <summary>
        /// 计算总重量
        /// </summary>
        /// <param name="items">处方项目列表</param>
        /// <param name="dosageCount">剂数</param>
        /// <returns>总重量</returns>
        public decimal CalculateTotalWeight(IEnumerable<PrescriptionItemDto> items, int dosageCount)
        {
            try
            {
                if (items == null || !items.Any() || dosageCount <= 0)
                {
                    return 0m;
                }

                // 单剂重量
                var singleDoseWeight = items.Sum(item => item.Quantity);

                // 总重量 = 单剂重量 × 剂数
                var totalWeight = singleDoseWeight * dosageCount;

                return Math.Round(totalWeight, 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算总重量时发生错误");
                return 0m;
            }
        }

        /// <summary>
        /// 验证价格合理性
        /// </summary>
        /// <param name="price">价格</param>
        /// <param name="maxPrice">最大合理价格（默认10000）</param>
        /// <returns>价格是否合理</returns>
        public bool ValidatePrice(decimal price, decimal maxPrice = 10000m)
        {
            return price >= 0m && price <= maxPrice;
        }

        /// <summary>
        /// 格式化价格显示
        /// </summary>
        /// <param name="price">价格</param>
        /// <param name="format">格式（默认C货币格式）</param>
        /// <returns>格式化后的价格字符串</returns>
        public string FormatPrice(decimal price, string format = "C")
        {
            try
            {
                return price.ToString(format);
            }
            catch
            {
                return price.ToString("F2");
            }
        }
    }

    /// <summary>
    /// 价格计算结果
    /// </summary>
    public class PriceCalculationResult
    {

        /// <summary>计算是否成功</summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>错误信息</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>单剂价格</summary>
        public decimal SingleDosePrice { get; set; }

        /// <summary>总价格</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>总重量</summary>
        public decimal TotalWeight { get; set; }

        /// <summary>药材种类数</summary>
        public int ItemCount { get; set; }

        /// <summary>剂数</summary>
        public int DosageCount { get; set; }

        /// <summary>折扣率</summary>
        public decimal Discount { get; set; } = 1.0m;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"单剂:{SingleDosePrice:C}, 总价:{TotalPrice:C}, 重量:{TotalWeight}g, {ItemCount}味药, {DosageCount}剂";
        }
    }
}

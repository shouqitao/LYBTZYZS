using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 处方价格计算器 - 负责处方价格计算逻辑
/// Issue #1807: 从PrescriptionEditorViewModel提取价格计算逻辑(~100行)
/// </summary>
public class PrescriptionCalculator
{
    private readonly ILogger<PrescriptionCalculator> _logger;

    /// <summary>
    /// 价格计算完成事件
    /// </summary>
    public event EventHandler<PriceCalculatedEventArgs>? PriceCalculated;

    public PrescriptionCalculator(ILogger<PrescriptionCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 计算单剂价格
    /// </summary>
    /// <param name="items">处方药材项列表</param>
    /// <param name="allHerbs">所有药材数据</param>
    /// <returns>单剂价格</returns>
    public decimal CalculateSingleDosagePrice(List<PrescriptionItemDto> items, List<HerbDto> allHerbs)
    {
        if (items == null || allHerbs == null)
        {
            _logger.LogWarning("计算单剂价格失败：输入参数为null");
            return 0m;
        }

        try
        {
            var singleDosagePrice = items.Sum(item =>
            {
                var herb = allHerbs.FirstOrDefault(h => h.Id == item.HerbId);
                var itemPrice = (herb?.Price ?? 0m) * item.Dosage;
                return itemPrice;
            });

            _logger.LogInformation("单剂价格计算完成：{Price:F2}元（{ItemCount}味药材）",
                singleDosagePrice, items.Count);

            return singleDosagePrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算单剂价格时发生异常");
            return 0m;
        }
    }

    /// <summary>
    /// 计算总价格
    /// </summary>
    /// <param name="singleDosagePrice">单剂价格</param>
    /// <param name="dosageCount">剂数</param>
    /// <returns>总价格</returns>
    public decimal CalculateTotalPrice(decimal singleDosagePrice, int dosageCount)
    {
        if (dosageCount <= 0)
        {
            _logger.LogWarning("剂数必须大于0，当前值：{DosageCount}", dosageCount);
            return 0m;
        }

        var totalPrice = singleDosagePrice * dosageCount;

        _logger.LogInformation("总价格计算完成：{TotalPrice:F2}元（单剂{SinglePrice:F2}元 × {DosageCount}剂）",
            totalPrice, singleDosagePrice, dosageCount);

        // 触发事件
        PriceCalculated?.Invoke(this, new PriceCalculatedEventArgs
        {
            SingleDosagePrice = singleDosagePrice,
            DosageCount = dosageCount,
            TotalPrice = totalPrice
        });

        return totalPrice;
    }

    /// <summary>
    /// 为药材项添加价格信息
    /// </summary>
    /// <param name="items">处方药材项列表</param>
    /// <param name="allHerbs">所有药材数据</param>
    /// <returns>包含价格信息的药材项列表</returns>
    public List<PrescriptionItemInputDto> BuildItemsWithPrice(List<PrescriptionItemDto> items, List<HerbDto> allHerbs)
    {
        if (items == null || allHerbs == null)
        {
            _logger.LogWarning("构建带价格的药材项失败：输入参数为null");
            return new List<PrescriptionItemInputDto>();
        }

        try
        {
            var itemsWithPrice = items.Select(item =>
            {
                var herb = allHerbs.FirstOrDefault(h => h.Id == item.HerbId);
                var unitPrice = herb?.Price ?? 0m;
                var subtotal = unitPrice * item.Dosage;

                return new PrescriptionItemInputDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = (int)item.Dosage,
                    Unit = item.Unit,
                    UnitPrice = unitPrice,
                    Subtotal = subtotal
                };
            }).ToList();

            _logger.LogInformation("构建带价格的药材项完成：{ItemCount}味药材",
                itemsWithPrice.Count);

            return itemsWithPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "构建带价格的药材项时发生异常");
            return new List<PrescriptionItemInputDto>();
        }
    }

    /// <summary>
    /// 计算并记录总金额
    /// </summary>
    /// <param name="items">处方药材项列表（带价格）</param>
    /// <param name="dosageCount">剂数</param>
    /// <returns>总金额</returns>
    public decimal CalculateAndLogTotalAmount(List<PrescriptionItemDto> items, int dosageCount)
    {
        if (items == null)
        {
            _logger.LogWarning("计算总金额失败：药材项列表为null");
            return 0m;
        }

        try
        {
            var totalAmount = items.Sum(item => item.UnitPrice * item.Dosage) * dosageCount;

            _logger.LogInformation("处方总金额：{TotalAmount:F2}元（{ItemCount}味药材 × {DosageCount}剂）",
                totalAmount, items.Count, dosageCount);

            return totalAmount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算总金额时发生异常");
            return 0m;
        }
    }
}

/// <summary>
/// 价格计算完成事件参数
/// </summary>
public class PriceCalculatedEventArgs : EventArgs
{
    /// <summary>
    /// 单剂价格
    /// </summary>
    public decimal SingleDosagePrice { get; set; }

    /// <summary>
    /// 剂数
    /// </summary>
    public int DosageCount { get; set; }

    /// <summary>
    /// 总价格
    /// </summary>
    public decimal TotalPrice { get; set; }
}

using LYBT.Desktop.Models.Items.Prescriptions;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方计算器
/// 负责处方价格计算、药材数量统计
/// OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanelViewModel拆分
/// </summary>
public class PrescriptionCalculator
{
    #region 字段

    private readonly ILogger<PrescriptionCalculator> _logger;

    #endregion

    #region 构造函数

    public PrescriptionCalculator(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PrescriptionCalculator>();
    }

    #endregion

    #region 价格计算

    /// <summary>
    /// 计算单剂价格
    /// 单剂价格 = 所有药材小计之和
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <returns>单剂价格</returns>
    public decimal CalculateSingleDosagePrice(ObservableCollection<PrescriptionHerbItem> herbItems)
    {
        if (herbItems == null || herbItems.Count == 0)
        {
            return 0m;
        }

        return herbItems
            .Where(h => h.HerbId != Guid.Empty)
            .Sum(h => h.ItemTotal);
    }

    /// <summary>
    /// 计算总价格
    /// 总价格 = 单剂价格 × 剂数
    /// </summary>
    /// <param name="singleDosagePrice">单剂价格</param>
    /// <param name="dosageCount">剂数</param>
    /// <returns>总价格</returns>
    public decimal CalculateTotalPrice(decimal singleDosagePrice, int dosageCount)
    {
        return singleDosagePrice * dosageCount;
    }

    /// <summary>
    /// 计算完整价格信息
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="dosageCount">剂数</param>
    /// <returns>价格计算结果</returns>
    public PriceCalculationResult CalculatePrices(
        ObservableCollection<PrescriptionHerbItem> herbItems,
        int dosageCount)
    {
        var singleDosagePrice = CalculateSingleDosagePrice(herbItems);
        var totalPrice = CalculateTotalPrice(singleDosagePrice, dosageCount);

        _logger.LogDebug("价格计算完成: 单剂={SinglePrice}, 剂数={DosageCount}, 总价={TotalPrice}",
            singleDosagePrice, dosageCount, totalPrice);

        return new PriceCalculationResult(singleDosagePrice, totalPrice);
    }

    #endregion

    #region 数量统计

    /// <summary>
    /// 计算有效药材数量
    /// 只统计已选择药材的项
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <returns>有效药材数量</returns>
    public int CalculateItemCount(ObservableCollection<PrescriptionHerbItem> herbItems)
    {
        if (herbItems == null)
        {
            return 0;
        }

        return herbItems.Count(h => h.HerbId != Guid.Empty);
    }

    #endregion
}

#region 结果类型

/// <summary>
/// 价格计算结果
/// </summary>
public record PriceCalculationResult(decimal SingleDosagePrice, decimal TotalPrice);

#endregion

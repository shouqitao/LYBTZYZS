using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Extensions;

/// <summary>
/// 处方导入扩展方法
/// OpenSpec: simplify-workspace-architecture - 替代PrescriptionImportHandler
/// OpenSpec: unify-control-data-binding - 统一使用PrescriptionItemDto
/// CODE-08: 导入/复制时主动填充当前价格
/// </summary>
public static class PrescriptionImportExtensions
{
    /// <summary>
    /// 将验方药材转换为PrescriptionItemDto列表
    /// CODE-08: herbPrices 提供当前价格查表，避免依赖 UI 被动同步
    /// </summary>
    public static IReadOnlyList<PrescriptionItemDto> ToPrescriptionItemDtos(
        this FormulaDetailDto formula,
        List<FormulaHerbItemDto> herbs,
        IReadOnlyDictionary<Guid, decimal>? herbPrices = null)
    {
        if (formula == null || herbs == null || !herbs.Any())
            return Array.Empty<PrescriptionItemDto>();

        return herbs
            .Where(h => h.HerbId.HasValue)
            .Select(h => new PrescriptionItemDto
            {
                HerbId = h.HerbId!.Value,
                HerbName = h.HerbName ?? string.Empty,
                Dosage = h.Dosage,
                DecocteMethod = h.DecocteMethod,
                UnitPrice = herbPrices != null && herbPrices.TryGetValue(h.HerbId!.Value, out var price)
                    ? price
                    : 0m
            })
            .ToList();
    }

    /// <summary>
    /// 将历史处方药材复制并刷新价格为当前价格
    /// CODE-08: herbPrices 不为 null 时刷新为当前价格；为 null 时保持原价
    /// </summary>
    public static IReadOnlyList<PrescriptionItemDto> ToPrescriptionItemDtos(
        this List<PrescriptionItemDto> items,
        IReadOnlyDictionary<Guid, decimal>? herbPrices = null)
    {
        if (items == null || !items.Any())
            return Array.Empty<PrescriptionItemDto>();

        if (herbPrices == null)
            return items.AsReadOnly();

        // CODE-08: 创建新对象，用当前价格替换历史价格
        return items
            .Select(item => new PrescriptionItemDto
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Dosage = item.Dosage,
                DecocteMethod = item.DecocteMethod,
                Unit = item.Unit,
                Usage = item.Usage,
                Remark = item.Remark,
                UnitPrice = herbPrices.TryGetValue(item.HerbId, out var currentPrice)
                    ? currentPrice
                    : item.UnitPrice // 查表中不存在则保持原价
            })
            .ToList();
    }
}

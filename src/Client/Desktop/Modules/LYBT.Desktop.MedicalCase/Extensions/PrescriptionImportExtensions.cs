using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Extensions;

/// <summary>
/// 处方导入扩展方法
/// OpenSpec: simplify-workspace-architecture - 替代PrescriptionImportHandler
/// OpenSpec: unify-control-data-binding - 统一使用PrescriptionItemDto
/// </summary>
public static class PrescriptionImportExtensions
{
    /// <summary>
    /// 将验方药材转换为PrescriptionItemDto列表
    /// </summary>
    public static IReadOnlyList<PrescriptionItemDto> ToPrescriptionItemDtos(
        this FormulaDetailDto formula,
        List<FormulaHerbItemDto> herbs)
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
                DecocteMethod = h.DecocteMethod
                // UnitPrice由HerbListControl从AllHerbs同步
            })
            .ToList();
    }

    /// <summary>
    /// 将历史处方药材直接返回（已经是PrescriptionItemDto类型）
    /// </summary>
    public static IReadOnlyList<PrescriptionItemDto> ToPrescriptionItemDtos(
        this List<PrescriptionItemDto> items)
    {
        if (items == null || !items.Any())
            return Array.Empty<PrescriptionItemDto>();

        return items.AsReadOnly();
    }
}

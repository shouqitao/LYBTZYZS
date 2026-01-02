using LYBT.Desktop.Infrastructure.Models;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方导入处理器
/// 负责验方导入和历史处方复制的DTO转换
/// OpenSpec: slim-medicalcase-workspace-viewmodel - Phase 5 简化后仅保留DTO转换
/// </summary>
public class PrescriptionImportHandler
{
    #region 字段

    private readonly ILogger<PrescriptionImportHandler> _logger;

    #endregion

    #region 构造函数

    public PrescriptionImportHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PrescriptionImportHandler>();
    }

    #endregion

    #region 验方导入

    /// <summary>
    /// 将验方药材转换为HerbItemDto列表
    /// 重复检测由HerbListControl内部处理
    /// </summary>
    /// <param name="formula">验方信息</param>
    /// <param name="herbs">验方药材列表</param>
    /// <returns>HerbItemDto列表</returns>
    public IReadOnlyList<HerbItemDto> ToHerbItemDtos(FormulaDetailDto formula, List<FormulaHerbItemDto> herbs)
    {
        if (formula == null || herbs == null || !herbs.Any())
        {
            _logger.LogWarning("[HDL] PrescriptionImport.ToHerbItemDtos - 验方无药材信息");
            return Array.Empty<HerbItemDto>();
        }

        var result = herbs
            .Where(h => h.HerbId.HasValue)
            .Select(h => new HerbItemDto
            {
                HerbId = h.HerbId!.Value,
                HerbName = h.HerbName ?? string.Empty,
                Dosage = h.Dosage,
                DecocteMethod = h.DecocteMethod
                // UnitPrice由HerbListControl从AllHerbs同步
            })
            .ToList();

        _logger.LogInformation("[HDL] PrescriptionImport.ToHerbItemDtos - FormulaName={FormulaName} ItemCount={Count}",
            formula.Name, result.Count);

        return result;
    }

    #endregion

    #region 历史处方复制

    /// <summary>
    /// 将历史处方药材转换为HerbItemDto列表
    /// 重复检测由HerbListControl内部处理
    /// </summary>
    /// <param name="items">历史处方药材项</param>
    /// <returns>HerbItemDto列表</returns>
    public IReadOnlyList<HerbItemDto> ToHerbItemDtos(List<PrescriptionItemDto> items)
    {
        if (items == null || !items.Any())
        {
            _logger.LogWarning("[HDL] PrescriptionImport.ToHerbItemDtos - 历史处方无药材记录");
            return Array.Empty<HerbItemDto>();
        }

        var result = items
            .Select(i => new HerbItemDto
            {
                HerbId = i.HerbId,
                HerbName = i.HerbName ?? string.Empty,
                Dosage = i.Dosage,
                DecocteMethod = i.DecocteMethod,
                UnitPrice = i.UnitPrice
            })
            .ToList();

        _logger.LogInformation("[HDL] PrescriptionImport.ToHerbItemDtos - ItemCount={Count}", result.Count);

        return result;
    }

    #endregion
}

using System.Collections.ObjectModel;
using LYBT.Desktop.Prescriptions.Models.Items;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方导入处理器
/// 负责验方导入和历史处方复制的数据处理逻辑
/// OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanelViewModel拆分
/// OpenSpec: enhance-dataflow-logging - LOG-018 统一[HDL]前缀
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
    /// 处理验方导入
    /// </summary>
    /// <param name="formula">验方信息</param>
    /// <param name="herbs">验方药材列表</param>
    /// <param name="existingHerbItems">当前处方药材项</param>
    /// <param name="allHerbs">所有可用药材</param>
    /// <returns>导入结果</returns>
    public FormulaImportResult ProcessFormulaImport(
        FormulaDetailDto formula,
        List<FormulaHerbItemDto> herbs,
        ObservableCollection<PrescriptionHerbItem> existingHerbItems,
        ObservableCollection<HerbListDto> allHerbs)
    {
        if (formula == null || herbs == null || !herbs.Any())
        {
            return FormulaImportResult.Failed("验方无药材信息");
        }

        // 检查重复药材（过滤掉HerbId为null的药材）
        // OpenSpec: enhance-duplicate-herb-dialog - 返回详细的重复药材信息用于逐个弹窗确认
        var existingHerbMap = existingHerbItems
            .Where(h => h.HerbId != Guid.Empty)
            .ToDictionary(h => h.HerbId, h => h);

        var duplicateInfos = new List<DuplicateHerbInfo>();
        foreach (var herb in herbs.Where(h => h.HerbId.HasValue && existingHerbMap.ContainsKey(h.HerbId.Value)))
        {
            var existing = existingHerbMap[herb.HerbId!.Value];
            duplicateInfos.Add(new DuplicateHerbInfo
            {
                HerbId = herb.HerbId.Value,
                HerbName = herb.HerbName ?? string.Empty,
                CurrentDosage = existing.Dosage,
                ImportedDosage = herb.Dosage
            });
        }

        var duplicates = duplicateInfos.Select(d => d.HerbName).ToList();

        // 准备要添加的药材
        var itemsToAdd = new List<HerbItemToAdd>();
        foreach (var herb in herbs)
        {
            // 跳过没有HerbId的药材
            if (!herb.HerbId.HasValue)
            {
                continue;
            }

            // 跳过已存在的药材
            if (existingHerbMap.ContainsKey(herb.HerbId.Value))
            {
                continue;
            }

            // 获取药材单价
            var herbInfo = allHerbs.FirstOrDefault(h => h.Id == herb.HerbId.Value);

            itemsToAdd.Add(new HerbItemToAdd
            {
                HerbId = herb.HerbId.Value,
                HerbName = herb.HerbName ?? string.Empty,
                Dosage = herb.Dosage,
                UnitPrice = herbInfo?.Price ?? 0m
            });
        }

        _logger.LogInformation("[HDL] PrescriptionImport.ProcessFormula completed - FormulaName={FormulaName} AddCount={AddCount} DuplicateCount={DuplicateCount}",
            formula.Name, itemsToAdd.Count, duplicateInfos.Count);

        return FormulaImportResult.Success(formula.Name, itemsToAdd, duplicateInfos);
    }

    #endregion

    #region 历史处方复制

    /// <summary>
    /// 处理历史处方复制
    /// </summary>
    /// <param name="historyItems">历史处方药材项</param>
    /// <param name="existingHerbItems">当前处方药材项</param>
    /// <returns>复制结果</returns>
    public HistoryCopyResult ProcessHistoryCopy(
        List<PrescriptionItemDto> historyItems,
        ObservableCollection<PrescriptionHerbItem> existingHerbItems)
    {
        if (historyItems == null || !historyItems.Any())
        {
            return HistoryCopyResult.Failed("历史处方无药材记录");
        }

        // 检查重复药材
        // OpenSpec: enhance-duplicate-herb-dialog - 返回详细的重复药材信息用于逐个弹窗确认
        var existingHerbMap = existingHerbItems
            .Where(h => h.HerbId != Guid.Empty)
            .ToDictionary(h => h.HerbId, h => h);

        var duplicateInfos = new List<DuplicateHerbInfo>();
        foreach (var item in historyItems.Where(i => existingHerbMap.ContainsKey(i.HerbId)))
        {
            var existing = existingHerbMap[item.HerbId];
            duplicateInfos.Add(new DuplicateHerbInfo
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName ?? string.Empty,
                CurrentDosage = existing.Dosage,
                ImportedDosage = item.Dosage
            });
        }

        // 准备要添加的药材
        var itemsToAdd = new List<HerbItemToAdd>();
        foreach (var item in historyItems)
        {
            // 跳过已存在的药材
            if (existingHerbMap.ContainsKey(item.HerbId))
            {
                continue;
            }

            itemsToAdd.Add(new HerbItemToAdd
            {
                HerbId = item.HerbId,
                HerbName = item.HerbName ?? string.Empty,
                Dosage = item.Dosage,
                UnitPrice = item.UnitPrice
            });
        }

        _logger.LogInformation("[HDL] PrescriptionImport.ProcessHistory completed - AddCount={AddCount} DuplicateCount={DuplicateCount}",
            itemsToAdd.Count, duplicateInfos.Count);

        return HistoryCopyResult.Success(itemsToAdd, duplicateInfos);
    }

    #endregion

    #region 通用添加方法

    /// <summary>
    /// 将药材项添加到处方
    /// </summary>
    /// <param name="herbItems">当前药材项集合</param>
    /// <param name="itemsToAdd">要添加的药材</param>
    /// <param name="createHerbItem">创建药材项的工厂方法</param>
    /// <returns>实际添加的数量</returns>
    public int AddHerbItemsToCollection(
        ObservableCollection<PrescriptionHerbItem> herbItems,
        List<HerbItemToAdd> itemsToAdd,
        Func<PrescriptionHerbItem> createHerbItem)
    {
        int addedCount = 0;
        foreach (var item in itemsToAdd)
        {
            // 找一个空槽位或添加新槽位
            var emptySlot = herbItems.FirstOrDefault(h => h.HerbId == Guid.Empty);
            if (emptySlot == null)
            {
                emptySlot = createHerbItem();
                herbItems.Add(emptySlot);
            }

            emptySlot.HerbId = item.HerbId;
            emptySlot.HerbName = item.HerbName;
            emptySlot.Dosage = item.Dosage;
            emptySlot.SetLoadedUnitPrice(item.UnitPrice);
            addedCount++;
        }

        _logger.LogDebug("[HDL] PrescriptionImport.AddItems - AddedCount={AddedCount}", addedCount);
        return addedCount;
    }

    #endregion
}

#region 数据传输对象

/// <summary>
/// 要添加的药材项
/// </summary>
public class HerbItemToAdd
{
    public Guid HerbId { get; init; }
    public string HerbName { get; init; } = string.Empty;
    public int Dosage { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// 验方导入结果
/// OpenSpec: enhance-duplicate-herb-dialog - 新增DuplicateInfos用于逐个弹窗确认
/// </summary>
public class FormulaImportResult
{
    public bool IsSuccess { get; private init; }
    public string FormulaName { get; private init; } = string.Empty;
    public List<HerbItemToAdd> ItemsToAdd { get; private init; } = new();
    public List<DuplicateHerbInfo> DuplicateInfos { get; private init; } = new();
    public string? ErrorMessage { get; private init; }

    public bool HasDuplicates => DuplicateInfos.Any();
    public string DuplicateWarningText => HasDuplicates
        ? $"发现重复药材：{string.Join("、", DuplicateInfos.Select(d => d.HerbName))}"
        : string.Empty;

    private FormulaImportResult() { }

    public static FormulaImportResult Success(string formulaName, List<HerbItemToAdd> itemsToAdd, List<DuplicateHerbInfo> duplicateInfos) => new()
    {
        IsSuccess = true,
        FormulaName = formulaName,
        ItemsToAdd = itemsToAdd,
        DuplicateInfos = duplicateInfos
    };

    public static FormulaImportResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// 历史处方复制结果
/// OpenSpec: enhance-duplicate-herb-dialog - 新增DuplicateInfos用于逐个弹窗确认
/// </summary>
public class HistoryCopyResult
{
    public bool IsSuccess { get; private init; }
    public List<HerbItemToAdd> ItemsToAdd { get; private init; } = new();
    public List<DuplicateHerbInfo> DuplicateInfos { get; private init; } = new();
    public string? ErrorMessage { get; private init; }

    public bool HasDuplicates => DuplicateInfos.Any();
    public string DuplicateWarningText => HasDuplicates
        ? $"发现重复药材：{string.Join("、", DuplicateInfos.Select(d => d.HerbName))}"
        : string.Empty;

    private HistoryCopyResult() { }

    public static HistoryCopyResult Success(List<HerbItemToAdd> itemsToAdd, List<DuplicateHerbInfo> duplicateInfos) => new()
    {
        IsSuccess = true,
        ItemsToAdd = itemsToAdd,
        DuplicateInfos = duplicateInfos
    };

    public static HistoryCopyResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

#endregion

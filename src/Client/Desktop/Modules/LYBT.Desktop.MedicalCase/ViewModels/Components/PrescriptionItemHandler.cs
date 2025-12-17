using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方项处理器
/// 负责药材项的创建、添加、删除、紧凑等操作
/// OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanelViewModel拆分
/// </summary>
public class PrescriptionItemHandler
{
    #region 字段

    private readonly ILogger<PrescriptionItemHandler> _logger;

    #endregion

    #region 常量

    /// <summary>
    /// 最小空槽位数量（只保留1个用于输入）
    /// OpenSpec: unify-medicalcase-view-edit-pattern - 用户要求只保留1个空白槽位
    /// </summary>
    private const int MinBlankSlots = 1;

    /// <summary>
    /// 初始槽位数量（1个空槽位用于输入）
    /// </summary>
    private const int InitialSlotCount = 1;

    #endregion

    #region 构造函数

    public PrescriptionItemHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PrescriptionItemHandler>();
    }

    #endregion

    #region 创建药材项

    /// <summary>
    /// 创建新的药材项ViewModel
    /// </summary>
    /// <param name="allHerbs">所有药材列表（用于下拉选择）</param>
    /// <param name="onItemChanged">项变化回调（HerbId或ItemTotal变化时触发）</param>
    /// <returns>新创建的药材项</returns>
    public PrescriptionHerbItemViewModel CreateHerbItem(
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        var item = new PrescriptionHerbItemViewModel
        {
            AllHerbs = allHerbs
        };

        // 订阅属性变化
        if (onItemChanged != null)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PrescriptionHerbItemViewModel.ItemTotal) ||
                    e.PropertyName == nameof(PrescriptionHerbItemViewModel.HerbId))
                {
                    onItemChanged(item, e.PropertyName);
                }
            };
        }

        return item;
    }

    /// <summary>
    /// 从DTO创建药材项
    /// </summary>
    /// <param name="itemDto">处方药材DTO</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    /// <returns>药材项ViewModel</returns>
    public PrescriptionHerbItemViewModel CreateHerbItemFromDto(
        PrescriptionItemDto itemDto,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        var item = CreateHerbItem(allHerbs, onItemChanged);

        item.HerbId = itemDto.HerbId;
        item.HerbName = itemDto.HerbName;
        item.Dosage = itemDto.Dosage;
        item.DecocteMethod = itemDto.DecocteMethod;
        // UnitPrice通过SetLoadedUnitPrice方法设置
        item.SetLoadedUnitPrice(itemDto.UnitPrice);

        return item;
    }

    #endregion

    #region 初始化和默认项

    /// <summary>
    /// 添加默认药材项（初始化时调用）
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    public void AddDefaultHerbItems(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        // 初始化8个空槽位（2行4列）
        for (int i = 0; i < InitialSlotCount; i++)
        {
            var item = CreateHerbItem(allHerbs, onItemChanged);
            herbItems.Add(item);
        }

        _logger.LogDebug("初始化{Count}个空槽位", InitialSlotCount);
    }

    #endregion

    #region 空槽位管理

    /// <summary>
    /// 确保至少有1个空槽位用于输入新药材
    /// OpenSpec: unify-medicalcase-view-edit-pattern - 改为只保留1个空白槽位
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    public void EnsureMinimumBlankRows(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        // 统计空槽位数量（未选择药材的槽位）
        var blankSlots = herbItems.Count(h => h.HerbId == Guid.Empty);

        // 如果空槽位不足4个，补充到4个
        var addedCount = 0;
        while (blankSlots < MinBlankSlots)
        {
            var item = CreateHerbItem(allHerbs, onItemChanged);
            herbItems.Add(item);
            blankSlots++;
            addedCount++;
        }

        if (addedCount > 0)
        {
            _logger.LogDebug("添加{Count}个空槽位以确保输入框可用", addedCount);
        }
    }

    #endregion

    #region 紧凑操作

    /// <summary>
    /// 紧凑药材列表：将所有非空药材移到前面，空槽位移到后面
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    public void CompactHerbItems(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        // 提取所有非空药材项（保持相对顺序）
        var nonEmptyItems = herbItems.Where(h => h.HerbId != Guid.Empty).ToList();

        // 清空集合
        herbItems.Clear();

        // 先添加非空药材
        foreach (var item in nonEmptyItems)
        {
            herbItems.Add(item);
        }

        // 再添加空槽位
        EnsureMinimumBlankRows(herbItems, allHerbs, onItemChanged);

        _logger.LogDebug("紧凑完成: {NonEmptyCount}个药材 + 空槽位", nonEmptyItems.Count);
    }

    #endregion

    #region 添加和删除

    /// <summary>
    /// 添加新行（在末尾添加一行4个空槽位）
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    public void AddNewRow(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        for (int i = 0; i < 4; i++)
        {
            var item = CreateHerbItem(allHerbs, onItemChanged);
            herbItems.Add(item);
        }

        _logger.LogDebug("添加新行: 4个空槽位");
    }

    /// <summary>
    /// 在指定位置后添加药材项
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="afterItem">在此项后添加</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    public void AddAfter(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        PrescriptionHerbItemViewModel afterItem,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        var index = herbItems.IndexOf(afterItem);
        if (index >= 0)
        {
            var newItem = CreateHerbItem(allHerbs, onItemChanged);
            herbItems.Insert(index + 1, newItem);
            _logger.LogDebug("在位置{Index}后添加新槽位", index);
        }
    }

    /// <summary>
    /// 删除药材项
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <param name="itemToDelete">要删除的项</param>
    /// <param name="allHerbs">所有药材列表</param>
    /// <param name="onItemChanged">项变化回调</param>
    /// <returns>是否成功删除</returns>
    public bool DeleteHerbItem(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems,
        PrescriptionHerbItemViewModel itemToDelete,
        ObservableCollection<HerbDto> allHerbs,
        Action<PrescriptionHerbItemViewModel, string>? onItemChanged = null)
    {
        if (itemToDelete == null)
        {
            return false;
        }

        var removed = herbItems.Remove(itemToDelete);
        if (removed)
        {
            _logger.LogDebug("删除药材项: {HerbName}", itemToDelete.HerbName);

            // 确保最少槽位
            EnsureMinimumBlankRows(herbItems, allHerbs, onItemChanged);
        }

        return removed;
    }

    #endregion

    #region 数据收集

    /// <summary>
    /// 收集处方药材项（用于保存）
    /// </summary>
    /// <param name="herbItems">药材项集合</param>
    /// <returns>处方药材DTO列表</returns>
    public List<PrescriptionItemInputDto> CollectPrescriptionItems(
        ObservableCollection<PrescriptionHerbItemViewModel> herbItems)
    {
        var items = new List<PrescriptionItemInputDto>();

        foreach (var herbItem in herbItems)
        {
            if (herbItem.HerbId != Guid.Empty && herbItem.Dosage > 0)
            {
                items.Add(new PrescriptionItemInputDto
                {
                    HerbId = herbItem.HerbId,
                    HerbName = herbItem.HerbName,
                    Dosage = herbItem.Dosage,
                    Unit = "g",
                    DecocteMethod = herbItem.DecocteMethod
                });
            }
        }

        return items;
    }

    #endregion
}

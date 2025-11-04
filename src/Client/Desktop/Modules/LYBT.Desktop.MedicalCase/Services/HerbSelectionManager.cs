using System.Collections.ObjectModel;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 8列DataGrid行模型（简化版）
/// 每行包含4个药材（药材+用量）
/// Issue #1807: 从PrescriptionEditorViewModel提取到HerbSelectionManager
/// </summary>
public class SimpleItemRow : BindableBase
{
    private PrescriptionItemDto _item1 = new();
    private PrescriptionItemDto _item2 = new();
    private PrescriptionItemDto _item3 = new();
    private PrescriptionItemDto _item4 = new();

    public PrescriptionItemDto Item1
    {
        get => _item1;
        set => SetProperty(ref _item1, value);
    }

    public PrescriptionItemDto Item2
    {
        get => _item2;
        set => SetProperty(ref _item2, value);
    }

    public PrescriptionItemDto Item3
    {
        get => _item3;
        set => SetProperty(ref _item3, value);
    }

    public PrescriptionItemDto Item4
    {
        get => _item4;
        set => SetProperty(ref _item4, value);
    }
}

/// <summary>
/// 药材选择管理器 - 负责管理已选择的药材列表
/// Issue #1807: 从PrescriptionEditorViewModel提取药材选择管理逻辑(~100行)
/// </summary>
public class HerbSelectionManager
{
    private readonly ILogger<HerbSelectionManager> _logger;

    /// <summary>
    /// 药材项行集合（8列DataGrid绑定）
    /// </summary>
    public ObservableCollection<SimpleItemRow> ItemRows { get; } = new();

    /// <summary>
    /// 药材总数
    /// </summary>
    public int ItemCount
    {
        get
        {
            var allItems = GetAllValidItems();
            return allItems.Count;
        }
    }

    /// <summary>
    /// 药材列表变更事件
    /// </summary>
    public event EventHandler<ItemsChangedEventArgs>? ItemsChanged;

    public HerbSelectionManager(ILogger<HerbSelectionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 添加8列行（包含4个药材空位）
    /// </summary>
    public void AddRow()
    {
        try
        {
            var newRow = new SimpleItemRow
            {
                Item1 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                Item2 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                Item3 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                Item4 = new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" }
            };

            ItemRows.Add(newRow);

            _logger.LogInformation("添加新行成功，当前共{RowCount}行", ItemRows.Count);

            // 触发事件
            ItemsChanged?.Invoke(this, new ItemsChangedEventArgs
            {
                ChangeType = ItemChangeType.RowAdded,
                ItemCount = ItemCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加新行失败");
        }
    }

    /// <summary>
    /// 从ItemRows提取所有非空药材
    /// Issue #1343: 阶段1修改 - 支持手工输入药材名称（不依赖HerbId）
    /// </summary>
    public List<PrescriptionItemDto> GetAllValidItems()
    {
        var result = new List<PrescriptionItemDto>();

        try
        {
            foreach (var row in ItemRows)
            {
                // 阶段1：检查药材名称而非HerbId，支持手工输入
                if (!string.IsNullOrWhiteSpace(row.Item1.HerbName))
                    result.Add(row.Item1);
                if (!string.IsNullOrWhiteSpace(row.Item2.HerbName))
                    result.Add(row.Item2);
                if (!string.IsNullOrWhiteSpace(row.Item3.HerbName))
                    result.Add(row.Item3);
                if (!string.IsNullOrWhiteSpace(row.Item4.HerbName))
                    result.Add(row.Item4);
            }

            _logger.LogInformation("提取有效药材：{ItemCount}味", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取有效药材失败");
        }

        return result;
    }

    /// <summary>
    /// 清空所有药材行
    /// </summary>
    public void ClearAll()
    {
        try
        {
            var previousCount = ItemRows.Count;
            ItemRows.Clear();

            _logger.LogInformation("清空所有药材行，清除了{PreviousCount}行", previousCount);

            // 触发事件
            ItemsChanged?.Invoke(this, new ItemsChangedEventArgs
            {
                ChangeType = ItemChangeType.AllCleared,
                ItemCount = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空药材行失败");
        }
    }

    /// <summary>
    /// 初始化指定数量的空行
    /// </summary>
    /// <param name="rowCount">行数</param>
    public void InitializeRows(int rowCount)
    {
        if (rowCount <= 0)
        {
            _logger.LogWarning("初始化行数必须大于0，当前值：{RowCount}", rowCount);
            return;
        }

        try
        {
            ClearAll();

            for (int i = 0; i < rowCount; i++)
            {
                AddRow();
            }

            _logger.LogInformation("初始化{RowCount}行完成", rowCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化药材行失败，RowCount: {RowCount}", rowCount);
        }
    }

    /// <summary>
    /// 设置药材项（用于导入验方等场景）
    /// </summary>
    /// <param name="items">药材列表</param>
    public void SetItems(List<PrescriptionItemDto> items)
    {
        if (items == null || items.Count == 0)
        {
            _logger.LogWarning("设置药材项失败：药材列表为空");
            return;
        }

        try
        {
            ClearAll();

            // 每行4个药材，计算需要多少行
            var rowCount = (int)Math.Ceiling(items.Count / 4.0);

            for (int i = 0; i < rowCount; i++)
            {
                var newRow = new SimpleItemRow
                {
                    Item1 = items.ElementAtOrDefault(i * 4) ?? new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                    Item2 = items.ElementAtOrDefault(i * 4 + 1) ?? new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                    Item3 = items.ElementAtOrDefault(i * 4 + 2) ?? new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" },
                    Item4 = items.ElementAtOrDefault(i * 4 + 3) ?? new PrescriptionItemDto { HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g" }
                };

                ItemRows.Add(newRow);
            }

            _logger.LogInformation("设置药材项完成：{ItemCount}味药材，{RowCount}行",
                items.Count, rowCount);

            // 触发事件
            ItemsChanged?.Invoke(this, new ItemsChangedEventArgs
            {
                ChangeType = ItemChangeType.ItemsSet,
                ItemCount = items.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置药材项失败");
        }
    }
}

/// <summary>
/// 药材变更类型
/// </summary>
public enum ItemChangeType
{
    /// <summary>
    /// 添加了新行
    /// </summary>
    RowAdded,

    /// <summary>
    /// 清空了所有行
    /// </summary>
    AllCleared,

    /// <summary>
    /// 设置了新的药材列表
    /// </summary>
    ItemsSet
}

/// <summary>
/// 药材列表变更事件参数
/// </summary>
public class ItemsChangedEventArgs : EventArgs
{
    /// <summary>
    /// 变更类型
    /// </summary>
    public ItemChangeType ChangeType { get; set; }

    /// <summary>
    /// 当前药材总数
    /// </summary>
    public int ItemCount { get; set; }
}

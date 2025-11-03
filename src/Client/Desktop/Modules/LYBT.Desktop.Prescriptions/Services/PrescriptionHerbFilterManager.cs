using System.Collections.ObjectModel;
using LYBT.Desktop.Modules.Prescriptions.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Modules.Prescriptions.Services;

/// <summary>
/// 处方药材过滤管理器 - 负责药材加载和拼音码过滤
/// Issue #1790: 从PrescriptionViewModel提取药材过滤逻辑(~100行)
/// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
/// </summary>
public class PrescriptionHerbFilterManager
{
    private readonly PrescriptionDataManager _dataManager;
    private readonly ILogger<PrescriptionHerbFilterManager> _logger;

    private List<HerbDto> _allHerbs = new();
    private ObservableCollection<HerbDto> _filteredHerbs = new();

    /// <summary>
    /// 所有药材列表（用于过滤）
    /// </summary>
    public List<HerbDto> AllHerbs
    {
        get => _allHerbs;
        set => _allHerbs = value;
    }

    /// <summary>
    /// 过滤后的药材列表（绑定到ComboBox）
    /// </summary>
    public ObservableCollection<HerbDto> FilteredHerbs => _filteredHerbs;

    public PrescriptionHerbFilterManager(
        PrescriptionDataManager dataManager,
        ILogger<PrescriptionHerbFilterManager> logger)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 加载所有药材数据
    /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
    /// </summary>
    public async Task LoadAllHerbsAsync()
    {
        try
        {
            // Issue #1786: 使用DataManager包装Repository方法
            var herbs = await _dataManager.SearchHerbsAsync(string.Empty);
            AllHerbs = herbs ?? new List<HerbDto>();
            _logger.LogInformation($"已加载 {AllHerbs.Count} 个药材");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载药材数据失败");
            AllHerbs = new List<HerbDto>();
        }
    }

    /// <summary>
    /// 根据输入文本过滤药材
    /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
    /// </summary>
    /// <param name="searchText">搜索文本（药材名称或拼音码）</param>
    public void FilterHerbs(string searchText)
    {
        try
        {
            _filteredHerbs.Clear();

            // 如果搜索文本为空，不显示任何结果
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            // 过滤逻辑：匹配药材名称或拼音码（不区分大小写）
            var filtered = AllHerbs
                .Where(h => h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                           (h.PinYinCode?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(5) // 限制最多5个结果
                .ToList();

            // 添加到过滤结果集合
            foreach (var herb in filtered)
            {
                _filteredHerbs.Add(herb);
            }

            _logger.LogDebug($"过滤药材：输入='{searchText}'，结果数={filtered.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "过滤药材时发生异常");
        }
    }
}

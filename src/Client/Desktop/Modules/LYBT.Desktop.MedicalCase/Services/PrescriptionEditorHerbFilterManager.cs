using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 处方编辑器药材过滤管理器 - 负责药材加载和拼音码过滤
/// Issue #1790: 从PrescriptionEditorViewModel提取药材过滤逻辑(~60行)
/// </summary>
public class PrescriptionEditorHerbFilterManager
{
    private readonly IPrescriptionEditorService _prescriptionEditorService;
    private readonly ILogger<PrescriptionEditorHerbFilterManager> _logger;

    private List<HerbDto> _allHerbs = new();
    private ObservableCollection<HerbDto> _filteredHerbs = new();

    /// <summary>
    /// 所有药材列表（缓存）
    /// </summary>
    public List<HerbDto> AllHerbs => _allHerbs;

    /// <summary>
    /// 过滤后的药材列表（绑定到ComboBox）
    /// </summary>
    public ObservableCollection<HerbDto> FilteredHerbs => _filteredHerbs;

    /// <summary>
    /// 药材加载完成事件
    /// </summary>
    public event EventHandler<HerbsLoadedEventArgs>? HerbsLoaded;

    public PrescriptionEditorHerbFilterManager(
        IPrescriptionEditorService prescriptionEditorService,
        ILogger<PrescriptionEditorHerbFilterManager> logger)
    {
        _prescriptionEditorService = prescriptionEditorService ?? throw new ArgumentNullException(nameof(prescriptionEditorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 加载所有药材数据
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    public async Task LoadHerbsAsync()
    {
        try
        {
            var herbs = await _prescriptionEditorService.LoadAllHerbsAsync();
            _allHerbs = herbs.ToList();

            // 初始化FilteredHerbs（显示所有药材）
            _filteredHerbs.Clear();
            foreach (var herb in _allHerbs)
            {
                _filteredHerbs.Add(herb);
            }

            _logger.LogInformation("成功加载{Count}味药材", _allHerbs.Count);

            // 触发事件
            HerbsLoaded?.Invoke(this, new HerbsLoadedEventArgs { HerbCount = _allHerbs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载药材数据时发生异常");
            throw;
        }
    }

    /// <summary>
    /// 过滤药材（支持拼音码模糊匹配）
    /// Issue #1790: 从PrescriptionEditorViewModel提取
    /// </summary>
    public void FilterHerbs(string searchText)
    {
        try
        {
            var filtered = _prescriptionEditorService.FilterHerbs(searchText);

            _filteredHerbs.Clear();
            foreach (var herb in filtered)
            {
                _filteredHerbs.Add(herb);
            }

            _logger.LogDebug("过滤药材：搜索'{SearchText}'，匹配{Count}味", searchText, _filteredHerbs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "过滤药材时发生异常");
            throw;
        }
    }
}

/// <summary>
/// 药材加载完成事件参数
/// Issue #1790: 封装事件数据
/// </summary>
public class HerbsLoadedEventArgs : EventArgs
{
    public int HerbCount { get; set; }
}

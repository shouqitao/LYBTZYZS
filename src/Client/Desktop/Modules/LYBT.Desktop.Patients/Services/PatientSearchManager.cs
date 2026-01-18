using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Patients.Interfaces;
// OpenSpec: refactor-frontend-srp-patterns - PatientService已迁移到Services命名空间
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者搜索管理器 - 负责患者搜索和分页逻辑
/// Issue #1790: 从PatientSelectionViewModel提取搜索和分页逻辑(~200行)
/// OpenSpec: refactor-patient-selection Task 1.3 - 集成搜索缓存
/// </summary>
public class PatientSearchManager
{
    private readonly PatientService _commandHandler;
    private readonly IPatientSearchCache _searchCache;
    private readonly ILogger<PatientSearchManager> _logger;

    private int _currentPage = 1;
    private int _totalPages = 0;
    private int _totalCount = 0;

    /// <summary>
    /// 患者列表（搜索结果或分页数据）
    /// </summary>
    public ObservableCollection<PatientListDto> Patients { get; } = new();

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage
    {
        get => _currentPage;
        set => _currentPage = value;
    }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages
    {
        get => _totalPages;
        set => _totalPages = value;
    }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => _totalCount = value;
    }

    /// <summary>
    /// 每页大小
    /// </summary>
    public static int PageSize => SystemConstants.DefaultPageSize;

    /// <summary>
    /// 搜索完成事件
    /// </summary>
    public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;

    public PatientSearchManager(
        PatientService commandHandler,
        IPatientSearchCache searchCache,
        ILogger<PatientSearchManager> logger)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _searchCache = searchCache ?? throw new ArgumentNullException(nameof(searchCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 执行搜索
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// OpenSpec: refactor-patient-selection Task 1.3 - 优先使用缓存
    /// </summary>
    public async Task<bool> ExecuteSearchAsync(string searchKeyword)
    {
        try
        {
            _logger.LogInformation("开始搜索患者，关键字：{SearchKeyword}，页码：{Page}", searchKeyword, CurrentPage);

            // 优先检查缓存
            var cachedResult = _searchCache.Get(searchKeyword, CurrentPage);
            if (cachedResult != null)
            {
                _logger.LogDebug("缓存命中，关键字：{SearchKeyword}，页码：{Page}", searchKeyword, CurrentPage);
                UpdatePatientsAndPaging(cachedResult.Items, cachedResult.TotalCount, cachedResult.CurrentPage, cachedResult.TotalPages);
                SearchCompleted?.Invoke(this, new SearchCompletedEventArgs
                {
                    Keyword = searchKeyword,
                    ResultCount = cachedResult.TotalCount,
                    CurrentPage = CurrentPage,
                    FromCache = true
                });
                return true;
            }

            // 缓存未命中，调用API
            var response = await _commandHandler.GetPatientsPagedAsync(CurrentPage, PageSize, searchKeyword);

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("搜索患者失败：{ErrorMessage}", response.Error);
                return false;
            }

            // 写入缓存
            _searchCache.Set(searchKeyword, CurrentPage, response.Data);

            UpdatePatientsAndPaging(response.Data.Items, response.Data.TotalCount, response.Data.CurrentPage, response.Data.TotalPages);

            _logger.LogInformation("搜索成功，共{Count}条患者", response.Data.TotalCount);

            // 触发事件
            SearchCompleted?.Invoke(this, new SearchCompletedEventArgs
            {
                Keyword = searchKeyword,
                ResultCount = response.Data.TotalCount,
                CurrentPage = CurrentPage,
                FromCache = false
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索患者失败");
            return false;
        }
    }

    /// <summary>
    /// 加载初始患者列表（首次加载）
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    public async Task LoadInitialPatientsAsync()
    {
        try
        {
            _logger.LogInformation("加载初始患者列表，第{Page}页", CurrentPage);

            var response = await _commandHandler.GetPatientsPagedAsync(CurrentPage, PageSize, string.Empty);

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("加载初始患者列表失败：{ErrorMessage}", response.Error);
                return;
            }

            UpdatePatientsAndPaging(response.Data.Items, response.Data.TotalCount, response.Data.CurrentPage, response.Data.TotalPages);

            _logger.LogInformation("初始患者列表加载成功，共{Count}条患者", response.Data.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载初始患者列表失败");
            throw;
        }
    }

    /// <summary>
    /// 加载当前页
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// OpenSpec: refactor-patient-selection Task 1.3 - 优先使用缓存
    /// </summary>
    public async Task LoadCurrentPageAsync(string searchKeyword)
    {
        try
        {
            _logger.LogInformation("加载第{Page}页，关键字：{SearchKeyword}", CurrentPage, searchKeyword);

            // 优先检查缓存
            var cachedResult = _searchCache.Get(searchKeyword, CurrentPage);
            if (cachedResult != null)
            {
                _logger.LogDebug("分页缓存命中，关键字：{SearchKeyword}，页码：{Page}", searchKeyword, CurrentPage);
                UpdatePatientsAndPaging(cachedResult.Items, cachedResult.TotalCount, cachedResult.CurrentPage, cachedResult.TotalPages);
                return;
            }

            // 缓存未命中，调用API
            var response = await _commandHandler.GetPatientsPagedAsync(CurrentPage, PageSize, searchKeyword);

            if (!response.Success || response.Data == null)
            {
                _logger.LogWarning("加载当前页失败：{ErrorMessage}", response.Error);
                return;
            }

            // 写入缓存
            _searchCache.Set(searchKeyword, CurrentPage, response.Data);

            UpdatePatientsAndPaging(response.Data.Items, response.Data.TotalCount, response.Data.CurrentPage, response.Data.TotalPages);

            _logger.LogInformation("第{Page}页加载成功", CurrentPage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载当前页失败");
            throw;
        }
    }

    /// <summary>
    /// 上一页
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    public async Task<bool> PreviousPageAsync(string searchKeyword)
    {
        if (CurrentPage <= 1)
        {
            _logger.LogWarning("已经是第一页");
            return false;
        }

        CurrentPage--;
        await LoadCurrentPageAsync(searchKeyword);
        return true;
    }

    /// <summary>
    /// 下一页
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    public async Task<bool> NextPageAsync(string searchKeyword)
    {
        if (CurrentPage >= TotalPages)
        {
            _logger.LogWarning("已经是最后一页");
            return false;
        }

        CurrentPage++;
        await LoadCurrentPageAsync(searchKeyword);
        return true;
    }

    /// <summary>
    /// 更新患者列表和分页信息
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    private void UpdatePatientsAndPaging(
        List<PatientListDto> patients,
        int totalCount,
        int currentPage,
        int totalPages)
    {
        Patients.Clear();
        foreach (var patient in patients)
        {
            Patients.Add(patient);
        }

        TotalCount = totalCount;
        CurrentPage = currentPage;
        TotalPages = totalPages;

        _logger.LogDebug("更新患者列表：当前页{CurrentPage}/{TotalPages}，共{TotalCount}条",
            CurrentPage, TotalPages, TotalCount);
    }

    /// <summary>
    /// 判断是否可以上一页
    /// </summary>
    public bool CanPreviousPage() => CurrentPage > 1;

    /// <summary>
    /// 判断是否可以下一页
    /// </summary>
    public bool CanNextPage() => CurrentPage < TotalPages;

    /// <summary>
    /// 使缓存失效
    /// OpenSpec: refactor-patient-selection Task 1.3 - 患者变更时调用
    /// </summary>
    /// <param name="keyword">可选的关键字，为null时清空所有缓存</param>
    public void InvalidateCache(string? keyword = null)
    {
        _searchCache.Invalidate(keyword);
        _logger.LogDebug("搜索缓存已失效，关键字：{Keyword}", keyword ?? "(全部)");
    }
}

/// <summary>
/// 搜索完成事件参数
/// Issue #1790: 封装事件数据
/// OpenSpec: refactor-patient-selection Task 1.3 - 添加FromCache属性
/// </summary>
public class SearchCompletedEventArgs : EventArgs
{
    public string Keyword { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public int CurrentPage { get; set; }
    /// <summary>
    /// 是否来自缓存
    /// </summary>
    public bool FromCache { get; set; }
}

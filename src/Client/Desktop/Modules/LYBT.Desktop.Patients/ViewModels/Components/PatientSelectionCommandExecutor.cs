using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Components;

/// <summary>
/// 患者选择命令执行器 - 封装分页和搜索命令的执行逻辑
/// OpenSpec: refactor-oversized-viewmodels Task 1.1
/// 
/// 职责:
/// - 封装分页操作的通用执行模式
/// - 同步SearchManager的分页状态到ViewModel属性
/// - 减少ViewModel中的重复代码
/// </summary>
public class PatientSelectionCommandExecutor
{
    private readonly PatientSearchManager _searchManager;
    private readonly ILogger _logger;
    private readonly Action<int, int, int> _syncPaginationProperties;
    private readonly Action<bool, string?> _setBusy;
    private readonly Func<string, Task> _showErrorMessage;
    private readonly IRelayCommand _previousPageCommand;
    private readonly IRelayCommand _nextPageCommand;

    /// <summary>
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm IRelayCommand
    /// </summary>
    public PatientSelectionCommandExecutor(
        PatientSearchManager searchManager,
        ILogger logger,
        Action<int, int, int> syncPaginationProperties,
        Action<bool, string?> setBusy,
        Func<string, Task> showErrorMessage,
        IRelayCommand previousPageCommand,
        IRelayCommand nextPageCommand)
    {
        _searchManager = searchManager ?? throw new ArgumentNullException(nameof(searchManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncPaginationProperties = syncPaginationProperties ?? throw new ArgumentNullException(nameof(syncPaginationProperties));
        _setBusy = setBusy ?? throw new ArgumentNullException(nameof(setBusy));
        _showErrorMessage = showErrorMessage ?? throw new ArgumentNullException(nameof(showErrorMessage));
        _previousPageCommand = previousPageCommand ?? throw new ArgumentNullException(nameof(previousPageCommand));
        _nextPageCommand = nextPageCommand ?? throw new ArgumentNullException(nameof(nextPageCommand));
    }

    /// <summary>
    /// 同步分页状态并刷新命令
    /// </summary>
    public void SyncPaginationState()
    {
        _syncPaginationProperties(
            _searchManager.CurrentPage,
            _searchManager.TotalPages,
            _searchManager.TotalCount);
        _previousPageCommand.NotifyCanExecuteChanged();
        _nextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    public async Task<bool> ExecuteSearchAsync(string keyword)
    {
        try
        {
            _setBusy(true, "正在搜索患者...");
            var success = await _searchManager.ExecuteSearchAsync(keyword);

            if (!success)
            {
                await _showErrorMessage("搜索失败");
                return false;
            }

            SyncPaginationState();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索患者失败");
            await _showErrorMessage(ClientErrorMessageMapper.GetSafeOperationFailureMessage("搜索", ex));
            return false;
        }
        finally
        {
            _setBusy(false, null);
        }
    }

    /// <summary>
    /// 执行上一页
    /// </summary>
    public async Task ExecutePreviousPageAsync(string keyword)
    {
        if (!_searchManager.CanPreviousPage()) return;

        try
        {
            _setBusy(true, "正在加载上一页...");
            await _searchManager.PreviousPageAsync(keyword);
            SyncPaginationState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载上一页失败");
            await _showErrorMessage(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载", ex));
        }
        finally
        {
            _setBusy(false, null);
        }
    }

    /// <summary>
    /// 执行下一页
    /// </summary>
    public async Task ExecuteNextPageAsync(string keyword)
    {
        if (!_searchManager.CanNextPage()) return;

        try
        {
            _setBusy(true, "正在加载下一页...");
            await _searchManager.NextPageAsync(keyword);
            SyncPaginationState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载下一页失败");
            await _showErrorMessage(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载", ex));
        }
        finally
        {
            _setBusy(false, null);
        }
    }

    /// <summary>
    /// 加载初始数据
    /// </summary>
    public async Task LoadInitialAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _setBusy(true, "正在加载患者列表...");
            await _searchManager.LoadInitialPatientsAsync();
            SyncPaginationState();

            stopwatch.Stop();
            _logger.LogInformation("患者列表加载完成: 数量={Count}, 耗时={ElapsedMs}ms",
                _searchManager.TotalCount, stopwatch.ElapsedMilliseconds);

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning("患者列表加载耗时过长: {ElapsedMs}ms > 500ms阈值", stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "加载患者列表失败，耗时={ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            await _showErrorMessage(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载患者列表", ex));
        }
        finally
        {
            _setBusy(false, null);
        }
    }
}

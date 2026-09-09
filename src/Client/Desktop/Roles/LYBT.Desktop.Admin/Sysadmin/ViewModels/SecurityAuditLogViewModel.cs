using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Admin.Sysadmin.ViewModels;

/// <summary>
/// 安全审计日志查看（US-SHELL-014）——独立页，与医案 AuditLog 分离
/// </summary>
public partial class SecurityAuditLogViewModel : NavigableViewModelBase
{
    private readonly ISecurityAuditQueryService _queryService;
    private readonly IConnectionModeService _connectionMode;
    private readonly INavigationCoordinator _navigationCoordinator;
    private const int PageSize = 20;

    [ObservableProperty]
    private ObservableCollection<SecurityAuditLogDto> _logs = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _currentPage = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _filterEventType = string.Empty;

    [ObservableProperty]
    private string _filterUserName = string.Empty;

    [ObservableProperty]
    private string _emptyMessage = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private SecurityAuditLogDto? _selectedLog;

    public SecurityAuditLogViewModel(
        IViewModelServices services,
        ISecurityAuditQueryService queryService,
        IConnectionModeService connectionMode,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _queryService = queryService;
        _connectionMode = connectionMode;
        _navigationCoordinator = navigationCoordinator;
        PageTitle = "安全审计日志";
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        CurrentPage = 1;
        _ = LoadLogsAsync();
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            IsLoading = true;
            Logs.Clear();
            IsEmpty = false;
            EmptyMessage = string.Empty;

            if (_connectionMode.IsLocal)
            {
                IsEmpty = true;
                EmptyMessage = "本地模式无安全审计数据（审计仅在远程模式记录）";
                TotalCount = 0;
                TotalPages = 1;
                return;
            }

            var result = await _queryService.GetLogsAsync(
                CurrentPage,
                PageSize,
                string.IsNullOrWhiteSpace(FilterEventType) ? null : FilterEventType.Trim(),
                string.IsNullOrWhiteSpace(FilterUserName) ? null : FilterUserName.Trim());

            if (result.Success && result.Data != null)
            {
                foreach (var log in result.Data.Items)
                    Logs.Add(log);
                TotalCount = result.Data.TotalCount;
                TotalPages = Math.Max(1, result.Data.TotalPages);
                if (Logs.Count == 0)
                {
                    IsEmpty = true;
                    EmptyMessage = "暂无安全审计记录";
                }
            }
            else
            {
                IsEmpty = true;
                EmptyMessage = result.Error ?? "查询失败";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载安全审计日志失败");
            IsEmpty = true;
            EmptyMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadLogsAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        FilterEventType = string.Empty;
        FilterUserName = string.Empty;
        CurrentPage = 1;
        await LoadLogsAsync();
    }

    private bool CanGoPrevious => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPageAsync()
    {
        CurrentPage--;
        await LoadLogsAsync();
    }

    private bool CanGoNext => CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync()
    {
        CurrentPage++;
        await LoadLogsAsync();
    }

    [RelayCommand]
    private void GoBack() => _navigationCoordinator.NavigateBack();
}

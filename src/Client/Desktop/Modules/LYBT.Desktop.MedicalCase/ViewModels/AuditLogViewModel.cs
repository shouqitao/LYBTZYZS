using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 医案审计日志视图模型
/// 无 MedicalCaseId 参数时仍展示状态提示，禁止空白页
/// </summary>
public partial class AuditLogViewModel : NavigableViewModelBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly INavigationCoordinator _navigationCoordinator;

    private Guid _medicalCaseId;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _logs = new();
    // IsLoading 复用基类（P2-A：移除遮蔽基类的重复声明）
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _currentPage = 1;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _totalPages;
    [ObservableProperty] private int _totalCount;

    /// <summary>空态/提示文案（无参或无数据时展示，禁止空白页）</summary>
    [ObservableProperty]
    private string _emptyStatusMessage = string.Empty;

    /// <summary>是否有空态提示可展示</summary>
    public bool HasEmptyStatus => !string.IsNullOrEmpty(EmptyStatusMessage) && Logs.Count == 0;

    private const int PageSize = 20;

    public AuditLogViewModel(IViewModelServices services, IAuditLogService auditLogService, INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        PageTitle = "审计日志";
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        if (navigationContext.Parameters.TryGetValue("MedicalCaseId", out Guid id) && id != Guid.Empty)
        {
            _medicalCaseId = id;
            EmptyStatusMessage = string.Empty;
            _ = LoadLogsAsync();
            return;
        }

        // 无参导航：不显示空白页——展示引导提示并尝试加载（接口仅支持按医案查询）
        _medicalCaseId = Guid.Empty;
        _ = LoadLogsAsync();
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            IsLoading = true;
            Logs.Clear();
            EmptyStatusMessage = string.Empty;

            if (_medicalCaseId == Guid.Empty)
            {
                // API 契约为 /medicalcases/{id}/audit-logs，无全局日志端点
                TotalCount = 0;
                TotalPages = 0;
                EmptyStatusMessage = "请从医案工作台或医案管理进入以查看审计日志";
                return;
            }

            var result = await _auditLogService.GetAuditLogsAsync(_medicalCaseId, CurrentPage, PageSize);
            if (result.Success && result.Data != null)
            {
                foreach (var log in result.Data.Items)
                    Logs.Add(log);
                TotalCount = result.Data.TotalCount;
                TotalPages = result.Data.TotalPages;

                if (Logs.Count == 0)
                    EmptyStatusMessage = "当前医案暂无审计日志";
            }
            else if (!result.Success)
            {
                EmptyStatusMessage = result.Error ?? "加载审计日志失败";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载审计日志失败");
            EmptyStatusMessage = "加载审计日志失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasEmptyStatus));
            // P2-A：翻页后刷新命令可执行状态（末页禁用 Next、回首页禁用 Prev）
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPageAsync()
    {
        CurrentPage--;
        await LoadLogsAsync();
    }

    private bool CanGoPrevious => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync()
    {
        CurrentPage++;
        await LoadLogsAsync();
    }

    private bool CanGoNext => CurrentPage < TotalPages;

    [RelayCommand]
    private void GoBack() => _navigationCoordinator.NavigateBack();
}

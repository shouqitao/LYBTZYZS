using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels;

/// <summary>
/// 医案审计日志视图模型
/// </summary>
public partial class AuditLogViewModel : NavigableViewModelBase
{
    private readonly IApiClientMedicalCases _medicalCaseApi;
    private readonly INavigationCoordinator _navigationCoordinator;

    private Guid _medicalCaseId;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _logs = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _totalCount;

    private const int PageSize = 20;

    public AuditLogViewModel(IViewModelServices services, IApiClientMedicalCases medicalCaseApi, INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _medicalCaseApi = medicalCaseApi;
        _navigationCoordinator = navigationCoordinator;
        PageTitle = "审计日志";
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        if (navigationContext.Parameters.TryGetValue("MedicalCaseId", out Guid id))
        {
            _medicalCaseId = id;
            _ = LoadLogsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            IsLoading = true;
            Logs.Clear();

            var result = await _medicalCaseApi.GetAuditLogsAsync(_medicalCaseId, CurrentPage, PageSize);
            if (result.Success && result.Data != null)
            {
                foreach (var log in result.Data.Items)
                    Logs.Add(log);
                TotalCount = result.Data.TotalCount;
                TotalPages = result.Data.TotalPages;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载审计日志失败");
        }
        finally
        {
            IsLoading = false;
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

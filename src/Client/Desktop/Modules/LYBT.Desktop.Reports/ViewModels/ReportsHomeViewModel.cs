using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Reports.ViewModels;

public partial class ReportsHomeViewModel : NavigableViewModelBase
{
    private readonly IApiClient _apiClient;

    [ObservableProperty]
    private DailyIncomeDto? _dailyIncome;

    [ObservableProperty]
    private DailyConsultationDto? _dailyConsultations;

    [ObservableProperty]
    private DailyHerbUsageDto? _dailyHerbUsage;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public ReportsHomeViewModel(
        IViewModelServices services,
        IApiClient apiClient)
        : base(services)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public override async void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        await LoadDataAsync();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        base.OnNavigatedFrom(navigationContext);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var incomeTask = _apiClient.Reports.GetDailyIncomeAsync();
            var consultationsTask = _apiClient.Reports.GetDailyConsultationsAsync();
            var herbsTask = _apiClient.Reports.GetDailyHerbUsageAsync();

            await Task.WhenAll(incomeTask, consultationsTask, herbsTask);

            var incomeResult = await incomeTask;
            var consultationsResult = await consultationsTask;
            var herbsResult = await herbsTask;

            if (incomeResult.Success)
                DailyIncome = incomeResult.Data;

            if (consultationsResult.Success)
                DailyConsultations = consultationsResult.Data;

            if (herbsResult.Success)
                DailyHerbUsage = herbsResult.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载报表数据失败");
            ErrorMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

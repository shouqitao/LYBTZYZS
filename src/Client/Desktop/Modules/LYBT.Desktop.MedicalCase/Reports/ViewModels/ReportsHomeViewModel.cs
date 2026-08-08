using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Reports;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.Reports.ViewModels;

public partial class ReportsHomeViewModel : NavigableViewModelBase
{
    private readonly IReportService _reportService;

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

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    public ReportsHomeViewModel(
        IViewModelServices services,
        IReportService reportService)
        : base(services)
    {
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadDataAsync();
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
    private void GoToToday() => SelectedDate = DateTime.Today;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var startDate = SelectedDate.Date;
            var endDate = SelectedDate.Date.AddDays(1);

            var incomeTask = _reportService.GetDailyIncomeAsync(startDate, endDate);
            var consultationsTask = _reportService.GetDailyConsultationsAsync(startDate, endDate);
            var herbsTask = _reportService.GetDailyHerbUsageAsync(startDate, endDate);

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
            ErrorMessage = "报表加载失败，请稍后重试";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

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

    /// <summary>加载版本号——丢弃过期完成（P1-E 日期快速切换竞态防护）。</summary>
    private int _loadVersion;
 
    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadDataAsync();
    }
 
    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        // P1-E：改普通 void——LoadDataAsync 内部已 try-catch，async void 重写有进程崩溃风险
        _ = LoadDataAsync();
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
        var version = ++_loadVersion;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var startDate = SelectedDate.Date;
            var endDate = SelectedDate.Date.AddDays(1);

            // P1-E：逐个 await 并独立 try-catch——单个报表异常不丢弃另两个的成功结果
            var incomeResult = await TryLoadAsync(() => _reportService.GetDailyIncomeAsync(startDate, endDate));
            var consultationsResult = await TryLoadAsync(() => _reportService.GetDailyConsultationsAsync(startDate, endDate));
            var herbsResult = await TryLoadAsync(() => _reportService.GetDailyHerbUsageAsync(startDate, endDate));

            if (version != _loadVersion) return; // 过期结果丢弃

            if (incomeResult is { Success: true })
                DailyIncome = incomeResult.Data;

            if (consultationsResult is { Success: true })
                DailyConsultations = consultationsResult.Data;

            if (herbsResult is { Success: true })
                DailyHerbUsage = herbsResult.Data;

            // 全部失败时给出统一提示（单个失败保持其他结果可用）
            if (incomeResult is null && consultationsResult is null && herbsResult is null)
                ErrorMessage = "报表加载失败，请稍后重试";
        }
        finally
        {
            if (version == _loadVersion) IsLoading = false;
        }
    }

    private static async Task<LYBT.Desktop.Contracts.Results.CommandResult<T>?> TryLoadAsync<T>(
        Func<Task<LYBT.Desktop.Contracts.Results.CommandResult<T>>> loader)
        where T : class
    {
        try
        {
            return await loader();
        }
        catch (Exception)
        {
            // 单个报表失败不影响其余；错误经 ErrorMessage 汇总提示
            return null;
        }
    }
}

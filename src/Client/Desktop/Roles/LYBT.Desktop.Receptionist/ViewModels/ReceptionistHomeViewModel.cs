using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Receptionist.ViewModels;

/// <summary>
/// 前台工作台主页ViewModel
/// OpenSpec: create-receptionist-workspace
/// 
/// 功能：
/// - 今日挂号统计
/// - 患者快速查询
/// - 挂号队列概览
/// - 快捷操作导航
/// </summary>
public partial class ReceptionistHomeViewModel : NavigableViewModelBase
{
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IRegistrationService _registrationService;
    private readonly IPatientService _patientService;
    private readonly ISessionManager _sessionManager;

    #region 可观察属性

    [ObservableProperty]
    private int _todayRegistrationCount;

    [ObservableProperty]
    private int _waitingCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private string _currentUserName = string.Empty;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RegistrationQueueItem> _registrationQueue = new();

    #endregion

    public ReceptionistHomeViewModel(
        IViewModelServices services,
        INavigationCoordinator navigationCoordinator,
        IRegistrationService registrationService,
        IPatientService patientService,
        ISessionManager sessionManager) : base(services)
    {
        _navigationCoordinator = navigationCoordinator;
        _registrationService = registrationService;
        _patientService = patientService;
        _sessionManager = sessionManager;
        
        PageTitle = "前台工作台";
        LoadCurrentUser();
    }

    private void LoadCurrentUser()
    {
        var user = _sessionManager.CurrentUser;
        CurrentUserName = user?.RealName ?? "前台接待";
    }

    #region 导航命令

    [RelayCommand]
    private void NavigateToPatientManagement()
        => NavigateToView(ViewNames.PatientManagement);

    [RelayCommand]
    private void NavigateToRegistrationQueue()
        => NavigateToView(ViewNames.RegistrationList);

    [RelayCommand]
    private void NavigateToCardReader()
    {
        // 打开读卡器对话框或导航到读卡器页面
        Logger.LogInformation("打开身份证读卡器");
        // TODO: 实现读卡器导航
    }

    [RelayCommand]
    private void CreateNewPatient()
    {
        NavigateToView(ViewNames.PatientManagement, new Dictionary<string, object> { { "Action", "Create" } });
    }

    [RelayCommand]
    private void CreateNewRegistration()
    {
        NavigateToView(ViewNames.RegistrationList, new Dictionary<string, object> { { "Action", "Create" } });
    }

    [RelayCommand]
    private async Task SearchPatientAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword))
        {
            await ShowWarningMessageAsync("请输入搜索关键词");
            return;
        }

        try
        {
            SetBusy(true, "搜索患者中...");
            var result = await _patientService.SearchPatientsAsync(SearchKeyword);
            
            if (!result || result.Data == null)
            {
                Logger.LogWarning("搜索患者失败: {Error}", result.Error);
                return;
            }

            var patients = result.Data.ToList();
            
            if (patients.Count == 0)
            {
                await ShowConfirmMessageAsync($"未找到患者 '{SearchKeyword}'，是否创建新患者？", "患者不存在");
            }
            else if (patients.Count == 1)
            {
                // 直接导航到挂号创建，预填充患者
                NavigateToView(ViewNames.RegistrationList, new Dictionary<string, object>
                {
                    { "PatientId", patients[0].Id },
                    { "PatientName", patients[0].Name }
                });
            }
            else
            {
                // 多个结果，导航到患者列表
                NavigateToView(ViewNames.PatientManagement, new Dictionary<string, object>
                {
                    { "SearchKeyword", SearchKeyword }
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "搜索患者失败");
            await ShowErrorMessageAsync($"搜索失败: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    #endregion

    #region 数据加载

    protected override async Task InitializeAsync(NavigationContext context)
    {
        await LoadStatisticsAsync();
        await LoadRegistrationQueueAsync();
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            // 使用GetPagedAsync获取今日挂号数据
            var result = await _registrationService.GetPagedAsync(pageSize: 100);
            
            if (result && result.Data != null)
            {
                var registrations = result.Data.Items;
                TodayRegistrationCount = registrations.Count;
                WaitingCount = registrations.Count(r => r.Status == RegistrationStatus.Waiting);
                CompletedCount = registrations.Count(r => r.Status == RegistrationStatus.Completed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载挂号统计失败");
        }
    }

    private async Task LoadRegistrationQueueAsync()
    {
        try
        {
            SetBusy(true, "加载挂号队列...");
            var result = await _registrationService.GetQueueAsync();
            
            if (result && result.Data != null)
            {
                var queueItems = result.Data.Select(r => new RegistrationQueueItem
                {
                    Id = r.Id,
                    PatientName = r.PatientName,
                    DoctorName = r.DoctorName,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status
                });
                RegistrationQueue = new ObservableCollection<RegistrationQueueItem>(queueItems);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载挂号队列失败");
            SetError("加载挂号队列失败");
        }
        finally
        {
            SetBusy(false);
        }
    }

    #endregion

    #region 辅助方法

    private void NavigateToView(string viewName, IDictionary<string, object>? parameters = null)
    {
        try
        {
            Logger.LogDebug("前台导航到: {ViewName}", viewName);
            _navigationCoordinator.NavigateTo(viewName, parameters);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航失败: {ViewName}", viewName);
            SetError($"导航失败: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// 挂号队列项（前台工作台显示用）
/// </summary>
public class RegistrationQueueItem
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public RegistrationStatus Status { get; set; }
    public TimeSpan WaitingTime => DateTime.Now - CreatedAt;
}

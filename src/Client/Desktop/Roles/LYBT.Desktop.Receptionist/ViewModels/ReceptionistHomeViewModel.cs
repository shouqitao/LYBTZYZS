using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.CardReader.Models;
using LYBT.Desktop.CardReader.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Patients;
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
    private readonly ICardReaderService _cardReaderService;
    private readonly IPatientCardReaderIntegration _patientCardReaderIntegration;

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
        ISessionManager sessionManager,
        ICardReaderService cardReaderService,
        IPatientCardReaderIntegration patientCardReaderIntegration) : base(services)
    {
        _navigationCoordinator = navigationCoordinator;
        _registrationService = registrationService;
        _patientService = patientService;
        _sessionManager = sessionManager;
        _cardReaderService = cardReaderService;
        _patientCardReaderIntegration = patientCardReaderIntegration;

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
    private async Task NavigateToCardReaderAsync()
    {
        Logger.LogInformation("前台工作台 - 打开身份证读卡器");

        try
        {
            // 检查读卡器是否已连接
            if (!_cardReaderService.IsConnected)
            {
                Logger.LogInformation("读卡器未连接，尝试初始化");
                SetBusy(true, "正在连接读卡器...");

                var initialized = await _cardReaderService.InitializeAsync();
                if (!initialized)
                {
                    SetBusy(false, null);
                    await ShowWarningMessageAsync("读卡器初始化失败，请检查设备连接");
                    Logger.LogWarning("读卡器初始化失败");
                    return;
                }

                Logger.LogInformation("读卡器初始化成功");
            }

            // 读取身份证
            SetBusy(true, "正在读取身份证...");
            var cardResult = await _cardReaderService.ReadCardAsync();

            if (!cardResult.IsSuccess)
            {
                SetBusy(false, null);
                await ShowErrorMessageAsync($"读卡失败：{cardResult.ErrorMessage}");
                Logger.LogWarning("读卡失败：{ErrorCode} - {ErrorMessage}",
                    cardResult.ErrorCode, cardResult.ErrorMessage);
                return;
            }

            Logger.LogInformation("读卡成功：{Name}，身份证号：{IdNumber}",
                cardResult.Name, MaskIdNumber(cardResult.IdNumber));

            // 查找或创建患者
            await ProcessCardReaderResultAsync(cardResult);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "读卡器操作失败");
            await ShowErrorMessageAsync($"读卡操作失败：{ex.Message}");
        }
        finally
        {
            SetBusy(false, null);
        }
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

    #region 读卡器辅助方法

    /// <summary>
    /// 处理读卡器结果，查找或创建患者
    /// </summary>
    private async Task ProcessCardReaderResultAsync(CardReadResult cardResult)
    {
        try
        {
            SetBusy(true, "正在查找患者...");

            // 根据身份证号查找患者
            var existingPatient = await _patientCardReaderIntegration.FindPatientByIdNumberAsync(cardResult.IdNumber);

            if (existingPatient != null)
            {
                Logger.LogInformation("找到现有患者：{PatientId}, {Name}",
                    existingPatient.PatientId, existingPatient.Name);

                var visitInfo = existingPatient.VisitCount > 0
                    ? $"，已就诊{existingPatient.VisitCount}次"
                    : "，首次就诊";

                await ShowConfirmMessageAsync($"找到患者：{existingPatient.Name}{visitInfo}\n\n是否前往挂号？", "患者已存在");

                // 导航到挂号创建页面，预填充患者信息
                NavigateToView(ViewNames.RegistrationList, new Dictionary<string, object>
                {
                    { "PatientId", existingPatient.PatientId },
                    { "PatientName", existingPatient.Name }
                });
            }
            else
            {
                // 患者不存在，提示创建
                Logger.LogInformation("患者不存在，身份证号：{IdNumber}", MaskIdNumber(cardResult.IdNumber));

                var confirmed = await ShowConfirmMessageAsync(
                    $"未找到患者记录\n姓名：{cardResult.Name}\n身份证号：{MaskIdNumber(cardResult.IdNumber)}\n\n是否创建新患者档案？",
                    "创建新患者");

                if (confirmed)
                {
                    SetBusy(true, "正在创建患者...");
                    var newPatient = await _patientCardReaderIntegration.FindOrCreatePatientAsync(cardResult);

                    Logger.LogInformation("患者创建成功：{PatientId}, {Name}",
                        newPatient.PatientId, newPatient.Name);

                    await ShowSuccessMessageAsync($"患者 {newPatient.Name} 创建成功");

                    // 导航到挂号创建页面，预填充患者信息
                    NavigateToView(ViewNames.RegistrationList, new Dictionary<string, object>
                    {
                        { "PatientId", newPatient.PatientId },
                        { "PatientName", newPatient.Name }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "处理读卡器结果时发生异常");
            await ShowErrorMessageAsync($"处理患者信息失败：{ex.Message}");
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// 掩码身份证号（保护隐私）
    /// </summary>
    private static string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length < 10)
            return idNumber;

        return idNumber.Substring(0, 6) + "****" + idNumber.Substring(idNumber.Length - 4);
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

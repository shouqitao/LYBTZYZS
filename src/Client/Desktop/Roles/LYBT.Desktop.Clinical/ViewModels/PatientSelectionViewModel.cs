using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 患者选择ViewModel - 医生工作台专用
/// 用于医生选择患者并开始看诊
/// OpenSpec: refactor-clinical-workflow
/// </summary>
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    #region 依赖服务

    private readonly IPatientApi _patientApi;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly ICommonDialogService _dialogService;

    #endregion

    #region 属性

    private ObservableCollection<PatientListDto> _patients = new();
    /// <summary>
    /// 患者列表
    /// </summary>
    public ObservableCollection<PatientListDto> Patients
    {
        get => _patients;
        set => SetProperty(ref _patients, value);
    }

    private PatientListDto? _selectedPatient;
    /// <summary>
    /// 选中的患者
    /// </summary>
    public PatientListDto? SelectedPatient
    {
        get => _selectedPatient;
        set
        {
            if (SetProperty(ref _selectedPatient, value))
            {
                _ = LoadPatientDetailAsync();
                RaisePropertyChanged(nameof(HasSelection));
                StartConsultationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>是否有选中患者</summary>
    public bool HasSelection => SelectedPatient != null;

    private PatientDetailDto? _patientDetail;
    /// <summary>
    /// 患者详情
    /// </summary>
    public PatientDetailDto? PatientDetail
    {
        get => _patientDetail;
        set => SetProperty(ref _patientDetail, value);
    }

    private string _searchKeyword = string.Empty;
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    private string _statusMessage = string.Empty;
    /// <summary>
    /// 状态消息
    /// </summary>
    public new string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _isError;
    /// <summary>
    /// 是否错误状态
    /// </summary>
    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    #endregion

    #region 命令

    /// <summary>返回主页</summary>
    public DelegateCommand BackToHomeCommand { get; }

    /// <summary>新建患者</summary>
    public DelegateCommand NewPatientCommand { get; }

    /// <summary>刷新列表</summary>
    public DelegateCommand RefreshCommand { get; }

    /// <summary>搜索</summary>
    public DelegateCommand SearchCommand { get; }

    /// <summary>开始看诊</summary>
    public DelegateCommand StartConsultationCommand { get; }

    #endregion

    #region 构造函数

    public PatientSelectionViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IPatientApi patientApi,
        IMedicalCaseApi medicalCaseApi,
        MedicalCaseLifecycleHandler lifecycleHandler,
        ICommonDialogService dialogService)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
        _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
        _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        // 初始化命令
        BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
        NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
        RefreshCommand = new DelegateCommand(async () => await LoadPatientsAsync());
        SearchCommand = new DelegateCommand(async () => await LoadPatientsAsync());
        StartConsultationCommand = new DelegateCommand(ExecuteStartConsultationAsync, CanStartConsultation);
    }

    #endregion

    #region 命令实现

    /// <summary>返回主页</summary>
    private void ExecuteBackToHome()
    {
        try
        {
            Logger.LogInformation("返回主页");
            ExecuteNavigateToHome();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "返回主页时发生异常");
        }
    }

    /// <summary>新建患者</summary>
    private void ExecuteNewPatient()
    {
        try
        {
            Logger.LogInformation("导航到新建患者视图");
            RegionManager.RequestNavigate("ContentRegion", "PatientDetailView");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航到新建患者视图时发生异常");
        }
    }

    private bool CanStartConsultation() => SelectedPatient != null;

    /// <summary>开始看诊</summary>
    private async void ExecuteStartConsultationAsync()
    {
        if (SelectedPatient == null) return;

        try
        {
            SetBusy(true, "正在检查医案状态...");
            IsError = false;

            // 检查是否有挂起医案
            var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(SelectedPatient.Id);
            var suspendedCase = pendingCases?.Data?.FirstOrDefault(c => c.Type == PendingCaseType.Suspended);

            if (suspendedCase != null)
            {
                SetBusy(false, null);
                await HandleSuspendedCaseAsync(suspendedCase);
            }
            else
            {
                // 无挂起医案，直接创建新医案
                await CreateAndNavigateToNewMedicalCaseAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "开始看诊失败");
            StatusMessage = "开始看诊失败，请重试";
            IsError = true;
            await ShowErrorMessageAsync("开始看诊失败：" + ex.Message);
            SetBusy(false, null);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 加载患者列表
    /// </summary>
    private async Task LoadPatientsAsync()
    {
        try
        {
            SetBusy(true, "正在加载患者列表...");
            IsError = false;

            var response = await _patientApi.GetPatientsAsync(
                page: 1,
                pageSize: 100,
                keyword: string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword);

            if (response.Success && response.Data != null)
            {
                Patients = new ObservableCollection<PatientListDto>(response.Data.Items);
                StatusMessage = $"共 {response.Data.TotalCount} 位患者";
                Logger.LogInformation("加载患者列表成功，共 {Count} 条", response.Data.TotalCount);
            }
            else
            {
                StatusMessage = "加载患者列表失败";
                IsError = true;
                Logger.LogWarning("加载患者列表失败：{Message}", response.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者列表失败");
            StatusMessage = "加载患者列表失败";
            IsError = true;
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// 加载患者详情
    /// </summary>
    private async Task LoadPatientDetailAsync()
    {
        if (SelectedPatient == null)
        {
            PatientDetail = null;
            return;
        }

        try
        {
            var response = await _patientApi.GetPatientByIdAsync(SelectedPatient.Id);
            if (response.Success && response.Data != null)
            {
                PatientDetail = response.Data;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者详情失败");
        }
    }

    /// <summary>
    /// 处理挂起医案 - 四选项弹窗
    /// OpenSpec: refactor-clinical-workflow
    /// </summary>
    private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto suspendedCase)
    {
        var message = $"患者 {SelectedPatient!.Name} 有未完成的医案。\n\n" +
            "请选择操作：\n" +
            "• 「继续」- 继续看诊原医案\n" +
            "• 「新建」- 关闭原医案并新建";

        var continueExisting = await _dialogService.ShowConfirmAsync(message, "选择操作");

        if (continueExisting)
        {
            // 继续原医案
            Logger.LogInformation("用户选择继续原医案：{MedicalCaseId}", suspendedCase.MedicalCaseId);
            if (suspendedCase.MedicalCaseId.HasValue)
            {
                NavigateToMedicalCase(suspendedCase.MedicalCaseId.Value);
            }
        }
        else
        {
            // 关闭原医案并新建
            Logger.LogInformation("用户选择关闭原医案并新建");
            if (suspendedCase.MedicalCaseId.HasValue)
            {
                SetBusy(true, "正在关闭旧医案...");
                var cancelResult = await _lifecycleHandler.CancelAsync(suspendedCase.MedicalCaseId.Value);
                if (!cancelResult.success)
                {
                    Logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                    await ShowErrorMessageAsync("关闭旧医案失败：" + cancelResult.errorMessage);
                    SetBusy(false, null);
                    return;
                }
            }
            await CreateAndNavigateToNewMedicalCaseAsync();
        }
    }

    /// <summary>
    /// 创建新医案并导航
    /// </summary>
    private async Task CreateAndNavigateToNewMedicalCaseAsync()
    {
        if (SelectedPatient == null) return;

        try
        {
            SetBusy(true, "正在创建医案...");

            var createResult = await _lifecycleHandler.CreateMedicalCaseAsync(SelectedPatient.Id);
            if (!createResult.success)
            {
                Logger.LogWarning("创建医案失败：{Error}", createResult.errorMessage);
                await ShowErrorMessageAsync("创建医案失败：" + createResult.errorMessage);
                return;
            }

            Logger.LogInformation("创建医案成功：{MedicalCaseId}", createResult.medicalCaseId);
            NavigateToMedicalCase(createResult.medicalCaseId);
        }
        finally
        {
            SetBusy(false, null);
        }
    }

    /// <summary>
    /// 导航到医案工作区
    /// </summary>
    private void NavigateToMedicalCase(Guid medicalCaseId)
    {
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", medicalCaseId },
            { "CurrentPatient", PatientDetail },
            { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
            { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
        };

        RegionManager.RequestNavigate(RegionNames.ContentRegion, "MedicalCaseWorkspaceView", parameters);
        Logger.LogInformation("导航到医案工作区：{MedicalCaseId}", medicalCaseId);
    }

    /// <summary>
    /// 设置忙碌状态
    /// </summary>
    private void SetBusy(bool isBusy, string? message)
    {
        IsBusy = isBusy;
        if (!string.IsNullOrEmpty(message))
        {
            StatusMessage = message;
        }
    }

    #endregion

    #region INavigationAware

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);

        // 加载患者列表
        _ = LoadPatientsAsync();
    }

    public override bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public override void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 清理状态
    }

    #endregion
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

// OpenSpec: unify-navigation-architecture Phase 6 - 整合INavigationCoordinator

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 患者选择ViewModel - 医生工作台专用
/// 用于医生选择患者并开始看诊
/// OpenSpec: refactor-clinical-workflow
/// OpenSpec: standardize-viewmodel-framework - 迁移到NavigableViewModelBase
/// </summary>
public partial class PatientSelectionViewModel : NavigableViewModelBase
{
    #region 依赖服务

    private readonly IPatientApi _patientApi;
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly ICommonDialogService _dialogService;
    private readonly INavigationCoordinator _navigationCoordinator;

    #endregion

    #region 可观察属性

    /// <summary>
    /// 患者列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<PatientListDto> _patients = new();

    /// <summary>
    /// 选中的患者
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(StartMedicalCaseCommand))]
    private PatientListDto? _selectedPatient;

    /// <summary>
    /// 患者详情
    /// </summary>
    [ObservableProperty]
    private PatientDetailDto? _patientDetail;

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    /// <summary>
    /// 页面状态消息
    /// </summary>
    [ObservableProperty]
    private string _pageStatusMessage = string.Empty;

    /// <summary>
    /// 是否错误状态
    /// </summary>
    [ObservableProperty]
    private bool _isError;

    #endregion

    #region 计算属性

    /// <summary>是否有选中患者</summary>
    public bool HasSelection => SelectedPatient != null;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// OpenSpec: enhance-viewmodel-architecture - 使用IViewModelServices聚合服务
    /// </summary>
    public PatientSelectionViewModel(
        IViewModelServices services,
        IPatientApi patientApi,
        IMedicalCaseApi medicalCaseApi,
        IMedicalCaseService medicalCaseService,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
        _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        // OpenSpec: enhance-viewmodel-architecture - 使用基类CommonDialogService替代本地_dialogService
        _dialogService = services.CommonDialogService;
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
    }

    #endregion

    #region 属性变更处理

    /// <summary>
    /// SelectedPatient 变更时加载详情
    /// </summary>
    partial void OnSelectedPatientChanged(PatientListDto? value)
    {
        _ = LoadPatientDetailAsync();
    }

    #endregion

    #region 命令

    /// <summary>返回主页</summary>
    [RelayCommand]
    private void BackToHome()
    {
        try
        {
            Logger.LogInformation("返回主页");
            NavigateToHome();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "返回主页时发生异常");
        }
    }

    /// <summary>新建患者 - OpenSpec: migrate-views-to-role-modules</summary>
    [RelayCommand]
    private void NewPatient()
    {
        // 导航到患者管理视图，用户可在MasterDetail界面点击"新建"按钮
        // OpenSpec: unify-navigation-architecture Phase 6 - 使用INavigationCoordinator
        Logger.LogInformation("导航到患者管理视图");
        _navigationCoordinator.NavigateTo(ViewNames.PatientManagement);
    }

    /// <summary>刷新列表</summary>
    [RelayCommand]
    private async Task RefreshAsync() => await LoadPatientsAsync();

    /// <summary>搜索</summary>
    [RelayCommand]
    private async Task SearchAsync() => await LoadPatientsAsync();

    /// <summary>开始看诊</summary>
    [RelayCommand(CanExecute = nameof(CanStartMedicalCase))]
    private async Task StartMedicalCaseAsync()
    {
        if (SelectedPatient == null) return;

        try
        {
            SetBusyWithMessage(true, "正在检查医案状态...");
            IsError = false;

            // 检查该患者是否有进行中的医案（任何待处理状态）
            // OpenSpec: unify-pending-query-api - 使用patientId参数按患者筛选
            var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(SelectedPatient.Id);
            var existingCase = pendingCases?.Data?.FirstOrDefault();

            if (existingCase != null)
            {
                SetBusyWithMessage(false, null);

                // OpenSpec: unify-case-status - 根据CaseStatus决定处理方式
                if (existingCase.CaseStatus == MedicalCaseStatus.Draft)
                {
                    // 暂存草稿：让用户选择继续或新建
                    await HandleSuspendedCaseAsync(existingCase);
                }
                else
                {
                    // Active状态：直接打开现有医案
                    Logger.LogInformation("患者已有进行中的医案，直接打开：{MedicalCaseId}, CaseStatus: {CaseStatus}",
                        existingCase.MedicalCaseId, existingCase.CaseStatus);
                    if (existingCase.MedicalCaseId.HasValue)
                    {
                        NavigateToMedicalCase(existingCase.MedicalCaseId.Value);
                    }
                    else
                    {
                        Logger.LogError("进行中医案MedicalCaseId为空。PatientId: {PatientId}", existingCase.PatientId);
                        await ShowErrorDialogAsync("无法打开医案：医案ID丢失，请刷新后重试");
                    }
                }
            }
            else
            {
                // 无进行中医案，创建新医案
                await CreateAndNavigateToNewMedicalCaseAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "开始看诊失败");
            PageStatusMessage = "开始看诊失败，请重试";
            IsError = true;
            await ShowErrorDialogAsync("开始看诊失败：" + ex.Message);
            SetBusyWithMessage(false, null);
        }
    }

    private bool CanStartMedicalCase() => SelectedPatient != null;

    #endregion

    #region 私有方法

    /// <summary>
    /// 加载患者列表
    /// </summary>
    private async Task LoadPatientsAsync()
    {
        try
        {
            SetBusyWithMessage(true, "正在加载患者列表...");
            IsError = false;

            var response = await _patientApi.GetPatientsAsync(
                page: 1,
                pageSize: 100,
                keyword: string.IsNullOrWhiteSpace(SearchKeyword) ? null : SearchKeyword);

            if (response.Success && response.Data != null)
            {
                Patients = new ObservableCollection<PatientListDto>(response.Data.Items);
                PageStatusMessage = $"共 {response.Data.TotalCount} 位患者";
                Logger.LogInformation("加载患者列表成功，共 {Count} 条", response.Data.TotalCount);
            }
            else
            {
                PageStatusMessage = "加载患者列表失败";
                IsError = true;
                Logger.LogWarning("加载患者列表失败：{Message}", response.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者列表失败");
            PageStatusMessage = "加载患者列表失败";
            IsError = true;
        }
        finally
        {
            SetBusyWithMessage(false, null);
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
    /// OpenSpec: unify-navigation-architecture Phase 6 - 添加MedicalCaseId为空时的错误处理
    /// </summary>
    private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto suspendedCase)
    {
        var message = $"患者 {SelectedPatient!.Name} 有未完成的医案。\n\n" +
            "请选择操作：\n" +
            "继续 - 继续看诊原医案\n" +
            "新建 - 关闭原医案并新建";

        var continueExisting = await _dialogService.ShowConfirmAsync(message, "选择操作");

        if (continueExisting)
        {
            // 继续原医案
            Logger.LogInformation("用户选择继续原医案：{MedicalCaseId}", suspendedCase.MedicalCaseId);
            if (suspendedCase.MedicalCaseId.HasValue)
            {
                NavigateToMedicalCase(suspendedCase.MedicalCaseId.Value);
            }
            else
            {
                // 修复静默失败：MedicalCaseId为空时显示错误
                Logger.LogError("暂存医案MedicalCaseId为空，无法导航。PatientId: {PatientId}, CaseStatus: {CaseStatus}",
                    suspendedCase.PatientId, suspendedCase.CaseStatus);
                await ShowErrorDialogAsync("无法打开医案：医案ID丢失，请刷新后重试或联系管理员");
            }
        }
        else
        {
            // 关闭原医案并新建
            Logger.LogInformation("用户选择关闭原医案并新建");
            if (suspendedCase.MedicalCaseId.HasValue)
            {
                SetBusyWithMessage(true, "正在关闭旧医案...");
                var cancelResult = await _medicalCaseService.CancelMedicalCaseAsync(suspendedCase.MedicalCaseId.Value);
                if (!cancelResult.success)
                {
                    Logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                    await ShowErrorDialogAsync("关闭旧医案失败：" + cancelResult.errorMessage);
                    SetBusyWithMessage(false, null);
                    return;
                }
            }
            await CreateAndNavigateToNewMedicalCaseAsync();
        }
    }

    /// <summary>
    /// 创建新医案并导航
    /// OpenSpec: simplify-medicalcase-module - 使用MedicalCaseService
    /// </summary>
    private async Task CreateAndNavigateToNewMedicalCaseAsync()
    {
        if (SelectedPatient == null) return;

        try
        {
            SetBusyWithMessage(true, "正在创建医案...");

            var createResult = await _medicalCaseService.CreateMedicalCaseAsync(SelectedPatient.Id);
            if (!createResult.success)
            {
                Logger.LogWarning("创建医案失败：{Error}", createResult.errorMessage);
                await ShowErrorDialogAsync("创建医案失败：" + createResult.errorMessage);
                return;
            }

            Logger.LogInformation("创建医案成功：{MedicalCaseId}", createResult.medicalCaseId);
            NavigateToMedicalCase(createResult.medicalCaseId);
        }
        finally
        {
            SetBusyWithMessage(false, null);
        }
    }

    /// <summary>
    /// 导航到医案工作区
    /// OpenSpec: unify-navigation-architecture Phase 6 - 使用INavigationCoordinator
    /// </summary>
    private void NavigateToMedicalCase(Guid medicalCaseId)
    {
        var parameters = new Dictionary<string, object>
        {
            { "MedicalCaseId", medicalCaseId },
            { "CurrentPatient", PatientDetail! },
            { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
            { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
        };

        Logger.LogInformation("导航到医案工作区：{MedicalCaseId}", medicalCaseId);
        _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
    }

    /// <summary>
    /// 设置忙碌状态并更新状态消息
    /// </summary>
    private void SetBusyWithMessage(bool isBusy, string? message)
    {
        IsBusy = isBusy;
        if (!string.IsNullOrEmpty(message))
        {
            PageStatusMessage = message;
        }
    }

    /// <summary>
    /// 显示错误消息对话框
    /// </summary>
    private async Task ShowErrorDialogAsync(string message)
    {
        await _dialogService.ShowErrorAsync(message, "错误");
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
        base.OnNavigatedFrom(navigationContext);
        // 清理状态
    }

    #endregion
}

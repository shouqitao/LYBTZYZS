// P2-16-1 Clinical/Admin 重复引用已评估：见 02-desktop.md P1-2 段，保留编译期依赖+ModuleCatalog懒加载
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Desktop.Registrations.Events;
using LYBT.Desktop.Registrations.Models;
using LYBT.Desktop.Registrations.Services;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Registrations.ViewModels;

/// <summary>
/// 挂号队列 ViewModel -- 展示等待队列和挂号列表
/// PRD: registration.md US-REG-003 (查看挂号队列)
///
/// 职责:
/// - Receptionist: 查看全部队列，创建/取消挂号
/// - Doctor: 查看个人队列，接诊
/// - 定时刷新队列（每30秒），确保状态同步
/// </summary>
public partial class RegistrationListViewModel : NavigableViewModelBase
{
    private readonly IRegistrationService _registrationService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IPatientService _patientService;
    private readonly ISignalRClient _signalRClient;
    private readonly IDialogService? _dialogService;
    private readonly PeriodicTimer _refreshTimer = new(TimeSpan.FromSeconds(QueueRefreshIntervalSeconds));
    private CancellationTokenSource? _timerCts;

    /// <summary>队列自动刷新间隔（秒）——魔法数字提取（P1-F）。</summary>
    private const int QueueRefreshIntervalSeconds = 30;

    // P2-14-7 SignalR 退订：保留 EventAggregator 订阅 token 以便 Destruct 时取消
    private SubscriptionToken? _registrationRefreshedToken;

    #region Observable Properties

    /// <summary>等待队列 (Waiting 状态，按挂号时间升序)</summary>
    [ObservableProperty]
    private ObservableCollection<RegistrationDetailModel> _waitingQueue = [];

    /// <summary>选中的队列项</summary>
    [ObservableProperty]
    private RegistrationDetailModel? _selectedRegistration;

    /// <summary>队列项计数</summary>
    [ObservableProperty]
    private int _queueCount;

    /// <summary>是否为 Receptionist 角色</summary>
    [ObservableProperty]
    private bool _isReceptionist;

    /// <summary>是否为 Doctor 角色</summary>
    [ObservableProperty]
    private bool _isDoctor;

    #endregion

    #region Computed Properties

    /// <summary>是否有选中项</summary>
    public bool HasSelection => SelectedRegistration is not null;

    /// <summary>是否有队列数据</summary>
    public bool HasQueueData => QueueCount > 0;

    /// <summary>队列为空</summary>
    public bool IsQueueEmpty => QueueCount == 0;

    #endregion

    public RegistrationListViewModel(
        IViewModelServices services,
        IRegistrationService registrationService,
        INavigationCoordinator navigationCoordinator,
        IPatientService patientService,
        ISignalRClient signalRClient,
        IEventAggregator eventAggregator,
        IDialogService? dialogService = null)
        : base(services)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _signalRClient = signalRClient ?? throw new ArgumentNullException(nameof(signalRClient));
        _dialogService = dialogService;
        _eventAggregatorAccessor = eventAggregator;
        PageTitle = "挂号队列";

        // US-REG-008: SignalR 推送 / 降级轮询触发时刷新队列 - P2-14-7 保留 token 便于退订（防已释放 VM 仍回调）
        _registrationRefreshedToken = eventAggregator.GetEvent<RegistrationRefreshedEvent>().Subscribe(OnRegistrationRefreshed);

        var currentRole = SessionManager.CurrentUser?.Role;
        IsReceptionist = currentRole == UserRole.Receptionist || currentRole == UserRole.Admin || currentRole == UserRole.SuperAdmin;
        IsDoctor = currentRole == UserRole.Doctor;

        // IsReceptionist 在命令初始化后赋值，需手动通知命令重新评估可执行状态
        CreateRegistrationCommand.NotifyCanExecuteChanged();
    }

    #region Lifecycle
    /// <summary>busy 状态变化时刷新全部命令 CanExecute（P1-F：IsBusy 影响 CanCreate/CanStartVisit/CanCancel）。</summary>
    protected override void OnIsBusyChangedCore(bool value)
    {
        base.OnIsBusyChangedCore(value);
        CreateRegistrationCommand.NotifyCanExecuteChanged();
        StartVisitCommand.NotifyCanExecuteChanged();
        CancelRegistrationCommand.NotifyCanExecuteChanged();
    }

    /// <summary>首次导航初始化</summary>
    protected override async Task InitializeAsync(NavigationContext context)
    {
        await LoadQueueAsync();
        StartAutoRefresh();

        // US-REG-008: 医生建立 SignalR 推送连接（≤3 秒实时更新待诊列表）
        if (IsDoctor && SessionManager.CurrentUserId is { } doctorId)
        {
            await _signalRClient.StartAsync(doctorId);
        }
    }

    /// <summary>每次导航到此页面时刷新</summary>
    protected override void OnNavigatedToCore(NavigationContext context)
    {
        if (IsInitialized)
        {
            _ = LoadQueueAsync();
            StartAutoRefresh();
        }
    }

    /// <summary>离开页面时停止刷新</summary>
    protected override void OnNavigatedFromCore(NavigationContext context)
    {
        StopAutoRefresh();
        _ = _signalRClient.StopAsync();
    }

    protected override void OnDisposing()
    {
        // P2-14-7 退订：ViewModel 销毁时取消 EventAggregator 强引用，避免 Dispatcher 回调已释放实例
        if (_registrationRefreshedToken != null)
        {
            try { _eventAggregatorAccessor?.GetEvent<RegistrationRefreshedEvent>().Unsubscribe(_registrationRefreshedToken); } catch { }
            // 备用：直接 via Services.EventAggregator（NavigableViewModelBase 持有）
            try { Services.EventAggregator.GetEvent<RegistrationRefreshedEvent>().Unsubscribe(OnRegistrationRefreshed); } catch { }
        }
        _ = _signalRClient.StopAsync();
        StopAutoRefresh();
        // P1-F：释放轮询计时器（PeriodicTimer 实现 IDisposable）
        _refreshTimer.Dispose();
        base.OnDisposing();
    }

    // 为 OnDisposing 退订提供 accessor（构造时已注入 eventAggregator，未存字段则通过 Services 兜底）
    private IEventAggregator? _eventAggregatorAccessor;


    private void StartAutoRefresh()
    {
        if (_timerCts is not null) return;

        _timerCts = new CancellationTokenSource();
        _ = RunAutoRefreshLoopAsync(_timerCts.Token);
    }

    private void StopAutoRefresh()
    {
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = null;
    }

    private async Task RunAutoRefreshLoopAsync(CancellationToken ct)
    {
        while (await _refreshTimer.WaitForNextTickAsync(ct))
        {
            if (IsBusy) continue;
            Logger.LogDebug("[REG-VM] 定时刷新队列");
            await LoadQueueAsync();
        }
    }

    /// <summary>SignalR 推送 / 降级轮询触发时的队列刷新（页面未初始化则跳过）。</summary>
    private void OnRegistrationRefreshed()
    {
        if (!IsInitialized) return;
        _ = LoadQueueAsync();
    }

    #endregion

    #region Commands

    /// <summary>刷新队列</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadQueueAsync();
    }

    /// <summary>新建挂号 -- 打开挂号弹窗</summary>
    [RelayCommand(CanExecute = nameof(CanCreateRegistration))]
    private void CreateRegistration()
    {
        if (_dialogService is null)
        {
            Logger.LogWarning("[REG-VM] IDialogService 未注入，无法打开新建挂号弹窗");
            return;
        }

        _dialogService.ShowDialog("RegistrationCreateDialog", null, result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                Logger.LogInformation("[REG-VM] 新建挂号成功，刷新队列");
                _ = LoadQueueAsync();
            }
        });
    }

    /// <summary>仅 Receptionist（含 Admin/SuperAdmin）且空闲时可新建挂号，Doctor 禁用</summary>
    private bool CanCreateRegistration() => IsReceptionist && !IsBusy;

    /// <summary>
    /// 接诊: 从队列选中患者，创建医案
    /// US-REG-003 验收标准第4条
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartVisit))]
    private async Task StartVisitAsync()
    {
        if (SelectedRegistration is null) return;

        var registrationId = SelectedRegistration.Id;
        var patientId = SelectedRegistration.PatientId;

        try
        {
            SetBusy(true, "正在接诊...");

            var result = await _registrationService.StartVisitAsync(registrationId);
            if (!result.Success || result.Data == Guid.Empty)
            {
                await ShowErrorMessageAsync(result.Error ?? "接诊失败，请稍后重试");
                return;
            }

            Logger.LogInformation("[REG-VM] 接诊成功: RegistrationId={Id}, MedicalCaseId={McId}",
                registrationId, result.Data);

            // 刷新队列
            await LoadQueueAsync();

            // 获取患者详情（MedicalCaseWorkspace 需要完整 PatientDetailDto）
            var patientResult = await _patientService.GetByIdAsync(patientId);
            if (!patientResult.Success || patientResult.Data == null)
            {
                await ShowErrorMessageAsync("接诊成功，但无法获取患者信息，请手动打开医案");
                return;
            }

            // 导航到医案工作区（Clinical 模式，编辑状态）
            var navParams = new Dictionary<string, object>
            {
                { MedicalCaseNavigationParameters.MedicalCaseIdKey, result.Data },
                { "CurrentPatient", patientResult.Data },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };
            _ = _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, navParams);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-VM] 接诊失败: RegistrationId={Id}", registrationId);
            await ShowErrorMessageAsync("接诊失败，请稍后重试");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool CanStartVisit() =>
        SelectedRegistration is { Status: RegistrationStatus.Waiting } && !IsBusy;

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Receptionist 可取消 Waiting 状态的挂号
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelRegistration))]
    private async Task CancelRegistrationAsync()
    {
        if (SelectedRegistration is null) return;

        var confirmed = await ShowConfirmMessageAsync(
            $"确定取消患者 [{SelectedRegistration.PatientName}] 的挂号吗？",
            "取消挂号");
        if (!confirmed) return;

        try
        {
            SetBusy(true, "正在取消挂号...");

            var result = await _registrationService.CancelAsync(SelectedRegistration.Id);
            if (result.Success)
            {
                Logger.LogInformation("[REG-VM] 取消挂号成功: RegistrationId={Id}", SelectedRegistration.Id);
                await LoadQueueAsync();
            }
            else
            {
                await ShowErrorMessageAsync(result.Error ?? "取消挂号失败，可能存在关联的活跃医案");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-VM] 取消挂号失败: RegistrationId={Id}", SelectedRegistration.Id);
            await ShowErrorMessageAsync("取消挂号失败，请稍后重试");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // P2-16-3 评估：Cancel 按钮 CanExecute 仅 Receptionist 且 Source==Receptionist，Server 策略 ReceptionistOnly 双校验，离线分叉已在 P1-29 对齐
    private bool CanCancelRegistration() =>
        IsReceptionist
        && SelectedRegistration is { Status: RegistrationStatus.Waiting, Source: RegistrationSource.Receptionist }
        && !IsBusy;

    #endregion

    #region Private Methods

    private async Task LoadQueueAsync()
    {
        try
        {
            SetBusy(true, "加载挂号队列...");
            ClearError();

            // Doctor 只看自己的队列，Receptionist/Admin 看全部
            var doctorId = IsDoctor ? SessionManager.CurrentUserId : null;
            var result = await _registrationService.GetQueueAsync(doctorId);

            if (!result.Success || result.Data == null)
            {
                SetError(result.Error ?? "加载队列失败");
                return;
            }

            WaitingQueue = new ObservableCollection<RegistrationDetailModel>(
                result.Data.Select(dto => new RegistrationDetailModel
                {
                    Id = dto.Id,
                    PatientId = dto.PatientId,
                    PatientName = dto.PatientName,
                    DoctorId = dto.DoctorId,
                    DoctorName = dto.DoctorName,
                    MedicalCaseId = dto.MedicalCaseId,
                    QueueNumber = dto.QueueNumber,
                    RegistrationFee = dto.RegistrationFee,
                    Source = dto.Source,
                    Status = dto.Status,
                    CreatedAt = dto.CreatedAt
                }));
            QueueCount = result.Data.Count;
            SelectedRegistration = null;

            OnPropertyChanged(nameof(HasQueueData));
            OnPropertyChanged(nameof(IsQueueEmpty));

            Logger.LogDebug("[REG-VM] 队列加载完成: {Count} 条记录", QueueCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-VM] 加载队列失败");
            SetError(ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载挂号列表", ex));
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>选中项变更时通知命令可执行状态</summary>
    partial void OnSelectedRegistrationChanged(RegistrationDetailModel? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        StartVisitCommand.NotifyCanExecuteChanged();
        CancelRegistrationCommand.NotifyCanExecuteChanged();
    }

    #endregion
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Registration.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Registration.ViewModels;

/// <summary>
/// 挂号队列 ViewModel -- 展示等待队列和挂号列表
/// PRD: registration.md US-REG-003 (查看挂号队列)
///
/// 职责:
/// - Receptionist: 查看全部队列，创建/取消挂号
/// - Doctor: 查看个人队列，接诊
/// </summary>
public partial class RegistrationListViewModel : NavigableViewModelBase
{
    private readonly IRegistrationRepository _repository;

    #region Observable Properties

    /// <summary>等待队列 (Waiting 状态，按挂号时间升序)</summary>
    [ObservableProperty]
    private ObservableCollection<RegistrationListDto> _waitingQueue = [];

    /// <summary>选中的队列项</summary>
    [ObservableProperty]
    private RegistrationListDto? _selectedRegistration;

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
        IRegistrationRepository repository)
        : base(services)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        PageTitle = "挂号队列";

        // 角色判断
        var currentRole = SessionManager.CurrentUser?.Role;
        IsReceptionist = currentRole == UserRole.Receptionist || currentRole == UserRole.Admin || currentRole == UserRole.SuperAdmin;
        IsDoctor = currentRole == UserRole.Doctor;
    }

    #region Lifecycle

    /// <summary>首次导航初始化</summary>
    protected override async Task InitializeAsync(NavigationContext context)
    {
        await LoadQueueAsync();
    }

    /// <summary>每次导航到此页面时刷新</summary>
    protected override void OnNavigatedToCore(NavigationContext context)
    {
        if (IsInitialized)
        {
            _ = LoadQueueAsync();
        }
    }

    #endregion

    #region Commands

    /// <summary>刷新队列</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadQueueAsync();
    }

    /// <summary>
    /// 接诊: 从队列选中患者，创建医案
    /// US-REG-003 验收标准第4条
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartVisit))]
    private async Task StartVisitAsync()
    {
        if (SelectedRegistration is null) return;

        try
        {
            SetBusy(true, "正在接诊...");

            var medicalCaseId = await _repository.StartVisitAsync(SelectedRegistration.Id);
            if (medicalCaseId.HasValue)
            {
                Logger.LogInformation("[REG-VM] 接诊成功: RegistrationId={Id}, MedicalCaseId={McId}",
                    SelectedRegistration.Id, medicalCaseId.Value);

                // 刷新队列
                await LoadQueueAsync();

                // FUTURE: 接诊后导航到医案工作区 (US-REG-004)
            }
            else
            {
                await ShowErrorMessageAsync("接诊失败，请稍后重试");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-VM] 接诊失败: RegistrationId={Id}", SelectedRegistration.Id);
            await ShowErrorMessageAsync($"接诊失败: {ex.Message}");
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

            var success = await _repository.CancelAsync(SelectedRegistration.Id);
            if (success)
            {
                Logger.LogInformation("[REG-VM] 取消挂号成功: RegistrationId={Id}", SelectedRegistration.Id);
                await LoadQueueAsync();
            }
            else
            {
                await ShowErrorMessageAsync("取消挂号失败，可能存在关联的活跃医案");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-VM] 取消挂号失败: RegistrationId={Id}", SelectedRegistration.Id);
            await ShowErrorMessageAsync($"取消挂号失败: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

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
            var queue = await _repository.GetWaitingQueueAsync(doctorId);

            WaitingQueue = new ObservableCollection<RegistrationListDto>(queue);
            QueueCount = queue.Count;
            SelectedRegistration = null;

            OnPropertyChanged(nameof(HasQueueData));
            OnPropertyChanged(nameof(IsQueueEmpty));

            Logger.LogDebug("[REG-VM] 队列加载完成: {Count} 条记录", QueueCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[REG-VM] 加载队列失败");
            SetError($"加载失败: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>选中项变更时通知命令可执行状态</summary>
    partial void OnSelectedRegistrationChanged(RegistrationListDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        StartVisitCommand.NotifyCanExecuteChanged();
        CancelRegistrationCommand.NotifyCanExecuteChanged();
    }

    #endregion
}

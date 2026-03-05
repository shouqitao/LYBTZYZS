using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;

namespace LYBT.Desktop.Clinical.ViewModels.Workspace;

/// <summary>
/// Child VM for pending queue management.
/// Upgraded from PendingQueueHandler, replacing callbacks with IWorkspaceHost/IMedicalCaseWorkspaceContext.
/// </summary>
public class PendingQueueViewModel : ChildViewModelBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly INavigationCoordinator _navigationCoordinator;

    /// <summary>
    /// Delegate from parent for suspend-before-switch (edit mode uses this to save current edits).
    /// </summary>
    public Func<Task>? SuspendCurrentCase { get; set; }

    /// <summary>
    /// Pending queue from the manager.
    /// </summary>
    public ObservableCollection<PendingMedicalCaseDto> Queue => _pendingQueueManager.PendingQueue;

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    public bool HasNoPendingCases => Queue.Count == 0;

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand<PendingMedicalCaseDto> SelectCommand { get; }

    public PendingQueueViewModel(
        IMedicalCaseWorkspaceContext context,
        IWorkspaceHost host,
        ILoggerFactory loggerFactory,
        IMedicalCaseService medicalCaseService,
        IPendingQueueManager pendingQueueManager,
        INavigationCoordinator navigationCoordinator)
        : base(host, loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));

        RefreshCommand = new DelegateCommand(async () => await RefreshQueueAsync());
        SelectCommand = new DelegateCommand<PendingMedicalCaseDto>(async c => await SelectPendingCaseAsync(c));
    }

    /// <summary>
    /// Refresh the pending queue.
    /// </summary>
    public async Task RefreshQueueAsync()
    {
        try
        {
            IsRefreshing = true;
            await _pendingQueueManager.LoadPendingCasesAsync();
            Logger.LogInformation("待诊队列加载完成，共{Count}条", _pendingQueueManager.PendingQueue.Count);
            OnPropertyChanged(nameof(Queue));
            OnPropertyChanged(nameof(HasNoPendingCases));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载待诊队列失败");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Select a pending case and switch to it.
    /// </summary>
    public async Task SelectPendingCaseAsync(PendingMedicalCaseDto? pendingCase)
    {
        if (pendingCase == null) return;

        var currentMedicalCaseId = _context.MedicalCaseId;

        Logger.LogInformation("选择待诊患者：{PatientName}，CaseStatus: {CaseStatus}，MedicalCaseId: {MedicalCaseId}",
            pendingCase.PatientName, pendingCase.CaseStatus, pendingCase.MedicalCaseId);

        // 1. Active case cannot be switched (early return before try-finally)
        if (pendingCase.CaseStatus == MedicalCaseStatus.Active)
        {
            Logger.LogInformation("选择的是当前正在看诊的患者，不可切换");
            return;
        }

        // Skip if selecting the same case (early return before try-finally)
        if (pendingCase.MedicalCaseId == currentMedicalCaseId && currentMedicalCaseId != Guid.Empty)
        {
            Logger.LogInformation("选择的是当前医案，无需切换");
            return;
        }

        try
        {
            var isReadOnly = _context.State.IsReadOnly;

            var hasCurrentCase = currentMedicalCaseId != Guid.Empty;

            // 2. Current case exists -> auto-suspend then switch
            if (hasCurrentCase)
            {
                Host.SetBusy(true, "正在暂存当前医案...");
                if (!isReadOnly)
                {
                    // Edit mode: execute suspend delegate (save current edits)
                    if (SuspendCurrentCase != null)
                        await SuspendCurrentCase.Invoke();
                    Logger.LogInformation("编辑模式，自动暂存后切换到患者：{PatientName}", pendingCase.PatientName);
                }
                else
                {
                    // View mode: suspend via service
                    var switchResult = await _medicalCaseService.SuspendAsync(currentMedicalCaseId);
                    if (!switchResult.success)
                    {
                        Logger.LogWarning("切换时暂存当前医案失败：{Error}", switchResult.errorMessage);
                    }
                    Logger.LogInformation("查看模式，直接切换到患者：{PatientName}", pendingCase.PatientName);
                }
            }
            else
            {
                Host.SetBusy(true, "正在切换患者...");
                Logger.LogInformation("当前无医案，直接切换到患者：{PatientName}", pendingCase.PatientName);
            }

            // 3. Handle target patient
            if (pendingCase.CaseStatus == MedicalCaseStatus.Suspended)
            {
                Host.SetBusy(false);
                await HandleSuspendedCaseAsync(pendingCase);
            }
            else
            {
                await NavigateToNewMedicalCaseAsync(pendingCase);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "切换患者失败");
            await Host.ShowErrorAsync("切换患者失败，请重试");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    /// <summary>
    /// Handle a suspended case: show dialog to choose continue or create new.
    /// </summary>
    private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        var dialogService = Host.CommonDialogService;
        if (dialogService == null)
        {
            Logger.LogWarning("CommonDialogService为空，无法显示弹窗");
            return;
        }

        Logger.LogInformation("显示双选项弹窗，患者：{PatientName}，挂起医案：{MedicalCaseId}",
            pendingCase.PatientName, pendingCase.MedicalCaseId);

        var message = $"患者 {pendingCase.PatientName ?? "未知"} 有未完成的医案。\n\n" +
            "点击「确定」继续看诊原医案\n" +
            "点击「取消」关闭原医案并新建";

        var choice = await dialogService.ShowConfirmAsync(message, "选择操作");

        if (choice)
        {
            // User chose "continue"
            Logger.LogInformation("用户选择继续看诊，导航到挂起医案");
            await NavigateToExistingMedicalCaseAsync(pendingCase);
        }
        else
        {
            // User chose "create new" - close old case first
            Logger.LogInformation("用户选择新建医案，关闭挂起医案并创建新医案");
            Host.SetBusy(true, "正在关闭旧医案...");
            if (pendingCase.MedicalCaseId.HasValue)
            {
                var cancelResult = await _medicalCaseService.CancelMedicalCaseAsync(pendingCase.MedicalCaseId.Value);
                if (!cancelResult.success)
                {
                    Logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                    await Host.ShowErrorAsync("关闭旧医案失败：" + cancelResult.errorMessage);
                    Host.SetBusy(false);
                    return;
                }
            }
            await NavigateToNewMedicalCaseAsync(pendingCase);
        }
    }

    /// <summary>
    /// Create a new medical case for the patient and navigate to it.
    /// </summary>
    private async Task NavigateToNewMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            Host.SetBusy(true, "正在创建新医案...");
            Logger.LogInformation("为患者创建新医案：{PatientName}", pendingCase.PatientName);

            var createResult = await _medicalCaseService.CreateMedicalCaseAsync(pendingCase.PatientId);
            if (!createResult.success)
            {
                Logger.LogWarning("创建医案失败：{Error}", createResult.errorMessage);
                await Host.ShowErrorAsync("创建医案失败：" + createResult.errorMessage);
                return;
            }

            var patientDetail = GetPatientDetail(pendingCase.PatientId);
            if (patientDetail is null)
            {
                Logger.LogWarning("获取患者详情失败：PatientId={PatientId}", pendingCase.PatientId);
                await Host.ShowErrorAsync("获取患者信息失败，请重试");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "MedicalCaseId", createResult.medicalCaseId },
                { "CurrentPatient", patientDetail },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
            Logger.LogInformation("已导航到新医案：{MedicalCaseId}", createResult.medicalCaseId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建新医案并导航失败");
            await Host.ShowErrorAsync("创建医案失败，请重试");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    /// <summary>
    /// Navigate to an existing (suspended) medical case.
    /// </summary>
    private async Task NavigateToExistingMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            Host.SetBusy(true, "正在加载医案...");

            if (!pendingCase.MedicalCaseId.HasValue)
            {
                Logger.LogWarning("挂起医案ID为空，无法导航");
                await Host.ShowErrorAsync("医案数据异常，请刷新后重试");
                return;
            }

            var patientDetail = GetPatientDetail(pendingCase.PatientId);
            if (patientDetail is null)
            {
                Logger.LogWarning("获取患者详情失败：PatientId={PatientId}", pendingCase.PatientId);
                await Host.ShowErrorAsync("获取患者信息失败，请重试");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "MedicalCaseId", pendingCase.MedicalCaseId.Value },
                { "CurrentPatient", patientDetail },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
            Logger.LogInformation("已导航到挂起医案：{MedicalCaseId}", pendingCase.MedicalCaseId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "导航到已存在医案失败");
            await Host.ShowErrorAsync("加载医案失败，请重试");
        }
        finally
        {
            Host.SetBusy(false);
        }
    }

    /// <summary>
    /// Get patient detail: return current patient if matching, otherwise null (OnNavigatedTo will handle).
    /// </summary>
    private PatientDetailDto? GetPatientDetail(Guid patientId)
    {
        try
        {
            var currentPatient = _context.CurrentPatient;
            if (currentPatient?.Id == patientId)
            {
                return currentPatient;
            }

            Logger.LogDebug("需要获取患者详情：{PatientId}", patientId);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取患者详情失败：{PatientId}", patientId);
            return null;
        }
    }
}

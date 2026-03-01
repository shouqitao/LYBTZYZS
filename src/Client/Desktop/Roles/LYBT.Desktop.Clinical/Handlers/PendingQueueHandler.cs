using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models; // WorkspaceMode, EditState
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Clinical.Handlers;

/// <summary>
/// 待诊队列操作处理器
/// 负责待诊队列的刷新、选择、切换等操作
/// OpenSpec: refactor-workspace-srp - 从MedicalCaseWorkspaceViewModel提取
/// </summary>
public class PendingQueueHandler
{
    #region 字段

    private readonly IMedicalCaseService _medicalCaseService;
    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly ILogger<PendingQueueHandler> _logger;

    #endregion

    #region 属性

    /// <summary>
    /// 获取弹窗服务（从ViewModel回调获取）
    /// </summary>
    public Func<ICommonDialogService?>? GetCommonDialogService { get; set; }

    /// <summary>
    /// 设置忙碌状态的回调
    /// </summary>
    public Action<bool, string?>? SetBusy { get; set; }

    /// <summary>
    /// 显示错误消息的回调
    /// </summary>
    public Func<string, Task>? ShowErrorMessage { get; set; }

    /// <summary>
    /// 获取当前医案ID的回调
    /// </summary>
    public Func<Guid>? GetCurrentMedicalCaseId { get; set; }

    /// <summary>
    /// 获取当前患者的回调
    /// </summary>
    public Func<PatientDetailDto?>? GetCurrentPatient { get; set; }

    /// <summary>
    /// 获取是否只读模式的回调
    /// </summary>
    public Func<bool>? GetIsReadOnly { get; set; }

    /// <summary>
    /// 挂起医案的回调（编辑模式使用）
    /// </summary>
    public Func<Task>? SuspendOnly { get; set; }

    /// <summary>
    /// 属性变更通知的回调
    /// </summary>
    public Action<string>? OnPropertyChanged { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public PendingQueueHandler(
        IMedicalCaseService medicalCaseService,
        IPendingQueueManager pendingQueueManager,
        INavigationCoordinator navigationCoordinator,
        ILoggerFactory loggerFactory)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _logger = loggerFactory.CreateLogger<PendingQueueHandler>();
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 刷新待诊队列
    /// </summary>
    /// <param name="setRefreshing">设置刷新状态的回调</param>
    public async Task RefreshQueueAsync(Action<bool> setRefreshing)
    {
        try
        {
            setRefreshing(true);
            await _pendingQueueManager.LoadPendingCasesAsync();
            _logger.LogInformation("待诊队列加载完成，共{Count}条", _pendingQueueManager.PendingQueue.Count);
            OnPropertyChanged?.Invoke("PendingQueue");
            OnPropertyChanged?.Invoke("HasNoPendingCases");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载待诊队列失败");
        }
        finally
        {
            setRefreshing(false);
        }
    }

    /// <summary>
    /// 选择待诊队列中的患者，切换到该患者的医案
    /// </summary>
    public async Task SelectPendingCaseAsync(PendingMedicalCaseDto? pendingCase)
    {
        if (pendingCase == null) return;

        try
        {
            var currentMedicalCaseId = GetCurrentMedicalCaseId?.Invoke() ?? Guid.Empty;
            var isReadOnly = GetIsReadOnly?.Invoke() ?? true;

            _logger.LogInformation("选择待诊患者：{PatientName}，CaseStatus: {CaseStatus}，MedicalCaseId: {MedicalCaseId}",
                pendingCase.PatientName, pendingCase.CaseStatus, pendingCase.MedicalCaseId);

            // 1. 正在看诊(Active) -> 不可操作
            if (pendingCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Active)
            {
                _logger.LogInformation("选择的是当前正在看诊的患者，不可切换");
                return;
            }

            // 如果选择的是当前医案，无需切换
            if (pendingCase.MedicalCaseId == currentMedicalCaseId && currentMedicalCaseId != Guid.Empty)
            {
                _logger.LogInformation("选择的是当前医案，无需切换");
                return;
            }

            var hasCurrentCase = currentMedicalCaseId != Guid.Empty;

            // 2. 当前有医案 -> 自动暂存后切换
            if (hasCurrentCase)
            {
                SetBusy?.Invoke(true, "正在暂存当前医案...");
                if (!isReadOnly)
                {
                    // 编辑模式：执行暂存回调（保存当前编辑内容）
                    if (SuspendOnly != null)
                        await SuspendOnly.Invoke();
                    _logger.LogInformation("编辑模式，自动暂存后切换到患者：{PatientName}", pendingCase.PatientName);
                }
                else
                {
                    // 查看模式：调用Service暂存
                    var switchResult = await _medicalCaseService.SuspendAsync(currentMedicalCaseId);
                    if (!switchResult.success)
                    {
                        _logger.LogWarning("切换时暂存当前医案失败：{Error}", switchResult.errorMessage);
                    }
                    _logger.LogInformation("查看模式，直接切换到患者：{PatientName}", pendingCase.PatientName);
                }
            }
            else
            {
                SetBusy?.Invoke(true, "正在切换患者...");
                _logger.LogInformation("当前无医案，直接切换到患者：{PatientName}", pendingCase.PatientName);
            }

            // 3. 处理目标患者
            if (pendingCase.CaseStatus == Shared.Models.Enums.MedicalCaseStatus.Suspended)
            {
                SetBusy?.Invoke(false, null);
                await HandleSuspendedCaseAsync(pendingCase);
            }
            else
            {
                await NavigateToNewMedicalCaseAsync(pendingCase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换患者失败");
            if (ShowErrorMessage != null)
                await ShowErrorMessage.Invoke("切换患者失败，请重试");
        }
        finally
        {
            SetBusy?.Invoke(false, null);
        }
    }

    /// <summary>
    /// 处理挂起医案（暂存状态的医案）
    /// </summary>
    private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        var dialogService = GetCommonDialogService?.Invoke();
        if (dialogService == null)
        {
            _logger.LogWarning("CommonDialogService为空，无法显示弹窗");
            return;
        }

        _logger.LogInformation("显示双选项弹窗，患者：{PatientName}，挂起医案：{MedicalCaseId}",
            pendingCase.PatientName, pendingCase.MedicalCaseId);

        // 使用ShowConfirmAsync: 确认=继续看诊，取消=新建医案
        var message = $"患者 {pendingCase.PatientName ?? "未知"} 有未完成的医案。\n\n" +
            "点击「确定」继续看诊原医案\n" +
            "点击「取消」关闭原医案并新建";

        var choice = await dialogService.ShowConfirmAsync(message, "选择操作");

        if (choice)
        {
            // 用户选择"继续看诊"
            _logger.LogInformation("用户选择继续看诊，导航到挂起医案");
            await NavigateToExistingMedicalCaseAsync(pendingCase);
        }
        else
        {
            // 用户选择"新建医案" - 需要先关闭旧医案
            _logger.LogInformation("用户选择新建医案，关闭挂起医案并创建新医案");
            SetBusy?.Invoke(true, "正在关闭旧医案...");
            if (pendingCase.MedicalCaseId.HasValue)
            {
                var cancelResult = await _medicalCaseService.CancelMedicalCaseAsync(pendingCase.MedicalCaseId.Value);
                if (!cancelResult.success)
                {
                    _logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                    if (ShowErrorMessage != null)
                        await ShowErrorMessage.Invoke("关闭旧医案失败：" + cancelResult.errorMessage);
                    SetBusy?.Invoke(false, null);
                    return;
                }
            }
            await NavigateToNewMedicalCaseAsync(pendingCase);
        }
    }

    /// <summary>
    /// 导航到新医案 - 为患者创建新医案并导航
    /// </summary>
    private async Task NavigateToNewMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            SetBusy?.Invoke(true, "正在创建新医案...");
            _logger.LogInformation("为患者创建新医案：{PatientName}", pendingCase.PatientName);

            // 创建新医案
            var createResult = await _medicalCaseService.CreateMedicalCaseAsync(pendingCase.PatientId);
            if (!createResult.success)
            {
                _logger.LogWarning("创建医案失败：{Error}", createResult.errorMessage);
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("创建医案失败：" + createResult.errorMessage);
                return;
            }

            // 获取患者详情
            var patientDetail = GetPatientDetailAsync(pendingCase.PatientId);
            if (patientDetail is null)
            {
                _logger.LogWarning("获取患者详情失败：PatientId={PatientId}", pendingCase.PatientId);
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("获取患者信息失败，请重试");
                return;
            }

            // 导航到医案工作台
            var parameters = new Dictionary<string, object>
            {
                { "MedicalCaseId", createResult.medicalCaseId },
                { "CurrentPatient", patientDetail },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
            _logger.LogInformation("已导航到新医案：{MedicalCaseId}", createResult.medicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建新医案并导航失败");
            if (ShowErrorMessage != null)
                await ShowErrorMessage.Invoke("创建医案失败，请重试");
        }
        finally
        {
            SetBusy?.Invoke(false, null);
        }
    }

    /// <summary>
    /// 导航到已存在的医案
    /// </summary>
    private async Task NavigateToExistingMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            SetBusy?.Invoke(true, "正在加载医案...");

            if (!pendingCase.MedicalCaseId.HasValue)
            {
                _logger.LogWarning("挂起医案ID为空，无法导航");
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("医案数据异常，请刷新后重试");
                return;
            }

            // 获取患者详情
            var patientDetail = GetPatientDetailAsync(pendingCase.PatientId);
            if (patientDetail is null)
            {
                _logger.LogWarning("获取患者详情失败：PatientId={PatientId}", pendingCase.PatientId);
                if (ShowErrorMessage != null)
                    await ShowErrorMessage.Invoke("获取患者信息失败，请重试");
                return;
            }

            // 导航到医案工作台
            var parameters = new Dictionary<string, object>
            {
                { "MedicalCaseId", pendingCase.MedicalCaseId.Value },
                { "CurrentPatient", patientDetail },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
            _logger.LogInformation("已导航到挂起医案：{MedicalCaseId}", pendingCase.MedicalCaseId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到已存在医案失败");
            if (ShowErrorMessage != null)
                await ShowErrorMessage.Invoke("加载医案失败，请重试");
        }
        finally
        {
            SetBusy?.Invoke(false, null);
        }
    }

    /// <summary>
    /// 获取患者详情
    /// </summary>
    private PatientDetailDto? GetPatientDetailAsync(Guid patientId)
    {
        try
        {
            // 如果当前患者就是目标患者，直接返回
            var currentPatient = GetCurrentPatient?.Invoke();
            if (currentPatient?.Id == patientId)
            {
                return currentPatient;
            }

            // 否则返回null，OnNavigatedTo会处理
            _logger.LogDebug("需要获取患者详情：{PatientId}", patientId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败：{PatientId}", patientId);
            return null;
        }
    }

    #endregion
}

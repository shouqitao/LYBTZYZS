using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.MedicalCase.Events;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 工作台待诊队列处理器
/// 负责待诊队列加载、患者切换、挂起医案处理等逻辑
/// OpenSpec: refactor-desktop-comprehensive - Phase 3 ViewModel瘦身
/// OpenSpec: redesign-pending-queue - 简化切换逻辑（自动暂存+双选项弹窗）
/// </summary>
public class WorkspacePendingQueueHandler
{
    #region 字段

    private readonly IPendingQueueManager _pendingQueueManager;
    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly IRegionManager _regionManager;
    private readonly ICommonDialogService? _commonDialogService;
    private readonly ILogger<WorkspacePendingQueueHandler> _logger;

    // 回调委托
    private readonly Func<Guid> _getMedicalCaseId;
    private readonly Func<bool> _getIsReadOnly;
    private readonly Func<Guid, Task<PatientDetailDto?>> _getPatientDetailAsync;
    private readonly Action<bool, string?> _setIsBusy;
    private readonly Func<string, Task> _showErrorAsync;
    private readonly Func<string, Task> _showSuccessAsync;

    #endregion

    #region 构造函数

    public WorkspacePendingQueueHandler(
        IPendingQueueManager pendingQueueManager,
        MedicalCaseLifecycleHandler lifecycleHandler,
        IRegionManager regionManager,
        ICommonDialogService? commonDialogService,
        ILoggerFactory loggerFactory,
        Func<Guid> getMedicalCaseId,
        Func<bool> getIsReadOnly,
        Func<Guid, Task<PatientDetailDto?>> getPatientDetailAsync,
        Action<bool, string?> setIsBusy,
        Func<string, Task> showErrorAsync,
        Func<string, Task> showSuccessAsync)
    {
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
        _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _commonDialogService = commonDialogService;
        _logger = loggerFactory.CreateLogger<WorkspacePendingQueueHandler>();

        _getMedicalCaseId = getMedicalCaseId ?? throw new ArgumentNullException(nameof(getMedicalCaseId));
        _getIsReadOnly = getIsReadOnly ?? throw new ArgumentNullException(nameof(getIsReadOnly));
        _getPatientDetailAsync = getPatientDetailAsync ?? throw new ArgumentNullException(nameof(getPatientDetailAsync));
        _setIsBusy = setIsBusy ?? throw new ArgumentNullException(nameof(setIsBusy));
        _showErrorAsync = showErrorAsync ?? throw new ArgumentNullException(nameof(showErrorAsync));
        _showSuccessAsync = showSuccessAsync ?? throw new ArgumentNullException(nameof(showSuccessAsync));
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 是否正在刷新待诊队列
    /// </summary>
    public bool IsRefreshingPendingQueue { get; private set; }

    #endregion

    #region 公共方法

    /// <summary>
    /// 加载待诊队列
    /// </summary>
    public async Task LoadPendingQueueAsync()
    {
        try
        {
            IsRefreshingPendingQueue = true;
            await _pendingQueueManager.LoadPendingCasesAsync();
            _logger.LogInformation("待诊队列加载完成，共{Count}条", _pendingQueueManager.PendingQueue.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载待诊队列失败");
        }
        finally
        {
            IsRefreshingPendingQueue = false;
        }
    }

    /// <summary>
    /// 选择待诊患者
    /// </summary>
    /// <param name="pendingCase">待诊医案</param>
    /// <param name="saveDraftOnlyAsync">暂存当前医案的回调</param>
    public async Task SelectPendingCaseAsync(
        PendingMedicalCaseDto? pendingCase,
        Func<Task> saveDraftOnlyAsync)
    {
        if (pendingCase == null) return;

        try
        {
            var medicalCaseId = _getMedicalCaseId();
            var isReadOnly = _getIsReadOnly();

            _logger.LogInformation("选择待诊患者：{PatientName}，Type: {Type}，MedicalCaseId: {MedicalCaseId}",
                pendingCase.PatientName, pendingCase.Type, pendingCase.MedicalCaseId);

            // 1. 正在看诊(InProgress) -> 不可操作
            if (pendingCase.Type == PendingCaseType.InProgress)
            {
                _logger.LogInformation("选择的是当前正在看诊的患者，不可切换");
                return;
            }

            // 如果选择的是当前医案，无需切换
            if (pendingCase.MedicalCaseId == medicalCaseId && medicalCaseId != Guid.Empty)
            {
                _logger.LogInformation("选择的是当前医案，无需切换");
                return;
            }

            var hasCurrentCase = medicalCaseId != Guid.Empty;

            // OpenSpec: redesign-pending-queue - 自动暂存，无需弹窗确认
            // 2. 当前有医案 -> 自动暂存后切换
            if (hasCurrentCase)
            {
                _setIsBusy(true, "正在暂存当前医案...");
                if (!isReadOnly)
                {
                    // 编辑模式：执行暂存回调（保存当前编辑内容）
                    await saveDraftOnlyAsync();
                    _logger.LogInformation("编辑模式，自动暂存后切换到患者：{PatientName}", pendingCase.PatientName);
                }
                else
                {
                    // 查看模式：调用生命周期暂存
                    var switchResult = await _lifecycleHandler.SaveDraftAsync(medicalCaseId);
                    if (!switchResult.success)
                    {
                        _logger.LogWarning("切换时暂存当前医案失败：{Error}", switchResult.errorMessage);
                    }
                    _logger.LogInformation("查看模式，直接切换到患者：{PatientName}", pendingCase.PatientName);
                }
            }
            else
            {
                _setIsBusy(true, "正在切换患者...");
                _logger.LogInformation("当前无医案，直接切换到患者：{PatientName}", pendingCase.PatientName);
            }

            // 3. 处理目标患者
            if (pendingCase.Type == PendingCaseType.Suspended)
            {
                _setIsBusy(false, null);
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
            await _showErrorAsync("切换患者失败，请重试");
        }
        finally
        {
            _setIsBusy(false, null);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 处理编辑模式下的切换确认
    /// </summary>
    private async Task<bool> HandleEditModeSwitch(Guid medicalCaseId, Func<Task> saveDraftOnlyAsync)
    {
        var message = "当前医案有未保存的更改，是否暂存后切换？\n\n" +
            "【是】暂存并切换 - 保存当前医案后切换\n" +
            "【否】取消医案 - 取消当前医案后切换\n" +
            "【取消】继续看诊 - 留在当前界面";

        if (_commonDialogService != null)
        {
            var dialogResult = await _commonDialogService.ShowTripleChoiceAsync(message, "切换患者确认");
            switch (dialogResult)
            {
                case TripleChoiceResult.Yes:
                    _setIsBusy(true, "正在暂存当前医案...");
                    await saveDraftOnlyAsync();
                    _logger.LogInformation("用户选择暂存当前医案后切换");
                    break;
                case TripleChoiceResult.No:
                    _setIsBusy(true, "正在取消当前医案...");
                    var cancelResult = await _lifecycleHandler.CancelAsync(medicalCaseId);
                    if (!cancelResult.success)
                    {
                        _logger.LogWarning("取消当前医案失败：{Error}", cancelResult.errorMessage);
                        await _showErrorAsync("取消医案失败，请重试");
                        _setIsBusy(false, null);
                        return false;
                    }
                    _logger.LogInformation("用户选择取消当前医案后切换");
                    break;
                case TripleChoiceResult.Cancel:
                default:
                    _logger.LogInformation("用户取消切换，留在当前界面");
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 处理挂起医案（暂存状态的医案）
    /// OpenSpec: redesign-pending-queue - 简化为双选项弹窗（继续看诊/新建医案）
    /// </summary>
    private async Task HandleSuspendedCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        if (_commonDialogService == null)
        {
            _logger.LogWarning("CommonDialogService为空，无法显示弹窗");
            return;
        }

        _logger.LogInformation("显示双选项弹窗，患者：{PatientName}，挂起医案：{MedicalCaseId}",
            pendingCase.PatientName, pendingCase.MedicalCaseId);

        // OpenSpec: redesign-pending-queue - 简化为双选项确认
        // 使用ShowConfirmAsync: 确认=继续看诊，取消=新建医案
        var message = $"患者 {pendingCase.PatientName ?? "未知"} 有未完成的医案。\n\n" +
            "点击「确定」继续看诊原医案\n" +
            "点击「取消」关闭原医案并新建";

        var choice = await _commonDialogService.ShowConfirmAsync(message, "选择操作");

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
            _setIsBusy(true, "正在关闭旧医案...");
            if (pendingCase.MedicalCaseId.HasValue)
            {
                var cancelResult = await _lifecycleHandler.CancelAsync(pendingCase.MedicalCaseId.Value);
                if (!cancelResult.success)
                {
                    _logger.LogWarning("取消挂起医案失败：{Error}", cancelResult.errorMessage);
                    await _showErrorAsync("关闭旧医案失败：" + cancelResult.errorMessage);
                    _setIsBusy(false, null);
                    return;
                }
            }
            await NavigateToNewMedicalCaseAsync(pendingCase);
        }
    }

    /// <summary>
    /// 导航到新医案
    /// </summary>
    private async Task NavigateToNewMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            _setIsBusy(true, "正在创建新医案...");
            _logger.LogInformation("为患者创建新医案：{PatientName}", pendingCase.PatientName);

            var createResult = await _lifecycleHandler.CreateMedicalCaseAsync(pendingCase.PatientId);
            if (!createResult.success)
            {
                _logger.LogWarning("创建医案失败：{Error}", createResult.errorMessage);
                await _showErrorAsync("创建医案失败：" + createResult.errorMessage);
                return;
            }

            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", createResult.medicalCaseId },
                { "CurrentPatient", await _getPatientDetailAsync(pendingCase.PatientId) },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _regionManager.RequestNavigate(RegionNames.ContentRegion, "MedicalCaseWorkspaceView", parameters);
            _logger.LogInformation("已导航到新医案：{MedicalCaseId}", createResult.medicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建新医案并导航失败");
            await _showErrorAsync("创建医案失败，请重试");
        }
        finally
        {
            _setIsBusy(false, null);
        }
    }

    /// <summary>
    /// 导航到已存在医案
    /// </summary>
    private async Task NavigateToExistingMedicalCaseAsync(PendingMedicalCaseDto pendingCase)
    {
        try
        {
            _setIsBusy(true, "正在加载医案...");

            if (!pendingCase.MedicalCaseId.HasValue)
            {
                _logger.LogWarning("挂起医案ID为空，无法导航");
                await _showErrorAsync("医案数据异常，请刷新后重试");
                return;
            }

            var parameters = new NavigationParameters
            {
                { "MedicalCaseId", pendingCase.MedicalCaseId.Value },
                { "CurrentPatient", await _getPatientDetailAsync(pendingCase.PatientId) },
                { MedicalCaseNavigationParameters.WorkspaceModeKey, WorkspaceMode.Clinical },
                { MedicalCaseNavigationParameters.InitialEditStateKey, EditState.Editing }
            };

            _regionManager.RequestNavigate(RegionNames.ContentRegion, "MedicalCaseWorkspaceView", parameters);
            _logger.LogInformation("已导航到挂起医案：{MedicalCaseId}", pendingCase.MedicalCaseId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到已存在医案失败");
            await _showErrorAsync("加载医案失败，请重试");
        }
        finally
        {
            _setIsBusy(false, null);
        }
    }

    #endregion
}

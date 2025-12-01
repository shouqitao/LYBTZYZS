using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 医案工作区协调器
/// 负责协调面板保存、生命周期操作和审计检查
/// OpenSpec: refactor-viewmodel-layer - VM-002 Components Pattern
/// </summary>
public class MedicalCaseWorkspaceCoordinator
{
    #region 字段

    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly MedicalCaseDataLoader _dataLoader;
    private readonly IAuditRequirementChecker? _auditRequirementChecker;
    private readonly ILogger<MedicalCaseWorkspaceCoordinator> _logger;

    #endregion

    #region 构造函数

    public MedicalCaseWorkspaceCoordinator(
        MedicalCaseLifecycleHandler lifecycleHandler,
        MedicalCaseDataLoader dataLoader,
        ILoggerFactory loggerFactory,
        IAuditRequirementChecker? auditRequirementChecker = null)
    {
        _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _logger = loggerFactory.CreateLogger<MedicalCaseWorkspaceCoordinator>();
        _auditRequirementChecker = auditRequirementChecker;
    }

    #endregion

    #region 面板保存操作

    /// <summary>
    /// 保存所有面板数据（静默模式）
    /// </summary>
    /// <param name="consultationPanel">诊断面板ViewModel</param>
    /// <param name="prescriptionPanel">处方面板ViewModel</param>
    /// <param name="syncRemarkAction">同步备注的回调（由ViewModel提供）</param>
    /// <returns>保存是否成功</returns>
    public async Task<bool> SavePanelsSilentlyAsync(
        ISaveable? consultationPanel,
        ISaveable? prescriptionPanel,
        Action? syncRemarkAction = null)
    {
        try
        {
            // 同步备注到诊断面板（通过回调）
            syncRemarkAction?.Invoke();

            // 保存诊断数据
            if (consultationPanel != null)
            {
                var consultationResult = await consultationPanel.SaveSilentlyAsync();
                _logger.LogDebug("诊断面板静默保存结果: {Result}", consultationResult);
            }

            // 保存处方数据
            if (prescriptionPanel != null)
            {
                var prescriptionResult = await prescriptionPanel.SaveSilentlyAsync();
                _logger.LogDebug("处方面板静默保存结果: {Result}", prescriptionResult);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "面板静默保存失败");
            return false;
        }
    }

    /// <summary>
    /// 保存所有面板数据（完整模式，显示验证错误）
    /// </summary>
    /// <param name="consultationPanel">诊断面板ViewModel</param>
    /// <param name="prescriptionPanel">处方面板ViewModel</param>
    /// <param name="syncRemarkAction">同步备注的回调</param>
    /// <param name="isPrescriptionEnabled">处方是否启用</param>
    /// <returns>保存结果</returns>
    public async Task<PanelSaveResult> SavePanelsAsync(
        ISaveable? consultationPanel,
        ISaveable? prescriptionPanel,
        Action? syncRemarkAction,
        bool isPrescriptionEnabled)
    {
        // 同步备注到诊断面板（通过回调）
        syncRemarkAction?.Invoke();

        // 保存诊断数据
        if (consultationPanel != null)
        {
            var consultationResult = await consultationPanel.SaveAsync();
            if (!consultationResult)
            {
                return PanelSaveResult.Failed("保存诊断数据失败");
            }
        }

        // 保存处方数据（如果启用）
        if (isPrescriptionEnabled && prescriptionPanel != null)
        {
            var prescriptionResult = await prescriptionPanel.SaveAsync();
            if (!prescriptionResult)
            {
                return PanelSaveResult.Failed("保存处方数据失败");
            }
        }

        return PanelSaveResult.Success();
    }

    #endregion

    #region 生命周期操作

    /// <summary>
    /// 暂存医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="consultationPanel">诊断面板</param>
    /// <param name="prescriptionPanel">处方面板</param>
    /// <param name="syncRemarkAction">同步备注的回调</param>
    /// <returns>操作结果</returns>
    public async Task<LifecycleResult> SaveDraftAsync(
        Guid medicalCaseId,
        ISaveable? consultationPanel,
        ISaveable? prescriptionPanel,
        Action? syncRemarkAction = null)
    {
        // 先保存面板数据
        await SavePanelsSilentlyAsync(consultationPanel, prescriptionPanel, syncRemarkAction);

        // 更新状态为Draft
        var result = await _lifecycleHandler.SaveDraftAsync(medicalCaseId);
        
        if (result.success)
        {
            _logger.LogInformation("医案已暂存，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    /// <summary>
    /// 取消医案（软删除）
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="consultationPanel">诊断面板</param>
    /// <param name="prescriptionPanel">处方面板</param>
    /// <param name="syncRemarkAction">同步备注的回调</param>
    /// <returns>操作结果</returns>
    public async Task<LifecycleResult> CancelAsync(
        Guid medicalCaseId,
        ISaveable? consultationPanel,
        ISaveable? prescriptionPanel,
        Action? syncRemarkAction = null)
    {
        // 取消前自动保存（供审计）
        try
        {
            await SavePanelsSilentlyAsync(consultationPanel, prescriptionPanel, syncRemarkAction);
            _logger.LogDebug("取消前数据已保存（供审计）");
        }
        catch (Exception saveEx)
        {
            _logger.LogWarning(saveEx, "取消前保存失败，继续执行取消操作");
        }

        // 执行软删除
        var result = await _lifecycleHandler.CancelAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已取消（软删除），MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    /// <summary>
    /// 完成医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="consultationPanel">诊断面板</param>
    /// <param name="prescriptionPanel">处方面板</param>
    /// <param name="syncRemarkAction">同步备注的回调</param>
    /// <param name="isPrescriptionEnabled">处方是否启用</param>
    /// <returns>操作结果</returns>
    public async Task<LifecycleResult> CompleteAsync(
        Guid medicalCaseId,
        ISaveable? consultationPanel,
        ISaveable? prescriptionPanel,
        Action? syncRemarkAction,
        bool isPrescriptionEnabled)
    {
        // 保存面板数据（完整模式）
        var saveResult = await SavePanelsAsync(consultationPanel, prescriptionPanel, syncRemarkAction, isPrescriptionEnabled);
        if (!saveResult.IsSuccess)
        {
            return new LifecycleResult(false, saveResult.ErrorMessage);
        }

        // 完成医案
        var result = await _lifecycleHandler.CompleteAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已完成，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    #endregion

    #region 审计检查

    /// <summary>
    /// 检查是否需要审计
    /// </summary>
    /// <param name="currentUserId">当前用户ID</param>
    /// <returns>
    /// null: 用户取消
    /// 空字符串: 无需审计
    /// 非空字符串: 需要审计，返回审计原因（由调用方获取）
    /// </returns>
    public bool CheckAuditRequired(Guid currentUserId)
    {
        if (_auditRequirementChecker == null)
        {
            return false;
        }

        var medicalCase = _dataLoader.CachedMedicalCase;
        if (medicalCase == null)
        {
            _logger.LogWarning("CheckAuditRequired: 无法获取当前医案数据");
            return false;
        }

        return _auditRequirementChecker.IsAuditRequired(medicalCase, currentUserId);
    }

    #endregion
}

#region 结果类型

/// <summary>
/// 面板保存结果
/// </summary>
public class PanelSaveResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    private PanelSaveResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static PanelSaveResult Success() => new(true, null);
    public static PanelSaveResult Failed(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// 生命周期操作结果
/// </summary>
public class LifecycleResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }

    public LifecycleResult(bool isSuccess, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }
}

#endregion

using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 医案工作区协调器
/// 负责协调面板保存、生命周期操作和审计检查
/// OpenSpec: refactor-viewmodel-layer - VM-002 Components Pattern
/// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4) - 支持聚合保存
/// </summary>
public class MedicalCaseWorkspaceCoordinator
{
    #region 字段

    private readonly MedicalCaseLifecycleHandler _lifecycleHandler;
    private readonly MedicalCaseDataLoader _dataLoader;
    private readonly IMedicalCaseRepository _repository;
    private readonly IAuditRequirementChecker? _auditRequirementChecker;
    private readonly ILogger<MedicalCaseWorkspaceCoordinator> _logger;

    #endregion

    #region 构造函数

    public MedicalCaseWorkspaceCoordinator(
        MedicalCaseLifecycleHandler lifecycleHandler,
        MedicalCaseDataLoader dataLoader,
        IMedicalCaseRepository repository,
        ILoggerFactory loggerFactory,
        IAuditRequirementChecker? auditRequirementChecker = null)
    {
        _lifecycleHandler = lifecycleHandler ?? throw new ArgumentNullException(nameof(lifecycleHandler));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = loggerFactory.CreateLogger<MedicalCaseWorkspaceCoordinator>();
        _auditRequirementChecker = auditRequirementChecker;
    }

    #endregion

    #region 聚合保存操作

    /// <summary>
    /// 使用IDataProvider收集数据并聚合保存（单次API调用）
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4)
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="consultationProvider">诊断数据提供者</param>
    /// <param name="prescriptionProvider">处方数据提供者</param>
    /// <param name="remark">医案备注</param>
    /// <param name="editReason">编辑原因（审计用）</param>
    /// <returns>保存结果</returns>
    public async Task<AggregateSaveResult> SaveAggregateAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        string? remark = null,
        string? editReason = null)
    {
        try
        {
            _logger.LogInformation("开始聚合保存，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            // 从IDataProvider收集数据
            var consultationData = consultationProvider?.GetConsultationData();
            var prescriptionData = prescriptionProvider?.GetPrescriptionData();

            // 构建聚合DTO
            var aggregateDto = new MedicalCaseAggregateInputDto
            {
                Id = medicalCaseId,
                Remark = remark,
                EditReason = editReason,
                Consultation = consultationData,
                Prescription = prescriptionData
            };

            // 调用聚合保存API（单次调用）
            var result = await _repository.SaveAggregateAsync(medicalCaseId, aggregateDto);

            _logger.LogInformation("聚合保存成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            return AggregateSaveResult.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚合保存失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            return AggregateSaveResult.Failed($"保存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 暂存医案（使用聚合保存）
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4)
    /// </summary>
    public async Task<LifecycleResult> SaveDraftWithAggregateAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        string? remark = null)
    {
        // 先聚合保存数据
        var saveResult = await SaveAggregateAsync(medicalCaseId, consultationProvider, prescriptionProvider, remark);
        if (!saveResult.IsSuccess)
        {
            return new LifecycleResult(false, saveResult.ErrorMessage);
        }

        // 更新状态为Draft
        var result = await _lifecycleHandler.SaveDraftAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已暂存（聚合模式），MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    /// <summary>
    /// 完成医案（使用聚合保存）
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4)
    /// </summary>
    public async Task<LifecycleResult> CompleteWithAggregateAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        IValidatable? consultationValidator,
        IValidatable? prescriptionValidator,
        string? remark = null,
        bool isPrescriptionEnabled = true)
    {
        // 验证诊断数据
        if (consultationValidator != null && !consultationValidator.Validate())
        {
            return new LifecycleResult(false, consultationValidator.ValidationMessage);
        }

        // 验证处方数据（如果启用）
        if (isPrescriptionEnabled && prescriptionValidator != null && !prescriptionValidator.Validate())
        {
            return new LifecycleResult(false, prescriptionValidator.ValidationMessage);
        }

        // 聚合保存数据
        var saveResult = await SaveAggregateAsync(medicalCaseId, consultationProvider, prescriptionProvider, remark);
        if (!saveResult.IsSuccess)
        {
            return new LifecycleResult(false, saveResult.ErrorMessage);
        }

        // 完成医案
        var result = await _lifecycleHandler.CompleteAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已完成（聚合模式），MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    /// <summary>
    /// 取消医案（使用聚合保存后取消）
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 4.2)
    /// </summary>
    public async Task<LifecycleResult> CancelWithAggregateAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        string? remark = null)
    {
        // 取消前先聚合保存数据（供审计）
        try
        {
            await SaveAggregateAsync(medicalCaseId, consultationProvider, prescriptionProvider, remark);
            _logger.LogDebug("取消前数据已保存（聚合模式，供审计）");
        }
        catch (Exception saveEx)
        {
            _logger.LogWarning(saveEx, "取消前聚合保存失败，继续执行取消操作");
        }

        // 执行软删除
        var result = await _lifecycleHandler.CancelAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已取消（聚合模式），MedicalCaseId: {MedicalCaseId}", medicalCaseId);
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

/// <summary>
/// 聚合保存结果
/// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4)
/// </summary>
public class AggregateSaveResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDetailDto? Data { get; }

    private AggregateSaveResult(bool isSuccess, string? errorMessage, LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDetailDto? data)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Data = data;
    }

    public static AggregateSaveResult Success(LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseDetailDto data)
        => new(true, null, data);

    public static AggregateSaveResult Failed(string errorMessage)
        => new(false, errorMessage, null);
}

#endregion

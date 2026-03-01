using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 医案工作区协调器
/// 负责协调面板保存、生命周期操作、审计检查和数据加载
/// OpenSpec: refactor-viewmodel-layer - VM-002 Components Pattern
/// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4) - 支持聚合保存
/// OpenSpec: simplify-workspace-architecture - 合并DataLoader功能
/// </summary>
public class MedicalCaseWorkspaceCoordinator
{
    #region 字段

    private readonly IMedicalCaseService _medicalCaseService;
    private readonly MedicalCaseService _dataManager;
    private readonly IMedicalCaseRepository _repository;
    private readonly ILogger<MedicalCaseWorkspaceCoordinator> _logger;

    #endregion

    #region 缓存属性 (合并自DataLoader)

    /// <summary>
    /// 缓存的医案详情
    /// OpenSpec: simplify-workspace-architecture - 从DataLoader合并
    /// </summary>
    public MedicalCaseDetailDto? CachedMedicalCase { get; private set; }

    /// <summary>
    /// 缓存的诊疗记录
    /// </summary>
    public ConsultationDetailDto? CachedConsultation { get; private set; }

    /// <summary>
    /// 缓存的处方信息
    /// </summary>
    public PrescriptionDetailDto? CachedPrescription { get; private set; }

    /// <summary>
    /// 数据加载完成事件
    /// </summary>
    public event EventHandler<DataLoadedEventArgs>? DataLoaded;

    #endregion

    #region 构造函数

    public MedicalCaseWorkspaceCoordinator(
        IMedicalCaseService medicalCaseService,
        MedicalCaseService dataManager,
        IMedicalCaseRepository repository,
        ILoggerFactory loggerFactory)
    {
        _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = loggerFactory.CreateLogger<MedicalCaseWorkspaceCoordinator>();
    }

    #endregion

    #region 数据加载 (合并自DataLoader)

    /// <summary>
    /// 加载医案详情及关联数据
    /// OpenSpec: simplify-workspace-architecture - 从DataLoader合并
    /// </summary>
    public async Task<(bool success, MedicalCaseDetailDto? detail, string? errorMessage)> LoadMedicalCaseDetailsAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("开始加载医案详情，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            var medicalCaseDetail = await _dataManager.GetByIdSimpleAsync(medicalCaseId);

            if (medicalCaseDetail == null)
            {
                _logger.LogWarning("未找到医案数据，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return (false, null, "未找到医案数据");
            }

            // 缓存数据
            CachedMedicalCase = medicalCaseDetail;
            CachedConsultation = medicalCaseDetail.Consultation;
            CachedPrescription = medicalCaseDetail.Prescription;

            _logger.LogInformation("医案数据加载完成");

            // 触发事件
            DataLoaded?.Invoke(this, new DataLoadedEventArgs
            {
                Success = true,
                MedicalCaseId = medicalCaseId,
                HasConsultation = CachedConsultation != null,
                HasPrescription = CachedPrescription != null
            });

            return (true, medicalCaseDetail, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载医案数据失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            var errorMsg = ClientErrorMessageMapper.GetSafeOperationFailureMessage("加载医案数据", ex);

            DataLoaded?.Invoke(this, new DataLoadedEventArgs
            {
                Success = false,
                MedicalCaseId = medicalCaseId,
                ErrorMessage = errorMsg
            });

            return (false, null, errorMsg);
        }
    }

    // OpenSpec: cleanup-medicalcase-dead-code - FormatPatientInfo已删除（0调用，患者信息由WorkspaceState.UpdateFromPatient处理）

    /// <summary>
    /// 清除所有缓存数据
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("清除数据缓存");
        CachedMedicalCase = null;
        CachedConsultation = null;
        CachedPrescription = null;
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
    public async Task<AggregateSaveResult> SaveAsync(
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
            var aggregateDto = new MedicalCaseInputDto
            {
                Id = medicalCaseId,
                Remark = remark,
                EditReason = editReason,
                Consultation = consultationData,
                Prescription = prescriptionData
            };

            // 调用聚合保存API（单次调用）
            var result = await _repository.SaveAsync(medicalCaseId, aggregateDto);

            _logger.LogInformation("聚合保存成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            return AggregateSaveResult.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "聚合保存失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            return AggregateSaveResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    /// <summary>
    /// 挂起医案（使用聚合保存）
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4)
    /// </summary>
    public async Task<LifecycleResult> SuspendAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        string? remark = null)
    {
        // 先聚合保存数据
        var saveResult = await SaveAsync(medicalCaseId, consultationProvider, prescriptionProvider, remark);
        if (!saveResult.IsSuccess)
        {
            return new LifecycleResult(false, saveResult.ErrorMessage);
        }

        // 更新状态为Suspended
        var result = await _medicalCaseService.SuspendAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已挂起（聚合模式），MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    /// <summary>
    /// 完成医案（使用聚合保存）
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.4)
    /// </summary>
    public async Task<LifecycleResult> CompleteAsync(
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
        var saveResult = await SaveAsync(medicalCaseId, consultationProvider, prescriptionProvider, remark);
        if (!saveResult.IsSuccess)
        {
            return new LifecycleResult(false, saveResult.ErrorMessage);
        }

        // 完成医案
        var result = await _medicalCaseService.CompleteMedicalCaseAsync(medicalCaseId);

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
    public async Task<LifecycleResult> CancelAsync(
        Guid medicalCaseId,
        IDataProvider? consultationProvider,
        IDataProvider? prescriptionProvider,
        string? remark = null)
    {
        // 取消前先聚合保存数据（供审计）
        try
        {
            await SaveAsync(medicalCaseId, consultationProvider, prescriptionProvider, remark);
            _logger.LogDebug("取消前数据已保存（聚合模式，供审计）");
        }
        catch (Exception saveEx)
        {
            _logger.LogWarning(saveEx, "取消前聚合保存失败，继续执行取消操作");
        }

        // 执行软删除
        var result = await _medicalCaseService.CancelMedicalCaseAsync(medicalCaseId);

        if (result.success)
        {
            _logger.LogInformation("医案已取消（聚合模式），MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }

        return new LifecycleResult(result.success, result.errorMessage);
    }

    #endregion

    // OpenSpec: cleanup-medicalcase-dead-code - 审计检查region已删除（CheckAuditRequired 0调用，审计功能后续单独规划）
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

/// <summary>
/// 数据加载完成事件参数
/// OpenSpec: simplify-workspace-architecture - 从DataLoader合并
/// </summary>
public class DataLoadedEventArgs : EventArgs
{
    public bool Success { get; set; }
    public Guid MedicalCaseId { get; set; }
    public bool HasConsultation { get; set; }
    public bool HasPrescription { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

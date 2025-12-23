using LYBT.Desktop.Infrastructure.Events;
using LYBT.Shared.ExceptionHandling.Mappers;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 处方保存处理器
/// 负责处方的创建和更新逻辑
/// OpenSpec: cleanup-ui-layer - Phase 1.1 PrescriptionPanelViewModel拆分
/// </summary>
public class PrescriptionSaveHandler
{
    #region 字段

    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<PrescriptionSaveHandler> _logger;

    #endregion

    #region 构造函数

    public PrescriptionSaveHandler(
        IMedicalCaseRepository medicalCaseRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory)
    {
        _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = loggerFactory.CreateLogger<PrescriptionSaveHandler>();
    }

    #endregion

    #region 保存方法

    /// <summary>
    /// 保存处方（带事件发布）
    /// OpenSpec: simplify-medicalcase-api - 通过聚合保存处理
    /// </summary>
    /// <param name="context">保存上下文</param>
    /// <returns>保存结果</returns>
    public async Task<PrescriptionSaveResult> SaveAsync(PrescriptionSaveContext context)
    {
        try
        {
            if (!context.Items.Any())
            {
                _logger.LogWarning("没有药材项，跳过保存");
                return PrescriptionSaveResult.Empty();
            }

            // OpenSpec: simplify-medicalcase-api - 通过聚合保存处理处方创建/更新
            var prescriptionInput = new PrescriptionInputDto
            {
                Id = context.PrescriptionId,
                DosageCount = context.DosageCount,
                Usage = context.Usage,
                Items = context.Items
            };

            var medicalCaseInput = new MedicalCaseInputDto
            {
                Id = context.MedicalCaseId,
                NeedsPrescription = true,
                Prescription = prescriptionInput
            };

            var result = await _medicalCaseRepository.SaveAsync(context.MedicalCaseId, medicalCaseInput);

            if (result?.Prescription != null)
            {
                _logger.LogInformation("处方数据保存成功");

                // 发布处方完成事件 - OpenSpec: unify-event-system
                _eventAggregator.GetEvent<CaseEvents.PrescriptionCompletedEvent>()
                    .Publish(new CasePrescriptionCompletedPayload
                    {
                        PrescriptionId = result.Prescription.Id,
                        TotalItems = context.Items.Count,
                        TotalAmount = context.TotalPrice
                    });

                return PrescriptionSaveResult.Success(result.Prescription.Id);
            }

            _logger.LogWarning("处方数据保存失败");
            return PrescriptionSaveResult.Failed("API返回null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存处方数据异常");
            return PrescriptionSaveResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    /// <summary>
    /// 静默保存处方（不发布事件，不显示错误）
    /// OpenSpec: simplify-medicalcase-api - 通过聚合保存处理
    /// </summary>
    /// <param name="context">保存上下文</param>
    /// <returns>保存结果</returns>
    public async Task<PrescriptionSaveResult> SaveSilentlyAsync(PrescriptionSaveContext context)
    {
        try
        {
            _logger.LogInformation("[处方诊断] SaveSilentlyAsync被调用，MedicalCaseId: {MedicalCaseId}, Items.Count: {Count}",
                context.MedicalCaseId, context.Items.Count);

            if (!context.Items.Any())
            {
                _logger.LogWarning("[处方诊断] 没有有效药材项，静默保存跳过");
                return PrescriptionSaveResult.Empty();
            }

            // OpenSpec: simplify-medicalcase-api - 通过聚合保存处理处方创建/更新
            var prescriptionInput = new PrescriptionInputDto
            {
                Id = context.PrescriptionId,
                DosageCount = context.DosageCount,
                Usage = context.Usage,
                Items = context.Items
            };

            var medicalCaseInput = new MedicalCaseInputDto
            {
                Id = context.MedicalCaseId,
                NeedsPrescription = true,
                Prescription = prescriptionInput
            };

            _logger.LogInformation("[处方诊断] 准备调用SaveAsync，MedicalCaseId: {MedicalCaseId}, Items: {Count}",
                context.MedicalCaseId, context.Items.Count);

            var result = await _medicalCaseRepository.SaveAsync(context.MedicalCaseId, medicalCaseInput);
            _logger.LogInformation("[处方诊断] SaveAsync返回: {Result}", result?.Prescription != null ? $"成功,Id={result.Prescription.Id}" : "null");

            if (result?.Prescription != null)
            {
                _logger.LogInformation("[处方诊断] 处方数据静默保存成功，PrescriptionId: {PrescriptionId}", result.Prescription.Id);
                // 静默保存不发布PrescriptionCompletedEvent
                return PrescriptionSaveResult.Success(result.Prescription.Id);
            }

            _logger.LogWarning("[处方诊断] 处方数据静默保存失败：API返回null");
            return PrescriptionSaveResult.Failed("API返回null");
        }
        catch (Exception ex)
        {
            // 静默保存不显示错误，只记录日志
            _logger.LogWarning(ex, "静默保存处方数据异常（不阻止后续操作）");
            return PrescriptionSaveResult.Failed(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
    }

    #endregion
}

#region 数据传输对象

/// <summary>
/// 处方保存上下文
/// </summary>
public class PrescriptionSaveContext
{
    public Guid MedicalCaseId { get; init; }
    public Guid? PrescriptionId { get; init; }
    public Guid PatientId { get; init; }
    public Guid UserId { get; init; }
    public int DosageCount { get; init; }
    public string Usage { get; init; } = string.Empty;
    public List<PrescriptionItemInputDto> Items { get; init; } = new();
    public decimal TotalPrice { get; init; }
}

/// <summary>
/// 处方保存结果
/// </summary>
public class PrescriptionSaveResult
{
    public bool IsSuccess { get; private init; }
    public bool IsEmpty { get; private init; }
    public Guid? PrescriptionId { get; private init; }
    public string? ErrorMessage { get; private init; }

    private PrescriptionSaveResult() { }

    public static PrescriptionSaveResult Success(Guid prescriptionId) => new()
    {
        IsSuccess = true,
        IsEmpty = false,
        PrescriptionId = prescriptionId
    };

    public static PrescriptionSaveResult Empty() => new()
    {
        IsSuccess = true,
        IsEmpty = true
    };

    public static PrescriptionSaveResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        IsEmpty = false,
        ErrorMessage = errorMessage
    };
}

#endregion

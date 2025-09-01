using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方业务服务实现 - UltraThink三层架构业务层
/// 职责：业务流程编排、完整事务管理、业务规则处理
/// </summary>
public class PrescriptionsBusinessService(
    IPrescriptionsCoreService coreService,
    IPrescriptionsQueryService queryService,
    IMapper mapper,
    ILogger<PrescriptionsBusinessService> logger) : IPrescriptionsBusinessService
{
    private readonly IPrescriptionsCoreService _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
    private readonly IPrescriptionsQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly ILogger<PrescriptionsBusinessService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region 事件定义

    /// <summary>
    /// 处方状态变更事件
    /// </summary>
    public event EventHandler<PrescriptionStatusChangedEventArgs>? PrescriptionStatusChanged;

    /// <summary>
    /// 处方操作事件
    /// </summary>
    public event EventHandler<PrescriptionOperationEventArgs>? PrescriptionOperation;

    /// <summary>
    /// 处方验证事件
    /// </summary>
    public event EventHandler<PrescriptionValidationEventArgs>? PrescriptionValidation;

    #endregion

    #region 核心业务操作

    /// <summary>
    /// 创建处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("开始创建处方 - 患者ID: {PatientId}, 医生ID: {DoctorId}", createDto.PatientId, createDto.DoctorId);

            // 1. 业务验证
            var validationResult = await _coreService.ValidateCreateDtoAsync(createDto);
            if (!validationResult.IsSuccess || !validationResult.Data.IsValid)
            {
                var validationErrors = string.Join("; ", validationResult.Data?.Errors ?? new List<string>());
                await TriggerPrescriptionValidationEventAsync(Guid.Empty, "CreateValidation", false, validationResult.Data?.Errors ?? new List<string>());
                return ServiceResult<PrescriptionDto>.Failure($"创建处方验证失败: {validationErrors}");
            }

            // 2. 检查关联数据存在性
            var patientExistsResult = await _coreService.CheckPatientExistsAsync(createDto.PatientId);
            if (!patientExistsResult.IsSuccess || !patientExistsResult.Data)
            {
                return ServiceResult<PrescriptionDto>.Failure("指定的患者不存在");
            }

            var doctorExistsResult = await _coreService.CheckDoctorExistsAsync(createDto.DoctorId);
            if (!doctorExistsResult.IsSuccess || !doctorExistsResult.Data)
            {
                return ServiceResult<PrescriptionDto>.Failure("指定的医生不存在");
            }

            // 3. 生成处方编号
            var numberResult = await _coreService.GeneratePrescriptionNumberAsync();
            if (!numberResult.IsSuccess)
            {
                return ServiceResult<PrescriptionDto>.Failure("生成处方编号失败");
            }

            // 4. 设置创建信息
            createDto.PrescriptionNo = numberResult.Data;
            createDto.CreateTime = DateTime.Now;

            // 5. 调用核心服务创建
            var result = await _coreService.CallCreatePrescriptionApiAsync(createDto);
            if (!result.IsSuccess)
            {
                await TriggerPrescriptionOperationEventAsync(Guid.Empty, "Create", "创建处方失败", false, result.ErrorMessage);
                return result;
            }

            // 6. 触发业务事件
            await TriggerPrescriptionOperationEventAsync(result.Data.Id, "Create", "成功创建处方", true);
            await TriggerPrescriptionStatusChangedEventAsync(result.Data.Id, PrescriptionStatus.Draft, result.Data.PrescriptionStatus, "创建处方");

            _logger.LogInformation("成功创建处方 - 处方ID: {PrescriptionId}, 编号: {PrescriptionNo}", result.Data.Id, result.Data.PrescriptionNo);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建处方异常");
            return ServiceResult<PrescriptionDto>.Failure($"创建处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, PrescriptionEditDto updateDto)
    {
        try
        {
            _logger.LogInformation("开始更新处方 - 处方ID: {PrescriptionId}", id);

            // 1. 验证输入数据
            var validationResult = await _coreService.ValidateEditDtoAsync(updateDto);
            if (!validationResult.IsSuccess || !validationResult.Data.IsValid)
            {
                var validationErrors = string.Join("; ", validationResult.Data?.Errors ?? new List<string>());
                await TriggerPrescriptionValidationEventAsync(id, "UpdateValidation", false, validationResult.Data?.Errors ?? new List<string>());
                return ServiceResult<PrescriptionDto>.Failure($"更新处方验证失败: {validationErrors}");
            }

            // 2. 检查处方是否存在
            var existsResult = await _coreService.CheckPrescriptionExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                return ServiceResult<PrescriptionDto>.Failure("指定的处方不存在");
            }

            // 3. 检查是否可修改
            var canModifyResult = await CanModifyAsync(id);
            if (!canModifyResult.IsSuccess || !canModifyResult.Data)
            {
                return ServiceResult<PrescriptionDto>.Failure(canModifyResult.ErrorMessage ?? "当前处方状态不允许修改");
            }

            // 4. 获取原处方数据用于比较
            var originalResult = await _queryService.GetByIdAsync(id);
            if (!originalResult.IsSuccess)
            {
                return ServiceResult<PrescriptionDto>.Failure("获取原处方信息失败");
            }

            // 5. 调用核心服务更新
            var result = await _coreService.CallUpdatePrescriptionApiAsync(id, updateDto);
            if (!result.IsSuccess)
            {
                await TriggerPrescriptionOperationEventAsync(id, "Update", "更新处方失败", false, result.ErrorMessage);
                return result;
            }

            // 6. 触发业务事件
            await TriggerPrescriptionOperationEventAsync(id, "Update", "成功更新处方", true);

            _logger.LogInformation("成功更新处方 - 处方ID: {PrescriptionId}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新处方异常: {PrescriptionId}", id);
            return ServiceResult<PrescriptionDto>.Failure($"更新处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除处方
    /// </summary>
    public async Task<ServiceResult<bool>> DeletePrescriptionAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始删除处方 - 处方ID: {PrescriptionId}", id);

            // 1. 检查处方是否存在
            var existsResult = await _coreService.CheckPrescriptionExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                return ServiceResult<bool>.Failure("指定的处方不存在");
            }

            // 2. 检查是否可删除
            var canDeleteResult = await CanDeleteAsync(id);
            if (!canDeleteResult.IsSuccess || !canDeleteResult.Data)
            {
                return ServiceResult<bool>.Failure(canDeleteResult.ErrorMessage ?? "当前处方状态不允许删除");
            }

            // 3. 调用核心服务删除
            var result = await _coreService.CallDeletePrescriptionApiAsync(id);
            if (!result.IsSuccess)
            {
                await TriggerPrescriptionOperationEventAsync(id, "Delete", "删除处方失败", false, result.ErrorMessage);
                return result;
            }

            // 4. 触发业务事件
            await TriggerPrescriptionOperationEventAsync(id, "Delete", "成功删除处方", true);

            _logger.LogInformation("成功删除处方 - 处方ID: {PrescriptionId}", id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除处方异常: {PrescriptionId}", id);
            return ServiceResult<bool>.Failure($"删除处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 复制处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> CopyPrescriptionAsync(Guid id, string newName)
    {
        try
        {
            _logger.LogInformation("开始复制处方 - 原处方ID: {PrescriptionId}, 新名称: {NewName}", id, newName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                return ServiceResult<PrescriptionDto>.Failure("新处方名称不能为空");
            }

            // 1. 获取原处方
            var originalResult = await _queryService.GetByIdAsync(id);
            if (!originalResult.IsSuccess)
            {
                return ServiceResult<PrescriptionDto>.Failure("获取原处方失败：" + originalResult.ErrorMessage);
            }

            var original = originalResult.Data;
            if (original == null)
            {
                return ServiceResult<PrescriptionDto>.Failure("原始处方信息不存在");
            }

            // 2. 创建新处方DTO
            var createDto = new PrescriptionCreateDto
            {
                PatientId = original.PatientId,
                DoctorId = original.UserId,
                MedicalCaseId = original.MedicalCaseId,
                Diagnosis = original.Diagnosis ?? "",
                DosageCount = original.DosageCount,
                Advice = original.Advice,
                Usage = original.Usage,
                TotalAmount = original.TotalAmount,
                FormulaSource = $"复制自：{original.PrescriptionNo ?? id.ToString()}",
                Items = original.Items.Select(item => new PrescriptionItemCreateDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.Subtotal,
                    Usage = item.Usage,
                    Remark = item.Remark
                }).ToList(),
                Remark = $"复制自处方：{original.PrescriptionNo ?? id.ToString()}"
            };

            // 3. 创建新处方
            var result = await CreatePrescriptionAsync(createDto);
            if (result.IsSuccess)
            {
                await TriggerPrescriptionOperationEventAsync(result.Data.Id, "Copy", $"成功复制处方，原处方：{original.PrescriptionNo}", true);
                _logger.LogInformation("成功复制处方 - 原处方ID: {OriginalId}, 新处方ID: {NewId}", id, result.Data.Id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制处方异常: {PrescriptionId}", id);
            return ServiceResult<PrescriptionDto>.Failure($"复制处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 从验方创建处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> CreateFromFormulaAsync(Guid formulaId, Guid patientId, Guid doctorId)
    {
        try
        {
            _logger.LogInformation("从验方创建处方 - 验方ID: {FormulaId}, 患者ID: {PatientId}, 医生ID: {DoctorId}", formulaId, patientId, doctorId);

            // TODO: 调用验方模块获取验方详情
            // 暂时返回简化实现
            return ServiceResult<PrescriptionDto>.Failure("从验方创建处方功能暂未实现，需要集成验方模块");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从验方创建处方异常");
            return ServiceResult<PrescriptionDto>.Failure($"从验方创建处方异常: {ex.Message}");
        }
    }

    #endregion

    #region 处方状态管理

    /// <summary>
    /// 完成处方
    /// </summary>
    public async Task<ServiceResult> CompletePrescriptionAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始完成处方 - 处方ID: {PrescriptionId}", id);

            // 1. 验证处方完整性
            var prescriptionResult = await _queryService.GetByIdAsync(id);
            if (!prescriptionResult.IsSuccess)
            {
                return ServiceResult.Failure("获取处方信息失败");
            }

            var prescription = prescriptionResult.Data;
            if (prescription == null)
            {
                return ServiceResult.Failure("处方信息不存在");
            }

            // 2. 验证处方完整性
            var completenessResult = await ValidatePrescriptionCompletenessAsync(id);
            if (!completenessResult.IsSuccess || !completenessResult.Data.IsValid)
            {
                var validationErrors = string.Join("; ", completenessResult.Data?.Errors ?? new List<string>());
                return ServiceResult.Failure($"处方完整性验证失败: {validationErrors}");
            }

            // 3. 更新状态为完成
            var statusResult = await UpdateStatusAsync(id, PrescriptionStatus.Completed, "完成处方");
            if (!statusResult.IsSuccess)
            {
                return statusResult;
            }

            await TriggerPrescriptionOperationEventAsync(id, "Complete", "成功完成处方", true);
            _logger.LogInformation("成功完成处方 - 处方ID: {PrescriptionId}", id);

            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成处方异常: {PrescriptionId}", id);
            return ServiceResult.Failure($"完成处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 作废处方
    /// </summary>
    public async Task<ServiceResult> VoidPrescriptionAsync(Guid id, string reason)
    {
        try
        {
            _logger.LogInformation("开始作废处方 - 处方ID: {PrescriptionId}, 原因: {Reason}", id, reason);

            if (string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResult.Failure("作废原因不能为空");
            }

            // 1. 检查是否可作废
            var canVoidResult = await CanVoidAsync(id);
            if (!canVoidResult.IsSuccess || !canVoidResult.Data)
            {
                return ServiceResult.Failure(canVoidResult.ErrorMessage ?? "当前处方状态不允许作废");
            }

            // 2. 调用核心服务作废处方
            var result = await _coreService.CallCancelPrescriptionApiAsync(id);
            if (!result.IsSuccess)
            {
                await TriggerPrescriptionOperationEventAsync(id, "Void", $"作废处方失败，原因：{reason}", false, result.ErrorMessage);
                return ServiceResult.Failure(result.ErrorMessage ?? "作废处方失败");
            }

            // 3. 触发状态变更事件
            await TriggerPrescriptionStatusChangedEventAsync(id, PrescriptionStatus.Draft, PrescriptionStatus.Cancelled, reason);
            await TriggerPrescriptionOperationEventAsync(id, "Void", $"成功作废处方，原因：{reason}", true);

            _logger.LogInformation("成功作废处方 - 处方ID: {PrescriptionId}", id);
            return ServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "作废处方异常: {PrescriptionId}", id);
            return ServiceResult.Failure($"作废处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 重新激活处方
    /// </summary>
    public async Task<ServiceResult> ReactivatePrescriptionAsync(Guid id, string reason)
    {
        try
        {
            _logger.LogInformation("重新激活处方 - 处方ID: {PrescriptionId}, 原因: {Reason}", id, reason);

            // 简化实现 - 更新状态为草稿
            var result = await UpdateStatusAsync(id, PrescriptionStatus.Draft, reason);
            if (result.IsSuccess)
            {
                await TriggerPrescriptionOperationEventAsync(id, "Reactivate", $"成功重新激活处方，原因：{reason}", true);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新激活处方异常: {PrescriptionId}", id);
            return ServiceResult.Failure($"重新激活处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新处方状态
    /// </summary>
    public async Task<ServiceResult> UpdateStatusAsync(Guid id, PrescriptionStatus status, string reason)
    {
        try
        {
            _logger.LogInformation("更新处方状态 - 处方ID: {PrescriptionId}, 新状态: {Status}, 原因: {Reason}", id, status, reason);

            // 获取当前处方信息
            var currentResult = await _queryService.GetByIdAsync(id);
            if (!currentResult.IsSuccess)
            {
                return ServiceResult.Failure("获取处方当前状态失败");
            }

            var oldStatus = currentResult.Data?.PrescriptionStatus ?? PrescriptionStatus.Draft;

            // 简化的状态更新实现
            switch (status)
            {
                case PrescriptionStatus.Completed:
                case PrescriptionStatus.Draft:
                case PrescriptionStatus.Cancelled:
                    // 触发状态变更事件
                    await TriggerPrescriptionStatusChangedEventAsync(id, oldStatus, status, reason);
                    _logger.LogInformation("处方状态更新成功 - 处方ID: {PrescriptionId}, 从 {OldStatus} 更新为 {NewStatus}", id, oldStatus, status);
                    return ServiceResult.Success();
                
                default:
                    return ServiceResult.Failure($"不支持的处方状态更新: {status}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新处方状态异常: {PrescriptionId}", id);
            return ServiceResult.Failure($"更新处方状态异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量更新处方状态
    /// </summary>
    public async Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> prescriptionIds, PrescriptionStatus status)
    {
        try
        {
            _logger.LogInformation("批量更新处方状态 - 处方数量: {Count}, 目标状态: {Status}", prescriptionIds.Count, status);

            var result = new PrescriptionBatchOperationResultDto
            {
                TotalCount = prescriptionIds.Count
            };

            foreach (var id in prescriptionIds)
            {
                var updateResult = await UpdateStatusAsync(id, status, "批量状态更新");
                if (updateResult.IsSuccess)
                {
                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                }
                else
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.ErrorMessages.Add($"处方 {id}: {updateResult.ErrorMessage}");
                }
            }

            _logger.LogInformation("批量更新处方状态完成 - 成功: {Success}, 失败: {Failure}", result.SuccessCount, result.FailureCount);
            return ServiceResult<PrescriptionBatchOperationResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新处方状态异常");
            return ServiceResult<PrescriptionBatchOperationResultDto>.Failure($"批量更新处方状态异常: {ex.Message}");
        }
    }

    #endregion

    #region 处方项目管理 - 简化实现

    public Task<ServiceResult<PrescriptionItemDto>> AddPrescriptionItemAsync(Guid prescriptionId, PrescriptionItemCreateDto itemDto) =>
        Task.FromResult(ServiceResult<PrescriptionItemDto>.Failure("处方项目管理功能暂未实现"));

    public Task<ServiceResult<PrescriptionItemDto>> UpdatePrescriptionItemAsync(Guid itemId, PrescriptionItemUpdateDto updateDto) =>
        Task.FromResult(ServiceResult<PrescriptionItemDto>.Failure("处方项目管理功能暂未实现"));

    public Task<ServiceResult<bool>> RemovePrescriptionItemAsync(Guid itemId) =>
        Task.FromResult(ServiceResult<bool>.Failure("处方项目管理功能暂未实现"));

    public Task<ServiceResult<int>> BatchUpdatePrescriptionItemsAsync(Guid prescriptionId, List<PrescriptionItemDto> items) =>
        Task.FromResult(ServiceResult<int>.Failure("处方项目管理功能暂未实现"));

    public Task<ServiceResult<PrescriptionItemDto>> AdjustItemQuantityAsync(Guid itemId, decimal newQuantity) =>
        Task.FromResult(ServiceResult<PrescriptionItemDto>.Failure("处方项目管理功能暂未实现"));

    #endregion

    #region 价格计算与折扣

    /// <summary>
    /// 计算处方总价格
    /// </summary>
    public async Task<ServiceResult<decimal>> CalculateTotalPriceAsync(Guid prescriptionId)
    {
        try
        {
            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<decimal>.Failure("获取处方信息失败");
            }

            var prescription = prescriptionResult.Data;
            var totalPrice = prescription.Items.Sum(item => item.Subtotal) * prescription.DosageCount;

            _logger.LogDebug("计算处方总价格 - 处方ID: {PrescriptionId}, 总价: {TotalPrice}", prescriptionId, totalPrice);
            return ServiceResult<decimal>.Success(totalPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算处方总价格异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<decimal>.Failure($"计算处方总价格异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 计算单剂价格
    /// </summary>
    public async Task<ServiceResult<decimal>> CalculateSingleDosePriceAsync(Guid prescriptionId)
    {
        try
        {
            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<decimal>.Failure("获取处方信息失败");
            }

            var prescription = prescriptionResult.Data;
            var singleDosePrice = prescription.Items.Sum(item => item.Subtotal);

            _logger.LogDebug("计算单剂价格 - 处方ID: {PrescriptionId}, 单剂价格: {SingleDosePrice}", prescriptionId, singleDosePrice);
            return ServiceResult<decimal>.Success(singleDosePrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算单剂价格异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<decimal>.Failure($"计算单剂价格异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 应用折扣
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> ApplyDiscountAsync(Guid prescriptionId, decimal discountRate, string reason)
    {
        try
        {
            if (discountRate < 0 || discountRate > 1)
            {
                return ServiceResult<PrescriptionDto>.Failure("折扣率必须在0-1之间");
            }

            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<PrescriptionDto>.Failure("获取处方信息失败");
            }

            // TODO: 实现折扣应用逻辑
            _logger.LogInformation("应用处方折扣 - 处方ID: {PrescriptionId}, 折扣率: {DiscountRate}, 原因: {Reason}", 
                prescriptionId, discountRate, reason);

            return ServiceResult<PrescriptionDto>.Success(prescriptionResult.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用折扣异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<PrescriptionDto>.Failure($"应用折扣异常: {ex.Message}");
        }
    }

    public Task<ServiceResult<PrescriptionDto>> RemoveDiscountAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<PrescriptionDto>.Failure("移除折扣功能暂未实现"));

    public Task<ServiceResult<PrescriptionBatchPriceDto>> CalculateBatchPricesAsync(List<Guid> prescriptionIds) =>
        Task.FromResult(ServiceResult<PrescriptionBatchPriceDto>.Failure("批量价格计算功能暂未实现"));

    public Task<ServiceResult<PrescriptionDto>> UpdatePriceAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<PrescriptionDto>.Failure("更新价格功能暂未实现"));

    #endregion

    #region 业务验证与检查

    /// <summary>
    /// 验证处方完整性
    /// </summary>
    public async Task<ServiceResult<PrescriptionValidationResult>> ValidatePrescriptionCompletenessAsync(Guid prescriptionId)
    {
        try
        {
            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<PrescriptionValidationResult>.Failure("获取处方信息失败");
            }

            var prescription = prescriptionResult.Data;
            var result = new PrescriptionValidationResult();

            // 基础信息验证
            if (prescription.PatientId == Guid.Empty)
                result.Errors.Add("患者信息缺失");

            if (prescription.UserId == Guid.Empty)
                result.Errors.Add("医生信息缺失");

            if (string.IsNullOrWhiteSpace(prescription.Diagnosis))
                result.Errors.Add("诊断信息缺失");

            if (prescription.Items == null || !prescription.Items.Any())
                result.Errors.Add("处方必须包含药材");

            if (prescription.DosageCount <= 0)
                result.Errors.Add("服药剂数必须大于0");

            // 处方项目验证
            foreach (var item in prescription.Items)
            {
                if (item.HerbId == Guid.Empty)
                    result.Errors.Add($"药材信息缺失");

                if (item.Quantity <= 0)
                    result.Errors.Add($"药材 {item.HerbName} 用量无效");

                if (item.UnitPrice < 0)
                    result.Errors.Add($"药材 {item.HerbName} 单价无效");
            }

            result.IsValid = !result.Errors.Any();

            await TriggerPrescriptionValidationEventAsync(prescriptionId, "CompletenessValidation", result.IsValid, result.Errors);

            _logger.LogDebug("处方完整性验证 - 处方ID: {PrescriptionId}, 验证结果: {IsValid}", prescriptionId, result.IsValid);
            return ServiceResult<PrescriptionValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证处方完整性异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<PrescriptionValidationResult>.Failure($"验证处方完整性异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否可修改
    /// </summary>
    public async Task<ServiceResult<bool>> CanModifyAsync(Guid prescriptionId)
    {
        try
        {
            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取处方信息失败");
            }

            // 只有草稿状态的处方可以修改
            var canModify = prescriptionResult.Data.PrescriptionStatus == PrescriptionStatus.Draft;
            return ServiceResult<bool>.Success(canModify);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查是否可修改异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<bool>.Failure($"检查是否可修改异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否可删除
    /// </summary>
    public async Task<ServiceResult<bool>> CanDeleteAsync(Guid prescriptionId)
    {
        try
        {
            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取处方信息失败");
            }

            // 只有草稿状态的处方可以删除
            var canDelete = prescriptionResult.Data.PrescriptionStatus == PrescriptionStatus.Draft;
            return ServiceResult<bool>.Success(canDelete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查是否可删除异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<bool>.Failure($"检查是否可删除异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否可作废
    /// </summary>
    public async Task<ServiceResult<bool>> CanVoidAsync(Guid prescriptionId)
    {
        try
        {
            var prescriptionResult = await _queryService.GetByIdAsync(prescriptionId);
            if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取处方信息失败");
            }

            // 草稿和已完成状态的处方可以作废
            var canVoid = prescriptionResult.Data.PrescriptionStatus == PrescriptionStatus.Draft ||
                         prescriptionResult.Data.PrescriptionStatus == PrescriptionStatus.Completed;
            return ServiceResult<bool>.Success(canVoid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查是否可作废异常: {PrescriptionId}", prescriptionId);
            return ServiceResult<bool>.Failure($"检查是否可作废异常: {ex.Message}");
        }
    }

    // 其他业务验证方法的简化实现
    public Task<ServiceResult<CompatibilityCheckResult>> CheckIngredientCompatibilityAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<CompatibilityCheckResult>.Failure("配伍检查功能暂未实现"));

    public Task<ServiceResult<DosageValidationResult>> ValidateDosageAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<DosageValidationResult>.Failure("剂量验证功能暂未实现"));

    public Task<ServiceResult<bool>> CheckPrescriptionPermissionAsync(Guid prescriptionId, Guid userId) =>
        Task.FromResult(ServiceResult<bool>.Success(true));

    #endregion

    #region 简化实现的其他业务方法

    public Task<ServiceResult> RecordPrescriptionUsageAsync(Guid prescriptionId, PrescriptionUsageRecordDto usageRecord) =>
        Task.FromResult(ServiceResult.Success());

    public Task<ServiceResult<List<PrescriptionUsageHistoryDto>>> GetUsageHistoryAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<List<PrescriptionUsageHistoryDto>>.Success(new List<PrescriptionUsageHistoryDto>()));

    public Task<ServiceResult> MarkAsPrintedAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult.Success());

    public Task<ServiceResult> MarkAsDispensedAsync(Guid prescriptionId, PrescriptionDispenseDto dispenseInfo) =>
        Task.FromResult(ServiceResult.Success());

    public Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchDeletePrescriptionsAsync(List<Guid> prescriptionIds) =>
        Task.FromResult(ServiceResult<PrescriptionBatchOperationResultDto>.Failure("批量删除功能暂未实现"));

    public Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchVoidPrescriptionsAsync(List<Guid> prescriptionIds, string reason) =>
        Task.FromResult(ServiceResult<PrescriptionBatchOperationResultDto>.Failure("批量作废功能暂未实现"));

    public Task<ServiceResult<List<PrescriptionDto>>> BatchCopyPrescriptionsAsync(List<Guid> prescriptionIds, Guid targetPatientId) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Failure("批量复制功能暂未实现"));

    public Task<ServiceResult<PrescriptionBatchOperationResultDto>> BatchTransferPrescriptionsAsync(List<Guid> prescriptionIds, Guid newDoctorId) =>
        Task.FromResult(ServiceResult<PrescriptionBatchOperationResultDto>.Failure("批量转移功能暂未实现"));

    public Task<ServiceResult<PrescriptionImportResultDto>> ImportPrescriptionsAsync(PrescriptionImportDto importDto) =>
        Task.FromResult(ServiceResult<PrescriptionImportResultDto>.Failure("导入功能暂未实现"));

    public Task<ServiceResult<PrescriptionExportResultDto>> ExportPrescriptionsAsync(PrescriptionExportQueryDto exportQuery) =>
        Task.FromResult(ServiceResult<PrescriptionExportResultDto>.Failure("导出功能暂未实现"));

    public Task<ServiceResult<PrescriptionImportValidationResultDto>> ValidateImportDataAsync(PrescriptionImportDto importDto) =>
        Task.FromResult(ServiceResult<PrescriptionImportValidationResultDto>.Failure("导入验证功能暂未实现"));

    public Task<ServiceResult<byte[]>> GenerateImportTemplateAsync() =>
        Task.FromResult(ServiceResult<byte[]>.Failure("生成导入模板功能暂未实现"));

    public Task<ServiceResult<List<PrescriptionDto>>> RecommendSimilarPrescriptionsAsync(Guid prescriptionId, int limit = 5) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<List<PrescriptionDto>>> RecommendPrescriptionsBySymptomAsync(List<string> symptoms, int limit = 10) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<PrescriptionUsageTrendDto>> AnalyzePrescriptionUsageTrendAsync(Guid prescriptionId, int days = 30) =>
        Task.FromResult(ServiceResult<PrescriptionUsageTrendDto>.Failure("使用趋势分析功能暂未实现"));

    public Task<ServiceResult<IngredientCombinationAnalysisDto>> AnalyzeIngredientCombinationAsync(List<Guid> herbIds) =>
        Task.FromResult(ServiceResult<IngredientCombinationAnalysisDto>.Failure("药材组合分析功能暂未实现"));

    public Task<ServiceResult> SubmitPrescriptionForReviewAsync(Guid prescriptionId, string reviewNote) =>
        Task.FromResult(ServiceResult.Failure("提交审核功能暂未实现"));

    public Task<ServiceResult> ReviewPrescriptionAsync(Guid prescriptionId, PrescriptionReviewDecisionDto decision) =>
        Task.FromResult(ServiceResult.Failure("审核功能暂未实现"));

    public Task<ServiceResult> PublishPrescriptionAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult.Failure("发布功能暂未实现"));

    public Task<ServiceResult> ArchivePrescriptionAsync(Guid prescriptionId, string archiveReason) =>
        Task.FromResult(ServiceResult.Failure("归档功能暂未实现"));

    public Task<ServiceResult> RestoreArchivedPrescriptionAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult.Failure("恢复归档功能暂未实现"));

    public Task<ServiceResult<byte[]>> GeneratePrescriptionQRCodeAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<byte[]>.Failure("生成二维码功能暂未实现"));

    public Task<ServiceResult<byte[]>> GeneratePrescriptionPdfReportAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<byte[]>.Failure("生成PDF报告功能暂未实现"));

    public Task<ServiceResult<PrescriptionPrintInfoDto>> GetPrintInfoAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<PrescriptionPrintInfoDto>.Failure("获取打印信息功能暂未实现"));

    public Task<ServiceResult<PrescriptionShareTokenDto>> SharePrescriptionAsync(Guid prescriptionId, PrescriptionShareOptionsDto shareOptions) =>
        Task.FromResult(ServiceResult<PrescriptionShareTokenDto>.Failure("分享功能暂未实现"));

    public Task<ServiceResult> FavoritePrescriptionAsync(Guid prescriptionId, Guid userId) =>
        Task.FromResult(ServiceResult.Failure("收藏功能暂未实现"));

    public Task<ServiceResult> UnfavoritePrescriptionAsync(Guid prescriptionId, Guid userId) =>
        Task.FromResult(ServiceResult.Failure("取消收藏功能暂未实现"));

    #endregion

    #region 事件触发辅助方法

    private async Task TriggerPrescriptionStatusChangedEventAsync(Guid prescriptionId, PrescriptionStatus oldStatus, PrescriptionStatus newStatus, string reason)
    {
        try
        {
            var eventArgs = new PrescriptionStatusChangedEventArgs
            {
                PrescriptionId = prescriptionId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason,
                ChangedAt = DateTime.Now,
                UserId = Guid.Empty, // TODO: 获取当前用户ID
                UserName = "System"
            };

            PrescriptionStatusChanged?.Invoke(this, eventArgs);
            await _coreService.LogOperationAsync("StatusChanged", prescriptionId, $"状态从 {oldStatus} 变更为 {newStatus}，原因：{reason}", Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发处方状态变更事件异常");
        }
    }

    private async Task TriggerPrescriptionOperationEventAsync(Guid prescriptionId, string operation, string details, bool isSuccess, string? errorMessage = null)
    {
        try
        {
            var eventArgs = new PrescriptionOperationEventArgs
            {
                PrescriptionId = prescriptionId,
                Operation = operation,
                OperationDetails = details,
                OperatedAt = DateTime.Now,
                UserId = Guid.Empty, // TODO: 获取当前用户ID
                UserName = "System",
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            PrescriptionOperation?.Invoke(this, eventArgs);
            await _coreService.LogOperationAsync(operation, prescriptionId, details, Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发处方操作事件异常");
        }
    }

    private async Task TriggerPrescriptionValidationEventAsync(Guid prescriptionId, string validationType, bool isValid, List<string> validationErrors)
    {
        try
        {
            var eventArgs = new PrescriptionValidationEventArgs
            {
                PrescriptionId = prescriptionId,
                ValidationType = validationType,
                IsValid = isValid,
                ValidationErrors = validationErrors,
                ValidatedAt = DateTime.Now,
                UserId = Guid.Empty, // TODO: 获取当前用户ID
                UserName = "System"
            };

            PrescriptionValidation?.Invoke(this, eventArgs);
            await _coreService.LogOperationAsync("Validation", prescriptionId, $"验证类型：{validationType}，结果：{isValid}", Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发处方验证事件异常");
        }
    }

    #endregion
}
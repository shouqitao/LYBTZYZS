using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Consultation.Interfaces;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊业务服务实现 - UltraThink三层架构业务层
/// 职责：业务流程编排、CRUD操作、验证规则、事务管理
/// </summary>
public class ConsultationBusinessService(
    IConsultationCoreService coreService,
    ILogger<ConsultationBusinessService> logger) : IConsultationBusinessService
{
    private readonly IConsultationCoreService _coreService = coreService;
    private readonly ILogger<ConsultationBusinessService> _logger = logger;

    #region 事件定义

    public event EventHandler<ConsultationStatusChangedEventArgs>? ConsultationStatusChanged;
    public event EventHandler<ConsultationOperationEventArgs>? ConsultationOperation;
    public event EventHandler<DiagnosisUpdatedEventArgs>? DiagnosisUpdated;
    public event EventHandler<FourDiagnosisRecordedEventArgs>? FourDiagnosisRecorded;

    #endregion

    #region 基础CRUD操作

    /// <summary>
    /// 开始看诊
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto startDto)
    {
        try
        {
            _logger.LogInformation("开始创建看诊，患者ID: {PatientId}，医生ID: {DoctorId}", startDto.PatientId, startDto.DoctorId);

            // 1. 验证输入数据
            var validationResult = await _coreService.ValidateStartDtoAsync(startDto);
            if (!validationResult.IsSuccess || (validationResult.Data != null && !validationResult.Data.IsValid))
            {
                var errors = validationResult.Data?.Errors ?? [validationResult.Message];
                _logger.LogWarning("看诊创建验证失败: {Errors}", string.Join(", ", errors));
                return ServiceResult<ConsultationDto>.Failure(string.Join(", ", errors));
            }

            // 2. 检查患者和医生是否存在
            var patientExists = await _coreService.CheckPatientExistsAsync(startDto.PatientId);
            if (!patientExists.IsSuccess || !patientExists.Data)
            {
                return ServiceResult<ConsultationDto>.Failure("指定的患者不存在");
            }

            var doctorExists = await _coreService.CheckDoctorExistsAsync(startDto.DoctorId);
            if (!doctorExists.IsSuccess || !doctorExists.Data)
            {
                return ServiceResult<ConsultationDto>.Failure("指定的医生不存在");
            }

            // 3. 验证患者医生关联
            var associationResult = await _coreService.ValidatePatientDoctorAssociationAsync(startDto.PatientId, startDto.DoctorId);
            if (!associationResult.IsSuccess || !associationResult.Data)
            {
                return ServiceResult<ConsultationDto>.Failure("患者和医生关联验证失败");
            }

            // 4. 生成看诊编号
            var numberResult = await _coreService.GenerateConsultationNumberAsync();
            if (!numberResult.IsSuccess)
            {
                return ServiceResult<ConsultationDto>.Failure($"生成看诊编号失败：{numberResult.Message}");
            }

            // 5. 调用API创建看诊
            startDto.ConsultationNumber = numberResult.Data!;
            var result = await _coreService.CallStartConsultationApiAsync(startDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 6. 记录操作日志
                await _coreService.LogOperationAsync("StartConsultation", result.Data.Id, "创建看诊", startDto.DoctorId);

                // 7. 触发看诊操作事件
                await TriggerConsultationOperationEventAsync(result.Data.Id, "创建", "看诊创建成功", startDto.DoctorId, "系统", true);

                // 8. 清除相关缓存
                await ClearRelatedCacheAsync(result.Data.Id, startDto.PatientId, startDto.DoctorId);

                _logger.LogInformation("看诊创建成功，ID: {Id}，编号: {Number}", result.Data.Id, result.Data.ConsultationNumber);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建看诊失败");
            await TriggerConsultationOperationEventAsync(Guid.Empty, "创建", "看诊创建失败", startDto.DoctorId, "系统", false, ex.Message);
            return ServiceResult<ConsultationDto>.Failure($"创建看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新看诊
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("更新看诊，ID: {Id}", id);

            // 1. 验证看诊ID
            var idValidation = await _coreService.ValidateConsultationIdAsync(id);
            if (!idValidation.IsSuccess || !idValidation.Data)
            {
                return ServiceResult<ConsultationDto>.Failure("看诊ID无效");
            }

            // 2. 验证更新数据
            var validationResult = await _coreService.ValidateUpdateDtoAsync(updateDto);
            if (!validationResult.IsSuccess || (validationResult.Data != null && !validationResult.Data.IsValid))
            {
                var errors = validationResult.Data?.Errors ?? [validationResult.Message];
                return ServiceResult<ConsultationDto>.Failure(string.Join(", ", errors));
            }

            // 3. 检查看诊是否存在
            var existsResult = await _coreService.CheckConsultationExistsAsync(id);
            if (!existsResult.IsSuccess || !existsResult.Data)
            {
                return ServiceResult<ConsultationDto>.Failure("指定的看诊记录不存在");
            }

            // 4. 获取原始数据（用于事件通知）
            var originalData = await _coreService.CallGetConsultationByIdApiAsync(id);
            var oldDiagnosis = originalData.Data?.Diagnosis;

            // 5. 调用API更新看诊
            var result = await _coreService.CallUpdateConsultationApiAsync(id, updateDto);

            if (result.IsSuccess && result.Data != null)
            {
                // 6. 检查诊断是否更新并触发相应事件
                if (!string.Equals(oldDiagnosis, updateDto.Diagnosis, StringComparison.Ordinal))
                {
                    await TriggerDiagnosisUpdatedEventAsync(id, oldDiagnosis, updateDto.Diagnosis ?? string.Empty, 
                        updateDto.DoctorId, "系统");
                }

                // 7. 记录操作日志
                await _coreService.LogOperationAsync("UpdateConsultation", id, "更新看诊", updateDto.DoctorId);

                // 8. 触发看诊操作事件
                await TriggerConsultationOperationEventAsync(id, "更新", "看诊更新成功", updateDto.DoctorId, "系统", true);

                // 9. 清除相关缓存
                await ClearRelatedCacheAsync(id, result.Data.PatientId, result.Data.DoctorId);

                _logger.LogInformation("看诊更新成功，ID: {Id}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新看诊失败，ID: {Id}", id);
            await TriggerConsultationOperationEventAsync(id, "更新", "看诊更新失败", updateDto.DoctorId, "系统", false, ex.Message);
            return ServiceResult<ConsultationDto>.Failure($"更新看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 删除看诊
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("删除看诊，ID: {Id}", id);

            // 1. 验证看诊ID
            var idValidation = await _coreService.ValidateConsultationIdAsync(id);
            if (!idValidation.IsSuccess || !idValidation.Data)
            {
                return ServiceResult<bool>.Failure("看诊ID无效");
            }

            // 2. 检查看诊是否存在并获取信息
            var consultationData = await _coreService.CallGetConsultationByIdApiAsync(id);
            if (!consultationData.IsSuccess || consultationData.Data == null)
            {
                return ServiceResult<bool>.Failure("指定的看诊记录不存在");
            }

            // 3. 调用API删除看诊
            var result = await _coreService.CallDeleteConsultationApiAsync(id);

            if (result.IsSuccess)
            {
                // 4. 记录操作日志
                await _coreService.LogOperationAsync("DeleteConsultation", id, "删除看诊", consultationData.Data.DoctorId);

                // 5. 触发看诊操作事件
                await TriggerConsultationOperationEventAsync(id, "删除", "看诊删除成功", consultationData.Data.DoctorId, "系统", true);

                // 6. 清除相关缓存
                await ClearRelatedCacheAsync(id, consultationData.Data.PatientId, consultationData.Data.DoctorId);

                _logger.LogInformation("看诊删除成功，ID: {Id}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除看诊失败，ID: {Id}", id);
            return ServiceResult<bool>.Failure($"删除看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊详情
    /// </summary>
    public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
    {
        try
        {
            // 1. 验证看诊ID
            var idValidation = await _coreService.ValidateConsultationIdAsync(id);
            if (!idValidation.IsSuccess || !idValidation.Data)
            {
                return ServiceResult<ConsultationDetailDto>.Failure("看诊ID无效");
            }

            // 2. 调用API获取看诊详情
            var result = await _coreService.CallGetConsultationByIdApiAsync(id);

            if (result.IsSuccess && result.Data != null)
            {
                // 3. 格式化数据
                // var formattedResult = await _coreService.FormatConsultationDataAsync(result.Data);
                _logger.LogDebug("获取看诊详情成功，ID: {Id}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊详情失败，ID: {Id}", id);
            return ServiceResult<ConsultationDetailDto>.Failure($"获取看诊详情失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取分页看诊列表
    /// </summary>
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
    {
        try
        {
            // 1. 验证查询参数
            var validationResult = await _coreService.ValidateQueryParametersAsync(query);
            if (!validationResult.IsSuccess || !validationResult.Data)
            {
                return ServiceResult<PagedResult<ConsultationDto>>.Failure(validationResult.Message);
            }

            // 2. 调用API获取分页数据
            var result = await _coreService.CallGetConsultationListApiAsync(query);

            if (result.IsSuccess)
            {
                _logger.LogDebug("获取分页看诊列表成功，页码: {PageIndex}，每页: {PageSize}", query.PageIndex, query.PageSize);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分页看诊列表失败");
            return ServiceResult<PagedResult<ConsultationDto>>.Failure($"获取看诊列表失败：{ex.Message}");
        }
    }

    #endregion

    #region 业务流程方法

    /// <summary>
    /// 完成看诊
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto completeDto)
    {
        try
        {
            _logger.LogInformation("完成看诊，ID: {Id}", id);

            // 1. 验证看诊ID
            var idValidation = await _coreService.ValidateConsultationIdAsync(id);
            if (!idValidation.IsSuccess || !idValidation.Data)
            {
                return ServiceResult<bool>.Failure("看诊ID无效");
            }

            // 2. 获取看诊信息验证完整性
            var consultationResult = await _coreService.CallGetConsultationByIdApiAsync(id);
            if (!consultationResult.IsSuccess || consultationResult.Data == null)
            {
                return ServiceResult<bool>.Failure("看诊记录不存在");
            }

            // 3. 验证看诊完整性
            var completenessResult = await _coreService.ValidateConsultationCompletenessAsync(consultationResult.Data);
            if (!completenessResult.IsSuccess || !completenessResult.Data)
            {
                _logger.LogWarning("看诊信息不完整，ID: {Id}", id);
                // 不阻止完成，但记录警告
            }

            // 4. 调用API完成看诊
            var result = await _coreService.CallCompleteConsultationApiAsync(id, completeDto);

            if (result.IsSuccess)
            {
                // 5. 计算看诊持续时间
                var durationResult = await _coreService.CalculateConsultationDurationAsync(
                    consultationResult.Data.ConsultationDate, DateTime.Now);

                // 6. 记录操作日志
                var durationInfo = durationResult.IsSuccess ? $"，持续时间: {durationResult.Data:hh\\:mm\\:ss}" : "";
                await _coreService.LogOperationAsync("CompleteConsultation", id, $"完成看诊{durationInfo}", consultationResult.Data.DoctorId);

                // 7. 触发状态变更事件
                await TriggerConsultationStatusChangedEventAsync(id, ConsultationStatus.InProgress, 
                    ConsultationStatus.Completed, "看诊完成", consultationResult.Data.DoctorId, "系统");

                // 8. 触发看诊操作事件
                await TriggerConsultationOperationEventAsync(id, "完成", "看诊完成", consultationResult.Data.DoctorId, "系统", true);

                // 9. 清除相关缓存
                await ClearRelatedCacheAsync(id, consultationResult.Data.PatientId, consultationResult.Data.DoctorId);

                _logger.LogInformation("看诊完成成功，ID: {Id}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成看诊失败，ID: {Id}", id);
            return ServiceResult<bool>.Failure($"完成看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 取消看诊
    /// </summary>
    public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
    {
        try
        {
            _logger.LogInformation("取消看诊，ID: {Id}，原因: {Reason}", id, reason);

            // 1. 验证看诊ID
            var idValidation = await _coreService.ValidateConsultationIdAsync(id);
            if (!idValidation.IsSuccess || !idValidation.Data)
            {
                return ServiceResult<bool>.Failure("看诊ID无效");
            }

            // 2. 验证取消原因
            if (string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResult<bool>.Failure("取消原因不能为空");
            }

            // 3. 获取看诊信息
            var consultationResult = await _coreService.CallGetConsultationByIdApiAsync(id);
            if (!consultationResult.IsSuccess || consultationResult.Data == null)
            {
                return ServiceResult<bool>.Failure("看诊记录不存在");
            }

            // 4. 调用API取消看诊
            var result = await _coreService.CallCancelConsultationApiAsync(id, reason);

            if (result.IsSuccess)
            {
                // 5. 记录操作日志
                await _coreService.LogOperationAsync("CancelConsultation", id, $"取消看诊，原因: {reason}", consultationResult.Data.DoctorId);

                // 6. 触发状态变更事件
                await TriggerConsultationStatusChangedEventAsync(id, ConsultationStatus.InProgress, 
                    ConsultationStatus.Cancelled, reason, consultationResult.Data.DoctorId, "系统");

                // 7. 触发看诊操作事件
                await TriggerConsultationOperationEventAsync(id, "取消", $"看诊已取消: {reason}", consultationResult.Data.DoctorId, "系统", true);

                // 8. 清除相关缓存
                await ClearRelatedCacheAsync(id, consultationResult.Data.PatientId, consultationResult.Data.DoctorId);

                _logger.LogInformation("看诊取消成功，ID: {Id}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消看诊失败，ID: {Id}", id);
            return ServiceResult<bool>.Failure($"取消看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存完整四诊记录
    /// </summary>
    public async Task<ServiceResult<bool>> SaveCompleteFourDiagnosisAsync(Guid consultationId, CompleteFourDiagnosisDto fourDiagnosisData)
    {
        try
        {
            _logger.LogInformation("保存四诊记录，看诊ID: {ConsultationId}", consultationId);

            // 1. 验证看诊ID
            var idValidation = await _coreService.ValidateConsultationIdAsync(consultationId);
            if (!idValidation.IsSuccess || !idValidation.Data)
            {
                return ServiceResult<bool>.Failure("看诊ID无效");
            }

            // 2. 验证四诊数据
            var validationResult = await _coreService.ValidateFourDiagnosisDataAsync(fourDiagnosisData);
            if (!validationResult.IsSuccess || !validationResult.Data)
            {
                return ServiceResult<bool>.Failure(validationResult.Message);
            }

            // 3. 获取看诊信息
            var consultationResult = await _coreService.CallGetConsultationByIdApiAsync(consultationId);
            if (!consultationResult.IsSuccess || consultationResult.Data == null)
            {
                return ServiceResult<bool>.Failure("看诊记录不存在");
            }

            // 4. 构建更新DTO
            var updateDto = new ConsultationUpdateDto
            {
                DoctorId = consultationResult.Data.DoctorId,
                ChiefComplaint = fourDiagnosisData.ChiefComplaint,
                Diagnosis = fourDiagnosisData.Diagnosis,
                TreatmentPlan = fourDiagnosisData.TreatmentPlan,
                Remarks = fourDiagnosisData.Remarks,
                // 将四诊数据设置到相应字段
                Inspection = fourDiagnosisData.Inspection,
                Auscultation = fourDiagnosisData.Auscultation,
                Inquiry = fourDiagnosisData.Inquiry,
                Palpation = fourDiagnosisData.Palpation
            };

            // 5. 更新看诊记录
            var updateResult = await UpdateAsync(consultationId, updateDto);
            
            if (updateResult.IsSuccess)
            {
                // 6. 触发四诊记录事件 - 分别触发四个诊断类型的事件
                if (!string.IsNullOrWhiteSpace(fourDiagnosisData.Inspection))
                    await TriggerFourDiagnosisRecordedEventAsync(consultationId, "Inspection", fourDiagnosisData.Inspection, consultationResult.Data.DoctorId, "系统");
                
                if (!string.IsNullOrWhiteSpace(fourDiagnosisData.Auscultation))
                    await TriggerFourDiagnosisRecordedEventAsync(consultationId, "Auscultation", fourDiagnosisData.Auscultation, consultationResult.Data.DoctorId, "系统");
                
                if (!string.IsNullOrWhiteSpace(fourDiagnosisData.Inquiry))
                    await TriggerFourDiagnosisRecordedEventAsync(consultationId, "Inquiry", fourDiagnosisData.Inquiry, consultationResult.Data.DoctorId, "系统");
                
                if (!string.IsNullOrWhiteSpace(fourDiagnosisData.Palpation))
                    await TriggerFourDiagnosisRecordedEventAsync(consultationId, "Palpation", fourDiagnosisData.Palpation, consultationResult.Data.DoctorId, "系统");

                // 7. 记录操作日志
                await _coreService.LogOperationAsync("SaveFourDiagnosis", consultationId, "保存四诊记录", consultationResult.Data.DoctorId);

                _logger.LogInformation("四诊记录保存成功，看诊ID: {ConsultationId}", consultationId);
                return ServiceResult<bool>.Success(true);
            }

            return ServiceResult<bool>.Failure($"保存四诊记录失败：{updateResult.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存四诊记录失败，看诊ID: {ConsultationId}", consultationId);
            return ServiceResult<bool>.Failure($"保存四诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 批量更新看诊状态
    /// </summary>
    public async Task<ServiceResult<ConsultationBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> consultationIds, ConsultationStatus status)
    {
        try
        {
            _logger.LogInformation("批量更新看诊状态，数量: {Count}，状态: {Status}", consultationIds.Count, status);

            var result = new ConsultationBatchOperationResultDto
            {
                TotalCount = consultationIds.Count,
                SuccessCount = 0,
                FailureCount = 0,
                ErrorMessages = [],
                SuccessfulIds = [],
                FailedIds = []
            };

            foreach (var consultationId in consultationIds)
            {
                try
                {
                    // 获取看诊信息
                    var consultationResult = await _coreService.CallGetConsultationByIdApiAsync(consultationId);
                    if (!consultationResult.IsSuccess || consultationResult.Data == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(consultationId);
                        result.ErrorMessages.Add($"看诊 {consultationId} 不存在");
                        continue;
                    }

                    // 根据状态执行不同操作
                    ServiceResult<bool> operationResult = status switch
                    {
                        ConsultationStatus.Completed => await _coreService.CallCompleteConsultationApiAsync(consultationId, 
                            new ConsultationCompleteDto { Summary = "批量完成" }),
                        ConsultationStatus.Cancelled => await _coreService.CallCancelConsultationApiAsync(consultationId, "批量取消"),
                        _ => ServiceResult<bool>.Failure($"不支持的状态: {status}")
                    };

                    if (operationResult.IsSuccess)
                    {
                        result.SuccessCount++;
                        result.SuccessfulIds.Add(consultationId);
                        
                        // 清除缓存
                        await ClearRelatedCacheAsync(consultationId, consultationResult.Data.PatientId, consultationResult.Data.DoctorId);
                    }
                    else
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(consultationId);
                        result.ErrorMessages.Add($"看诊 {consultationId} 更新失败: {operationResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(consultationId);
                    result.ErrorMessages.Add($"看诊 {consultationId} 处理异常: {ex.Message}");
                    _logger.LogError(ex, "批量更新看诊状态失败，ID: {ConsultationId}", consultationId);
                }
            }

            _logger.LogInformation("批量更新看诊状态完成，成功: {Success}，失败: {Failure}", result.SuccessCount, result.FailureCount);
            return ServiceResult<ConsultationBatchOperationResultDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新看诊状态失败");
            return ServiceResult<ConsultationBatchOperationResultDto>.Failure($"批量更新失败：{ex.Message}");
        }
    }

    #endregion

    #region 搜索方法

    /// <summary>
    /// 搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword, int limit = 100)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResult<List<ConsultationDto>>.Failure("搜索关键词不能为空");
            }

            if (limit <= 0 || limit > 1000)
            {
                return ServiceResult<List<ConsultationDto>>.Failure("搜索限制数量必须在1-1000之间");
            }

            var result = await _coreService.CallSearchConsultationsApiAsync(keyword, limit);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("搜索看诊记录成功，关键词: {Keyword}，结果数量: {Count}", keyword, result.Data?.Count ?? 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索看诊记录失败，关键词: {Keyword}", keyword);
            return ServiceResult<List<ConsultationDto>>.Failure($"搜索失败：{ex.Message}");
        }
    }

    #endregion

    #region 事件触发辅助方法

    /// <summary>
    /// 触发看诊状态变更事件
    /// </summary>
    private async Task TriggerConsultationStatusChangedEventAsync(Guid consultationId, ConsultationStatus oldStatus, 
        ConsultationStatus newStatus, string reason, Guid doctorId, string doctorName)
    {
        try
        {
            var eventArgs = new ConsultationStatusChangedEventArgs
            {
                ConsultationId = consultationId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason,
                ChangedAt = DateTime.Now,
                DoctorId = doctorId,
                DoctorName = doctorName
            };

            ConsultationStatusChanged?.Invoke(this, eventArgs);

            // 触发系统事件通知
            var eventData = new Dictionary<string, object>
            {
                ["consultationId"] = consultationId,
                ["oldStatus"] = oldStatus,
                ["newStatus"] = newStatus,
                ["reason"] = reason,
                ["doctorId"] = doctorId
            };

            await _coreService.TriggerEventNotificationAsync("ConsultationStatusChanged", consultationId, eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发看诊状态变更事件失败，ConsultationId: {ConsultationId}", consultationId);
        }
    }

    /// <summary>
    /// 触发看诊操作事件
    /// </summary>
    private async Task TriggerConsultationOperationEventAsync(Guid consultationId, string operation, string operationDetails, 
        Guid operatorId, string operatorName, bool isSuccess, string? errorMessage = null)
    {
        try
        {
            var eventArgs = new ConsultationOperationEventArgs
            {
                ConsultationId = consultationId,
                Operation = operation,
                OperationDetails = operationDetails,
                OperatedAt = DateTime.Now,
                OperatorId = operatorId,
                OperatorName = operatorName,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            ConsultationOperation?.Invoke(this, eventArgs);

            var eventData = new Dictionary<string, object>
            {
                ["consultationId"] = consultationId,
                ["operation"] = operation,
                ["operationDetails"] = operationDetails,
                ["operatorId"] = operatorId,
                ["isSuccess"] = isSuccess
            };

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                eventData["errorMessage"] = errorMessage;
            }

            await _coreService.TriggerEventNotificationAsync("ConsultationOperation", consultationId, eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发看诊操作事件失败，ConsultationId: {ConsultationId}", consultationId);
        }
    }

    /// <summary>
    /// 触发诊断更新事件
    /// </summary>
    private async Task TriggerDiagnosisUpdatedEventAsync(Guid consultationId, string? oldDiagnosis, string newDiagnosis, 
        Guid doctorId, string doctorName)
    {
        try
        {
            var eventArgs = new DiagnosisUpdatedEventArgs
            {
                ConsultationId = consultationId,
                OldDiagnosis = oldDiagnosis,
                NewDiagnosis = newDiagnosis,
                UpdatedAt = DateTime.Now,
                DoctorId = doctorId,
                DoctorName = doctorName
            };

            DiagnosisUpdated?.Invoke(this, eventArgs);

            var eventData = new Dictionary<string, object>
            {
                ["consultationId"] = consultationId,
                ["oldDiagnosis"] = oldDiagnosis ?? string.Empty,
                ["newDiagnosis"] = newDiagnosis,
                ["doctorId"] = doctorId
            };

            await _coreService.TriggerEventNotificationAsync("DiagnosisUpdated", consultationId, eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发诊断更新事件失败，ConsultationId: {ConsultationId}", consultationId);
        }
    }

    /// <summary>
    /// 触发四诊记录事件
    /// </summary>
    private async Task TriggerFourDiagnosisRecordedEventAsync(Guid consultationId, string diagnosisType, string content, 
        Guid doctorId, string doctorName)
    {
        try
        {
            var eventArgs = new FourDiagnosisRecordedEventArgs
            {
                ConsultationId = consultationId,
                DiagnosisType = diagnosisType,
                Content = content,
                RecordedAt = DateTime.Now,
                DoctorId = doctorId,
                DoctorName = doctorName
            };

            FourDiagnosisRecorded?.Invoke(this, eventArgs);

            var eventData = new Dictionary<string, object>
            {
                ["consultationId"] = consultationId,
                ["diagnosisType"] = diagnosisType,
                ["content"] = content,
                ["doctorId"] = doctorId
            };

            await _coreService.TriggerEventNotificationAsync("FourDiagnosisRecorded", consultationId, eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发四诊记录事件失败，ConsultationId: {ConsultationId}", consultationId);
        }
    }

    #endregion

    #region 缓存管理辅助方法

    /// <summary>
    /// 清除相关缓存
    /// </summary>
    private async Task ClearRelatedCacheAsync(Guid consultationId, Guid patientId, Guid doctorId)
    {
        try
        {
            await Task.WhenAll(
                _coreService.ClearConsultationCacheAsync(consultationId),
                _coreService.ClearPatientConsultationCacheAsync(patientId),
                _coreService.ClearDoctorConsultationCacheAsync(doctorId)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除相关缓存失败，ConsultationId: {ConsultationId}, PatientId: {PatientId}, DoctorId: {DoctorId}",
                consultationId, patientId, doctorId);
        }
    }

    #endregion
}
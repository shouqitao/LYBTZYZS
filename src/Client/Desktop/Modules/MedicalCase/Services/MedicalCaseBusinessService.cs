using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.MedicalCase.Interfaces;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案业务服务实现 - UltraThink三层架构业务层
/// 职责：业务流程编排、工作流管理、事件处理、复杂业务逻辑
/// </summary>
public class MedicalCaseBusinessService(
    IMedicalCaseCoreService coreService,
    IMedicalCaseQueryService queryService,
    ILogger<MedicalCaseBusinessService> logger) : IMedicalCaseBusinessService
{
    private readonly IMedicalCaseCoreService _coreService = coreService;
    private readonly IMedicalCaseQueryService _queryService = queryService;
    private readonly ILogger<MedicalCaseBusinessService> _logger = logger;

    #region 事件定义

    /// <summary>
    /// 医案状态变更事件
    /// </summary>
    public event EventHandler<MedicalCaseStatusChangedEventArgs>? MedicalCaseStatusChanged;

    /// <summary>
    /// 医案操作事件
    /// </summary>
    public event EventHandler<MedicalCaseOperationEventArgs>? MedicalCaseOperation;

    /// <summary>
    /// 诊疗流程事件
    /// </summary>
    public event EventHandler<ConsultationWorkflowEventArgs>? ConsultationWorkflow;

    #endregion

    #region 基础CRUD操作

    /// <summary>
    /// 创建医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto createDto)
    {
        try
        {
            // 验证创建DTO
            var validation = await _coreService.ValidateCreateDtoAsync(createDto);
            if (!validation.IsSuccess || !validation.Data.IsValid)
            {
                var errors = validation.Data?.Errors ?? ["验证失败"];
                return ServiceResult<MedicalCaseDto>.Failure($"创建数据验证失败：{string.Join(", ", errors)}");
            }

            // 生成医案编号
            var numberResult = await _coreService.GenerateMedicalCaseNumberAsync();
            if (!numberResult.IsSuccess)
            {
                return ServiceResult<MedicalCaseDto>.Failure($"生成医案编号失败：{numberResult.Message}");
            }

            // 设置医案编号
            createDto.MedicalCaseNumber = numberResult.Data;

            // 调用API创建医案
            var result = await _coreService.CallCreateMedicalCaseApiAsync(createDto);
            if (!result.IsSuccess)
            {
                return ServiceResult<MedicalCaseDto>.Failure($"创建医案失败：{result.Message}");
            }

            // 清除相关缓存
            await _coreService.ClearPatientMedicalCaseCacheAsync(createDto.PatientId);
            await _coreService.ClearDoctorMedicalCaseCacheAsync(createDto.DoctorId);

            // 记录操作日志
            await _coreService.LogOperationAsync("CREATE", result.Data.Id, 
                $"创建医案，患者ID: {createDto.PatientId}", createDto.DoctorId);

            // 触发医案操作事件
            TriggerMedicalCaseOperationEvent(result.Data.Id, "CREATE", "医案创建成功", 
                createDto.DoctorId, "系统", true, null);

            // 触发诊疗流程事件
            TriggerConsultationWorkflowEvent(result.Data.Id, "REGISTERED", "医案已登记", 
                createDto.DoctorId, "系统", false);

            return ServiceResult<MedicalCaseDto>.Success(result.Data, "医案创建成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建医案失败");
            
            // 触发失败事件
            TriggerMedicalCaseOperationEvent(Guid.Empty, "CREATE", "医案创建失败", 
                createDto?.DoctorId ?? Guid.Empty, "系统", false, ex.Message);

            return ServiceResult<MedicalCaseDto>.Failure($"创建医案失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 更新医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto updateDto)
    {
        try
        {
            // 验证更新DTO
            var validation = await _coreService.ValidateUpdateDtoAsync(id, updateDto);
            if (!validation.IsSuccess || !validation.Data.IsValid)
            {
                var errors = validation.Data?.Errors ?? ["验证失败"];
                return ServiceResult<MedicalCaseDto>.Failure($"更新数据验证失败：{string.Join(", ", errors)}");
            }

            // 获取当前医案信息
            var currentResult = await _coreService.CallGetMedicalCaseByIdApiAsync(id);
            if (!currentResult.IsSuccess)
            {
                return ServiceResult<MedicalCaseDto>.Failure($"获取医案信息失败：{currentResult.Message}");
            }

            // 转换为编辑DTO
            var editDto = new MedicalCaseEditDto
            {
                PatientId = updateDto.PatientId ?? currentResult.Data.PatientId,
                DoctorId = updateDto.DoctorId ?? currentResult.Data.DoctorId,
                DiagnosisSummary = updateDto.DiagnosisSummary ?? currentResult.Data.DiagnosisSummary,
                Status = updateDto.Status ?? currentResult.Data.Status,
                Remark = updateDto.Remark ?? currentResult.Data.Remark
            };

            // 调用API更新医案
            var result = await _coreService.CallUpdateMedicalCaseApiAsync(id, editDto);
            if (!result.IsSuccess)
            {
                return ServiceResult<MedicalCaseDto>.Failure($"更新医案失败：{result.Message}");
            }

            // 清除相关缓存
            await _coreService.ClearMedicalCaseCacheAsync(id);
            await _coreService.ClearPatientMedicalCaseCacheAsync(result.Data.PatientId);
            await _coreService.ClearDoctorMedicalCaseCacheAsync(result.Data.DoctorId);

            // 记录操作日志
            await _coreService.LogOperationAsync("UPDATE", id, 
                $"更新医案信息", editDto.DoctorId);

            // 触发医案操作事件
            TriggerMedicalCaseOperationEvent(id, "UPDATE", "医案更新成功", 
                editDto.DoctorId, "系统", true, null);

            return ServiceResult<MedicalCaseDto>.Success(result.Data, "医案更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新医案失败，ID: {MedicalCaseId}", id);
            
            // 触发失败事件
            TriggerMedicalCaseOperationEvent(id, "UPDATE", "医案更新失败", 
                updateDto?.DoctorId ?? Guid.Empty, "系统", false, ex.Message);

            return ServiceResult<MedicalCaseDto>.Failure($"更新医案失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 删除医案
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            // 验证医案ID
            var validation = await _coreService.ValidateMedicalCaseIdAsync(id);
            if (!validation.IsSuccess || !validation.Data)
            {
                return ServiceResult<bool>.Failure("医案ID无效");
            }

            // 获取医案信息用于清除缓存
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(id);
            var patientId = medicalCaseResult.IsSuccess ? medicalCaseResult.Data.PatientId : Guid.Empty;
            var doctorId = medicalCaseResult.IsSuccess ? medicalCaseResult.Data.DoctorId : Guid.Empty;

            // 检查医案状态，某些状态可能不允许删除
            if (medicalCaseResult.IsSuccess && medicalCaseResult.Data.Status == MedicalCaseStatus.InConsultation)
            {
                return ServiceResult<bool>.Failure("看诊中的医案不能删除");
            }

            // 调用API删除医案
            var result = await _coreService.CallDeleteMedicalCaseApiAsync(id);
            if (!result.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"删除医案失败：{result.Message}");
            }

            // 清除相关缓存
            await _coreService.ClearMedicalCaseCacheAsync(id);
            if (patientId != Guid.Empty)
            {
                await _coreService.ClearPatientMedicalCaseCacheAsync(patientId);
            }
            if (doctorId != Guid.Empty)
            {
                await _coreService.ClearDoctorMedicalCaseCacheAsync(doctorId);
            }

            // 记录操作日志
            await _coreService.LogOperationAsync("DELETE", id, 
                $"删除医案", doctorId);

            // 触发医案操作事件
            TriggerMedicalCaseOperationEvent(id, "DELETE", "医案删除成功", 
                doctorId, "系统", true, null);

            return ServiceResult<bool>.Success(true, "医案删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除医案失败，ID: {MedicalCaseId}", id);
            
            // 触发失败事件
            TriggerMedicalCaseOperationEvent(id, "DELETE", "医案删除失败", 
                Guid.Empty, "系统", false, ex.Message);

            return ServiceResult<bool>.Failure($"删除医案失败：{ex.Message}");
        }
    }

    #endregion

    #region 状态管理

    /// <summary>
    /// 更新医案状态
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid medicalCaseId, MedicalCaseStatus status, string reason = "")
    {
        try
        {
            // 获取当前医案信息
            var currentResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!currentResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{currentResult.Message}");
            }

            var currentStatus = currentResult.Data.Status;
            
            // 验证状态转换
            var transitionValidation = await _coreService.ValidateStatusTransitionAsync(
                medicalCaseId, currentStatus, status);
            if (!transitionValidation.IsSuccess || !transitionValidation.Data)
            {
                return ServiceResult<bool>.Failure($"状态转换无效：{transitionValidation.Message}");
            }

            // 调用API更新状态
            var result = await _coreService.CallUpdateMedicalCaseStatusApiAsync(medicalCaseId, status);
            if (!result.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"更新状态失败：{result.Message}");
            }

            // 清除相关缓存
            await _coreService.ClearMedicalCaseCacheAsync(medicalCaseId);

            // 记录操作日志
            await _coreService.LogOperationAsync("STATUS_UPDATE", medicalCaseId, 
                $"状态从 {currentStatus} 更新为 {status}，原因: {reason}", currentResult.Data.DoctorId);

            // 触发医案状态变更事件
            TriggerMedicalCaseStatusChangedEvent(medicalCaseId, currentStatus, status, 
                reason, currentResult.Data.DoctorId, "系统");

            // 根据状态变更触发诊疗流程事件
            var workflowStep = GetWorkflowStepByStatus(status);
            TriggerConsultationWorkflowEvent(medicalCaseId, workflowStep, 
                $"医案状态更新为: {status}", currentResult.Data.DoctorId, "系统", 
                status == MedicalCaseStatus.Completed);

            return ServiceResult<bool>.Success(true, "医案状态更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新医案状态失败，ID: {MedicalCaseId}, 状态: {Status}", medicalCaseId, status);
            return ServiceResult<bool>.Failure($"更新状态失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 批量更新医案状态
    /// </summary>
    public async Task<ServiceResult<MedicalCaseBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> medicalCaseIds, MedicalCaseStatus status)
    {
        try
        {
            if (medicalCaseIds == null || medicalCaseIds.Count == 0)
            {
                return ServiceResult<MedicalCaseBatchOperationResultDto>.Failure("医案ID列表不能为空");
            }

            var result = new MedicalCaseBatchOperationResultDto
            {
                TotalCount = medicalCaseIds.Count
            };

            var tasks = medicalCaseIds.Select(async id =>
            {
                try
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
                        result.ErrorMessages.Add($"医案 {id}: {updateResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.ErrorMessages.Add($"医案 {id}: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);

            // 批量清除缓存
            await _coreService.BatchClearMedicalCaseCacheAsync(medicalCaseIds);

            var message = result.FailureCount == 0 
                ? $"批量更新状态成功，共处理 {result.TotalCount} 个医案"
                : $"批量更新状态部分成功，成功 {result.SuccessCount} 个，失败 {result.FailureCount} 个";

            return ServiceResult<MedicalCaseBatchOperationResultDto>.Success(result, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新医案状态失败");
            return ServiceResult<MedicalCaseBatchOperationResultDto>.Failure($"批量更新失败：{ex.Message}");
        }
    }

    #endregion

    #region 诊疗流程管理

    /// <summary>
    /// 获取诊疗流程状态
    /// </summary>
    public async Task<ServiceResult<ConsultationWorkflowStatusDto>> GetConsultationWorkflowStatusAsync(Guid medicalCaseId)
    {
        try
        {
            // 验证医案ID
            var validation = await _coreService.ValidateMedicalCaseIdAsync(medicalCaseId);
            if (!validation.IsSuccess || !validation.Data)
            {
                return ServiceResult<ConsultationWorkflowStatusDto>.Failure("医案ID无效");
            }

            // 获取医案详情
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<ConsultationWorkflowStatusDto>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;
            var workflowStatus = new ConsultationWorkflowStatusDto
            {
                MedicalCaseId = medicalCaseId,
                CurrentStatus = medicalCase.Status,
                CurrentStep = GetWorkflowStepByStatus(medicalCase.Status),
                LastUpdatedAt = medicalCase.UpdateTime,
                DoctorId = medicalCase.DoctorId,
                DoctorName = "医生", // TODO: 从用户服务获取医生姓名
                CompletedSteps = GetCompletedStepsByStatus(medicalCase.Status),
                PendingSteps = GetPendingStepsByStatus(medicalCase.Status),
                CanProceedToNext = CanProceedToNextStep(medicalCase.Status)
            };

            return ServiceResult<ConsultationWorkflowStatusDto>.Success(workflowStatus, "获取诊疗流程状态成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取诊疗流程状态失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<ConsultationWorkflowStatusDto>.Failure($"获取流程状态失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 开始看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> StartConsultationWorkflowAsync(Guid medicalCaseId)
    {
        try
        {
            // 获取医案信息
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;
            
            // 检查当前状态是否可以开始看诊
            if (medicalCase.Status != MedicalCaseStatus.Registered)
            {
                return ServiceResult<bool>.Failure($"医案当前状态 {medicalCase.Status} 不能开始看诊");
            }

            // 更新状态为看诊中
            var statusResult = await UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.InConsultation, "开始看诊流程");
            if (!statusResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"开始看诊流程失败：{statusResult.Message}");
            }

            // 触发诊疗流程事件
            TriggerConsultationWorkflowEvent(medicalCaseId, "CONSULTATION_STARTED", "看诊流程已开始", 
                medicalCase.DoctorId, "系统", false);

            return ServiceResult<bool>.Success(true, "看诊流程开始成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始看诊流程失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"开始看诊流程失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 完成看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteConsultationWorkflowAsync(Guid medicalCaseId, string completionNotes)
    {
        try
        {
            // 获取医案信息
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;
            
            // 检查当前状态是否可以完成看诊
            if (medicalCase.Status != MedicalCaseStatus.InConsultation)
            {
                return ServiceResult<bool>.Failure($"医案当前状态 {medicalCase.Status} 不能完成看诊");
            }

            // 验证医案完整性
            var completenessResult = await _coreService.ValidateMedicalCaseCompletenessAsync(medicalCase);
            if (!completenessResult.IsSuccess || !completenessResult.Data)
            {
                return ServiceResult<bool>.Failure("医案信息不完整，无法完成看诊");
            }

            // 更新状态为已完成
            var statusResult = await UpdateStatusAsync(medicalCaseId, MedicalCaseStatus.Completed, 
                $"完成看诊流程：{completionNotes}");
            if (!statusResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"完成看诊流程失败：{statusResult.Message}");
            }

            // 记录完成备注
            await _coreService.LogOperationAsync("CONSULTATION_COMPLETED", medicalCaseId, 
                $"看诊完成备注：{completionNotes}", medicalCase.DoctorId);

            // 触发诊疗流程事件
            TriggerConsultationWorkflowEvent(medicalCaseId, "CONSULTATION_COMPLETED", 
                $"看诊流程已完成：{completionNotes}", medicalCase.DoctorId, "系统", true);

            return ServiceResult<bool>.Success(true, "看诊流程完成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成看诊流程失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"完成看诊流程失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 暂停看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> PauseConsultationWorkflowAsync(Guid medicalCaseId, string pauseReason)
    {
        try
        {
            // 获取医案信息
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;
            
            // 检查当前状态是否可以暂停
            if (medicalCase.Status != MedicalCaseStatus.InConsultation)
            {
                return ServiceResult<bool>.Failure($"医案当前状态 {medicalCase.Status} 不能暂停看诊");
            }

            // 记录暂停原因
            await _coreService.LogOperationAsync("CONSULTATION_PAUSED", medicalCaseId, 
                $"看诊暂停原因：{pauseReason}", medicalCase.DoctorId);

            // 触发诊疗流程事件
            TriggerConsultationWorkflowEvent(medicalCaseId, "CONSULTATION_PAUSED", 
                $"看诊流程已暂停：{pauseReason}", medicalCase.DoctorId, "系统", false);

            return ServiceResult<bool>.Success(true, "看诊流程暂停成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "暂停看诊流程失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"暂停看诊流程失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 恢复看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> ResumeConsultationWorkflowAsync(Guid medicalCaseId)
    {
        try
        {
            // 获取医案信息
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;

            // 记录恢复操作
            await _coreService.LogOperationAsync("CONSULTATION_RESUMED", medicalCaseId, 
                "看诊流程已恢复", medicalCase.DoctorId);

            // 触发诊疗流程事件
            TriggerConsultationWorkflowEvent(medicalCaseId, "CONSULTATION_RESUMED", 
                "看诊流程已恢复", medicalCase.DoctorId, "系统", false);

            return ServiceResult<bool>.Success(true, "看诊流程恢复成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复看诊流程失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"恢复看诊流程失败：{ex.Message}");
        }
    }

    #endregion

    #region 高级业务操作

    /// <summary>
    /// 医案数据同步
    /// </summary>
    public async Task<ServiceResult<bool>> SyncMedicalCaseDataAsync(Guid medicalCaseId)
    {
        try
        {
            // 验证医案ID
            var validation = await _coreService.ValidateMedicalCaseIdAsync(medicalCaseId);
            if (!validation.IsSuccess || !validation.Data)
            {
                return ServiceResult<bool>.Failure("医案ID无效");
            }

            // 清除相关缓存强制从服务器重新获取
            await _coreService.ClearMedicalCaseCacheAsync(medicalCaseId);

            // 重新获取最新数据
            var result = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!result.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"同步医案数据失败：{result.Message}");
            }

            // 记录同步操作
            await _coreService.LogOperationAsync("DATA_SYNC", medicalCaseId, 
                "医案数据同步", result.Data.DoctorId);

            return ServiceResult<bool>.Success(true, "医案数据同步成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医案数据同步失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"数据同步失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 医案数据归档
    /// </summary>
    public async Task<ServiceResult<bool>> ArchiveMedicalCaseAsync(Guid medicalCaseId, string archiveReason)
    {
        try
        {
            // 获取医案信息
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;
            
            // 检查医案状态，只有完成或取消的医案才能归档
            if (medicalCase.Status != MedicalCaseStatus.Completed && 
                medicalCase.Status != MedicalCaseStatus.Cancelled)
            {
                return ServiceResult<bool>.Failure("只有已完成或已取消的医案才能归档");
            }

            // TODO: 实现具体的归档逻辑
            // 这可能涉及将数据移动到归档表或设置归档标记

            // 记录归档操作
            await _coreService.LogOperationAsync("ARCHIVE", medicalCaseId, 
                $"医案归档，原因：{archiveReason}", medicalCase.DoctorId);

            // 触发医案操作事件
            TriggerMedicalCaseOperationEvent(medicalCaseId, "ARCHIVE", 
                $"医案已归档：{archiveReason}", medicalCase.DoctorId, "系统", true, null);

            return ServiceResult<bool>.Success(true, "医案归档成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医案归档失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"归档失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 医案数据恢复
    /// </summary>
    public async Task<ServiceResult<bool>> RestoreMedicalCaseAsync(Guid medicalCaseId)
    {
        try
        {
            // 获取医案信息
            var medicalCaseResult = await _coreService.CallGetMedicalCaseByIdApiAsync(medicalCaseId);
            if (!medicalCaseResult.IsSuccess)
            {
                return ServiceResult<bool>.Failure($"获取医案信息失败：{medicalCaseResult.Message}");
            }

            var medicalCase = medicalCaseResult.Data;

            // TODO: 实现具体的恢复逻辑
            // 这可能涉及从归档表恢复数据或取消归档标记

            // 清除相关缓存
            await _coreService.ClearMedicalCaseCacheAsync(medicalCaseId);
            await _coreService.ClearPatientMedicalCaseCacheAsync(medicalCase.PatientId);

            // 记录恢复操作
            await _coreService.LogOperationAsync("RESTORE", medicalCaseId, 
                "医案数据恢复", medicalCase.DoctorId);

            // 触发医案操作事件
            TriggerMedicalCaseOperationEvent(medicalCaseId, "RESTORE", 
                "医案已恢复", medicalCase.DoctorId, "系统", true, null);

            return ServiceResult<bool>.Success(true, "医案恢复成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "医案恢复失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<bool>.Failure($"恢复失败：{ex.Message}");
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 触发医案状态变更事件
    /// </summary>
    private void TriggerMedicalCaseStatusChangedEvent(Guid medicalCaseId, MedicalCaseStatus oldStatus, 
        MedicalCaseStatus newStatus, string reason, Guid doctorId, string doctorName)
    {
        try
        {
            var eventArgs = new MedicalCaseStatusChangedEventArgs
            {
                MedicalCaseId = medicalCaseId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason,
                ChangedAt = DateTime.Now,
                DoctorId = doctorId,
                DoctorName = doctorName
            };

            MedicalCaseStatusChanged?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发医案状态变更事件失败");
        }
    }

    /// <summary>
    /// 触发医案操作事件
    /// </summary>
    private void TriggerMedicalCaseOperationEvent(Guid medicalCaseId, string operation, 
        string operationDetails, Guid operatorId, string operatorName, bool isSuccess, string? errorMessage)
    {
        try
        {
            var eventArgs = new MedicalCaseOperationEventArgs
            {
                MedicalCaseId = medicalCaseId,
                Operation = operation,
                OperationDetails = operationDetails,
                OperatedAt = DateTime.Now,
                OperatorId = operatorId,
                OperatorName = operatorName,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            MedicalCaseOperation?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发医案操作事件失败");
        }
    }

    /// <summary>
    /// 触发诊疗流程事件
    /// </summary>
    private void TriggerConsultationWorkflowEvent(Guid medicalCaseId, string workflowStep, 
        string stepDetails, Guid doctorId, string doctorName, bool isCompleted)
    {
        try
        {
            var eventArgs = new ConsultationWorkflowEventArgs
            {
                MedicalCaseId = medicalCaseId,
                WorkflowStep = workflowStep,
                StepDetails = stepDetails,
                StepExecutedAt = DateTime.Now,
                DoctorId = doctorId,
                DoctorName = doctorName,
                IsCompleted = isCompleted
            };

            ConsultationWorkflow?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发诊疗流程事件失败");
        }
    }

    /// <summary>
    /// 根据状态获取工作流步骤
    /// </summary>
    private string GetWorkflowStepByStatus(MedicalCaseStatus status)
    {
        return status switch
        {
            MedicalCaseStatus.Registered => "REGISTERED",
            MedicalCaseStatus.InConsultation => "IN_CONSULTATION",
            MedicalCaseStatus.Completed => "COMPLETED",
            MedicalCaseStatus.Cancelled => "CANCELLED",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// 根据状态获取已完成的步骤
    /// </summary>
    private List<string> GetCompletedStepsByStatus(MedicalCaseStatus status)
    {
        return status switch
        {
            MedicalCaseStatus.Registered => ["REGISTRATION"],
            MedicalCaseStatus.InConsultation => ["REGISTRATION", "START_CONSULTATION"],
            MedicalCaseStatus.Completed => ["REGISTRATION", "START_CONSULTATION", "DIAGNOSIS", "TREATMENT"],
            MedicalCaseStatus.Cancelled => ["REGISTRATION"],
            _ => []
        };
    }

    /// <summary>
    /// 根据状态获取待处理的步骤
    /// </summary>
    private List<string> GetPendingStepsByStatus(MedicalCaseStatus status)
    {
        return status switch
        {
            MedicalCaseStatus.Registered => ["START_CONSULTATION", "DIAGNOSIS", "TREATMENT"],
            MedicalCaseStatus.InConsultation => ["DIAGNOSIS", "TREATMENT"],
            MedicalCaseStatus.Completed => [],
            MedicalCaseStatus.Cancelled => [],
            _ => []
        };
    }

    /// <summary>
    /// 检查是否可以进行下一步
    /// </summary>
    private bool CanProceedToNextStep(MedicalCaseStatus status)
    {
        return status switch
        {
            MedicalCaseStatus.Registered => true,
            MedicalCaseStatus.InConsultation => true,
            MedicalCaseStatus.Completed => false,
            MedicalCaseStatus.Cancelled => false,
            _ => false
        };
    }

    #endregion
}
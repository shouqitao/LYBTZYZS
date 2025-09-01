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
/// 医案模块服务 - UltraThink三层架构纯委托层
/// 职责：统一模块入口，事件管理，模块间协调
/// </summary>
public class MedicalCaseModule(
    IMedicalCaseBusinessService businessService,
    IMedicalCaseQueryService queryService,
    IMedicalCaseCoreService coreService,
    ILogger<MedicalCaseModule> logger) : IMedicalCaseModule
{
    private readonly IMedicalCaseBusinessService _businessService = businessService;
    private readonly IMedicalCaseQueryService _queryService = queryService;
    private readonly IMedicalCaseCoreService _coreService = coreService;
    private readonly ILogger<MedicalCaseModule> _logger = logger;
    #region 事件转发

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

    #region 初始化

    /// <summary>
    /// 构造函数后初始化
    /// </summary>
    private void InitializeEventForwarding()
    {
        // 转发业务服务事件
        _businessService.MedicalCaseStatusChanged += (sender, e) => MedicalCaseStatusChanged?.Invoke(this, e);
        _businessService.MedicalCaseOperation += (sender, e) => MedicalCaseOperation?.Invoke(this, e);
        _businessService.ConsultationWorkflow += (sender, e) => ConsultationWorkflow?.Invoke(this, e);
    }

    #endregion

    #region IMedicalCaseService 基础接口委托

    /// <summary>
    /// 分页查询医案记录
    /// </summary>
    public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        => await _queryService.GetPagedAsync(query);

    /// <summary>
    /// 根据ID获取医案详情
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <summary>
    /// 创建医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto createDto)
        => await _businessService.CreateAsync(createDto);

    /// <summary>
    /// 更新医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    /// <summary>
    /// 删除医案
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteAsync(id);

    /// <summary>
    /// 根据患者ID获取医案记录
    /// </summary>
    public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.GetByPatientIdAsync(patientId);

    /// <summary>
    /// 获取患者的活跃医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        => await _queryService.GetActiveByPatientIdAsync(patientId);

    /// <summary>
    /// 搜索医案记录
    /// </summary>
    public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    /// <summary>
    /// 更新医案状态
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
    {
        var medicalCaseStatus = (MedicalCaseStatus)status;
        return await _businessService.UpdateStatusAsync(id, medicalCaseStatus, "状态更新");
    }

    /// <summary>
    /// 完成医案
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        => await _businessService.CompleteConsultationWorkflowAsync(id, completionReason);

    /// <summary>
    /// 暂停医案
    /// </summary>
    public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        => await _businessService.PauseConsultationWorkflowAsync(id, reason);

    /// <summary>
    /// 恢复医案
    /// </summary>
    public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        => await _businessService.ResumeConsultationWorkflowAsync(id);

    /// <summary>
    /// 取消看诊
    /// </summary>
    public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
    {
        return await _businessService.UpdateStatusAsync(id, MedicalCaseStatus.Cancelled, reason);
    }

    /// <summary>
    /// 归档医案
    /// </summary>
    public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        => await _businessService.ArchiveMedicalCaseAsync(id, archiveReason);

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
    {
        var result = await _queryService.GetMedicalCaseStatisticsAsync(startDate, endDate);
        return result.IsSuccess 
            ? ServiceResult<object>.Success(result.Data, result.Message)
            : ServiceResult<object>.Failure(result.Message);
    }

    /// <summary>
    /// 获取历史记录
    /// </summary>
    public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
    {
        try
        {
            // 获取患者医案历史
            var historyResult = await _queryService.GetPatientMedicalCaseHistoryAsync(id);
            if (historyResult.IsSuccess && historyResult.Data != null)
            {
                var history = historyResult.Data.Cast<object>().ToList();
                return ServiceResult<List<object>>.Success(history);
            }

            return ServiceResult<List<object>>.Success([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医案历史记录失败，ID: {MedicalCaseId}", id);
            return ServiceResult<List<object>>.Failure($"获取历史记录失败：{ex.Message}");
        }
    }

    #endregion

    #region 模块特定方法

    /// <summary>
    /// 获取医案统计摘要
    /// </summary>
    public async Task<ServiceResult<MedicalCaseStatisticsSummaryDto>> GetStatisticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        => await _queryService.GetMedicalCaseStatisticsAsync(startDate, endDate);

    /// <summary>
    /// 获取患者医案历史统计
    /// </summary>
    public async Task<ServiceResult<PatientMedicalCaseStatDto>> GetPatientMedicalCaseStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null)
        => await _queryService.GetPatientMedicalCaseStatAsync(patientId, startDate, endDate);

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    public async Task<ServiceResult<DoctorMedicalCaseStatisticsDto>> GetDoctorMedicalCaseStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
        => await _queryService.GetDoctorMedicalCaseStatisticsAsync(doctorId, startDate, endDate);

    /// <summary>
    /// 批量更新医案状态
    /// </summary>
    public async Task<ServiceResult<MedicalCaseBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> medicalCaseIds, MedicalCaseStatus status)
        => await _businessService.BatchUpdateStatusAsync(medicalCaseIds, status);

    /// <summary>
    /// 获取诊疗流程状态
    /// </summary>
    public async Task<ServiceResult<ConsultationWorkflowStatusDto>> GetConsultationWorkflowStatusAsync(Guid medicalCaseId)
        => await _businessService.GetConsultationWorkflowStatusAsync(medicalCaseId);

    /// <summary>
    /// 开始看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> StartConsultationWorkflowAsync(Guid medicalCaseId)
        => await _businessService.StartConsultationWorkflowAsync(medicalCaseId);

    /// <summary>
    /// 完成看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteConsultationWorkflowAsync(Guid medicalCaseId, string completionNotes)
        => await _businessService.CompleteConsultationWorkflowAsync(medicalCaseId, completionNotes);

    /// <summary>
    /// 暂停看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> PauseConsultationWorkflowAsync(Guid medicalCaseId, string pauseReason)
        => await _businessService.PauseConsultationWorkflowAsync(medicalCaseId, pauseReason);

    /// <summary>
    /// 恢复看诊流程
    /// </summary>
    public async Task<ServiceResult<bool>> ResumeConsultationWorkflowAsync(Guid medicalCaseId)
        => await _businessService.ResumeConsultationWorkflowAsync(medicalCaseId);

    #endregion

    #region 资源释放

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 取消事件订阅
        if (_businessService != null)
        {
            _businessService.MedicalCaseStatusChanged -= (sender, e) => MedicalCaseStatusChanged?.Invoke(this, e);
            _businessService.MedicalCaseOperation -= (sender, e) => MedicalCaseOperation?.Invoke(this, e);
            _businessService.ConsultationWorkflow -= (sender, e) => ConsultationWorkflow?.Invoke(this, e);
        }

        _logger.LogInformation("MedicalCaseModule资源已释放");
    }

    #endregion
}
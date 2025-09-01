using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Consultation.Interfaces;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊模块服务 - UltraThink三层架构纯委托层
/// 职责：统一模块入口，事件管理，模块间协调
/// </summary>
public class ConsultationModule(
    IConsultationBusinessService businessService,
    IConsultationQueryService queryService,
    IConsultationCoreService coreService,
    ILogger<ConsultationModule> logger) : IConsultationModule
{
    private readonly IConsultationBusinessService _businessService = businessService;
    private readonly IConsultationQueryService _queryService = queryService;
    private readonly IConsultationCoreService _coreService = coreService;
    private readonly ILogger<ConsultationModule> _logger = logger;

    #region 事件转发

    /// <summary>
    /// 看诊状态变更事件
    /// </summary>
    public event EventHandler<ConsultationStatusChangedEventArgs>? ConsultationStatusChanged;

    /// <summary>
    /// 看诊操作事件
    /// </summary>
    public event EventHandler<ConsultationOperationEventArgs>? ConsultationOperation;

    /// <summary>
    /// 诊断更新事件
    /// </summary>
    public event EventHandler<DiagnosisUpdatedEventArgs>? DiagnosisUpdated;

    /// <summary>
    /// 四诊记录事件
    /// </summary>
    public event EventHandler<FourDiagnosisRecordedEventArgs>? FourDiagnosisRecorded;

    #endregion

    #region 初始化

    /// <summary>
    /// 构造函数后初始化
    /// </summary>
    private void InitializeEventForwarding()
    {
        // 转发业务服务事件
        _businessService.ConsultationStatusChanged += (sender, e) => ConsultationStatusChanged?.Invoke(this, e);
        _businessService.ConsultationOperation += (sender, e) => ConsultationOperation?.Invoke(this, e);
        _businessService.DiagnosisUpdated += (sender, e) => DiagnosisUpdated?.Invoke(this, e);
        _businessService.FourDiagnosisRecorded += (sender, e) => FourDiagnosisRecorded?.Invoke(this, e);
    }

    #endregion

    #region IConsultationService 基础接口委托

    /// <summary>
    /// 开始看诊
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto startDto)
        => await _businessService.StartAsync(startDto);

    /// <summary>
    /// 更新看诊
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto updateDto)
    {
        // 转换DTO格式
        var businessUpdateDto = new ConsultationUpdateDto
        {
            DoctorId = updateDto.DoctorId,
            ChiefComplaint = updateDto.ChiefComplaint,
            Inspection = updateDto.Inspection,
            Auscultation = updateDto.AuscultationOlfaction,
            Inquiry = updateDto.Inquiry,
            Palpation = updateDto.Palpation,
            Diagnosis = updateDto.Diagnosis,
            Remarks = updateDto.Remark
        };

        return await _businessService.UpdateAsync(id, businessUpdateDto);
    }

    /// <summary>
    /// 删除看诊
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteAsync(id);

    /// <summary>
    /// 获取看诊详情
    /// </summary>
    public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <summary>
    /// 获取分页看诊列表
    /// </summary>
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        => await _queryService.GetPagedAsync(query);

    /// <summary>
    /// 搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    /// <summary>
    /// 根据患者ID获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.GetByPatientIdAsync(patientId);

    /// <summary>
    /// 根据医案ID获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        => await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);

    /// <summary>
    /// 根据医生ID获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        => await _queryService.GetByDoctorIdAsync(doctorId);

    /// <summary>
    /// 获取患者看诊历史
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        => await _queryService.GetPatientHistoryAsync(patientId);

    /// <summary>
    /// 批量删除看诊记录
    /// </summary>
    public async Task<ServiceResult> BatchDeleteAsync(List<Guid> ids)
    {
        try
        {
            var results = new List<ServiceResult<bool>>();
            foreach (var id in ids)
            {
                var result = await _businessService.DeleteAsync(id);
                results.Add(result);
            }

            var successCount = results.Count(r => r.IsSuccess);
            var failureCount = results.Count - successCount;

            if (failureCount == 0)
            {
                return ServiceResult.Success($"成功批量删除 {successCount} 条记录");
            }
            else
            {
                return ServiceResult.Failure($"批量删除部分失败，成功: {successCount}, 失败: {failureCount}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除看诊记录失败");
            return ServiceResult.Failure($"批量删除失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否可删除
    /// </summary>
    public async Task<ServiceResult<bool>> CanDeleteAsync(Guid id)
        => await _coreService.ValidateConsultationIdAsync(id);

    /// <summary>
    /// 检查是否可修改
    /// </summary>
    public async Task<ServiceResult<bool>> CanModifyAsync(Guid id)
        => await _coreService.ValidateConsultationIdAsync(id);

    /// <summary>
    /// 更新诊断信息
    /// </summary>
    public async Task<ServiceResult> UpdateDiagnosisAsync(Guid consultationId, ConsultationUpdateDto diagnosisData)
    {
        var result = await _businessService.UpdateAsync(consultationId, diagnosisData);
        return result.IsSuccess ? ServiceResult.Success("诊断信息更新成功") : ServiceResult.Failure(result.Message);
    }

    /// <summary>
    /// 完成看诊
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        => await _businessService.CompleteConsultationAsync(id, dto);

    /// <summary>
    /// 取消看诊
    /// </summary>
    public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        => await _businessService.CancelConsultationAsync(id, reason);

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        => await _coreService.CallGetStatisticsApiAsync(startDate, endDate);

    /// <summary>
    /// 根据医案ID获取四诊数据
    /// </summary>
    public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            var consultationsResult = await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);
            if (!consultationsResult.IsSuccess || consultationsResult.Data == null || consultationsResult.Data.Count == 0)
            {
                return ServiceResult<object>.Failure("未找到对应的看诊记录");
            }

            var consultation = consultationsResult.Data[0];
            var fourDiagnosis = new
            {
                Inspection = consultation.Inspection,
                Auscultation = consultation.Auscultation,
                Inquiry = consultation.Inquiry,
                Palpation = consultation.Palpation
            };

            return ServiceResult<object>.Success(fourDiagnosis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取四诊数据失败，医案ID: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<object>.Failure($"获取四诊数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存四诊数据
    /// </summary>
    public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
    {
        try
        {
            // 这里需要根据实际的四诊数据格式进行转换
            var fourDiagnosisDto = new CompleteFourDiagnosisDto();
            // TODO: 实际转换逻辑需要根据fourDiagnosisData的实际格式实现
            
            return await _businessService.SaveCompleteFourDiagnosisAsync(consultationId, fourDiagnosisDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存四诊数据失败，看诊ID: {ConsultationId}", consultationId);
            return ServiceResult<bool>.Failure($"保存四诊数据失败：{ex.Message}");
        }
    }

    #endregion

    #region 模块特定方法

    /// <summary>
    /// 获取看诊统计摘要
    /// </summary>
    public async Task<ServiceResult<ConsultationStatisticsSummaryDto>> GetStatisticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        => await _queryService.GetConsultationStatisticsAsync(startDate, endDate);

    /// <summary>
    /// 获取四诊详细信息
    /// </summary>
    public async Task<ServiceResult<FourDiagnosisDetailDto>> GetFourDiagnosisDetailAsync(Guid consultationId)
    {
        try
        {
            var consultationResult = await _queryService.GetByIdAsync(consultationId);
            if (!consultationResult.IsSuccess || consultationResult.Data == null)
            {
                return ServiceResult<FourDiagnosisDetailDto>.Failure("看诊记录不存在");
            }

            var consultation = consultationResult.Data;
            var fourDiagnosisDetail = new FourDiagnosisDetailDto
            {
                ConsultationId = consultationId,
                Inspection = consultation.Inspection ?? string.Empty,
                Auscultation = consultation.AuscultationOlfaction ?? string.Empty,
                Inquiry = consultation.Inquiry ?? string.Empty,
                Palpation = consultation.Palpation ?? string.Empty,
                LastUpdatedAt = consultation.UpdateTime ?? consultation.CreateTime,
                DoctorId = consultation.DoctorId,
                DoctorName = "系统", // TODO: 获取医生姓名
                IsComplete = !string.IsNullOrWhiteSpace(consultation.Diagnosis)
            };

            return ServiceResult<FourDiagnosisDetailDto>.Success(fourDiagnosisDetail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取四诊详细信息失败，看诊ID: {ConsultationId}", consultationId);
            return ServiceResult<FourDiagnosisDetailDto>.Failure($"获取四诊详细信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 保存完整四诊记录
    /// </summary>
    public async Task<ServiceResult<bool>> SaveCompleteFourDiagnosisAsync(Guid consultationId, CompleteFourDiagnosisDto fourDiagnosisData)
        => await _businessService.SaveCompleteFourDiagnosisAsync(consultationId, fourDiagnosisData);

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    public async Task<ServiceResult<DoctorWorkStatisticsDto>> GetDoctorWorkStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
        => await _queryService.GetDoctorWorkStatisticsAsync(doctorId, startDate, endDate);

    /// <summary>
    /// 批量更新看诊状态
    /// </summary>
    public async Task<ServiceResult<ConsultationBatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> consultationIds, ConsultationStatus status)
        => await _businessService.BatchUpdateStatusAsync(consultationIds, status);

    /// <summary>
    /// 获取患者看诊趋势
    /// </summary>
    public async Task<ServiceResult<List<PatientConsultationTrendDto>>> GetPatientConsultationTrendAsync(Guid patientId, int months = 6)
        => await _queryService.GetPatientConsultationTrendAsync(patientId, months);

    /// <summary>
    /// 智能诊断建议
    /// </summary>
    public async Task<ServiceResult<List<DiagnosisSuggestionDto>>> GetDiagnosisSuggestionsAsync(FourDiagnosisDataDto fourDiagnosisData)
    {
        try
        {
            // 这里可以实现智能诊断建议逻辑
            // 暂时返回空列表，实际需要调用AI服务或规则引擎
            var suggestions = new List<DiagnosisSuggestionDto>();
            return ServiceResult<List<DiagnosisSuggestionDto>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取诊断建议失败");
            return ServiceResult<List<DiagnosisSuggestionDto>>.Failure($"获取诊断建议失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊模板
    /// </summary>
    public async Task<ServiceResult<List<ConsultationTemplateDto>>> GetConsultationTemplatesAsync(string? category = null)
    {
        try
        {
            // 这里可以实现看诊模板获取逻辑
            // 暂时返回空列表，实际需要从配置或数据库获取
            var templates = new List<ConsultationTemplateDto>();
            return ServiceResult<List<ConsultationTemplateDto>>.Success(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊模板失败");
            return ServiceResult<List<ConsultationTemplateDto>>.Failure($"获取看诊模板失败：{ex.Message}");
        }
    }

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
            _businessService.ConsultationStatusChanged -= (sender, e) => ConsultationStatusChanged?.Invoke(this, e);
            _businessService.ConsultationOperation -= (sender, e) => ConsultationOperation?.Invoke(this, e);
            _businessService.DiagnosisUpdated -= (sender, e) => DiagnosisUpdated?.Invoke(this, e);
            _businessService.FourDiagnosisRecorded -= (sender, e) => FourDiagnosisRecorded?.Invoke(this, e);
        }

        _logger.LogInformation("ConsultationModule资源已释放");
    }

    #endregion
}
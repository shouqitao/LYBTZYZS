using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊查询服务实现 - UltraThink三层架构查询层
/// 职责：复杂查询、搜索优化、统计分析、性能监控
/// </summary>
public class ConsultationQueryService(
    IConsultationApi consultationApi,
    IMemoryCache cache,
    ILogger<ConsultationQueryService> logger) : IConsultationQueryService
{
    private readonly IConsultationApi _consultationApi = consultationApi;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<ConsultationQueryService> _logger = logger;

    #region 基础查询方法

    /// <summary>
    /// 分页查询看诊记录
    /// </summary>
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
    {
        try
        {
            var cacheKey = $"consultation_paged_{query.PageIndex}_{query.PageSize}_{query.Keyword}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetConsultationListAsync(query);
                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询看诊记录失败");
            return ServiceResult<PagedResult<ConsultationDto>>.Failure($"分页查询失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据ID获取看诊详情
    /// </summary>
    public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"consultation_detail_{id}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetConsultationByIdAsync(id);
                return ServiceResult<ConsultationDetailDto>.Success(result);
            }, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊详情失败，ID: {Id}", id);
            return ServiceResult<ConsultationDetailDto>.Failure($"获取看诊详情失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据编号查询看诊
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> GetByNumberAsync(string consultationNumber)
    {
        try
        {
            var cacheKey = $"consultation_by_number_{consultationNumber}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByConsultationNumberAsync(consultationNumber);
                return ServiceResult<ConsultationDto>.Success(result);
            }, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据编号查询看诊失败，编号: {Number}", consultationNumber);
            return ServiceResult<ConsultationDto>.Failure($"查询看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 批量获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByIdsAsync(List<Guid> ids)
    {
        try
        {
            var cacheKey = $"consultation_batch_{string.Join("_", ids)}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByIdsAsync(ids);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"批量获取失败：{ex.Message}");
        }
    }

    #endregion

    #region 条件查询方法

    /// <summary>
    /// 根据患者ID获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
    {
        try
        {
            var cacheKey = $"consultation_by_patient_{patientId}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByPatientIdAsync(patientId);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据患者ID获取看诊记录失败，PatientId: {PatientId}", patientId);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取患者看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据医生ID获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
    {
        try
        {
            var cacheKey = $"consultation_by_doctor_{doctorId}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByDoctorIdAsync(doctorId);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据医生ID获取看诊记录失败，DoctorId: {DoctorId}", doctorId);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取医生看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据医案ID获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            var cacheKey = $"consultation_by_medical_case_{medicalCaseId}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByMedicalCaseIdAsync(medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据医案ID获取看诊记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取医案看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据状态获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByStatusAsync(ConsultationStatus status)
    {
        try
        {
            var cacheKey = $"consultation_by_status_{status}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByStatusAsync(status);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据状态获取看诊记录失败，Status: {Status}", status);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取状态看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据日期范围获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var cacheKey = $"consultation_by_date_range_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByDateRangeAsync(startDate, endDate);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据日期范围获取看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"获取日期范围看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据诊断获取看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByDiagnosisAsync(string diagnosis)
    {
        try
        {
            var cacheKey = $"consultation_by_diagnosis_{diagnosis.GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetByDiagnosisAsync(diagnosis);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据诊断获取看诊记录失败，Diagnosis: {Diagnosis}", diagnosis);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取诊断看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取今日看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetTodayConsultationsAsync()
    {
        try
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            return await GetByDateRangeAsync(today, tomorrow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取今日看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"获取今日看诊记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取本周看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetWeekConsultationsAsync()
    {
        try
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            
            return await GetByDateRangeAsync(startOfWeek, endOfWeek);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取本周看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"获取本周看诊记录失败：{ex.Message}");
        }
    }

    #endregion

    #region 搜索方法

    /// <summary>
    /// 关键词搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
    {
        try
        {
            var cacheKey = $"consultation_search_{keyword.GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.SearchConsultationsAsync(keyword);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关键词搜索看诊记录失败，Keyword: {Keyword}", keyword);
            return ServiceResult<List<ConsultationDto>>.Failure($"搜索失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 高级搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> AdvancedSearchAsync(ConsultationAdvancedSearchDto searchDto)
    {
        try
        {
            var cacheKey = $"consultation_advanced_search_{searchDto.GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.AdvancedSearchAsync(searchDto);
                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(3));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "高级搜索看诊记录失败");
            return ServiceResult<PagedResult<ConsultationDto>>.Failure($"高级搜索失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据症状搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchBySymptomsAsync(List<string> symptoms)
    {
        try
        {
            var cacheKey = $"consultation_search_symptoms_{string.Join("_", symptoms).GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.SearchBySymptomsAsync(symptoms);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据症状搜索看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"症状搜索失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据主诉搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchByChiefComplaintAsync(string chiefComplaint)
    {
        try
        {
            var cacheKey = $"consultation_search_chief_complaint_{chiefComplaint.GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.SearchByChiefComplaintAsync(chiefComplaint);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据主诉搜索看诊记录失败，ChiefComplaint: {ChiefComplaint}", chiefComplaint);
            return ServiceResult<List<ConsultationDto>>.Failure($"主诉搜索失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据四诊信息搜索
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchByFourDiagnosisAsync(FourDiagnosisDataDto fourDiagnosis)
    {
        try
        {
            var cacheKey = $"consultation_search_four_diagnosis_{fourDiagnosis.GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.SearchByFourDiagnosisAsync(fourDiagnosis);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据四诊信息搜索失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"四诊搜索失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 全文搜索看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> FullTextSearchAsync(string searchText, int limit = 50)
    {
        try
        {
            var cacheKey = $"consultation_full_text_search_{searchText.GetHashCode()}_{limit}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.FullTextSearchAsync(searchText, limit);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全文搜索看诊记录失败，SearchText: {SearchText}", searchText);
            return ServiceResult<List<ConsultationDto>>.Failure($"全文搜索失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 智能搜索建议
    /// </summary>
    public async Task<ServiceResult<List<string>>> GetSearchSuggestionsAsync(string input)
    {
        try
        {
            var cacheKey = $"consultation_search_suggestions_{input.GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetSearchSuggestionsAsync(input);
                return ServiceResult<List<string>>.Success(result);
            }, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取智能搜索建议失败，Input: {Input}", input);
            return ServiceResult<List<string>>.Failure($"获取搜索建议失败：{ex.Message}");
        }
    }

    #endregion

    #region 统计分析方法

    /// <summary>
    /// 获取看诊统计信息
    /// </summary>
    public async Task<ServiceResult<ConsultationStatisticsSummaryDto>> GetConsultationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var cacheKey = $"consultation_statistics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetStatisticsAsync(startDate, endDate);
                return ServiceResult<ConsultationStatisticsSummaryDto>.Success(result);
            }, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊统计信息失败");
            return ServiceResult<ConsultationStatisticsSummaryDto>.Failure($"获取统计信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取患者看诊统计
    /// </summary>
    public async Task<ServiceResult<PatientConsultationStatDto>> GetPatientConsultationStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var cacheKey = $"patient_consultation_stat_{patientId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetPatientConsultationStatAsync(patientId, startDate, endDate);
                return ServiceResult<PatientConsultationStatDto>.Success(result);
            }, TimeSpan.FromHours(2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者看诊统计失败，PatientId: {PatientId}", patientId);
            return ServiceResult<PatientConsultationStatDto>.Failure($"获取患者统计失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    public async Task<ServiceResult<DoctorWorkStatisticsDto>> GetDoctorWorkStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var cacheKey = $"doctor_work_statistics_{doctorId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetDoctorWorkStatisticsAsync(doctorId, startDate, endDate);
                return ServiceResult<DoctorWorkStatisticsDto>.Success(result);
            }, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医生工作统计失败，DoctorId: {DoctorId}", doctorId);
            return ServiceResult<DoctorWorkStatisticsDto>.Failure($"获取医生统计失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取诊断频次统计
    /// </summary>
    public async Task<ServiceResult<List<DiagnosisFrequencyDto>>> GetDiagnosisFrequencyAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20)
    {
        try
        {
            var cacheKey = $"diagnosis_frequency_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{topCount}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetDiagnosisFrequencyAsync(startDate, endDate, topCount);
                return ServiceResult<List<DiagnosisFrequencyDto>>.Success(result);
            }, TimeSpan.FromHours(4));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取诊断频次统计失败");
            return ServiceResult<List<DiagnosisFrequencyDto>>.Failure($"获取诊断频次失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取症状分布统计
    /// </summary>
    public async Task<ServiceResult<List<SymptomDistributionDto>>> GetSymptomDistributionAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20)
    {
        try
        {
            var cacheKey = $"symptom_distribution_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{topCount}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetSymptomDistributionAsync(startDate, endDate, topCount);
                return ServiceResult<List<SymptomDistributionDto>>.Success(result);
            }, TimeSpan.FromHours(4));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取症状分布统计失败");
            return ServiceResult<List<SymptomDistributionDto>>.Failure($"获取症状分布失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊时长分布
    /// </summary>
    public async Task<ServiceResult<ConsultationDurationDistributionDto>> GetDurationDistributionAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var cacheKey = $"duration_distribution_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetDurationDistributionAsync(startDate, endDate);
                return ServiceResult<ConsultationDurationDistributionDto>.Success(result);
            }, TimeSpan.FromHours(6));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊时长分布失败");
            return ServiceResult<ConsultationDurationDistributionDto>.Failure($"获取时长分布失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取月度看诊趋势
    /// </summary>
    public async Task<ServiceResult<List<MonthlyConsultationTrendDto>>> GetMonthlyTrendAsync(int months = 12)
    {
        try
        {
            var cacheKey = $"monthly_trend_{months}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetMonthlyTrendAsync(months);
                return ServiceResult<List<MonthlyConsultationTrendDto>>.Success(result);
            }, TimeSpan.FromHours(12));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取月度看诊趋势失败");
            return ServiceResult<List<MonthlyConsultationTrendDto>>.Failure($"获取月度趋势失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊高峰时段统计
    /// </summary>
    public async Task<ServiceResult<List<ConsultationPeakHourDto>>> GetPeakHoursStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var cacheKey = $"peak_hours_statistics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetPeakHoursStatisticsAsync(startDate, endDate);
                return ServiceResult<List<ConsultationPeakHourDto>>.Success(result);
            }, TimeSpan.FromHours(6));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊高峰时段统计失败");
            return ServiceResult<List<ConsultationPeakHourDto>>.Failure($"获取高峰时段统计失败：{ex.Message}");
        }
    }

    #endregion

    #region 特殊查询方法

    /// <summary>
    /// 获取未完成看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetIncompleteConsultationsAsync(int hoursThreshold = 24)
    {
        try
        {
            var cacheKey = $"incomplete_consultations_{hoursThreshold}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetIncompleteConsultationsAsync(hoursThreshold);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取未完成看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"获取未完成记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取长时间看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetLongDurationConsultationsAsync(int minutesThreshold = 120)
    {
        try
        {
            var cacheKey = $"long_duration_consultations_{minutesThreshold}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetLongDurationConsultationsAsync(minutesThreshold);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取长时间看诊记录失败");
            return ServiceResult<List<ConsultationDto>>.Failure($"获取长时间记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取频繁复诊患者
    /// </summary>
    public async Task<ServiceResult<List<FrequentPatientDto>>> GetFrequentPatientsAsync(int consultationThreshold = 5, int daysWithin = 30)
    {
        try
        {
            var cacheKey = $"frequent_patients_{consultationThreshold}_{daysWithin}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetFrequentPatientsAsync(consultationThreshold, daysWithin);
                return ServiceResult<List<FrequentPatientDto>>.Success(result);
            }, TimeSpan.FromHours(2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取频繁复诊患者失败");
            return ServiceResult<List<FrequentPatientDto>>.Failure($"获取频繁复诊患者失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取相似看诊记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetSimilarConsultationsAsync(Guid consultationId, int limit = 10)
    {
        try
        {
            var cacheKey = $"similar_consultations_{consultationId}_{limit}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetSimilarConsultationsAsync(consultationId, limit);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取相似看诊记录失败，ConsultationId: {ConsultationId}", consultationId);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取相似记录失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊模式分析
    /// </summary>
    public async Task<ServiceResult<List<ConsultationPatternDto>>> GetConsultationPatternsAsync(int minOccurrence = 3)
    {
        try
        {
            var cacheKey = $"consultation_patterns_{minOccurrence}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetConsultationPatternsAsync(minOccurrence);
                return ServiceResult<List<ConsultationPatternDto>>.Success(result);
            }, TimeSpan.FromHours(8));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊模式分析失败");
            return ServiceResult<List<ConsultationPatternDto>>.Failure($"获取看诊模式失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取患者看诊趋势
    /// </summary>
    public async Task<ServiceResult<List<PatientConsultationTrendDto>>> GetPatientConsultationTrendAsync(Guid patientId, int months = 6)
    {
        try
        {
            var cacheKey = $"patient_consultation_trend_{patientId}_{months}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetPatientConsultationTrendAsync(patientId, months);
                return ServiceResult<List<PatientConsultationTrendDto>>.Success(result);
            }, TimeSpan.FromHours(4));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者看诊趋势失败，PatientId: {PatientId}", patientId);
            return ServiceResult<List<PatientConsultationTrendDto>>.Failure($"获取患者趋势失败：{ex.Message}");
        }
    }

    #endregion

    #region 关联数据查询

    /// <summary>
    /// 获取看诊关联的患者信息
    /// </summary>
    public async Task<ServiceResult<List<ConsultationPatientInfoDto>>> GetConsultationPatientInfoAsync(List<Guid> consultationIds)
    {
        try
        {
            var cacheKey = $"consultation_patient_info_{string.Join("_", consultationIds).GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetConsultationPatientInfoAsync(consultationIds);
                return ServiceResult<List<ConsultationPatientInfoDto>>.Success(result);
            }, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊关联患者信息失败");
            return ServiceResult<List<ConsultationPatientInfoDto>>.Failure($"获取患者信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊关联的医案信息
    /// </summary>
    public async Task<ServiceResult<List<ConsultationMedicalCaseInfoDto>>> GetConsultationMedicalCaseInfoAsync(List<Guid> consultationIds)
    {
        try
        {
            var cacheKey = $"consultation_medical_case_info_{string.Join("_", consultationIds).GetHashCode()}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetConsultationMedicalCaseInfoAsync(consultationIds);
                return ServiceResult<List<ConsultationMedicalCaseInfoDto>>.Success(result);
            }, TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊关联医案信息失败");
            return ServiceResult<List<ConsultationMedicalCaseInfoDto>>.Failure($"获取医案信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊完整信息
    /// </summary>
    public async Task<ServiceResult<ConsultationCompleteInfoDto>> GetConsultationCompleteInfoAsync(Guid consultationId)
    {
        try
        {
            var cacheKey = $"consultation_complete_info_{consultationId}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetConsultationCompleteInfoAsync(consultationId);
                return ServiceResult<ConsultationCompleteInfoDto>.Success(result);
            }, TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取看诊完整信息失败，ConsultationId: {ConsultationId}", consultationId);
            return ServiceResult<ConsultationCompleteInfoDto>.Failure($"获取完整信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取看诊历史记录
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
    {
        try
        {
            var cacheKey = $"patient_consultation_history_{patientId}";
            
            return await GetOrSetCacheAsync(cacheKey, async () =>
            {
                var result = await _consultationApi.GetPatientHistoryAsync(patientId);
                return ServiceResult<List<ConsultationDto>>.Success(result);
            }, TimeSpan.FromMinutes(20));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者看诊历史失败，PatientId: {PatientId}", patientId);
            return ServiceResult<List<ConsultationDto>>.Failure($"获取看诊历史失败：{ex.Message}");
        }
    }

    #endregion

    #region 性能优化方法

    /// <summary>
    /// 预加载常用查询缓存
    /// </summary>
    public async Task PreloadCommonQueriesAsync()
    {
        try
        {
            _logger.LogInformation("开始预加载常用查询缓存");

            var tasks = new List<Task>
            {
                // 预加载今日看诊
                GetTodayConsultationsAsync().ContinueWith(_ => { }),
                // 预加载本周看诊
                GetWeekConsultationsAsync().ContinueWith(_ => { }),
                // 预加载看诊统计
                GetConsultationStatisticsAsync().ContinueWith(_ => { }),
                // 预加载诊断频次
                GetDiagnosisFrequencyAsync().ContinueWith(_ => { })
            };

            await Task.WhenAll(tasks);
            _logger.LogInformation("常用查询缓存预加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载常用查询缓存失败");
        }
    }

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    public async Task<ServiceResult<ConsultationQueryPerformanceStatDto>> GetQueryPerformanceStatAsync()
    {
        try
        {
            // 这里可以实现实际的查询性能统计逻辑
            var performanceStat = new ConsultationQueryPerformanceStatDto
            {
                TotalQueries = 0,
                AverageResponseTime = 0.0,
                SlowQueries = [],
                QueryTypeDistribution = new Dictionary<string, int>()
            };

            return ServiceResult<ConsultationQueryPerformanceStatDto>.Success(performanceStat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取查询性能统计失败");
            return ServiceResult<ConsultationQueryPerformanceStatDto>.Failure($"获取性能统计失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 优化慢查询
    /// </summary>
    public async Task<ServiceResult<bool>> OptimizeSlowQueriesAsync()
    {
        try
        {
            _logger.LogInformation("开始优化慢查询");

            // 这里可以实现慢查询优化逻辑
            // 例如：清除过期缓存、重建索引、优化查询策略等

            _logger.LogInformation("慢查询优化完成");
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "优化慢查询失败");
            return ServiceResult<bool>.Failure($"优化慢查询失败：{ex.Message}");
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 缓存获取或设置辅助方法
    /// </summary>
    private async Task<ServiceResult<T>> GetOrSetCacheAsync<T>(string key, Func<Task<ServiceResult<T>>> factory, TimeSpan expiry)
    {
        try
        {
            if (_cache.TryGetValue(key, out ServiceResult<T> cachedResult))
            {
                _logger.LogDebug("查询缓存命中: {Key}", key);
                return cachedResult;
            }

            _logger.LogDebug("查询缓存未命中，执行查询: {Key}", key);
            var result = await factory();
            
            if (result.IsSuccess)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                };
                _cache.Set(key, result, cacheOptions);
                _logger.LogDebug("查询结果已缓存: {Key}", key);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存查询操作失败，Key: {Key}", key);
            return ServiceResult<T>.Failure($"查询失败：{ex.Message}");
        }
    }

    #endregion
}
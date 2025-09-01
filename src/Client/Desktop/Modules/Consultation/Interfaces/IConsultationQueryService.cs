using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 看诊查询服务接口 - UltraThink三层架构查询层
/// 职责：复杂查询、搜索优化、统计分析、性能监控
/// </summary>
public interface IConsultationQueryService
{
    #region 基础查询方法

    /// <summary>
    /// 分页查询看诊记录
    /// </summary>
    Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);

    /// <summary>
    /// 根据ID获取看诊详情
    /// </summary>
    Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据编号查询看诊
    /// </summary>
    Task<ServiceResult<ConsultationDto>> GetByNumberAsync(string consultationNumber);

    /// <summary>
    /// 批量获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByIdsAsync(List<Guid> ids);

    #endregion

    #region 条件查询方法

    /// <summary>
    /// 根据患者ID获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 根据医生ID获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId);

    /// <summary>
    /// 根据医案ID获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    /// <summary>
    /// 根据状态获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByStatusAsync(ConsultationStatus status);

    /// <summary>
    /// 根据日期范围获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 根据诊断获取看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetByDiagnosisAsync(string diagnosis);

    /// <summary>
    /// 获取今日看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetTodayConsultationsAsync();

    /// <summary>
    /// 获取本周看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetWeekConsultationsAsync();

    #endregion

    #region 搜索方法

    /// <summary>
    /// 关键词搜索看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 高级搜索看诊记录
    /// </summary>
    Task<ServiceResult<PagedResult<ConsultationDto>>> AdvancedSearchAsync(ConsultationAdvancedSearchDto searchDto);

    /// <summary>
    /// 根据症状搜索看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchBySymptomsAsync(List<string> symptoms);

    /// <summary>
    /// 根据主诉搜索看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchByChiefComplaintAsync(string chiefComplaint);

    /// <summary>
    /// 根据四诊信息搜索
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> SearchByFourDiagnosisAsync(FourDiagnosisDataDto fourDiagnosis);

    /// <summary>
    /// 全文搜索看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> FullTextSearchAsync(string searchText, int limit = 50);

    /// <summary>
    /// 智能搜索建议
    /// </summary>
    Task<ServiceResult<List<string>>> GetSearchSuggestionsAsync(string input);

    #endregion

    #region 统计分析方法

    /// <summary>
    /// 获取看诊统计信息
    /// </summary>
    Task<ServiceResult<ConsultationStatisticsSummaryDto>> GetConsultationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取患者看诊统计
    /// </summary>
    Task<ServiceResult<PatientConsultationStatDto>> GetPatientConsultationStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    Task<ServiceResult<DoctorWorkStatisticsDto>> GetDoctorWorkStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取诊断频次统计
    /// </summary>
    Task<ServiceResult<List<DiagnosisFrequencyDto>>> GetDiagnosisFrequencyAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20);

    /// <summary>
    /// 获取症状分布统计
    /// </summary>
    Task<ServiceResult<List<SymptomDistributionDto>>> GetSymptomDistributionAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20);

    /// <summary>
    /// 获取看诊时长分布
    /// </summary>
    Task<ServiceResult<ConsultationDurationDistributionDto>> GetDurationDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取月度看诊趋势
    /// </summary>
    Task<ServiceResult<List<MonthlyConsultationTrendDto>>> GetMonthlyTrendAsync(int months = 12);

    /// <summary>
    /// 获取看诊高峰时段统计
    /// </summary>
    Task<ServiceResult<List<ConsultationPeakHourDto>>> GetPeakHoursStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    #endregion

    #region 特殊查询方法

    /// <summary>
    /// 获取未完成看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetIncompleteConsultationsAsync(int hoursThreshold = 24);

    /// <summary>
    /// 获取长时间看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetLongDurationConsultationsAsync(int minutesThreshold = 120);

    /// <summary>
    /// 获取频繁复诊患者
    /// </summary>
    Task<ServiceResult<List<FrequentPatientDto>>> GetFrequentPatientsAsync(int consultationThreshold = 5, int daysWithin = 30);

    /// <summary>
    /// 获取相似看诊记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetSimilarConsultationsAsync(Guid consultationId, int limit = 10);

    /// <summary>
    /// 获取看诊模式分析
    /// </summary>
    Task<ServiceResult<List<ConsultationPatternDto>>> GetConsultationPatternsAsync(int minOccurrence = 3);

    /// <summary>
    /// 获取患者看诊趋势
    /// </summary>
    Task<ServiceResult<List<PatientConsultationTrendDto>>> GetPatientConsultationTrendAsync(Guid patientId, int months = 6);

    #endregion

    #region 关联数据查询

    /// <summary>
    /// 获取看诊关联的患者信息
    /// </summary>
    Task<ServiceResult<List<ConsultationPatientInfoDto>>> GetConsultationPatientInfoAsync(List<Guid> consultationIds);

    /// <summary>
    /// 获取看诊关联的医案信息
    /// </summary>
    Task<ServiceResult<List<ConsultationMedicalCaseInfoDto>>> GetConsultationMedicalCaseInfoAsync(List<Guid> consultationIds);

    /// <summary>
    /// 获取看诊完整信息
    /// </summary>
    Task<ServiceResult<ConsultationCompleteInfoDto>> GetConsultationCompleteInfoAsync(Guid consultationId);

    /// <summary>
    /// 获取看诊历史记录
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

    #endregion

    #region 性能优化方法

    /// <summary>
    /// 预加载常用查询缓存
    /// </summary>
    Task PreloadCommonQueriesAsync();

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    Task<ServiceResult<ConsultationQueryPerformanceStatDto>> GetQueryPerformanceStatAsync();

    /// <summary>
    /// 优化慢查询
    /// </summary>
    Task<ServiceResult<bool>> OptimizeSlowQueriesAsync();

    #endregion
}

/// <summary>
/// 看诊高级搜索DTO
/// </summary>
public class ConsultationAdvancedSearchDto : PagedQueryBaseDto
{
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public ConsultationStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Diagnosis { get; set; }
    public string? ChiefComplaint { get; set; }
    public List<string>? Symptoms { get; set; }
    public bool IncludeFourDiagnosis { get; set; }
}

/// <summary>
/// 患者看诊统计DTO
/// </summary>
public class PatientConsultationStatDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int TotalConsultations { get; set; }
    public int CompletedConsultations { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime FirstConsultationDate { get; set; }
    public DateTime LastConsultationDate { get; set; }
    public List<string> TopDiagnoses { get; set; } = new();
    public List<string> CommonSymptoms { get; set; } = new();
    public decimal AverageConsultationDuration { get; set; }
}

/// <summary>
/// 诊断频次DTO
/// </summary>
public class DiagnosisFrequencyDto
{
    public string Diagnosis { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<string> CommonSymptoms { get; set; } = new();
    public List<Guid> DoctorIds { get; set; } = new();
}

/// <summary>
/// 症状分布DTO
/// </summary>
public class SymptomDistributionDto
{
    public string Symptom { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public List<string> RelatedDiagnoses { get; set; } = new();
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// 看诊时长分布DTO
/// </summary>
public class ConsultationDurationDistributionDto
{
    public List<ConsultationDurationRangeDto> Ranges { get; set; } = new();
    public decimal AverageMinutes { get; set; }
    public decimal MedianMinutes { get; set; }
    public decimal MinMinutes { get; set; }
    public decimal MaxMinutes { get; set; }
}

/// <summary>
/// 看诊时长范围DTO
/// </summary>
public class ConsultationDurationRangeDto
{
    public decimal MinMinutes { get; set; }
    public decimal MaxMinutes { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// 月度看诊趋势DTO
/// </summary>
public class MonthlyConsultationTrendDto
{
    public DateTime Month { get; set; }
    public int ConsultationCount { get; set; }
    public int PatientCount { get; set; }
    public decimal AverageDuration { get; set; }
    public int CompletedCount { get; set; }
    public List<string> TopDiagnoses { get; set; } = new();
}

/// <summary>
/// 看诊高峰时段DTO
/// </summary>
public class ConsultationPeakHourDto
{
    public int Hour { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
    public decimal AverageWaitTime { get; set; }
}

/// <summary>
/// 频繁复诊患者DTO
/// </summary>
public class FrequentPatientDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int ConsultationCount { get; set; }
    public int DaysSpan { get; set; }
    public List<string> MainDiagnoses { get; set; } = new();
    public DateTime LastConsultationDate { get; set; }
}

/// <summary>
/// 看诊模式DTO
/// </summary>
public class ConsultationPatternDto
{
    public List<string> DiagnosisPattern { get; set; } = new();
    public List<string> SymptomPattern { get; set; } = new();
    public int OccurrenceCount { get; set; }
    public double Percentage { get; set; }
    public string PatternType { get; set; } = string.Empty;
}

/// <summary>
/// 看诊患者信息DTO
/// </summary>
public class ConsultationPatientInfoDto
{
    public Guid ConsultationId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
}

/// <summary>
/// 看诊医案信息DTO
/// </summary>
public class ConsultationMedicalCaseInfoDto
{
    public Guid ConsultationId { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public string MedicalCaseNumber { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// 看诊完整信息DTO
/// </summary>
public class ConsultationCompleteInfoDto
{
    public ConsultationDetailDto Consultation { get; set; } = new();
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? MedicalCaseNumber { get; set; }
    public List<string> PreviousDiagnoses { get; set; } = new();
    public List<string> RelatedConsultations { get; set; } = new();
}

/// <summary>
/// 看诊查询性能统计DTO
/// </summary>
public class ConsultationQueryPerformanceStatDto
{
    public int TotalQueries { get; set; }
    public double AverageResponseTime { get; set; }
    public List<ConsultationSlowQueryDto> SlowQueries { get; set; } = new();
    public Dictionary<string, int> QueryTypeDistribution { get; set; } = new();
}

/// <summary>
/// 看诊慢查询DTO
/// </summary>
public class ConsultationSlowQueryDto
{
    public string QueryType { get; set; } = string.Empty;
    public double ResponseTime { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string Parameters { get; set; } = string.Empty;
}
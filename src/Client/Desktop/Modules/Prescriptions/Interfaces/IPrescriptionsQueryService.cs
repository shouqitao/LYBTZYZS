using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方查询服务接口 - UltraThink三层架构查询层
/// 职责：复杂查询、搜索优化、统计分析、性能监控
/// </summary>
public interface IPrescriptionsQueryService
{
    #region 基础查询方法

    /// <summary>
    /// 分页查询处方列表
    /// </summary>
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);

    /// <summary>
    /// 根据ID获取处方详情
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据编号查询处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> GetByNumberAsync(string prescriptionNumber);

    /// <summary>
    /// 批量获取处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByIdsAsync(List<Guid> ids);

    #endregion

    #region 条件查询方法

    /// <summary>
    /// 根据患者ID获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 根据医生ID获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByDoctorIdAsync(Guid doctorId);

    /// <summary>
    /// 根据医案ID获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    /// <summary>
    /// 根据状态获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByStatusAsync(CommonStatus status);

    /// <summary>
    /// 根据处方状态获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByPrescriptionStatusAsync(PrescriptionStatus status);

    /// <summary>
    /// 根据日期范围获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 根据价格范围获取处方列表
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    #endregion

    #region 搜索方法

    /// <summary>
    /// 关键词搜索处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 高级搜索处方
    /// </summary>
    Task<ServiceResult<PagedResult<PrescriptionDto>>> AdvancedSearchAsync(PrescriptionAdvancedSearchDto searchDto);

    /// <summary>
    /// 根据诊断搜索处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> SearchByDiagnosisAsync(string diagnosis);

    /// <summary>
    /// 根据药材搜索处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> SearchByHerbAsync(Guid herbId);

    /// <summary>
    /// 根据验方搜索处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> SearchByFormulaAsync(Guid formulaId);

    /// <summary>
    /// 全文搜索处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> FullTextSearchAsync(string searchText, int limit = 50);

    /// <summary>
    /// 智能搜索建议
    /// </summary>
    Task<ServiceResult<List<string>>> GetSearchSuggestionsAsync(string input);

    #endregion

    #region 统计分析方法

    /// <summary>
    /// 获取处方统计信息
    /// </summary>
    Task<ServiceResult<PrescriptionStatisticsDto>> GetPrescriptionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取患者处方统计
    /// </summary>
    Task<ServiceResult<PatientPrescriptionStatDto>> GetPatientPrescriptionStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取医生处方统计
    /// </summary>
    Task<ServiceResult<DoctorPrescriptionStatDto>> GetDoctorPrescriptionStatAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取药材使用统计
    /// </summary>
    Task<ServiceResult<List<HerbUsageStatDto>>> GetHerbUsageStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20);

    /// <summary>
    /// 获取诊断频次统计
    /// </summary>
    Task<ServiceResult<List<DiagnosisFrequencyDto>>> GetDiagnosisFrequencyAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20);

    /// <summary>
    /// 获取处方价格分布
    /// </summary>
    Task<ServiceResult<PriceDistributionDto>> GetPriceDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取月度处方趋势
    /// </summary>
    Task<ServiceResult<List<MonthlyTrendDto>>> GetMonthlyTrendAsync(int months = 12);

    #endregion

    #region 特殊查询方法

    /// <summary>
    /// 获取重复处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetDuplicatePrescriptionsAsync(Guid patientId, TimeSpan withinPeriod);

    /// <summary>
    /// 获取高价值处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetHighValuePrescriptionsAsync(decimal minAmount, int limit = 50);

    /// <summary>
    /// 获取异常处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetAbnormalPrescriptionsAsync();

    /// <summary>
    /// 获取未完成处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetIncompletePrescriptionsAsync(int daysThreshold = 7);

    /// <summary>
    /// 获取相似处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> GetSimilarPrescriptionsAsync(Guid prescriptionId, int limit = 10);

    /// <summary>
    /// 获取常用处方模式
    /// </summary>
    Task<ServiceResult<List<PrescriptionPatternDto>>> GetCommonPrescriptionPatternsAsync(int minOccurrence = 3);

    #endregion

    #region 关联数据查询

    /// <summary>
    /// 获取处方关联的患者信息
    /// </summary>
    Task<ServiceResult<List<PrescriptionPatientInfoDto>>> GetPrescriptionPatientInfoAsync(List<Guid> prescriptionIds);

    /// <summary>
    /// 获取处方关联的医案信息
    /// </summary>
    Task<ServiceResult<List<PrescriptionMedicalCaseInfoDto>>> GetPrescriptionMedicalCaseInfoAsync(List<Guid> prescriptionIds);

    /// <summary>
    /// 获取处方完整信息
    /// </summary>
    Task<ServiceResult<PrescriptionDetailDto>> GetPrescriptionDetailAsync(Guid prescriptionId);

    #endregion

    #region 性能优化方法

    /// <summary>
    /// 预加载常用查询缓存
    /// </summary>
    Task PreloadCommonQueriesAsync();

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    Task<ServiceResult<QueryPerformanceStatDto>> GetQueryPerformanceStatAsync();

    /// <summary>
    /// 优化慢查询
    /// </summary>
    Task<ServiceResult<bool>> OptimizeSlowQueriesAsync();

    #endregion
}

/// <summary>
/// 处方高级搜索DTO
/// </summary>
public class PrescriptionAdvancedSearchDto : PagedQueryBaseDto
{
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public CommonStatus? Status { get; set; }
    public PrescriptionStatus? PrescriptionStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Diagnosis { get; set; }
    public List<Guid>? HerbIds { get; set; }
    public string? Usage { get; set; }
    public bool IncludeArchived { get; set; }
}

/// <summary>
/// 患者处方统计DTO
/// </summary>
public class PatientPrescriptionStatDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int TotalPrescriptions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public DateTime FirstPrescriptionDate { get; set; }
    public DateTime LastPrescriptionDate { get; set; }
    public List<string> TopDiagnoses { get; set; } = new();
    public List<string> TopUsedHerbs { get; set; } = new();
}

/// <summary>
/// 医生处方统计DTO
/// </summary>
public class DoctorPrescriptionStatDto
{
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int TotalPrescriptions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public int UniquePatients { get; set; }
    public List<string> TopDiagnoses { get; set; } = new();
    public List<string> TopUsedHerbs { get; set; } = new();
}

/// <summary>
/// 药材使用统计DTO
/// </summary>
public class HerbUsageStatDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public double UsagePercentage { get; set; }
}

/// <summary>
/// 诊断频次DTO
/// </summary>
public class DiagnosisFrequencyDto
{
    public string Diagnosis { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// 价格分布DTO
/// </summary>
public class PriceDistributionDto
{
    public List<PriceRangeDto> Ranges { get; set; } = new();
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal MedianAmount { get; set; }
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// 价格范围DTO
/// </summary>
public class PriceRangeDto
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// 月度趋势DTO
/// </summary>
public class MonthlyTrendDto
{
    public DateTime Month { get; set; }
    public int PrescriptionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int PatientCount { get; set; }
    public decimal AverageAmount { get; set; }
}

/// <summary>
/// 处方模式DTO
/// </summary>
public class PrescriptionPatternDto
{
    public List<Guid> HerbIds { get; set; } = new();
    public List<string> HerbNames { get; set; } = new();
    public int OccurrenceCount { get; set; }
    public double Percentage { get; set; }
    public string? CommonDiagnosis { get; set; }
}

/// <summary>
/// 处方患者信息DTO
/// </summary>
public class PrescriptionPatientInfoDto
{
    public Guid PrescriptionId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// 处方医案信息DTO
/// </summary>
public class PrescriptionMedicalCaseInfoDto
{
    public Guid PrescriptionId { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public string MedicalCaseNumber { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
}

/// <summary>
/// 查询性能统计DTO
/// </summary>
public class QueryPerformanceStatDto
{
    public int TotalQueries { get; set; }
    public double AverageResponseTime { get; set; }
    public List<SlowQueryDto> SlowQueries { get; set; } = new();
    public Dictionary<string, int> QueryTypeDistribution { get; set; } = new();
}

/// <summary>
/// 慢查询DTO
/// </summary>
public class SlowQueryDto
{
    public string QueryType { get; set; } = string.Empty;
    public double ResponseTime { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string Parameters { get; set; } = string.Empty;
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者查询服务接口 - UltraThink三层架构查询专业层
/// 职责：复杂查询、搜索、筛选、统计、报表查询
/// </summary>
public interface IPatientQueryService
{
    #region 分页和列表查询
    
    /// <summary>
    /// 分页查询患者
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query);
    
    /// <summary>
    /// 获取患者列表（无分页）
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetPatientListAsync(PatientQueryOptions? options = null);
    
    /// <summary>
    /// 根据ID列表批量获取患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetPatientsByIdsAsync(List<Guid> patientIds);
    
    /// <summary>
    /// 获取患者概要信息
    /// </summary>
    Task<ServiceResult<List<PatientSummaryDto>>> GetPatientSummariesAsync(PatientQueryOptions? options = null);
    
    #endregion
    
    #region 搜索和筛选
    
    /// <summary>
    /// 搜索患者（关键词搜索）
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> SearchPatientsAsync(PatientSearchDto searchDto);
    
    /// <summary>
    /// 按姓名搜索
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchByNameAsync(string name);
    
    /// <summary>
    /// 按手机号搜索
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchByPhoneAsync(string phone);
    
    /// <summary>
    /// 按身份证号搜索
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchByIdCardAsync(string idCard);
    
    /// <summary>
    /// 按性别筛选患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetPatientsByGenderAsync(Gender gender);
    
    /// <summary>
    /// 按年龄段筛选患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetPatientsByAgeRangeAsync(int minAge, int maxAge);
    
    /// <summary>
    /// 按状态筛选患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetPatientsByStatusAsync(bool isEnabled);
    
    /// <summary>
    /// 高级筛选患者
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPatientsWithAdvancedFilterAsync(PatientAdvancedFilterDto filter);
    
    #endregion
    
    #region 特定查询
    
    /// <summary>
    /// 根据姓名获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetPatientByNameAsync(string name);
    
    /// <summary>
    /// 根据手机号获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetPatientByPhoneAsync(string phone);
    
    /// <summary>
    /// 根据身份证号获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetPatientByIdCardAsync(string idCard);
    
    /// <summary>
    /// 获取活跃患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetActivePatientsAsync();
    
    /// <summary>
    /// 获取禁用患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetDisabledPatientsAsync();
    
    /// <summary>
    /// 获取最近注册的患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetRecentlyRegisteredPatientsAsync(int days = 30);
    
    /// <summary>
    /// 获取最近就诊的患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetRecentlyVisitedPatientsAsync(int days = 30);
    
    /// <summary>
    /// 获取长时间未就诊的患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetInactivePatientsAsync(int days = 90);
    
    #endregion
    
    #region 统计查询
    
    /// <summary>
    /// 获取患者统计信息
    /// </summary>
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync();
    
    /// <summary>
    /// 获取患者数量统计
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetPatientCountStatisticsAsync();
    
    /// <summary>
    /// 获取性别分布统计
    /// </summary>
    Task<ServiceResult<Dictionary<Gender, int>>> GetGenderDistributionAsync();
    
    /// <summary>
    /// 获取年龄分布统计
    /// </summary>
    Task<ServiceResult<Dictionary<string, int>>> GetAgeDistributionAsync();
    
    /// <summary>
    /// 获取注册趋势数据
    /// </summary>
    Task<ServiceResult<List<PatientRegistrationTrendDto>>> GetRegistrationTrendAsync(int days = 30);
    
    /// <summary>
    /// 获取就诊频次统计
    /// </summary>
    Task<ServiceResult<PatientVisitStatisticsDto>> GetPatientVisitStatisticsAsync(int days = 30);
    
    #endregion
    
    #region 查询优化和缓存
    
    /// <summary>
    /// 预加载查询缓存
    /// </summary>
    Task<ServiceResult> PreloadQueryCacheAsync();
    
    /// <summary>
    /// 清除查询缓存
    /// </summary>
    ServiceResult ClearQueryCache();
    
    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    ServiceResult<QueryPerformanceDto> GetQueryPerformanceStats();
    
    /// <summary>
    /// 优化查询索引
    /// </summary>
    Task<ServiceResult> OptimizeQueryIndexAsync();
    
    #endregion
    
    #region 导出查询
    
    /// <summary>
    /// 查询患者数据用于导出
    /// </summary>
    Task<ServiceResult<List<PatientExportDto>>> GetPatientsForExportAsync(PatientExportQueryDto query);
    
    /// <summary>
    /// 获取患者基础信息（轻量级）
    /// </summary>
    Task<ServiceResult<List<PatientBasicInfoDto>>> GetPatientBasicInfoAsync(List<Guid>? patientIds = null);
    
    /// <summary>
    /// 获取患者详细信息（完整数据）
    /// </summary>
    Task<ServiceResult<List<PatientDetailedInfoDto>>> GetPatientDetailedInfoAsync(List<Guid> patientIds);
    
    #endregion
}

/// <summary>
/// 患者查询选项
/// </summary>
public class PatientQueryOptions
{
    public bool IncludeDisabled { get; set; } = false;
    public Gender? FilterByGender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public string? SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// 患者搜索DTO
/// </summary>
public class PatientSearchDto : PagedQueryBaseDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? IdCard { get; set; }
    public Gender? Gender { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 患者高级筛选DTO
/// </summary>
public class PatientAdvancedFilterDto : PagedQueryBaseDto
{
    public List<Gender>? Genders { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastVisitAfter { get; set; }
    public DateTime? LastVisitBefore { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public List<Guid>? ExcludePatientIds { get; set; }
}

/// <summary>
/// 患者统计DTO
/// </summary>
public class PatientStatisticsDto
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int DisabledPatients { get; set; }
    public int MalePatients { get; set; }
    public int FemalePatients { get; set; }
    public int RecentRegistrations { get; set; }
    public int RecentVisits { get; set; }
    public int InactivePatients { get; set; }
    public double AverageAge { get; set; }
}

/// <summary>
/// 患者注册趋势DTO
/// </summary>
public class PatientRegistrationTrendDto
{
    public DateTime Date { get; set; }
    public int RegistrationCount { get; set; }
    public int CumulativeCount { get; set; }
}

/// <summary>
/// 患者就诊统计DTO
/// </summary>
public class PatientVisitStatisticsDto
{
    public int DailyVisits { get; set; }
    public int WeeklyVisits { get; set; }
    public int MonthlyVisits { get; set; }
    public double AverageVisitsPerPatient { get; set; }
    public DateTime LastVisitTime { get; set; }
}

/// <summary>
/// 患者概要DTO
/// </summary>
public class PatientSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public string Phone { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? LastVisitTime { get; set; }
}

/// <summary>
/// 患者导出查询DTO
/// </summary>
public class PatientExportQueryDto
{
    public List<Guid>? PatientIds { get; set; }
    public Gender? Gender { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public bool IncludePersonalInfo { get; set; } = true;
    public bool IncludeMedicalInfo { get; set; } = false;
}

/// <summary>
/// 患者导出DTO
/// </summary>
public class PatientExportDto
{
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string IdCard { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime? LastVisitTime { get; set; }
}

/// <summary>
/// 患者基础信息DTO
/// </summary>
public class PatientBasicInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public int Age { get; set; }
    public string Phone { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 患者详细信息DTO
/// </summary>
public class PatientDetailedInfoDto : PatientBasicInfoDto
{
    public string IdCard { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Profession { get; set; }
    public string? MaritalStatus { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicDiseases { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public DateTime? LastVisitTime { get; set; }
}
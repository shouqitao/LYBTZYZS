using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案查询服务接口 - UltraThink三层架构查询层
/// 职责：复杂查询、搜索优化、统计分析、性能监控
/// </summary>
public interface IMedicalCaseQueryService
{
    #region 基础查询方法

    /// <summary>
    /// 分页查询医案记录
    /// </summary>
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query);

    /// <summary>
    /// 根据ID获取医案详情
    /// </summary>
    Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据编号查询医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> GetByNumberAsync(string medicalCaseNumber);

    /// <summary>
    /// 批量获取医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByIdsAsync(List<Guid> ids);

    #endregion

    #region 条件查询方法

    /// <summary>
    /// 根据患者ID获取医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 根据医生ID获取医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByDoctorIdAsync(Guid doctorId);

    /// <summary>
    /// 根据状态获取医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByStatusAsync(MedicalCaseStatus status);

    /// <summary>
    /// 根据日期范围获取医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 根据诊断获取医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByDiagnosisAsync(string diagnosis);

    /// <summary>
    /// 获取今日医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetTodayMedicalCasesAsync();

    /// <summary>
    /// 获取本周医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetWeekMedicalCasesAsync();

    /// <summary>
    /// 获取患者的活跃医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId);

    #endregion

    #region 搜索方法

    /// <summary>
    /// 关键词搜索医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 高级搜索医案记录
    /// </summary>
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> AdvancedSearchAsync(MedicalCaseAdvancedSearchDto searchDto);

    /// <summary>
    /// 根据诊断摘要搜索医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> SearchByDiagnosisSummaryAsync(string diagnosisSummary);

    /// <summary>
    /// 根据患者姓名搜索医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> SearchByPatientNameAsync(string patientName);

    /// <summary>
    /// 全文搜索医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> FullTextSearchAsync(string searchText, int limit = 50);

    /// <summary>
    /// 智能搜索建议
    /// </summary>
    Task<ServiceResult<List<string>>> GetSearchSuggestionsAsync(string input);

    #endregion

    #region 统计分析方法

    /// <summary>
    /// 获取医案统计信息
    /// </summary>
    Task<ServiceResult<MedicalCaseStatisticsSummaryDto>> GetMedicalCaseStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取患者医案统计
    /// </summary>
    Task<ServiceResult<PatientMedicalCaseStatDto>> GetPatientMedicalCaseStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取医生工作统计
    /// </summary>
    Task<ServiceResult<DoctorMedicalCaseStatisticsDto>> GetDoctorMedicalCaseStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取诊断频次统计
    /// </summary>
    Task<ServiceResult<List<DiagnosisFrequencyDto>>> GetDiagnosisFrequencyAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20);

    /// <summary>
    /// 获取医案时长分布
    /// </summary>
    Task<ServiceResult<MedicalCaseDurationDistributionDto>> GetDurationDistributionAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取月度医案趋势
    /// </summary>
    Task<ServiceResult<List<MonthlyMedicalCaseTrendDto>>> GetMonthlyTrendAsync(int months = 12);

    /// <summary>
    /// 获取医案高峰时段统计
    /// </summary>
    Task<ServiceResult<List<MedicalCasePeakHourDto>>> GetPeakHoursStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    #endregion

    #region 特殊查询方法

    /// <summary>
    /// 获取未完成医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetIncompleteMedicalCasesAsync(int hoursThreshold = 24);

    /// <summary>
    /// 获取长时间医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetLongDurationMedicalCasesAsync(int minutesThreshold = 120);

    /// <summary>
    /// 获取频繁就诊患者
    /// </summary>
    Task<ServiceResult<List<FrequentPatientDto>>> GetFrequentPatientsAsync(int medicalCaseThreshold = 5, int daysWithin = 30);

    /// <summary>
    /// 获取相似医案记录
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetSimilarMedicalCasesAsync(Guid medicalCaseId, int limit = 10);

    /// <summary>
    /// 获取医案模式分析
    /// </summary>
    Task<ServiceResult<List<MedicalCasePatternDto>>> GetMedicalCasePatternsAsync(int minOccurrence = 3);

    /// <summary>
    /// 获取患者医案趋势
    /// </summary>
    Task<ServiceResult<List<PatientMedicalCaseTrendDto>>> GetPatientMedicalCaseTrendAsync(Guid patientId, int months = 6);

    #endregion

    #region 关联数据查询

    /// <summary>
    /// 获取医案关联的患者信息
    /// </summary>
    Task<ServiceResult<List<MedicalCasePatientInfoDto>>> GetMedicalCasePatientInfoAsync(List<Guid> medicalCaseIds);

    /// <summary>
    /// 获取医案关联的看诊信息
    /// </summary>
    Task<ServiceResult<List<MedicalCaseConsultationInfoDto>>> GetMedicalCaseConsultationInfoAsync(List<Guid> medicalCaseIds);

    /// <summary>
    /// 获取医案完整信息
    /// </summary>
    Task<ServiceResult<MedicalCaseCompleteInfoDto>> GetMedicalCaseCompleteInfoAsync(Guid medicalCaseId);

    /// <summary>
    /// 获取患者医案历史
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetPatientMedicalCaseHistoryAsync(Guid patientId);

    #endregion

    #region 性能优化方法

    /// <summary>
    /// 预加载常用查询缓存
    /// </summary>
    Task PreloadCommonQueriesAsync();

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    Task<ServiceResult<MedicalCaseQueryPerformanceStatDto>> GetQueryPerformanceStatAsync();

    /// <summary>
    /// 优化慢查询
    /// </summary>
    Task<ServiceResult<bool>> OptimizeSlowQueriesAsync();

    #endregion
}
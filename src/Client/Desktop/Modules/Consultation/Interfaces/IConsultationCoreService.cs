using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// 看诊核心服务接口 - UltraThink三层架构核心层
/// 职责：API通信、数据验证、缓存管理、基础操作
/// </summary>
public interface IConsultationCoreService
{
    #region API通信层

    /// <summary>
    /// 调用创建看诊API
    /// </summary>
    Task<ServiceResult<ConsultationDto>> CallStartConsultationApiAsync(ConsultationStartDto startDto);

    /// <summary>
    /// 调用更新看诊API
    /// </summary>
    Task<ServiceResult<ConsultationDto>> CallUpdateConsultationApiAsync(Guid id, ConsultationUpdateDto updateDto);

    /// <summary>
    /// 调用删除看诊API
    /// </summary>
    Task<ServiceResult<bool>> CallDeleteConsultationApiAsync(Guid id);

    /// <summary>
    /// 调用获取看诊详情API
    /// </summary>
    Task<ServiceResult<ConsultationDetailDto>> CallGetConsultationByIdApiAsync(Guid id);

    /// <summary>
    /// 调用获取看诊列表API
    /// </summary>
    Task<ServiceResult<PagedResult<ConsultationDto>>> CallGetConsultationListApiAsync(PagedQueryBaseDto query);

    /// <summary>
    /// 调用完成看诊API
    /// </summary>
    Task<ServiceResult<bool>> CallCompleteConsultationApiAsync(Guid id, ConsultationCompleteDto completeDto);

    /// <summary>
    /// 调用取消看诊API
    /// </summary>
    Task<ServiceResult<bool>> CallCancelConsultationApiAsync(Guid id, string reason);

    /// <summary>
    /// 调用获取统计信息API
    /// </summary>
    Task<ServiceResult<object>> CallGetStatisticsApiAsync(DateTime? startDate, DateTime? endDate);

    /// <summary>
    /// 调用搜索看诊API
    /// </summary>
    Task<ServiceResult<List<ConsultationDto>>> CallSearchConsultationsApiAsync(string keyword, int limit = 100);

    #endregion

    #region 数据验证层

    /// <summary>
    /// 验证看诊ID有效性
    /// </summary>
    Task<ServiceResult<bool>> ValidateConsultationIdAsync(Guid consultationId);

    /// <summary>
    /// 验证创建看诊DTO
    /// </summary>
    Task<ServiceResult<ConsultationValidationResult>> ValidateStartDtoAsync(ConsultationStartDto startDto);

    /// <summary>
    /// 验证更新看诊DTO
    /// </summary>
    Task<ServiceResult<ConsultationValidationResult>> ValidateUpdateDtoAsync(ConsultationUpdateDto updateDto);

    /// <summary>
    /// 验证四诊数据完整性
    /// </summary>
    Task<ServiceResult<bool>> ValidateFourDiagnosisDataAsync(CompleteFourDiagnosisDto fourDiagnosis);

    /// <summary>
    /// 验证诊断信息
    /// </summary>
    Task<ServiceResult<bool>> ValidateDiagnosisAsync(string diagnosis);

    /// <summary>
    /// 验证主诉信息
    /// </summary>
    Task<ServiceResult<bool>> ValidateChiefComplaintAsync(string chiefComplaint);

    /// <summary>
    /// 验证查询参数
    /// </summary>
    Task<ServiceResult<bool>> ValidateQueryParametersAsync(PagedQueryBaseDto query);

    /// <summary>
    /// 验证患者和医生关联
    /// </summary>
    Task<ServiceResult<bool>> ValidatePatientDoctorAssociationAsync(Guid patientId, Guid doctorId);

    /// <summary>
    /// 验证医案关联性
    /// </summary>
    Task<ServiceResult<bool>> ValidateMedicalCaseAssociationAsync(Guid consultationId, Guid medicalCaseId);

    #endregion

    #region 缓存管理层

    /// <summary>
    /// 获取或设置看诊缓存
    /// </summary>
    Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// 清除看诊缓存
    /// </summary>
    Task ClearConsultationCacheAsync(Guid consultationId);

    /// <summary>
    /// 清除患者看诊缓存
    /// </summary>
    Task ClearPatientConsultationCacheAsync(Guid patientId);

    /// <summary>
    /// 清除医生看诊缓存
    /// </summary>
    Task ClearDoctorConsultationCacheAsync(Guid doctorId);

    /// <summary>
    /// 清除医案看诊缓存
    /// </summary>
    Task ClearMedicalCaseConsultationCacheAsync(Guid medicalCaseId);

    /// <summary>
    /// 批量清除看诊缓存
    /// </summary>
    Task BatchClearConsultationCacheAsync(List<Guid> consultationIds);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    Task<ServiceResult<ConsultationCacheStatisticsDto>> GetCacheStatisticsAsync();

    /// <summary>
    /// 预加载常用看诊缓存
    /// </summary>
    Task PreloadCommonConsultationCacheAsync();

    #endregion

    #region 基础操作层

    /// <summary>
    /// 检查看诊是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckConsultationExistsAsync(Guid consultationId);

    /// <summary>
    /// 检查患者是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckPatientExistsAsync(Guid patientId);

    /// <summary>
    /// 检查医生是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckDoctorExistsAsync(Guid doctorId);

    /// <summary>
    /// 检查医案是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckMedicalCaseExistsAsync(Guid medicalCaseId);

    /// <summary>
    /// 生成看诊编号
    /// </summary>
    Task<ServiceResult<string>> GenerateConsultationNumberAsync();

    /// <summary>
    /// 格式化看诊数据
    /// </summary>
    Task<ServiceResult<ConsultationDto>> FormatConsultationDataAsync(ConsultationDto consultation);

    /// <summary>
    /// 计算看诊持续时间
    /// </summary>
    Task<ServiceResult<TimeSpan>> CalculateConsultationDurationAsync(DateTime startTime, DateTime? endTime = null);

    /// <summary>
    /// 验证看诊完整性
    /// </summary>
    Task<ServiceResult<bool>> ValidateConsultationCompletenessAsync(ConsultationDetailDto consultation);

    /// <summary>
    /// 转换DTO格式
    /// </summary>
    Task<ServiceResult<TTarget>> ConvertDtoAsync<TSource, TTarget>(TSource source) where TTarget : class, new();

    #endregion

    #region 系统集成层

    /// <summary>
    /// 记录操作日志
    /// </summary>
    Task LogOperationAsync(string operation, Guid consultationId, string details, Guid userId);

    /// <summary>
    /// 触发事件通知
    /// </summary>
    Task TriggerEventNotificationAsync(string eventType, Guid consultationId, Dictionary<string, object> eventData);

    /// <summary>
    /// 获取系统配置
    /// </summary>
    Task<ServiceResult<T>> GetSystemConfigAsync<T>(string configKey, T defaultValue);

    /// <summary>
    /// 健康检查
    /// </summary>
    Task<ServiceResult<bool>> HealthCheckAsync();

    /// <summary>
    /// 获取时间戳
    /// </summary>
    Task<DateTime> GetCurrentTimestampAsync();

    /// <summary>
    /// 发送通知
    /// </summary>
    Task SendNotificationAsync(string notificationType, Dictionary<string, object> notificationData);

    #endregion
}

/// <summary>
/// 看诊验证结果
/// </summary>
public class ConsultationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 看诊缓存统计DTO
/// </summary>
public class ConsultationCacheStatisticsDto
{
    public int TotalCacheItems { get; set; }
    public int ConsultationCacheCount { get; set; }
    public int PatientConsultationCacheCount { get; set; }
    public int DoctorConsultationCacheCount { get; set; }
    public long TotalMemoryUsage { get; set; }
    public double HitRate { get; set; }
    public DateTime LastClearTime { get; set; }
    public List<ConsultationCacheItemDto> TopCacheItems { get; set; } = new();
}

/// <summary>
/// 看诊缓存项目DTO
/// </summary>
public class ConsultationCacheItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public long Size { get; set; }
    public int HitCount { get; set; }
}
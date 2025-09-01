using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案核心服务接口 - UltraThink三层架构核心层
/// 职责：API通信、数据验证、缓存管理、基础操作
/// </summary>
public interface IMedicalCaseCoreService
{
    #region API通信层

    /// <summary>
    /// 调用创建医案API
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> CallCreateMedicalCaseApiAsync(MedicalCaseCreateDto createDto);

    /// <summary>
    /// 调用更新医案API
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> CallUpdateMedicalCaseApiAsync(Guid id, MedicalCaseEditDto editDto);

    /// <summary>
    /// 调用删除医案API
    /// </summary>
    Task<ServiceResult<bool>> CallDeleteMedicalCaseApiAsync(Guid id);

    /// <summary>
    /// 调用获取医案详情API
    /// </summary>
    Task<ServiceResult<MedicalCaseDetailDto>> CallGetMedicalCaseByIdApiAsync(Guid id);

    /// <summary>
    /// 调用获取医案列表API
    /// </summary>
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> CallGetMedicalCaseListApiAsync(int pageIndex, int pageSize);

    /// <summary>
    /// 调用更新医案状态API
    /// </summary>
    Task<ServiceResult<bool>> CallUpdateMedicalCaseStatusApiAsync(Guid id, MedicalCaseStatus status);

    #endregion

    #region 数据验证层

    /// <summary>
    /// 验证医案ID有效性
    /// </summary>
    Task<ServiceResult<bool>> ValidateMedicalCaseIdAsync(Guid medicalCaseId);

    /// <summary>
    /// 验证创建医案DTO
    /// </summary>
    Task<ServiceResult<MedicalCaseValidationResult>> ValidateCreateDtoAsync(MedicalCaseCreateDto createDto);

    /// <summary>
    /// 验证更新医案DTO
    /// </summary>
    Task<ServiceResult<MedicalCaseValidationResult>> ValidateUpdateDtoAsync(Guid id, MedicalCaseUpdateDto updateDto);

    /// <summary>
    /// 验证医案状态转换
    /// </summary>
    Task<ServiceResult<bool>> ValidateStatusTransitionAsync(Guid medicalCaseId, MedicalCaseStatus fromStatus, MedicalCaseStatus toStatus);

    /// <summary>
    /// 验证患者信息
    /// </summary>
    Task<ServiceResult<bool>> ValidatePatientInfoAsync(Guid patientId);

    /// <summary>
    /// 验证医生信息
    /// </summary>
    Task<ServiceResult<bool>> ValidateDoctorInfoAsync(Guid doctorId);

    /// <summary>
    /// 验证诊断摘要
    /// </summary>
    Task<ServiceResult<bool>> ValidateDiagnosisSummaryAsync(string diagnosisSummary);

    /// <summary>
    /// 验证查询参数
    /// </summary>
    Task<ServiceResult<bool>> ValidateQueryParametersAsync(PagedQueryBaseDto query);

    /// <summary>
    /// 验证患者和医生关联
    /// </summary>
    Task<ServiceResult<bool>> ValidatePatientDoctorAssociationAsync(Guid patientId, Guid doctorId);

    #endregion

    #region 缓存管理层

    /// <summary>
    /// 获取或设置医案缓存
    /// </summary>
    Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// 清除医案缓存
    /// </summary>
    Task ClearMedicalCaseCacheAsync(Guid medicalCaseId);

    /// <summary>
    /// 清除患者医案缓存
    /// </summary>
    Task ClearPatientMedicalCaseCacheAsync(Guid patientId);

    /// <summary>
    /// 清除医生医案缓存
    /// </summary>
    Task ClearDoctorMedicalCaseCacheAsync(Guid doctorId);

    /// <summary>
    /// 批量清除医案缓存
    /// </summary>
    Task BatchClearMedicalCaseCacheAsync(List<Guid> medicalCaseIds);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    Task<ServiceResult<MedicalCaseCacheStatisticsDto>> GetCacheStatisticsAsync();

    /// <summary>
    /// 预加载常用医案缓存
    /// </summary>
    Task PreloadCommonMedicalCaseCacheAsync();

    #endregion

    #region 基础操作层

    /// <summary>
    /// 检查医案是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckMedicalCaseExistsAsync(Guid medicalCaseId);

    /// <summary>
    /// 检查患者是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckPatientExistsAsync(Guid patientId);

    /// <summary>
    /// 检查医生是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckDoctorExistsAsync(Guid doctorId);

    /// <summary>
    /// 生成医案编号
    /// </summary>
    Task<ServiceResult<string>> GenerateMedicalCaseNumberAsync();

    /// <summary>
    /// 格式化医案数据
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> FormatMedicalCaseDataAsync(MedicalCaseDto medicalCase);

    /// <summary>
    /// 计算医案持续时间
    /// </summary>
    Task<ServiceResult<TimeSpan>> CalculateMedicalCaseDurationAsync(DateTime startTime, DateTime? endTime = null);

    /// <summary>
    /// 验证医案完整性
    /// </summary>
    Task<ServiceResult<bool>> ValidateMedicalCaseCompletenessAsync(MedicalCaseDetailDto medicalCase);

    /// <summary>
    /// 转换DTO格式
    /// </summary>
    Task<ServiceResult<TTarget>> ConvertDtoAsync<TSource, TTarget>(TSource source) where TTarget : class, new();

    #endregion

    #region 系统集成层

    /// <summary>
    /// 记录操作日志
    /// </summary>
    Task LogOperationAsync(string operation, Guid medicalCaseId, string details, Guid userId);

    /// <summary>
    /// 触发事件通知
    /// </summary>
    Task TriggerEventNotificationAsync(string eventType, Guid medicalCaseId, Dictionary<string, object> eventData);

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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方核心服务接口 - UltraThink三层架构核心层
/// 职责：API通信、数据验证、缓存管理、基础操作
/// </summary>
public interface IPrescriptionsCoreService
{
    #region API通信层

    /// <summary>
    /// 调用创建处方API
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CallCreatePrescriptionApiAsync(PrescriptionCreateDto createDto);

    /// <summary>
    /// 调用更新处方API
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CallUpdatePrescriptionApiAsync(Guid id, PrescriptionEditDto updateDto);

    /// <summary>
    /// 调用删除处方API
    /// </summary>
    Task<ServiceResult<bool>> CallDeletePrescriptionApiAsync(Guid id);

    /// <summary>
    /// 调用获取处方详情API
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> CallGetPrescriptionByIdApiAsync(Guid id);

    /// <summary>
    /// 调用获取处方列表API
    /// </summary>
    Task<ServiceResult<PagedResult<PrescriptionDto>>> CallGetPrescriptionListApiAsync(PrescriptionQueryDto query);

    /// <summary>
    /// 调用作废处方API
    /// </summary>
    Task<ServiceResult<bool>> CallCancelPrescriptionApiAsync(Guid id);

    /// <summary>
    /// 调用处方搜索API
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> CallSearchPrescriptionsApiAsync(string keyword, int limit = 100);

    /// <summary>
    /// 调用批量获取处方API
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> CallGetPrescriptionsByIdsApiAsync(List<Guid> ids);

    #endregion

    #region 数据验证层

    /// <summary>
    /// 验证处方ID有效性
    /// </summary>
    Task<ServiceResult<bool>> ValidatePrescriptionIdAsync(Guid prescriptionId);

    /// <summary>
    /// 验证创建处方DTO
    /// </summary>
    Task<ServiceResult<PrescriptionValidationResult>> ValidateCreateDtoAsync(PrescriptionCreateDto createDto);

    /// <summary>
    /// 验证编辑处方DTO
    /// </summary>
    Task<ServiceResult<PrescriptionValidationResult>> ValidateEditDtoAsync(PrescriptionEditDto editDto);

    /// <summary>
    /// 验证查询参数
    /// </summary>
    Task<ServiceResult<bool>> ValidateQueryParametersAsync(PrescriptionQueryDto query);

    /// <summary>
    /// 验证处方项目数据
    /// </summary>
    Task<ServiceResult<bool>> ValidatePrescriptionItemsAsync(List<PrescriptionItemCreateDto> items);

    /// <summary>
    /// 验证药材数据完整性
    /// </summary>
    Task<ServiceResult<bool>> ValidateHerbDataAsync(Guid herbId, string herbName);

    /// <summary>
    /// 验证价格数据合理性
    /// </summary>
    Task<ServiceResult<bool>> ValidatePriceDataAsync(decimal unitPrice, decimal quantity, decimal subtotal);

    /// <summary>
    /// 验证剂量数据
    /// </summary>
    Task<ServiceResult<bool>> ValidateDosageDataAsync(int dosageCount, decimal totalAmount);

    /// <summary>
    /// 验证用法用量
    /// </summary>
    Task<ServiceResult<bool>> ValidateUsageInstructionAsync(string usage, string advice);

    #endregion

    #region 缓存管理层

    /// <summary>
    /// 获取或设置处方缓存
    /// </summary>
    Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// 清除处方缓存
    /// </summary>
    Task ClearPrescriptionCacheAsync(Guid prescriptionId);

    /// <summary>
    /// 清除患者处方缓存
    /// </summary>
    Task ClearPatientPrescriptionCacheAsync(Guid patientId);

    /// <summary>
    /// 清除医案处方缓存
    /// </summary>
    Task ClearMedicalCasePrescriptionCacheAsync(Guid medicalCaseId);

    /// <summary>
    /// 批量清除处方缓存
    /// </summary>
    Task BatchClearPrescriptionCacheAsync(List<Guid> prescriptionIds);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    Task<ServiceResult<CacheStatisticsDto>> GetCacheStatisticsAsync();

    /// <summary>
    /// 预加载常用处方缓存
    /// </summary>
    Task PreloadCommonPrescriptionCacheAsync();

    #endregion

    #region 基础操作层

    /// <summary>
    /// 检查处方是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckPrescriptionExistsAsync(Guid prescriptionId);

    /// <summary>
    /// 检查患者是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckPatientExistsAsync(Guid patientId);

    /// <summary>
    /// 检查医生是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckDoctorExistsAsync(Guid doctorId);

    /// <summary>
    /// 检查药材是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckHerbExistsAsync(Guid herbId);

    /// <summary>
    /// 检查医案是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckMedicalCaseExistsAsync(Guid medicalCaseId);

    /// <summary>
    /// 生成处方编号
    /// </summary>
    Task<ServiceResult<string>> GeneratePrescriptionNumberAsync();

    /// <summary>
    /// 格式化处方数据
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> FormatPrescriptionDataAsync(PrescriptionDto prescription);

    /// <summary>
    /// 计算处方基础价格
    /// </summary>
    Task<ServiceResult<decimal>> CalculateBasicPriceAsync(List<PrescriptionItemCreateDto> items);

    /// <summary>
    /// 验证处方完整性
    /// </summary>
    Task<ServiceResult<bool>> ValidatePrescriptionCompletenessAsync(PrescriptionDto prescription);

    #endregion

    #region 系统集成层

    /// <summary>
    /// 记录操作日志
    /// </summary>
    Task LogOperationAsync(string operation, Guid prescriptionId, string details, Guid userId);

    /// <summary>
    /// 触发事件通知
    /// </summary>
    Task TriggerEventNotificationAsync(string eventType, Guid prescriptionId, Dictionary<string, object> eventData);

    /// <summary>
    /// 获取系统配置
    /// </summary>
    Task<ServiceResult<T>> GetSystemConfigAsync<T>(string configKey, T defaultValue);

    /// <summary>
    /// 健康检查
    /// </summary>
    Task<ServiceResult<bool>> HealthCheckAsync();

    #endregion
}

/// <summary>
/// 缓存统计信息DTO
/// </summary>
public class CacheStatisticsDto
{
    public int TotalCacheItems { get; set; }
    public int PrescriptionCacheCount { get; set; }
    public int PatientPrescriptionCacheCount { get; set; }
    public long TotalMemoryUsage { get; set; }
    public double HitRate { get; set; }
    public DateTime LastClearTime { get; set; }
    public List<CacheItemDto> TopCacheItems { get; set; } = new();
}

/// <summary>
/// 缓存项目DTO
/// </summary>
public class CacheItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public long Size { get; set; }
    public int HitCount { get; set; }
}
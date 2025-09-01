using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者核心服务接口 - UltraThink三层架构核心操作层
/// 职责：API通信、基础CRUD操作、数据验证、缓存管理
/// </summary>
public interface IPatientCoreService
{
    #region API通信操作
    
    /// <summary>
    /// 调用创建患者API
    /// </summary>
    Task<ServiceResult<PatientDto>> CallCreatePatientApiAsync(PatientCreateDto createDto);
    
    /// <summary>
    /// 调用更新患者API
    /// </summary>
    Task<ServiceResult<PatientDto>> CallUpdatePatientApiAsync(Guid id, PatientUpdateDto updateDto);
    
    /// <summary>
    /// 调用删除患者API
    /// </summary>
    Task<ServiceResult<bool>> CallDeletePatientApiAsync(Guid id);
    
    /// <summary>
    /// 调用获取患者详情API
    /// </summary>
    Task<ServiceResult<PatientDto>> CallGetPatientByIdApiAsync(Guid id);
    
    /// <summary>
    /// 调用获取患者列表API
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> CallGetPatientsApiAsync(int page, int pageSize, string? keyword = null);
    
    /// <summary>
    /// 调用切换患者状态API
    /// </summary>
    Task<ServiceResult<bool>> CallTogglePatientStatusApiAsync(Guid id);
    
    #endregion
    
    #region 基础数据操作
    
    /// <summary>
    /// 获取患者信息（带缓存）
    /// </summary>
    Task<ServiceResult<PatientDto>> GetPatientByIdAsync(Guid id);
    
    /// <summary>
    /// 获取所有患者（带缓存）
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetAllPatientsAsync();
    
    /// <summary>
    /// 验证患者是否存在
    /// </summary>
    Task<ServiceResult<bool>> ValidatePatientExistsAsync(Guid id);
    
    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckPhoneExistsAsync(string phone, Guid? excludeId = null);
    
    /// <summary>
    /// 检查身份证号是否存在
    /// </summary>
    Task<ServiceResult<bool>> CheckIdCardExistsAsync(string idCard, Guid? excludeId = null);
    
    #endregion
    
    #region 数据验证操作
    
    /// <summary>
    /// 验证患者创建数据
    /// </summary>
    ServiceResult ValidatePatientCreateData(PatientCreateDto createDto);
    
    /// <summary>
    /// 验证患者更新数据
    /// </summary>
    ServiceResult ValidatePatientUpdateData(PatientUpdateDto updateDto);
    
    /// <summary>
    /// 验证患者姓名格式
    /// </summary>
    ServiceResult ValidatePatientName(string? name);
    
    /// <summary>
    /// 验证手机号格式
    /// </summary>
    ServiceResult ValidatePhone(string? phone);
    
    /// <summary>
    /// 验证身份证号格式
    /// </summary>
    ServiceResult ValidateIdCard(string? idCard);
    
    /// <summary>
    /// 验证患者基础信息
    /// </summary>
    ServiceResult ValidatePatientBasicInfo(string? name, string? phone, string? idCard);
    
    #endregion
    
    #region 患者状态管理
    
    /// <summary>
    /// 更新患者状态
    /// </summary>
    void UpdatePatientStatus(Guid patientId, bool isEnabled);
    
    /// <summary>
    /// 批量更新患者状态
    /// </summary>
    Task<ServiceResult<int>> BatchUpdatePatientStatusAsync(List<Guid> patientIds, bool isEnabled);
    
    /// <summary>
    /// 获取患者状态信息
    /// </summary>
    ServiceResult<PatientStatusInfo> GetPatientStatusInfo(Guid patientId);
    
    #endregion
    
    #region 缓存和性能优化
    
    /// <summary>
    /// 预加载常用患者数据
    /// </summary>
    Task<ServiceResult> PreloadCommonPatientsAsync();
    
    /// <summary>
    /// 清除患者缓存
    /// </summary>
    ServiceResult ClearPatientCache();
    
    /// <summary>
    /// 获取缓存的患者数据
    /// </summary>
    ServiceResult<List<PatientDto>> GetCachedPatients();
    
    /// <summary>
    /// 刷新患者缓存
    /// </summary>
    Task<ServiceResult> RefreshPatientCacheAsync(Guid patientId);
    
    #endregion
}

/// <summary>
/// 患者状态信息
/// </summary>
public class PatientStatusInfo
{
    public bool IsEnabled { get; set; }
    public DateTime LastVisitTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public int VisitCount { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
}
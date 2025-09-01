using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者模块统一接口 - UltraThink三层架构统一入口
/// 继承共享接口以保持向后兼容性
/// </summary>
public interface IPatientModule : LYBT.Shared.Interfaces.Services.IPatientService
{
    #region 模块特定方法（不在共享接口中）
    
    /// <summary>
    /// 根据姓名获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByNameAsync(string name);
    
    /// <summary>
    /// 根据手机号获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByPhoneAsync(string phone);
    
    /// <summary>
    /// 根据身份证号获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard);
    
    /// <summary>
    /// 搜索患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 获取活跃患者列表
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetActivePatientsAsync();
    
    /// <summary>
    /// 完善患者档案
    /// </summary>
    Task<ServiceResult<PatientDto>> CompletePatientProfileAsync(Guid patientId, PatientProfileDto profileDto);
    
    /// <summary>
    /// 记录患者就诊
    /// </summary>
    Task<ServiceResult> RecordPatientVisitAsync(Guid patientId, PatientVisitDto visitInfo);
    
    /// <summary>
    /// 获取患者就诊历史
    /// </summary>
    Task<ServiceResult<List<PatientVisitHistoryDto>>> GetPatientVisitHistoryAsync(Guid patientId);
    
    /// <summary>
    /// 导入患者数据
    /// </summary>
    Task<ServiceResult<PatientImportResultDto>> ImportPatientsAsync(PatientImportDto importDto);
    
    /// <summary>
    /// 导出患者数据
    /// </summary>
    Task<ServiceResult<PatientExportResultDto>> ExportPatientsAsync(PatientExportQueryDto exportQuery);
    
    /// <summary>
    /// 批量启用患者
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
    
    /// <summary>
    /// 批量禁用患者
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);
    
    /// <summary>
    /// 获取患者统计信息
    /// </summary>
    Task<ServiceResult<PatientStatisticsDto>> GetPatientStatisticsAsync();
    
    /// <summary>
    /// 检查手机号可用性
    /// </summary>
    Task<ServiceResult<bool>> CheckPhoneAvailabilityAsync(string phone, Guid? excludePatientId = null);
    
    /// <summary>
    /// 检查身份证号可用性
    /// </summary>
    Task<ServiceResult<bool>> CheckIdCardAvailabilityAsync(string idCard, Guid? excludePatientId = null);
    
    #endregion
}
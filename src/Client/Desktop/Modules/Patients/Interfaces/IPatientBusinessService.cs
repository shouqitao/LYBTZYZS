using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者业务服务接口 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public interface IPatientBusinessService
{
    /// <summary>
    /// 创建患者
    /// </summary>
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto);
    
    /// <summary>
    /// 更新患者
    /// </summary>
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto);
    
    /// <summary>
    /// 启用患者
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid patientId);
    
    /// <summary>
    /// 禁用患者
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid patientId);
    
    /// <summary>
    /// 删除患者
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid patientId);
}
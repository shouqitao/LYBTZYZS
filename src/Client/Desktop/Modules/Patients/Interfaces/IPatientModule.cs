using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// </summary>
public interface IPatientModule
{
    /// <summary>
    /// 分页查询患者
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query);
    
    /// <summary>
    /// 根据ID获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 搜索患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 获取患者统计
    /// </summary>
    Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync();
    
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
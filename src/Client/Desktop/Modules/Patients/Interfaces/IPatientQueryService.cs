using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IPatientQueryService
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
}
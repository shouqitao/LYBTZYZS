using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IPrescriptionsQueryService
{
    /// <summary>
    /// 分页查询处方
    /// </summary>
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);
    
    /// <summary>
    /// 根据ID获取处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 搜索处方
    /// </summary>
    Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 获取处方统计
    /// </summary>
    Task<ServiceResult<PrescriptionStatisticsDto>> GetStatisticsAsync();
}
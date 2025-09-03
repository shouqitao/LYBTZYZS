using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 药材查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IHerbQueryService
{
    #region 基础查询操作
    
    /// <summary>
    /// 分页查询药材
    /// </summary>
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query);
    
    /// <summary>
    /// 根据ID获取药材
    /// </summary>
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    
    /// <summary>
    /// 搜索药材
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
    
    /// <summary>
    /// 获取药材统计
    /// </summary>
    Task<ServiceResult<HerbStatisticsDto>> GetStatisticsAsync();
    
    /// <summary>
    /// 批量获取药材（用于处方）
    /// </summary>
    Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);
    
    /// <summary>
    /// 获取药材统计（详细版本）
    /// </summary>
    Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync();
    
    #endregion
}
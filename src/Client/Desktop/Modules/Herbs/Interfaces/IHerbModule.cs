using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Interfaces;

/// <summary>
/// 药材模块接口 - UltraThink双层架构简化版
/// 职责：统一服务入口，纯委托模式
/// </summary>
public interface IHerbModule : IDisposable
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
    
    #endregion
    
    #region 基础业务操作
    
    /// <summary>
    /// 创建药材
    /// </summary>
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto);
    
    /// <summary>
    /// 更新药材
    /// </summary>
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto);
    
    /// <summary>
    /// 启用药材
    /// </summary>
    Task<ServiceResult<bool>> EnableAsync(Guid herbId);
    
    /// <summary>
    /// 禁用药材
    /// </summary>
    Task<ServiceResult<bool>> DisableAsync(Guid herbId);
    
    /// <summary>
    /// 删除药材
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid herbId);
    
    #endregion
}
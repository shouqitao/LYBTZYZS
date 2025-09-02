using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 药材模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 简化版本：仅支持基础操作
/// </summary>
public class HerbModule(
    IHerbQueryService queryService,
    IHerbBusinessService businessService) : IHerbModule, IDisposable
{
    private readonly IHerbQueryService _queryService = queryService;
    private readonly IHerbBusinessService _businessService = businessService;

    #region 基础查询操作 - 对应简化接口

    /// <summary>
    /// 分页查询药材
    /// </summary>
    public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query)
        => await _queryService.GetPagedAsync(query);

    /// <summary>
    /// 根据ID获取药材
    /// </summary>
    public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <summary>
    /// 搜索药材
    /// </summary>
    public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    /// <summary>
    /// 获取药材统计
    /// </summary>
    public async Task<ServiceResult<HerbStatisticsDto>> GetStatisticsAsync()
        => await _queryService.GetStatisticsAsync();

    #endregion

    #region 基础业务操作 - 对应简化接口

    /// <summary>
    /// 创建药材
    /// </summary>
    public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto)
        => await _businessService.CreateAsync(createDto);

    /// <summary>
    /// 更新药材
    /// </summary>
    public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    /// <summary>
    /// 启用药材
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid herbId)
        => await _businessService.EnableAsync(herbId);

    /// <summary>
    /// 禁用药材
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid herbId)
        => await _businessService.DisableAsync(herbId);

    /// <summary>
    /// 删除药材
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid herbId)
        => await _businessService.DeleteAsync(herbId);

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 简化版本无需特殊清理
        GC.SuppressFinalize(this);
    }

    #endregion
}
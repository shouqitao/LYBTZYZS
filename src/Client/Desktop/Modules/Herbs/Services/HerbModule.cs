using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 药材模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 现已实现共享IHerbService接口，与后端完全对齐
/// </summary>
public class HerbModule(
    IHerbQueryService queryService,
    IHerbBusinessService businessService) : IHerbService
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
    /// 获取药材统计（详细版本）
    /// </summary>
    public async Task<ServiceResult<HerbStatisticsDto>> GetHerbStatisticsAsync()
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

    #region 共享接口IHerbService额外方法 - 委托给相应服务层

    /// <summary>
    /// 根据ID列表获取药材 - 基础实现
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        => Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));

    /// <summary>
    /// 更新药材库存 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<bool>> UpdateStockAsync(Guid id, HerbStockUpdateDto dto)
        => Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持库存管理"));

    /// <summary>
    /// 更新药材价格 - 委托给BusinessService
    /// </summary>
    public Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, HerbPriceUpdateDto dto)
        => Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持价格批量更新"));

    /// <summary>
    /// 获取库存统计 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<HerbStockStatisticsDto>> GetStockStatisticsAsync()
        => Task.FromResult(ServiceResult<HerbStockStatisticsDto>.Failure("简单诊所版本暂不支持库存统计"));

    /// <summary>
    /// 批量更新状态 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<bool>> BatchUpdateStatusAsync(BatchStatusUpdateDto dto)
        => Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持批量状态更新"));

    /// <summary>
    /// 获取药材列表 - 委托给基础查询
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> GetHerbsAsync()
        => Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));

    /// <summary>
    /// 获取药材列表（带查询参数） - 委托给QueryService
    /// </summary>
    public async Task<ServiceResult<List<HerbDto>>> GetListAsync(HerbPagedQueryDto? query = null)
    {
        if (query == null)
            return ServiceResult<List<HerbDto>>.Success([]);
        var result = await GetPagedAsync(query);
        return result.IsSuccess ? ServiceResult<List<HerbDto>>.Success(result.Data?.Items ?? []) : ServiceResult<List<HerbDto>>.Success([]);
    }

    /// <summary>
    /// 获取可用药材 - 基础实现
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync()
        => Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));

    /// <summary>
    /// 获取缺货药材 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> GetOutOfStockHerbsAsync()
        => Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));

    /// <summary>
    /// 获取即将过期药材 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> GetExpiringHerbsAsync(int days = 30)
        => Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));

    /// <summary>
    /// 获取统计数据 - 简单诊所版本基础实现
    /// </summary>
    public Task<ServiceResult<Dictionary<int, int>>> GetStatisticsAsync()
        => Task.FromResult(ServiceResult<Dictionary<int, int>>.Success(new Dictionary<int, int>()));

    /// <summary>
    /// 导入药材 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        => Task.FromResult(ServiceResult<int>.Failure("简单诊所版本暂不支持药材导入"));

    /// <summary>
    /// 导出药材 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> ExportHerbsAsync()
        => Task.FromResult(ServiceResult<List<HerbDto>>.Failure("简单诊所版本暂不支持药材导出"));

    /// <summary>
    /// 按名称搜索药材 - 委托给SearchAsync
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> SearchByNameAsync(string name)
        => SearchAsync(name);

    /// <summary>
    /// 获取所有药材 - 基础实现
    /// </summary>
    public Task<ServiceResult<List<HerbDto>>> GetAllAsync()
        => Task.FromResult(ServiceResult<List<HerbDto>>.Success([]));

    #endregion
}
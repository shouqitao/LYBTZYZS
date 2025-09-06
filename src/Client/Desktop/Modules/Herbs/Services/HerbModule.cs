using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 中药材管理模块 - UltraThink双层架构纯委托层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：统一服务入口，请求路由分发到QueryService和BusinessService
/// 实现IHerbService接口，与后端标准完全对齐
/// 集成中药材档案管理、用法用量、价格管理、Excel导入导出功能
/// 适配中医诊所药材管理需求，确保药材信息准确和处方选择便利性
/// </summary>
public class HerbModule(
    IHerbQueryService queryService,
    IHerbBusinessService businessService) : IHerbService, IDisposable {
    private readonly IHerbQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IHerbBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

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

    #endregion 基础查询操作 - 对应简化接口

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
    /// 启用药材 (IHerbService版本)
    /// </summary>
    async Task<ServiceResult> IHerbService.EnableAsync(Guid herbId) {
        var result = await _businessService.EnableAsync(herbId);
        return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "启用失败");
    }

    /// <summary>
    /// 禁用药材 (IHerbService版本)
    /// </summary>
    async Task<ServiceResult> IHerbService.DisableAsync(Guid herbId) {
        var result = await _businessService.DisableAsync(herbId);
        return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "禁用失败");
    }

    /// <summary>
    /// 启用药材 (IHerbModule版本)
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid herbId) {
        var result = await _businessService.EnableAsync(herbId);
        return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "启用失败");
    }

    /// <summary>
    /// 禁用药材 (IHerbModule版本)
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid herbId) {
        var result = await _businessService.DisableAsync(herbId);
        return result.IsSuccess ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.ErrorMessage ?? "禁用失败");
    }

    /// <summary>
    /// 删除药材
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid herbId)
        => await _businessService.DeleteAsync(herbId);

    #endregion 基础业务操作 - 对应简化接口

    #region 批量操作 - 必需功能（用户明确需求）

    /// <summary>
    /// 批量导入药材
    /// </summary>
    public async Task<ServiceResult<object>> ImportHerbsAsync(List<HerbCreateDto> herbs)
        => await _businessService.ImportHerbsAsync(herbs);

    /// <summary>
    /// 导出药材数据
    /// </summary>
    public async Task<ServiceResult<byte[]>> ExportHerbsAsync(PagedQueryBaseDto query)
        => await _businessService.ExportHerbsAsync(query);

    /// <summary>
    /// 批量获取药材（用于处方）
    /// </summary>
    public async Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids)
        => await _queryService.GetByIdsAsync(ids);

    /// <summary>
    /// 获取药材统计 (IHerbModule接口)
    /// </summary>
    public async Task<ServiceResult<HerbStatisticsDto>> GetStatisticsAsync()
        => await _queryService.GetHerbStatisticsAsync();

    #endregion 批量操作 - 必需功能（用户明确需求）

    #region IDisposable Support

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose() {
        // 简单诊所版本：无资源需要释放
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}

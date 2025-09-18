using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{

    /// <summary>
    /// 中药材查询服务接口 - UltraThink双层架构Query层抽象
    /// 职责：分页查询、搜索、筛选等查询相关功能
    /// </summary>
    public interface IHerbQueryService
    {

        /// <summary>
        /// 获取所有中药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetAllAsync();

        /// <summary>
        /// 分页查询中药材
        /// </summary>
        Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbPagedQueryDto query);

        /// <summary>
        /// 关键词搜索中药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取可用的中药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetAvailableHerbsAsync();

        /// <summary>
        /// 根据ID列表获取中药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetByIdsAsync(List<Guid> ids);

        /// <summary>
        /// 按价格区间查询中药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

        /// <summary>
        /// 根据名称获取中药材
        /// </summary>
        Task<ServiceResult<HerbDto>> GetByNameAsync(string name);

        /// <summary>
        /// 获取热门中药材
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> GetPopularHerbsAsync(int count = 20);
    }
}
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材只读仓储接口 - 专门为QueryService提供数据访问
    /// 继承IReadOnlyRepository提供基础查询功能，扩展药材特定的查询方法
    /// </summary>
    public interface IHerbReadRepository : IReadOnlyRepository<LYBT.Entities.Herbs.Herb>
    {
        /// <summary>
        /// 根据ID获取药材详情DTO
        /// </summary>
        Task<HerbDto?> GetHerbDtoByIdAsync(Guid id);

        /// <summary>
        /// 获取所有启用状态的药材DTO列表
        /// </summary>
        Task<List<HerbDto>> GetAllHerbDtosAsync();

        /// <summary>
        /// 分页查询药材并映射为DTO
        /// </summary>
        Task<PagedResult<HerbDto>> GetPagedHerbDtosAsync(HerbSearchDto query);

        /// <summary>
        /// 搜索药材并映射为DTO（根据名称、拼音码）
        /// </summary>
        Task<List<HerbDto>> SearchHerbDtosAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 获取可用药材DTO列表（状态为启用）
        /// </summary>
        Task<List<HerbDto>> GetAvailableHerbDtosAsync();

        /// <summary>
        /// 根据ID列表批量获取药材DTO
        /// </summary>
        Task<List<HerbDto>> GetHerbDtosByIdsAsync(List<Guid> ids);

        /// <summary>
        /// 按价格区间查询药材DTO
        /// </summary>
        Task<List<HerbDto>> GetHerbDtosByPriceRangeAsync(decimal minPrice, decimal maxPrice);

        /// <summary>
        /// 根据名称精确查找药材DTO
        /// </summary>
        Task<HerbDto?> GetHerbDtoByNameAsync(string name);

        /// <summary>
        /// 获取热门药材DTO（按使用频率排序）
        /// </summary>
        Task<List<HerbDto>> GetPopularHerbDtosAsync(int count = 20);
    }
}
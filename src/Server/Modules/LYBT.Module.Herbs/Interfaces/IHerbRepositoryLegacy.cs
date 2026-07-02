using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材仓储接口（Legacy） - 继承IRepository&lt;Herb&gt;标准接口
    /// 保留给现有HerbService使用，新CQRS层请使用IHerbRepository
    /// </summary>
    public interface IHerbRepositoryLegacy : IRepository<Herb>
    {
        /// <summary>
        /// 根据名称精确获取药材
        /// </summary>
        Task<Herb?> GetByNameAsync(string name);

        /// <summary>
        /// 按名称或拼音码查询药材
        /// </summary>
        Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);

        /// <summary>
        /// 检查药材名称是否存在（支持排除指定ID）
        /// </summary>
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

        /// <summary>
        /// 分页查询药材（支持关键字 + 分类筛选）
        /// </summary>
        Task<PagedResult<Herb>> GetPagedAsync(int pageNumber, int pageSize, string? keyword, string? category);

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// </summary>
        Task<Herb?> GetByIdIncludingDeletedAsync(Guid id);
    }
}



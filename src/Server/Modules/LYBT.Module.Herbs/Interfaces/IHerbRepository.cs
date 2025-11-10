using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材仓储接口 - 简化版，包含基础CRUD和分页功能
    /// Phase 2: Repository层简化（Epic #1725）- 新增分页支持
    /// </summary>
    public interface IHerbRepository : IRepository<Herb>
    {
        /// <summary>
        /// 根据名称获取药材
        /// </summary>
        Task<Herb?> GetByNameAsync(string name);

        /// <summary>
        /// 按名称或拼音码查询药材 (Issue #1351)
        /// 优先精确匹配名称，其次模糊匹配拼音码
        /// </summary>
        /// <param name="searchTerm">搜索词（药材名称或拼音码）</param>
        Task<Herb?> GetByNameOrPinyinAsync(string searchTerm);

        /// <summary>
        /// 获取分页列表
        /// Phase 2: Repository层简化（Epic #1725）- 新增分页功能（支持300+药材）
        /// </summary>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">搜索关键字（名称或拼音码）</param>
        Task<PagedResult<Herb>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null);

        /// <summary>
        /// 检查药材名称是否存在（支持排除指定ID，用于更新时验证）
        /// Epic #1962 Task 1.2: 批量导入重复检查
        /// </summary>
        /// <param name="name">药材名称</param>
        /// <param name="excludeId">排除的ID（更新时传入当前记录ID）</param>
        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null);

        /// <summary>
        /// 按分类查询药材列表
        /// Epic #1962 Task 1.2: 分类管理支持
        /// </summary>
        /// <param name="category">分类名称</param>
        Task<List<Herb>> GetByCategoryAsync(string category);

        /// <summary>
        /// 软删除药材（覆盖BaseRepository的硬删除）
        /// Epic #1962 Task 1.2: BR-007软删除支持
        /// </summary>
        /// <param name="id">药材ID</param>
        new Task DeleteAsync(Guid id);
    }
}

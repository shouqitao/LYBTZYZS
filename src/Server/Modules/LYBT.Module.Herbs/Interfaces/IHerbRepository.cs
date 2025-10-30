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
    }
}

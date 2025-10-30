using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Repositories
{
    /// <summary>
    /// 药材仓储 - 简化版，包含基础CRUD和分页功能
    /// Phase 2: Repository层简化（Epic #1725）- 新增分页支持
    /// </summary>
    internal class HerbRepository : BaseRepository<Herb>, IHerbRepository
    {
        public HerbRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据名称获取药材
        /// </summary>
        public async Task<Herb?> GetByNameAsync(string name)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Name == name && !h.IsDeleted);
        }

        /// <summary>
        /// 按名称或拼音码查询药材 (Issue #1351)
        /// 优先精确匹配名称，其次模糊匹配拼音码
        /// </summary>
        public async Task<Herb?> GetByNameOrPinyinAsync(string searchTerm)
        {
            // 1. 优先精确匹配名称
            var exactMatch = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Name == searchTerm && !h.IsDeleted);

            if (exactMatch != null)
                return exactMatch;

            // 2. 模糊匹配拼音码
            var pinyinMatch = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.PinYinCode != null
                    && h.PinYinCode.Contains(searchTerm)
                    && !h.IsDeleted);

            return pinyinMatch;
        }

        /// <summary>
        /// 获取分页列表
        /// Phase 2: Repository层简化（Epic #1725）- 新增分页功能（支持300+药材）
        /// </summary>
        public async Task<PagedResult<Herb>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(h => !h.IsDeleted);

            // 关键字搜索（名称或拼音码）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(h =>
                    h.Name.Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            }

            // 使用BaseRepository辅助方法处理分页（Epic #1725）
            return await GetPagedResultAsync(
                query.OrderBy(h => h.Name),
                pageNumber,
                pageSize);
        }
    }
}

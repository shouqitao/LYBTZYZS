using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Repositories
{
    /// <summary>
    /// 药材仓储 - 简化版，只包含基础CRUD
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
    }
}

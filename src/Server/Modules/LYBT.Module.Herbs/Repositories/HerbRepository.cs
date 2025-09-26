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
    public class HerbRepository : BaseRepository<Herb>, IHerbRepository
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
                .FirstOrDefaultAsync(h => h.Name == name && !h.IsDeleted);
        }
    }
}
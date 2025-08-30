using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Repositories
{

    /// <summary>
    /// 药材仓储实现类 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，实现药材特定业务逻辑
    /// </summary>
    public class HerbRepository : BaseRepository<Herb>, IHerbRepository
    {
        public HerbRepository(AppDbContext context) : base(context)
        {
            // BaseRepository会处理基础的数据库操作
        }

        // 注意：GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync等基础CRUD方法由BaseRepository提供

        /// <summary>
        /// 批量新增药材
        /// </summary>
        public async Task<bool> AddRangeAsync(List<Herb> herbs)
        {
            if (herbs == null || herbs.Count == 0)
                return false;

            await _dbSet.AddRangeAsync(herbs);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        /// <summary>
        /// 检查药材名称是否存在
        /// </summary>
        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
        {
            var query = _dbSet.AsQueryable()
                .Where(h => h.Name == name);

            if (excludeId.HasValue)
            {
                query = query.Where(h => h.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 根据拼音码搜索药材
        /// </summary>
        public async Task<List<Herb>> SearchByPinyinAsync(string pinyin)
        {
            return await _dbSet
                .Where(h => h.PinYinCode != null && h.PinYinCode.Contains(pinyin.ToUpperInvariant()))
                .OrderBy(h => h.Name)
                .ToListAsync();
        }
    }
}
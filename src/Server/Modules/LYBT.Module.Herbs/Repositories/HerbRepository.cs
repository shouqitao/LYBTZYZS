using System.Linq.Expressions;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Repositories
{
    /// <summary>
    /// 药材仓储实现 - 实现IRepository&lt;Herb&gt;标准接口
    /// Phase 1 Task 1.4: 基础数据模块Repository层统一重构
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 统一共性：实现IRepository&lt;Herb&gt;的11个标准CRUD方法
    /// - 保持特性：保留药材模块4个特定业务方法
    /// - 软删除模式：所有查询自动过滤IsDeleted=true的数据
    /// - 查询优化：只读查询使用AsNoTracking提升性能
    /// </remarks>
    internal class HerbRepository : IHerbRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<Herb> _dbSet;

        public HerbRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<Herb>();
        }

        #region IRepository<Herb> 标准方法实现

        /// <summary>
        /// 根据ID获取药材（包含软删除过滤）
        /// </summary>
        public async Task<Herb?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
        }

        /// <summary>
        /// 获取所有药材（⚠️ 仅用于下拉列表等小数据量场景）
        /// </summary>
        public async Task<IEnumerable<Herb>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(h => !h.IsDeleted)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 分页查询药材（支持名称/拼音码搜索）
        /// </summary>
        public async Task<PagedResult<Herb>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(h => !h.IsDeleted);

            // 关键字搜索：名称、拼音码
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var searchTerm = keyword.Trim();
                query = query.Where(h =>
                    h.Name.Contains(searchTerm) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(searchTerm))
                );
            }

            query = query.OrderBy(h => h.Name);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Herb>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// 条件查询（⚠️ 谨慎使用，建议使用具体业务方法）
        /// </summary>
        public async Task<IEnumerable<Herb>> FindAsync(Expression<Func<Herb, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(h => !h.IsDeleted)
                .Where(predicate)
                .ToListAsync();
        }

        /// <summary>
        /// 获取单个药材（条件查询）
        /// </summary>
        public async Task<Herb?> GetSingleAsync(Expression<Func<Herb, bool>> predicate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(h => !h.IsDeleted)
                .FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<Herb> AddAsync(Herb entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 更新药材
        /// </summary>
        public async Task<Herb> UpdateAsync(Herb entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            entity.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 检查药材是否存在
        /// </summary>
        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(h => h.Id == id && !h.IsDeleted);
        }

        /// <summary>
        /// 获取药材总数
        /// </summary>
        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync(h => !h.IsDeleted);
        }

        /// <summary>
        /// 保存更改（⚠️ 通常由Service层调用）
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion

        #region IHerbRepository 特定业务方法

        /// <summary>
        /// 根据名称精确获取药材
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
        /// 检查药材名称是否存在（支持排除指定ID，用于更新时验证）
        /// Epic #1962 Task 1.2: 批量导入重复检查
        /// </summary>
        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(h => h.Name == name && !h.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(h => h.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 按分类查询药材列表
        /// Epic #1962 Task 1.2: 分类管理支持
        /// </summary>
        public async Task<List<Herb>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(h => h.Category == category && !h.IsDeleted)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        #endregion
    }
}

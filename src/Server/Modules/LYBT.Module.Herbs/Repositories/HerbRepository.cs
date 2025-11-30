using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Herbs.Repositories
{
    /// <summary>
    /// 药材仓储实现 - 继承BaseRepository并实现IHerbRepository
    /// Task 1.4: Repository重构，适配新的简化Repository设计
    /// </summary>
    /// <remarks>
    /// 设计原则：
    /// - 继承BaseRepository：复用11个标准CRUD方法
    /// - 业务扩展：实现药材特定的业务查询方法
    /// - 软删除模式：所有查询自动过滤IsDeleted=true的数据
    /// - 查询优化：只读查询使用AsNoTracking提升性能
    /// </remarks>
    internal class HerbRepository : BaseRepository<Herb>, IHerbRepository
    {
        public HerbRepository(AppDbContext context, ILogger<HerbRepository> logger)
            : base(context, logger)
        {
        }

        #region 模板方法覆盖 - 药材关键字搜索和排序

        /// <summary>
        /// 关键字过滤：名称、拼音码
        /// </summary>
        protected override IQueryable<Herb> ApplyKeywordFilter(IQueryable<Herb> query, string keyword)
        {
            return query.Where(h =>
                h.Name.Contains(keyword) ||
                (h.PinYinCode != null && h.PinYinCode.Contains(keyword))
            );
        }

        /// <summary>
        /// 默认排序：按名称升序
        /// </summary>
        protected override IQueryable<Herb> ApplyDefaultOrdering(IQueryable<Herb> query)
        {
            return query.OrderBy(h => h.Name);
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

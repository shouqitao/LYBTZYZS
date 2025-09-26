using FormulaEntity = LYBT.Entities.Formula.Formula;
using LYBT.Entities.Formula;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Repositories
{
    /// <summary>
    /// 方剂仓储 - 优化版，包含Include策略以解决N+1查询问题
    /// </summary>
    public class FormulaRepository : BaseRepository<FormulaEntity>, IFormulaRepository
    {
        private readonly ILogger<FormulaRepository> _logger;

        public FormulaRepository(AppDbContext context) : base(context)
        {
            _logger = null; // 暂时设为null，后续可通过DI注入
        }

        public FormulaRepository(AppDbContext context, ILogger<FormulaRepository> logger)
            : base(context, logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取启用的方剂模板（包含药材配伍信息）
        /// </summary>
        public async Task<List<FormulaEntity>> GetTemplatesAsync()
        {
            return await _dbSet
                .Include(f => f.Herbs)  // 预加载药材配伍信息
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取方剂（包含所有药材配伍）
        /// </summary>
        public async Task<FormulaEntity> GetByIdWithHerbsAsync(Guid id)
        {
            return await _dbSet
                .Include(f => f.Herbs)
                .Where(f => f.Id == id && !f.IsDeleted)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取分页列表（包含药材配伍信息）
        /// 优化：预加载Herbs集合，避免N+1查询
        /// </summary>
        public async Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string keyword = null)
        {
            var query = _dbSet
                .Include(f => f.Herbs)  // 预加载药材配伍
                .Where(f => !f.IsDeleted);

            // 关键字搜索
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(f =>
                    f.Name.Contains(keyword) ||
                    f.Effect.Contains(keyword) ||
                    f.Usage.Contains(keyword) ||
                    f.Herbs.Any(h => h.HerbName.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<FormulaEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 根据用户ID获取方剂列表
        /// </summary>
        public async Task<List<FormulaEntity>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(f => f.Herbs)
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取共享的方剂列表
        /// </summary>
        public async Task<List<FormulaEntity>> GetSharedFormulasAsync()
        {
            return await _dbSet
                .Include(f => f.Herbs)
                .Where(f => f.IsShared && !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据类别获取方剂列表
        /// </summary>
        public async Task<List<FormulaEntity>> GetByCategoryAsync(string category)
        {
            return await _dbSet
                .Include(f => f.Herbs)
                .Where(f => f.Category == category && !f.IsDeleted)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
    }
}
}
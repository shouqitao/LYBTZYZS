using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formula.Repositories
{
    /// <summary>
    /// 方剂仓储 - 简化版，合并冗余查询方法
    /// </summary>
    internal class FormulaRepository : BaseRepository<FormulaEntity>, IFormulaRepository
    {
        public FormulaRepository(AppDbContext context) : base(context)
        {
        }

        public FormulaRepository(AppDbContext context, ILogger<FormulaRepository> logger)
            : base(context, logger)
        {
        }

        /// <summary>
        /// 统一的查询方法 - 合并原有的多个查询方法
        /// </summary>
        private IQueryable<FormulaEntity> GetBaseQuery()
        {
            return _dbSet
                .Include(f => f.Herbs)
                .Where(f => !f.IsDeleted);
        }

        /// <summary>
        /// 获取启用的方剂模板
        /// </summary>
        public async Task<List<FormulaEntity>> GetTemplatesAsync()
        {
            return await GetBaseQuery()
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取方剂（简化版，使用统一查询）
        /// </summary>
        public async Task<FormulaEntity> GetByIdWithHerbsAsync(Guid id)
        {
            return (await GetBaseQuery()
                .Where(f => f.Id == id)
                .SingleOrDefaultAsync())!;
        }

        /// <summary>
        /// 获取分页列表（简化版，减少复杂搜索逻辑）
        /// Phase 2: Repository层简化（Epic #1725）- 使用BaseRepository辅助方法
        /// </summary>
        public async Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = GetBaseQuery();

            // 简化搜索逻辑 - 只搜索名称和功效
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(f => f.Name.Contains(keyword) || (f.Effect != null && f.Effect.Contains(keyword)));
            }

            // 使用BaseRepository辅助方法处理分页（Epic #1725）
            return await GetPagedResultAsync(
                query.OrderByDescending(f => f.CreatedAt),
                pageNumber,
                pageSize);
        }

        /// <summary>
        /// 根据用户ID和权限获取方剂列表（合并权限逻辑）
        /// </summary>
        public async Task<List<FormulaEntity>> GetByUserIdAsync(Guid userId)
        {
            return await GetBaseQuery()
                .Where(f => f.UserId == userId || f.IsShared) // 简化权限逻辑：自己的+共享的
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取共享的方剂列表（保留但简化）
        /// </summary>
        public async Task<List<FormulaEntity>> GetSharedFormulasAsync()
        {
            return await GetBaseQuery()
                .Where(f => f.IsShared)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据类别获取方剂列表（简化版）
        /// </summary>
        public async Task<List<FormulaEntity>> GetByCategoryAsync(string category)
        {
            return await GetBaseQuery()
                .Where(f => f.Category == category)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
    }
}

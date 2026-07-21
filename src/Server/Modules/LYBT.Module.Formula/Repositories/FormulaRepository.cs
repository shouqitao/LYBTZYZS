using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Extensions;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

namespace LYBT.Module.Formulas.Repositories
{
    /// <summary>
    /// 方剂仓储 - 简化版，合并冗余查询方法
    /// </summary>
    internal class FormulaRepository : BaseRepository<FormulaEntity>, IFormulaRepositoryLegacy
    {
        public FormulaRepository(AppDbContext context, ILogger<FormulaRepository> logger)
            : base(context, logger)
        {
        }

        #region 模板方法覆盖 - 方剂关键字搜索

        /// <summary>
        /// 关键字过滤：名称、功效
        /// </summary>
        protected override IQueryable<FormulaEntity> ApplyKeywordFilter(IQueryable<FormulaEntity> query, string keyword)
        {
            return query.Where(f =>
                f.Name.Contains(keyword) ||
                (f.Effect != null && f.Effect.Contains(keyword))
            );
        }

        // ApplyDefaultOrdering 使用基类默认实现（CreatedAt降序）

        #endregion

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
        /// 根据谓词条件，查询包含药材的验方，排除软删除，并按创建时间降序排序
        /// </summary>
        public async Task<List<FormulaEntity>> FindWithHerbsAsync(Expression<Func<FormulaEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await GetBaseQuery()
                .Where(predicate)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
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
        /// 获取分页列表（包含药材配伍，使用模板方法）
        /// Phase 3: 使用模板方法统一过滤和排序逻辑
        /// </summary>
        public async Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(
            int pageNumber,
            int pageSize,
            string? keyword = null)
        {
            var query = GetBaseQuery();

            // 使用模板方法进行关键字过滤
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = ApplyKeywordFilter(query, keyword.Trim());
            }

            // 使用模板方法（基类默认）进行排序
            query = ApplyDefaultOrdering(query);

            return await query.GetPagedResultAsync(pageNumber, pageSize);
        }

        /// <summary>
        /// 获取分页列表（包含药材配伍信息 + category/role 筛选，DB 层执行）
        /// Sprint3-X6: 从 Service 内存过滤迁移到 Repository DB 查询
        /// </summary>
        public async Task<PagedResult<FormulaEntity>> GetPagedWithDetailsAsync(
            int pageNumber, int pageSize, string? keyword,
            string? category, Guid? userId, bool isAdmin)
        {
            var query = GetBaseQuery();

            // 关键字过滤（复用模板方法）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = ApplyKeywordFilter(query, keyword.Trim());
            }

            // 分类筛选（DB 层执行）
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(f => f.Category != null && f.Category.Contains(category));
            }

            // 角色过滤（DB 层执行）
            // Admin/SuperAdmin 可以看到所有 Formula
            // Doctor 只能看到自己创建的或共享的 Formula
            if (!isAdmin && userId.HasValue)
            {
                query = query.Where(f =>
                    f.UserId == userId.Value ||
                    f.CreatedBy == userId.Value ||
                    f.IsShared);
            }

            // 默认排序
            query = ApplyDefaultOrdering(query);

            return await query.GetPagedResultAsync(pageNumber, pageSize);
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
        /// T5-P2-36: 获取所有验方（包含药材组成），用于导出
        /// </summary>
        public async Task<List<FormulaEntity>> GetAllWithHerbsAsync()
        {
            return await GetBaseQuery()
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 按分类筛选验方（包含药材组成，DB层执行过滤），用于导出
        /// </summary>
        public async Task<List<FormulaEntity>> GetByCategoryWithHerbsAsync(string category)
        {
            return await GetBaseQuery()
                .Where(f => f.Category != null && f.Category.Contains(category))
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取实体（包括已软删除的）
        /// 使用EF Core FindAsync直接通过主键查询，绕过软删除过滤器
        /// </summary>
        public async Task<FormulaEntity?> GetByIdIncludingDeletedAsync(Guid id)
        {
            // FindAsync在EF Core 8中受全局查询过滤器影响，改用IgnoreQueryFilters
            return await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id);
        }
    }
}



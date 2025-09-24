using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Formula.Repositories
{
    /// <summary>
    /// 验方只读仓储实现
    /// </summary>
    public class FormulaReadRepository : ReadOnlyRepository<LYBT.Entities.Formula.Formula>, IFormulaReadRepository
    {
        /// <summary>
        /// 初始化验方只读仓储
        /// </summary>
        /// <param name="context">数据库上下文</param>
        /// <param name="mapper">映射器</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="cache">缓存</param>
        public FormulaReadRepository(
            AppDbContext context,
            IMapper mapper,
            ILogger<FormulaReadRepository> logger,
            IMemoryCache cache)
            : base(context, mapper, logger, cache)
        {
        }

        /// <summary>
        /// 分页查询验方DTO
        /// </summary>
        public async Task<PagedResult<FormulaDto>> GetPagedFormulaDtosAsync(FormulaQueryDto query, CancellationToken cancellationToken = default)
        {
            var predicate = BuildFormulaFilter(query);
            return await GetPagedAsync<FormulaDto>(
                predicate,
                query.PageIndex,
                query.PageSize,
                f => f.Name,
                false);
        }

        /// <summary>
        /// 根据名称搜索验方
        /// </summary>
        public async Task<List<FormulaDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => f.Name.Contains(name));
            return results.ToList();
        }

        /// <summary>
        /// 获取共享验方列表
        /// </summary>
        public async Task<List<FormulaDto>> GetSharedFormulasAsync(CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => f.IsShared);
            return results.ToList();
        }

        /// <summary>
        /// 根据状态获取验方
        /// </summary>
        public async Task<List<FormulaDto>> GetByStatusAsync(CommonStatus status, CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => f.Status == status);
            return results.ToList();
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        public async Task<FormulaDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var formula = await _dbSet.AsNoTracking()
                .Where(f => f.Id == id)
                .ProjectTo<FormulaDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);
            
            return formula;
        }

        /// <summary>
        /// 获取验方的药材关联
        /// </summary>
        public async Task<List<FormulaHerbItemDto>> GetFormulaHerbsAsync(Guid formulaId, CancellationToken cancellationToken = default)
        {
            var formula = await _context.Set<LYBT.Entities.Formula.Formula>()
                .Include(f => f.Herbs)
                .Where(f => f.Id == formulaId)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (formula?.Herbs == null)
                return new List<FormulaHerbItemDto>();
                
            return _mapper.Map<List<FormulaHerbItemDto>>(formula.Herbs);
        }

        /// <summary>
        /// 检查验方名称是否重复
        /// </summary>
        public async Task<bool> IsNameDuplicateAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().Where(f => f.Name == name);
            if (excludeId.HasValue)
            {
                query = query.Where(f => f.Id != excludeId.Value);
            }
            
            return await query.AnyAsync(cancellationToken);
        }

        /// <summary>
        /// 获取热门验方
        /// </summary>
        public async Task<List<FormulaDto>> GetPopularFormulasAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var results = await _dbSet.AsNoTracking()
                .Where(f => f.Status == CommonStatus.Enabled)
                .OrderBy(f => f.Name)
                .Take(count)
                .ProjectTo<FormulaDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            
            return results;
        }

        /// <summary>
        /// 根据效能搜索验方
        /// </summary>
        public async Task<List<FormulaDto>> SearchByEffectAsync(string effect, CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => f.Effect != null && f.Effect.Contains(effect));
            return results.ToList();
        }

        /// <summary>
        /// 获取验方统计信息
        /// </summary>
        public async Task<object> GetStatisticsAsync()
        {
            var total = await _dbSet.AsNoTracking().CountAsync();
            var enabled = await _dbSet.AsNoTracking().CountAsync(f => f.Status == CommonStatus.Enabled);
            var shared = await _dbSet.AsNoTracking().CountAsync(f => f.IsShared);
            
            return new { Total = total, Enabled = enabled, Shared = shared };
        }

        /// <summary>
        /// 根据用法搜索验方
        /// </summary>
        public async Task<List<FormulaDto>> SearchByUsageAsync(string usage, CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => f.Usage != null && f.Usage.Contains(usage));
            return results.ToList();
        }

        /// <summary>
        /// 验证验方是否可用
        /// </summary>
        public async Task<bool> IsAvailableAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await ExistsAsync(f => f.Id == id && f.Status == CommonStatus.Enabled);
        }

        /// <summary>
        /// 搜索验方DTO（分页）
        /// </summary>
        public async Task<PagedResult<FormulaDto>> SearchFormulaDtosAsync(PagedQueryBaseDto query, CancellationToken cancellationToken = default)
        {
            Expression<Func<LYBT.Entities.Formula.Formula, bool>> predicate = f => 
                f.Status == CommonStatus.Enabled &&
                (string.IsNullOrWhiteSpace(query.Keyword) || 
                 f.Name.Contains(query.Keyword) || 
                 (f.Effect != null && f.Effect.Contains(query.Keyword)));

            return await GetPagedAsync<FormulaDto>(
                predicate,
                query.PageIndex,
                query.PageSize,
                f => f.Name,
                false);
        }

        /// <summary>
        /// 获取验方DTO列表
        /// </summary>
        public async Task<List<FormulaDto>> GetFormulaDtosAsync(string? keyword = null, CancellationToken cancellationToken = default)
        {
            Expression<Func<LYBT.Entities.Formula.Formula, bool>> predicate;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                predicate = f => f.Status == CommonStatus.Enabled;
            }
            else
            {
                predicate = f => f.Status == CommonStatus.Enabled && f.Name.Contains(keyword);
            }

            var results = await FindAndProjectAsync<FormulaDto>(predicate);
            return results.ToList();
        }

        /// <summary>
        /// 获取所有启用状态的验方DTO
        /// </summary>
        public async Task<List<FormulaDto>> GetAllFormulaDtosAsync(CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => f.Status == CommonStatus.Enabled);
            return results.ToList();
        }

        /// <summary>
        /// 获取验方DTO详情（包含药材组成）
        /// </summary>
        public async Task<FormulaDto?> GetFormulaDtoByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await GetDetailAsync(id, cancellationToken);
        }

        /// <summary>
        /// 获取验方模板DTO列表
        /// </summary>
        public async Task<List<FormulaDto>> GetTemplateDtosAsync(CancellationToken cancellationToken = default)
        {
            return await GetSharedFormulasAsync(cancellationToken);
        }

        /// <summary>
        /// 根据验方类型查询验方DTO
        /// </summary>
        public async Task<List<FormulaDto>> GetFormulaDtosByTypeAsync(string formulaType, CancellationToken cancellationToken = default)
        {
            var results = await FindAndProjectAsync<FormulaDto>(f => 
                f.Status == CommonStatus.Enabled && 
                f.Name.Contains(formulaType));
            return results.ToList();
        }

        /// <summary>
        /// 根据关键字和分类查询验方DTO
        /// </summary>
        public async Task<List<FormulaDto>> GetFormulaDtosAsync(string? keyword, string? category, CancellationToken cancellationToken = default)
        {
            Expression<Func<LYBT.Entities.Formula.Formula, bool>> predicate = f => f.Status == CommonStatus.Enabled;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                Expression<Func<LYBT.Entities.Formula.Formula, bool>> keywordPredicate = f => f.Name.Contains(keyword) || 
                                          (f.Effect != null && f.Effect.Contains(keyword));
                predicate = CombineExpressions(predicate, keywordPredicate, ExpressionType.AndAlso);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                Expression<Func<LYBT.Entities.Formula.Formula, bool>> categoryPredicate = f => f.Effect != null && f.Effect.Contains(category);
                predicate = CombineExpressions(predicate, categoryPredicate, ExpressionType.AndAlso);
            }

            var results = await FindAndProjectAsync<FormulaDto>(predicate);
            return results.ToList();
        }

        /// <summary>
        /// 获取验方分类列表
        /// </summary>
        public async Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _dbSet.AsNoTracking()
                .Where(f => f.Status == CommonStatus.Enabled && f.Effect != null)
                .Select(f => f.Effect!)
                .Distinct()
                .ToListAsync(cancellationToken);
            
            return categories;
        }

        /// <summary>
        /// 检查验方名称是否可用
        /// </summary>
        public async Task<bool> IsNameAvailableAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var isDuplicate = await IsNameDuplicateAsync(name, excludeId, cancellationToken);
            return !isDuplicate;
        }

        /// <summary>
        /// 获取共享验方数量
        /// </summary>
        public async Task<int> GetSharedFormulaCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking()
                .CountAsync(f => f.Status == CommonStatus.Enabled && f.IsShared, cancellationToken);
        }

        /// <summary>
        /// 获取最近创建的验方DTO列表
        /// </summary>
        public async Task<List<FormulaDto>> GetRecentFormulaDtosAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var results = await _dbSet.AsNoTracking()
                .Where(f => f.Status == CommonStatus.Enabled)
                .OrderBy(f => f.Name)
                .Take(count)
                .ProjectTo<FormulaDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
            
            return results;
        }

        private Expression<Func<LYBT.Entities.Formula.Formula, bool>> BuildFormulaFilter(FormulaQueryDto query)
        {
            var parameter = Expression.Parameter(typeof(LYBT.Entities.Formula.Formula), "f");
            Expression? body = null;

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var nameContains = Expression.Call(
                    Expression.Property(parameter, "Name"),
                    "Contains",
                    null,
                    Expression.Constant(query.Keyword));
                body = body == null ? nameContains : Expression.AndAlso(body, nameContains);
            }

            if (query.Status.HasValue)
            {
                var statusEquals = Expression.Equal(
                    Expression.Property(parameter, "Status"),
                    Expression.Constant(query.Status.Value));
                body = body == null ? statusEquals : Expression.AndAlso(body, statusEquals);
            }

            if (query.IsShared.HasValue)
            {
                var isSharedEquals = Expression.Equal(
                    Expression.Property(parameter, "IsShared"),
                    Expression.Constant(query.IsShared.Value));
                body = body == null ? isSharedEquals : Expression.AndAlso(body, isSharedEquals);
            }

            // 如果没有任何条件，返回一个总是true的表达式
            if (body == null)
            {
                body = Expression.Constant(true);
            }

            return Expression.Lambda<Func<LYBT.Entities.Formula.Formula, bool>>(body, parameter);
        }

        private Expression<Func<T, bool>> CombineExpressions<T>(
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right,
            ExpressionType expressionType)
        {
            var parameter = left.Parameters.First();
            var body = expressionType == ExpressionType.AndAlso
                ? Expression.AndAlso(left.Body, Expression.Invoke(right, parameter))
                : Expression.OrElse(left.Body, Expression.Invoke(right, parameter));

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }
}
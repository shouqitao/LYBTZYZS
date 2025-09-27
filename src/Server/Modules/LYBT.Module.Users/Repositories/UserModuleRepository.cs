using LYBT.Entities.Common;
using LYBT.Module.Users.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace LYBT.Module.Users.Repositories
{
    /// <summary>
    /// 用户模块仓储基类
    /// 提供基于UserDbContext的通用数据访问功能
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public abstract class UserModuleRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly UserDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly ILogger? _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">用户数据库上下文</param>
        /// <param name="logger">日志记录器（可选）</param>
        protected UserModuleRepository(UserDbContext context, ILogger? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>实体，如果不存在则返回null</returns>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        /// <returns>实体列表</returns>
        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// 根据条件查找实体
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>实体列表</returns>
        public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// 根据条件获取单个实体
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>实体，如果不存在则返回null</returns>
        public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="predicate">查询条件（可选）</param>
        /// <param name="orderBy">排序条件（可选）</param>
        /// <param name="descending">是否降序</param>
        /// <returns>分页结果</returns>
        public virtual async Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool descending = true)
        {
            var query = _dbSet.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync();

            if (orderBy != null)
            {
                query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            else
            {
                // 默认按创建时间降序排列
                query = query.OrderByDescending(e => e.CreatedAt);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="entity">要添加的实体</param>
        /// <returns>添加的实体</returns>
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 批量添加实体
        /// </summary>
        /// <param name="entities">要添加的实体列表</param>
        /// <returns>添加的实体数量</returns>
        public virtual async Task<int> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities);
            return await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        /// <param name="entity">要更新的实体</param>
        /// <returns>更新的实体</returns>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// 删除实体（软删除）
        /// </summary>
        /// <param name="id">实体ID</param>
        /// <returns>是否删除成功</returns>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 删除实体（软删除）
        /// </summary>
        /// <param name="entity">要删除的实体</param>
        /// <returns>是否删除成功</returns>
        public virtual async Task<bool> DeleteAsync(TEntity entity)
        {
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <returns>是否存在</returns>
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// 获取数量
        /// </summary>
        /// <param name="predicate">查询条件（可选）</param>
        /// <returns>数量</returns>
        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            return predicate == null ? 
                await _dbSet.CountAsync() : 
                await _dbSet.CountAsync(predicate);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LYBT.Core.Entities.Common;
using LYBT.Core.Infrastructure.Data;
using LYBT.Core.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Core.Infrastructure.Repositories
{
    /// <summary>
    /// 仓储基类
    /// 提供通用的CRUD操作和查询功能
    /// </summary>
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity>, IRepository<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly ILogger _logger;

        protected BaseRepository(AppDbContext context, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = _context.Set<TEntity>();
        }

        protected BaseRepository(AppDbContext context) : this(context, NullLogger.Instance)
        {
        }

        #region 查询操作

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 根据ID获取实体（包含关联数据）
        /// </summary>
        public virtual async Task<TEntity?> GetByIdWithIncludesAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
        {
            var query = _dbSet.Where(e => e.Id == id && !e.IsDeleted);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        // IReadOnlyRepository实现 - 返回IEnumerable
        async Task<IEnumerable<TEntity>> IReadOnlyRepository<TEntity>.GetAllAsync()
        {
            return await GetAllAsync();
        }

        /// <summary>
        /// 根据条件查询
        /// </summary>
        public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        // IReadOnlyRepository实现 - 返回IEnumerable
        async Task<IEnumerable<TEntity>> IReadOnlyRepository<TEntity>.FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await FindAsync(predicate);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public virtual async Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool descending = true)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

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
                query = query.OrderByDescending(e => e.CreatedAt);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // IReadOnlyRepository GetPagedAsync实现
        async Task<PagedResult<TEntity>> IReadOnlyRepository<TEntity>.GetPagedAsync(int pageNumber, int pageSize)
        {
            var (items, totalCount) = await GetPagedAsync(pageNumber, pageSize);
            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        async Task<PagedResult<TEntity>> IReadOnlyRepository<TEntity>.GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy,
            bool ascending)
        {
            var (items, totalCount) = await GetPagedAsync(pageNumber, pageSize, predicate, orderBy, !ascending);
            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        // IReadOnlyRepository GetSingleAsync实现
        async Task<TEntity?> IReadOnlyRepository<TEntity>.GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 检查是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .AnyAsync(predicate);
        }

        /// <summary>
        /// 根据ID检查是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await ExistsAsync(e => e.Id == id);
        }

        // IReadOnlyRepository ExistsAsync实现
        async Task<bool> IReadOnlyRepository<TEntity>.ExistsAsync(Guid id)
        {
            return await ExistsAsync(id);
        }

        async Task<bool> IReadOnlyRepository<TEntity>.ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await ExistsAsync(predicate);
        }

        /// <summary>
        /// 获取数量
        /// </summary>
        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.LongCountAsync();
        }

        // IReadOnlyRepository CountAsync实现
        async Task<long> IReadOnlyRepository<TEntity>.CountAsync()
        {
            return await CountAsync();
        }

        async Task<long> IReadOnlyRepository<TEntity>.CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await CountAsync(predicate);
        }

        #endregion

        #region 创建操作

        /// <summary>
        /// 添加实体
        /// </summary>
        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.CreatedAt = DateTime.Now;
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

            await _dbSet.AddAsync(entity);
            await SaveChangesAsync();

            _logger?.LogDebug("实体已添加 - 类型: {EntityType}, ID: {Id}", typeof(TEntity).Name, entity.Id);

            return entity;
        }

        /// <summary>
        /// 批量添加实体
        /// </summary>
        public virtual async Task<List<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();

            foreach (var entity in entityList)
            {
                entity.CreatedAt = DateTime.Now;
                entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            }

            await _dbSet.AddRangeAsync(entityList);
            await SaveChangesAsync();

            _logger?.LogDebug("批量添加实体 - 类型: {EntityType}, 数量: {Count}",
                typeof(TEntity).Name, entityList.Count);

            return entityList;
        }

        // IRepository AddRangeAsync实现
        async Task<IEnumerable<TEntity>> IRepository<TEntity>.AddRangeAsync(IEnumerable<TEntity> entities)
        {
            return await AddRangeAsync(entities);
        }

        #endregion

        #region 更新操作

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.UpdatedAt = DateTime.Now;

            _dbSet.Update(entity);
            await SaveChangesAsync();

            _logger?.LogDebug("实体已更新 - 类型: {EntityType}, ID: {Id}", typeof(TEntity).Name, entity.Id);

            return entity;
        }

        /// <summary>
        /// 批量更新实体
        /// </summary>
        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();

            foreach (var entity in entityList)
            {
                entity.UpdatedAt = DateTime.Now;
            }

            _dbSet.UpdateRange(entityList);
            await SaveChangesAsync();

            _logger?.LogDebug("批量更新实体 - 类型: {EntityType}, 数量: {Count}",
                typeof(TEntity).Name, entityList.Count);
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 软删除实体
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                _logger?.LogWarning("实体不存在，无法删除 - 类型: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
                return false;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.Now;

            _dbSet.Update(entity);
            await SaveChangesAsync();

            _logger?.LogDebug("实体已软删除 - 类型: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
            return true;
        }

        // IRepository DeleteAsync(TEntity)实现
        async Task<bool> IRepository<TEntity>.DeleteAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return await DeleteAsync(entity.Id);
        }

        // IRepository DeleteAsync(Guid)实现
        async Task<bool> IRepository<TEntity>.DeleteAsync(Guid id)
        {
            return await DeleteAsync(id);
        }

        /// <summary>
        /// 批量软删除
        /// </summary>
        public virtual async Task<int> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = await _dbSet
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .ToListAsync();

            if (!entities.Any())
            {
                _logger?.LogWarning("没有找到符合条件的实体进行删除 - 类型: {EntityType}", typeof(TEntity).Name);
                return 0;
            }

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.Now;
            }

            _dbSet.UpdateRange(entities);
            await SaveChangesAsync();

            _logger?.LogDebug("批量软删除实体 - 类型: {EntityType}, 数量: {Count}",
                typeof(TEntity).Name, entities.Count);

            return entities.Count;
        }

        // IRepository DeleteRangeAsync(IEnumerable<TEntity>)实现
        async Task<int> IRepository<TEntity>.DeleteRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            var entityList = entities.ToList();
            foreach (var entity in entityList)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.Now;
            }

            _dbSet.UpdateRange(entityList);
            await SaveChangesAsync();

            _logger?.LogDebug("批量软删除实体 - 类型: {EntityType}, 数量: {Count}",
                typeof(TEntity).Name, entityList.Count);

            return entityList.Count;
        }

        // IRepository DeleteRangeAsync(IEnumerable<Guid>)实现
        async Task<int> IRepository<TEntity>.DeleteRangeAsync(IEnumerable<Guid> ids)
        {
            var idList = ids.ToList();
            var entities = await _dbSet
                .Where(e => !e.IsDeleted && idList.Contains(e.Id))
                .ToListAsync();

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.Now;
            }

            _dbSet.UpdateRange(entities);
            await SaveChangesAsync();

            _logger?.LogDebug("批量软删除实体 - 类型: {EntityType}, 数量: {Count}",
                typeof(TEntity).Name, entities.Count);

            return entities.Count;
        }

        /// <summary>
        /// 物理删除实体（谨慎使用）
        /// </summary>
        public virtual async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                _logger?.LogWarning("实体不存在，无法物理删除 - 类型: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
                return false;
            }

            _dbSet.Remove(entity);
            await SaveChangesAsync();

            _logger?.LogDebug("实体已物理删除 - 类型: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
            return true;
        }

        #endregion

        #region 高级查询

        /// <summary>
        /// 获取可查询对象
        /// </summary>
        public virtual IQueryable<TEntity> GetQueryable()
        {
            return _dbSet.Where(e => !e.IsDeleted);
        }

        /// <summary>
        /// 获取不跟踪的查询对象
        /// </summary>
        public virtual IQueryable<TEntity> GetNoTrackingQueryable()
        {
            return _dbSet.AsNoTracking().Where(e => !e.IsDeleted);
        }

        /// <summary>
        /// 执行SQL查询
        /// </summary>
        public virtual async Task<List<TEntity>> FromSqlRawAsync(string sql, params object[] parameters)
        {
            return await _dbSet.FromSqlRaw(sql, parameters).ToListAsync();
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量软删除
        /// </summary>
        public virtual async Task<int> BulkDeleteAsync(List<Guid> ids)
        {
            if (ids == null || !ids.Any())
                throw new ArgumentException("ID列表不能为空", nameof(ids));

            var entities = await _dbSet
                .Where(e => ids.Contains(e.Id) && !e.IsDeleted)
                .ToListAsync();

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.Now;
            }

            _dbSet.UpdateRange(entities);
            await SaveChangesAsync();

            _logger?.LogDebug("批量软删除实体 - 类型: {EntityType}, 数量: {Count}",
                typeof(TEntity).Name, entities.Count);

            return entities.Count;
        }

        #endregion

        #region 事务支持

        /// <summary>
        /// 开始事务
        /// </summary>
        public virtual async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public virtual async Task CommitTransactionAsync(IDbContextTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            await transaction.CommitAsync();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public virtual async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            await transaction.RollbackAsync();
        }

        #endregion

        #region 保护方法

        /// <summary>
        /// 保存更改
        /// </summary>
        public virtual async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger?.LogError(ex, "并发冲突 - 类型: {EntityType}", typeof(TEntity).Name);
                throw new InvalidOperationException("数据已被其他用户修改，请刷新后重试", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger?.LogError(ex, "数据库更新失败 - 类型: {EntityType}", typeof(TEntity).Name);
                throw;
            }
        }

        #endregion
    }
}
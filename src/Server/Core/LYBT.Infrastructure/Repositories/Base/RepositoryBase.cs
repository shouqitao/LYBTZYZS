using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Repositories.Base
{
    /// <summary>
    /// Repository基础实现类 - UltraThink重构架构
    /// 实现了CQRS模式的读写分离和高性能查询优化
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey> 
        where TEntity : class
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<TEntity> DbSet;
        protected readonly ILogger Logger;
        private IDbContextTransaction? _transaction;

        protected RepositoryBase(AppDbContext context, ILogger logger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            DbSet = context.Set<TEntity>();
        }

        #region Query Operations (优化的查询操作)

        public virtual async Task<TEntity?> GetByIdAsync(TKey id)
        {
            Logger.LogDebug("Getting entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            return await DbSet.FindAsync(id);
        }

        public virtual async Task<TEntity?> GetByIdAsNoTrackingAsync(TKey id)
        {
            Logger.LogDebug("Getting entity {EntityType} as no tracking with ID: {Id}", typeof(TEntity).Name, id);
            
            if (id is Guid guidId)
            {
                return await DbSet.AsNoTracking()
                    .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == guidId);
            }
            
            return await DbSet.AsNoTracking()
                .FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id").Equals(id));
        }

        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            Logger.LogDebug("Getting all entities of type {EntityType}", typeof(TEntity).Name);
            return await DbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            Logger.LogDebug("Finding entities of type {EntityType} with predicate", typeof(TEntity).Name);
            return await DbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            Logger.LogDebug("Getting first entity of type {EntityType} with predicate", typeof(TEntity).Name);
            return await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync<TDto>(
            IPagedQuery<TDto> query,
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false) where TDto : class
        {
            Logger.LogDebug("Getting paged entities of type {EntityType}, Page: {Page}, Size: {Size}", 
                typeof(TEntity).Name, query.PageIndex, query.PageSize);

            var queryable = DbSet.AsNoTracking();

            // 应用过滤条件
            if (predicate != null)
            {
                queryable = queryable.Where(predicate);
            }

            // 搜索条件
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                queryable = ApplySearch(queryable, query.SearchTerm);
            }

            // 获取总数
            var totalCount = await queryable.CountAsync();

            // 排序
            if (orderBy != null)
            {
                queryable = isDescending 
                    ? queryable.OrderByDescending(orderBy)
                    : queryable.OrderBy(orderBy);
            }
            else
            {
                queryable = ApplyDefaultSorting(queryable);
            }

            // 分页
            var items = await queryable
                .Skip(query.PageIndex * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            Logger.LogDebug("Counting entities of type {EntityType}", typeof(TEntity).Name);
            
            if (predicate == null)
            {
                return await DbSet.CountAsync();
            }
            
            return await DbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            Logger.LogDebug("Checking existence of entity {EntityType}", typeof(TEntity).Name);
            return await DbSet.AsNoTracking().AnyAsync(predicate);
        }

        public virtual IQueryable<TEntity> Query()
        {
            return DbSet.AsQueryable();
        }

        public virtual IQueryable<TEntity> QueryAsNoTracking()
        {
            return DbSet.AsNoTracking();
        }

        #endregion

        #region Command Operations (写操作)

        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            Logger.LogDebug("Adding entity of type {EntityType}", typeof(TEntity).Name);
            
            var entry = await DbSet.AddAsync(entity);
            return entry.Entity;
        }

        public virtual async Task<List<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            Logger.LogDebug("Adding multiple entities of type {EntityType}", typeof(TEntity).Name);
            
            var entityList = entities.ToList();
            await DbSet.AddRangeAsync(entityList);
            return entityList;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            Logger.LogDebug("Updating entity of type {EntityType}", typeof(TEntity).Name);
            
            DbSet.Update(entity);
            return await Task.FromResult(entity);
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        {
            Logger.LogDebug("Updating multiple entities of type {EntityType}", typeof(TEntity).Name);
            
            DbSet.UpdateRange(entities);
            await Task.CompletedTask;
        }

        public virtual async Task<bool> DeleteAsync(TKey id)
        {
            Logger.LogDebug("Deleting entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            DbSet.Remove(entity);
            return true;
        }

        public virtual async Task DeleteAsync(TEntity entity)
        {
            Logger.LogDebug("Deleting entity of type {EntityType}", typeof(TEntity).Name);
            
            DbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities)
        {
            Logger.LogDebug("Deleting multiple entities of type {EntityType}", typeof(TEntity).Name);
            
            DbSet.RemoveRange(entities);
            await Task.CompletedTask;
        }

        public virtual async Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate)
        {
            Logger.LogDebug("Deleting entities of type {EntityType} with predicate", typeof(TEntity).Name);
            
            var entities = await DbSet.Where(predicate).ToListAsync();
            DbSet.RemoveRange(entities);
            return entities.Count;
        }

        #endregion

        #region Unit of Work Support

        public virtual async Task<int> SaveChangesAsync()
        {
            Logger.LogDebug("Saving changes for {EntityType}", typeof(TEntity).Name);
            return await Context.SaveChangesAsync();
        }

        public virtual async Task BeginTransactionAsync()
        {
            Logger.LogDebug("Beginning transaction for {EntityType}", typeof(TEntity).Name);
            _transaction = await Context.Database.BeginTransactionAsync();
        }

        public virtual async Task CommitTransactionAsync()
        {
            Logger.LogDebug("Committing transaction for {EntityType}", typeof(TEntity).Name);
            
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public virtual async Task RollbackTransactionAsync()
        {
            Logger.LogDebug("Rolling back transaction for {EntityType}", typeof(TEntity).Name);
            
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        #endregion

        #region Protected Virtual Methods (子类可重写)

        /// <summary>
        /// 应用搜索条件 - 子类可重写实现具体的搜索逻辑
        /// </summary>
        protected virtual IQueryable<TEntity> ApplySearch(IQueryable<TEntity> queryable, string searchTerm)
        {
            // 默认实现，子类应该重写这个方法
            return queryable;
        }

        /// <summary>
        /// 应用默认排序 - 子类可重写实现具体的排序逻辑
        /// </summary>
        protected virtual IQueryable<TEntity> ApplyDefaultSorting(IQueryable<TEntity> queryable)
        {
            // 默认按创建时间倒序，如果实体有CreatedAt属性
            var propertyInfo = typeof(TEntity).GetProperty("CreatedAt");
            if (propertyInfo != null)
            {
                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var property = Expression.Property(parameter, "CreatedAt");
                var lambda = Expression.Lambda<Func<TEntity, object>>(
                    Expression.Convert(property, typeof(object)), parameter);
                
                return queryable.OrderByDescending(lambda);
            }

            return queryable;
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    /// 简化的Repository基类 - Guid主键
    /// </summary>
    public abstract class RepositoryBase<TEntity> : RepositoryBase<TEntity, Guid>, IRepository<TEntity> 
        where TEntity : class
    {
        protected RepositoryBase(AppDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
}
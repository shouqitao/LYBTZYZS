using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;

namespace LYBT.Infrastructure.Repositories.DDD
{
    /// <summary>
    /// DDD聚合根Repository基础实现类 - 实现Domain层Repository接口
    /// </summary>
    /// <typeparam name="TAggregateRoot">聚合根类型</typeparam>
    public abstract class DomainRepositoryBase<TAggregateRoot> : IRepository<TAggregateRoot> 
        where TAggregateRoot : AggregateRoot
    {
        protected readonly AppDbContext Context;
        protected readonly DbSet<TAggregateRoot> DbSet;
        protected readonly ILogger Logger;

        protected DomainRepositoryBase(AppDbContext context, ILogger logger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            DbSet = context.Set<TAggregateRoot>();
        }

        #region Query Operations

        public virtual async Task<TAggregateRoot> GetByIdAsync(Guid id)
        {
            Logger.LogDebug("Getting {AggregateType} with ID: {Id}", typeof(TAggregateRoot).Name, id);
            return await DbSet.FindAsync(id);
        }

        public virtual async Task<List<TAggregateRoot>> GetByIdsAsync(List<Guid> ids)
        {
            Logger.LogDebug("Getting {AggregateType} with IDs: {Ids}", typeof(TAggregateRoot).Name, string.Join(",", ids));
            return await DbSet.Where(x => ids.Contains(x.Id)).ToListAsync();
        }

        public virtual async Task<List<TAggregateRoot>> GetAllAsync()
        {
            Logger.LogDebug("Getting all {AggregateType}", typeof(TAggregateRoot).Name);
            return await DbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<List<TAggregateRoot>> FindAsync(Expression<Func<TAggregateRoot, bool>> predicate)
        {
            Logger.LogDebug("Finding {AggregateType} with predicate", typeof(TAggregateRoot).Name);
            return await DbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<TAggregateRoot> FirstOrDefaultAsync(Expression<Func<TAggregateRoot, bool>> predicate)
        {
            Logger.LogDebug("Getting first {AggregateType} with predicate", typeof(TAggregateRoot).Name);
            return await DbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> predicate)
        {
            Logger.LogDebug("Checking existence of {AggregateType}", typeof(TAggregateRoot).Name);
            return await DbSet.AsNoTracking().AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TAggregateRoot, bool>> predicate = null)
        {
            Logger.LogDebug("Counting {AggregateType}", typeof(TAggregateRoot).Name);
            
            if (predicate == null)
            {
                return await DbSet.CountAsync();
            }
            
            return await DbSet.CountAsync(predicate);
        }

        #endregion

        #region Command Operations

        public virtual async Task<TAggregateRoot> AddAsync(TAggregateRoot aggregateRoot)
        {
            Logger.LogDebug("Adding {AggregateType}", typeof(TAggregateRoot).Name);
            
            var entry = await DbSet.AddAsync(aggregateRoot);
            return entry.Entity;
        }

        public virtual async Task<TAggregateRoot> UpdateAsync(TAggregateRoot aggregateRoot)
        {
            Logger.LogDebug("Updating {AggregateType}", typeof(TAggregateRoot).Name);
            
            DbSet.Update(aggregateRoot);
            return await Task.FromResult(aggregateRoot);
        }

        public virtual async Task DeleteAsync(TAggregateRoot aggregateRoot)
        {
            Logger.LogDebug("Deleting {AggregateType}", typeof(TAggregateRoot).Name);
            
            DbSet.Remove(aggregateRoot);
            await Task.CompletedTask;
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            Logger.LogDebug("Deleting {AggregateType} with ID: {Id}", typeof(TAggregateRoot).Name, id);
            
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                DbSet.Remove(entity);
            }
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// 获取可查询对象
        /// </summary>
        protected IQueryable<TAggregateRoot> Query()
        {
            return DbSet.AsQueryable();
        }

        /// <summary>
        /// 获取只读查询对象
        /// </summary>
        protected IQueryable<TAggregateRoot> QueryAsNoTracking()
        {
            return DbSet.AsNoTracking();
        }

        /// <summary>
        /// 包含导航属性的查询
        /// </summary>
        protected virtual IQueryable<TAggregateRoot> IncludeNavigationProperties(IQueryable<TAggregateRoot> query)
        {
            // 子类可以重写此方法来包含特定的导航属性
            return query;
        }

        #endregion
    }
}
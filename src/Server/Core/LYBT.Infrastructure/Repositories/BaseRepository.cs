using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 基础仓储实现 - Solution级架构标准化
    /// 提供所有模块仓储的通用实现，确保数据访问层架构一致性
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        protected BaseRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(null, pageNumber, pageSize);
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate, 
            int pageNumber, 
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true)
        {
            var query = _dbSet.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.SingleOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.FindAsync(id) != null;
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public virtual async Task<long> CountAsync()
        {
            return await _dbSet.LongCountAsync();
        }

        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.LongCountAsync(predicate);
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entry.Entity;
        }

        public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            return entities;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            return await DeleteAsync(entity);
        }

        public virtual async Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
            return await _context.SaveChangesAsync();
        }

        public virtual async Task<int> DeleteRangeAsync(IEnumerable<Guid> ids)
        {
            var entities = new List<TEntity>();
            foreach (var id in ids)
            {
                var entity = await GetByIdAsync(id);
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }

            if (entities.Count > 0)
            {
                return await DeleteRangeAsync(entities);
            }

            return 0;
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query)
        {
            // 子类可以重写此方法以添加必要的Include
            return query;
        }
    }

    /// <summary>
    /// 只读基础仓储实现
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public abstract class ReadOnlyBaseRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        protected ReadOnlyBaseRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await GetPagedAsync(null, pageNumber, pageSize);
        }

        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate, 
            int pageNumber, 
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true)
        {
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().SingleOrDefaultAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AsNoTracking().AnyAsync(e => EF.Property<Guid>(e, "Id") == id);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().AnyAsync(predicate);
        }

        public virtual async Task<long> CountAsync()
        {
            return await _dbSet.AsNoTracking().LongCountAsync();
        }

        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.AsNoTracking().LongCountAsync(predicate);
        }

        protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query)
        {
            // 子类可以重写此方法以添加必要的Include
            return query;
        }
    }
}
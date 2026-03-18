using System.Linq.Expressions;
using LYBT.Entities.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 仓储基类 - 简化版本
    /// 提供标准CRUD操作和基础查询功能
    /// 遵循接口隔离原则，只保留核心11个方法
    /// Issue #2103: Server端重构 - BaseRepository简化
    /// </summary>
    public abstract class BaseRepository<TEntity> : IRepository<TEntity>
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

        #region 模板方法

        /// <summary>
        /// 模板方法：子类覆盖提供关键字过滤逻辑
        /// </summary>
        /// <param name="query">基础查询</param>
        /// <param name="keyword">搜索关键字</param>
        /// <returns>应用过滤后的查询</returns>
        protected virtual IQueryable<TEntity> ApplyKeywordFilter(IQueryable<TEntity> query, string keyword)
        {
            // 默认不过滤，子类覆盖实现具体逻辑
            return query;
        }

        /// <summary>
        /// 模板方法：子类覆盖提供默认排序逻辑
        /// </summary>
        /// <param name="query">基础查询</param>
        /// <returns>应用排序后的查询</returns>
        protected virtual IQueryable<TEntity> ApplyDefaultOrdering(IQueryable<TEntity> query)
        {
            // 默认按CreatedAt降序
            return query.OrderByDescending(e => e.CreatedAt);
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id)
        {
            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            var entity = await _dbSet
                .Where(e => e.Id == id && !e.IsDeleted)
                .SingleOrDefaultAsync();

            _logger.LogDebug("[REPO] {EntityType}.GetById({Id}) → {Result}",
                typeof(TEntity).Name, id, entity != null ? "Found" : "NotFound");

            return entity;
        }

        // Issue #1766: 删除GetByIdAsync(Guid, params string[]) - 未被使用，MVP阶段Repository子类都实现自己的WithDetails方法

        // Issue #1766: 删除显式接口实现GetByIdAsync(Guid) - public方法已自动实现接口

        // Issue #1766: 删除GetByIdWithIncludesAsync - 未被使用，MVP阶段Repository子类都实现自己的WithDetails方法

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

        // IRepository实现
        async Task<IEnumerable<TEntity>> IRepository<TEntity>.GetAllAsync()
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

        /// <summary>
        /// 根据条件查询（优化版，支持预加载和分页）
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="includes">要预加载的导航属性</param>
        /// <param name="orderBy">排序表达式</param>
        /// <param name="skip">跳过的记录数</param>
        /// <param name="take">获取的记录数</param>
        /// <returns>查询结果</returns>
        public virtual async Task<List<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            string[]? includes = null,
            int? skip = null,
            int? take = null)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

            // 应用查询条件
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // 应用Include
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            // 应用排序
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            else
            {
                query = query.OrderByDescending(e => e.CreatedAt);
            }

            // 应用分页
            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// 获取投影查询结果（减少数据传输）
        /// </summary>
        /// <typeparam name="TResult">投影结果类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="selector">投影选择器</param>
        /// <returns>投影结果列表</returns>
        public virtual async Task<List<TResult>> SelectAsync<TResult>(
            Expression<Func<TEntity, bool>>? predicate,
            Expression<Func<TEntity, TResult>> selector)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.Select(selector).ToListAsync();
        }

        /// <summary>
        // Issue #1756: 删除GetPaginatedAsync - 未使用，功能与GetPagedAsync重复
        // 使用GetPagedAsync替代

        // IRepository实现
        async Task<IEnumerable<TEntity>> IRepository<TEntity>.FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await FindAsync(predicate);
        }

        /// <summary>
        /// 分页查询（使用模板方法模式）
        /// </summary>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字（可选）</param>
        /// <returns>分页结果</returns>
        /// <remarks>
        /// 子类通过覆盖ApplyKeywordFilter和ApplyDefaultOrdering提供自定义逻辑，
        /// 不再需要重写整个GetPagedAsync方法
        /// </remarks>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
        {
            var query = _dbSet.AsNoTracking().Where(e => !e.IsDeleted);

            // 应用关键字过滤（模板方法）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = ApplyKeywordFilter(query, keyword.Trim());
            }

            // 应用默认排序（模板方法）
            query = ApplyDefaultOrdering(query);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// 高级分页查询（支持动态过滤、排序和分页）
        /// Phase 6: 补全IRepository高级分页方法实现（Epic #2016）
        /// </summary>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="predicate">查询条件表达式（可选）</param>
        /// <param name="orderBy">排序表达式（可选）</param>
        /// <param name="ascending">是否升序排序（默认false降序）</param>
        /// <returns>分页结果对象</returns>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = false)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

            // 应用查询条件
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // 获取总数
            var totalCount = await query.CountAsync();

            // 应用排序
            if (orderBy != null)
            {
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            }
            else
            {
                query = query.OrderByDescending(e => e.CreatedAt);
            }

            // 应用分页
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
        }

        // Issue #1756: 删除GetPagedWithIncludesAsync - 未使用，功能与GetPagedAsync重复
        // 使用GetPagedAsync替代





        // IRepository GetSingleAsync实现
        async Task<TEntity?> IRepository<TEntity>.GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .SingleOrDefaultAsync();
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
        /// 检查指定ID的实体是否存在 (CODE-33)
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .AnyAsync(e => e.Id == id);
        }

        // Issue #1766: 删除显式接口实现ExistsAsync(Expression) - public方法已自动实现接口

        /// <summary>
        /// 获取实体总数（IRepository接口实现）
        /// </summary>
        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync(e => !e.IsDeleted);
        }

        /// <summary>
        /// 获取符合条件的实体数量
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.CountAsync();
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

            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

            await _dbSet.AddAsync(entity);
            await SaveChangesAsync();

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.Add({Id})", typeof(TEntity).Name, entity.Id);

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
                entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            }

            await _dbSet.AddRangeAsync(entityList);
            await SaveChangesAsync();

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.AddRange Count={Count}",
                typeof(TEntity).Name, entityList.Count);

            return entityList;
        }

        // IRepository AddRangeAsync显式接口实现
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

            _dbSet.Update(entity);
            await SaveChangesAsync();

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.Update({Id})", typeof(TEntity).Name, entity.Id);

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

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.UpdateRange Count={Count}",
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
                // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
                _logger.LogWarning("[REPO] {EntityType}.Delete({Id}) → NotFound", typeof(TEntity).Name, id);
                return false;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(entity);
            await SaveChangesAsync();

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.Delete({Id})", typeof(TEntity).Name, id);
            return true;
        }



        // Issue #1766: 删除显式接口实现DeleteAsync(Guid) - public方法已自动实现接口

        /// <summary>
        /// 批量软删除（根据条件表达式）
        /// </summary>
        public virtual async Task<int> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var entities = await _dbSet
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .ToListAsync();

            if (!entities.Any())
            {
                // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
                _logger.LogWarning("[REPO] {EntityType}.DeleteRange → NoMatch", typeof(TEntity).Name);
                return 0;
            }

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            _dbSet.UpdateRange(entities);
            await SaveChangesAsync();

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.DeleteRange Count={Count}",
                typeof(TEntity).Name, entities.Count);

            return entities.Count;
        }

        /// <summary>
        /// 恢复软删除记录 (CODE-32)
        /// </summary>
        public virtual async Task<bool> RestoreAsync(Guid id)
        {
            // 使用 IgnoreQueryFilters 查询已软删除的记录
            var entity = await _dbSet
                .IgnoreQueryFilters()
                .Where(e => e.Id == id && e.IsDeleted)
                .SingleOrDefaultAsync();

            if (entity == null)
            {
                _logger.LogWarning("[REPO] {EntityType}.Restore({Id}) -> NotFound or NotDeleted", typeof(TEntity).Name, id);
                return false;
            }

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(entity);
            await SaveChangesAsync();

            _logger.LogDebug("[REPO] {EntityType}.Restore({Id})", typeof(TEntity).Name, id);
            return true;
        }

        /// <summary>
        /// 物理删除实体（谨慎使用）
        /// </summary>
        public virtual async Task<bool> HardDeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
                _logger.LogWarning("[REPO] {EntityType}.HardDelete({Id}) → NotFound", typeof(TEntity).Name, id);
                return false;
            }

            _dbSet.Remove(entity);
            await SaveChangesAsync();

            // OpenSpec: enhance-dataflow-logging - LOG-015 Repository操作日志
            _logger.LogDebug("[REPO] {EntityType}.HardDelete({Id})", typeof(TEntity).Name, id);
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

        /// <summary>
        /// 通用分页查询助手 - 返回PagedResult<T>
        /// Phase 2: Repository层简化（Epic #1725）
        /// </summary>
        /// <param name="query">已配置的查询（包含Where和Include）</param>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>PagedResult分页结果</returns>
        protected async Task<PagedResult<TEntity>> GetPagedResultAsync(
            IQueryable<TEntity> query,
            int pageNumber,
            int pageSize)
        {
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
        }

        #endregion

        // Issue #1756: 删除BulkDeleteAsync - 未使用，EF Core已原生支持批量操作
        // 使用DeleteRangeAsync替代

        // Issue #1756: 删除事务方法 - 事务管理应在Service层直接使用 DbContext.Database.BeginTransactionAsync()
        // 移除的方法: BeginTransactionAsync, CommitTransactionAsync, RollbackTransactionAsync
        // 示例用法（Service层）:
        //   using var transaction = await _context.Database.BeginTransactionAsync();
        //   try {
        //       // 操作
        //       await transaction.CommitAsync();
        //   } catch {
        //       await transaction.RollbackAsync();
        //       throw;
        //   }

        #region 保护方法

        /// <summary>
        /// 保存更改
        /// 全局RowVersion同步：在SaveChanges前同步所有tracked实体的RowVersion，
        /// 防止同一请求内多次操作导致的不必要并发异常
        /// </summary>
        public virtual async Task<int> SaveChangesAsync()
        {
            try
            {
                // 全局RowVersion同步：遍历所有tracked实体
                // 将OriginalValue同步为CurrentValue，跳过乐观并发检查
                // 这对于同一请求内的多次操作是安全的，因为每次请求的数据都是最新查询的
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    // 只处理Modified和Unchanged状态的实体
                    // Added状态的实体没有RowVersion问题
                    // Deleted状态的实体不应该跳过并发检查
                    if (entry.State == EntityState.Modified || entry.State == EntityState.Unchanged)
                    {
                        // Issue #2250: 使用Metadata检查RowVersion属性是否存在
                        // 避免对PrescriptionItem等非BaseEntity实体抛出异常
                        var rowVersionPropertyMetadata = entry.Metadata.FindProperty("RowVersion");
                        if (rowVersionPropertyMetadata != null)
                        {
                            var rowVersionProperty = entry.Property("RowVersion");
                            rowVersionProperty.OriginalValue = rowVersionProperty.CurrentValue;
                        }
                    }
                }

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

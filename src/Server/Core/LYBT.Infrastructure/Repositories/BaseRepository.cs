using LYBT.Entities.Common;
using LYBT.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 仓储基类 - 精简版本
    /// 只保留核心CRUD操作（GetById/Add/Update/Delete）
    /// 复杂查询由各模块 Repository 自定义方法实现
    /// ADR-0017: 支持模块级 DbContext（TDbContext 泛型），模块仓储注入自己的 DbContext
    /// A-31-C5-3: 镜像方法模板化（GetByIdIncludingDeleted/Exists 谓词尾）——各模块仓储同构方法上收
    /// </summary>
    public abstract class BaseRepository<TEntity, TDbContext> : IRepository<TEntity>
        where TEntity : BaseEntity
        where TDbContext : DbContext
    {
        protected readonly TDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly ILogger _logger;

        protected BaseRepository(TDbContext context, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = _context.Set<TEntity>();
        }

        #region 查询操作

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet
                .Where(e => e.Id == id && !e.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            _logger.LogDebug("[REPO] {EntityType}.GetById({Id}) → {Result}",
                typeof(TEntity).Name, id, entity != null ? "Found" : "NotFound");

            return entity;
        }

        /// <summary>
        /// 根据ID获取实体（包含已删除记录，恢复操作使用）
        /// A-31-C5-3: Patient/Herb 等仓储同构方法上收；含额外 Include 的仓储（如 Formula）保留特化
        /// </summary>
        public virtual async Task<TEntity?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        #endregion

        #region 创建操作

        /// <summary>
        /// 添加实体
        /// </summary>
        public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;

            await _dbSet.AddAsync(entity, cancellationToken);
            await SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[REPO] {EntityType}.Add({Id})", typeof(TEntity).Name, entity.Id);

            return entity;
        }

        #endregion

        #region 更新操作

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[REPO] {EntityType}.Update({Id})", typeof(TEntity).Name, entity.Id);

            return entity;
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 软删除实体
        /// </summary>
        public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity == null)
            {
                _logger.LogWarning("[REPO] {EntityType}.Delete({Id}) → NotFound", typeof(TEntity).Name, id);
                return false;
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(entity);
            await SaveChangesAsync(cancellationToken);
            _logger.LogDebug("[REPO] {EntityType}.Delete({Id})", typeof(TEntity).Name, id);
            return true;
        }

        #endregion

        #region 保护方法

        /// <summary>
        /// 按谓词判断实体是否存在（软删除过滤 + 可选排除指定 ID）
        /// A-31-C5-3: 各模块 ExistsByNameAsync 镜像方法上收，仅传入名称匹配谓词
        /// </summary>
        protected virtual async Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate,
            Guid? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(predicate);

            if (excludeId.HasValue)
                query = query.Where(e => e.Id != excludeId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        /// <summary>
        /// 保存更改 — 依赖EF Core原生乐观并发检查（RowVersion）
        /// </summary>
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "并发冲突 - 类型: {EntityType}", typeof(TEntity).Name);
                throw new InvalidOperationException("数据已被其他用户修改，请刷新后重试", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "数据库更新失败 - 类型: {EntityType}", typeof(TEntity).Name);
                throw;
            }
        }

        #endregion
    }
}

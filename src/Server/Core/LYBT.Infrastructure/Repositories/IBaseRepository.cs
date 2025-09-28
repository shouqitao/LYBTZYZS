using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LYBT.Entities.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// 仓储基础接口
    /// 定义通用的数据访问操作契约
    /// </summary>
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        #region 查询操作

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        Task<TEntity> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据ID获取实体（包含关联数据）
        /// </summary>
        Task<TEntity> GetByIdWithIncludesAsync(Guid id, params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        Task<List<TEntity>> GetAllAsync();

        /// <summary>
        /// 根据条件查询
        /// </summary>
        Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<(List<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool descending = true);

        /// <summary>
        /// 检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 获取数量
        /// </summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

        #endregion

        #region 创建操作

        /// <summary>
        /// 添加实体
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        Task<List<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

        #endregion

        #region 更新操作

        /// <summary>
        /// 更新实体
        /// </summary>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// 批量更新实体
        /// </summary>
        Task UpdateRangeAsync(IEnumerable<TEntity> entities);

        #endregion

        #region 删除操作

        /// <summary>
        /// 软删除实体
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量软删除实体
        /// </summary>
        Task<int> DeleteRangeAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 物理删除实体（谨慎使用）
        /// </summary>
        Task<bool> HardDeleteAsync(Guid id);

        #endregion

        #region 高级查询

        /// <summary>
        /// 获取可查询对象
        /// </summary>
        IQueryable<TEntity> GetQueryable();

        /// <summary>
        /// 获取不跟踪的查询对象
        /// </summary>
        IQueryable<TEntity> GetNoTrackingQueryable();

        /// <summary>
        /// 执行SQL查询
        /// </summary>
        Task<List<TEntity>> FromSqlRawAsync(string sql, params object[] parameters);

        #endregion

        #region 事务操作

        /// <summary>
        /// 开始事务
        /// </summary>
        Task<IDbContextTransaction> BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync(IDbContextTransaction transaction);

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync(IDbContextTransaction transaction);

        #endregion
    }
}
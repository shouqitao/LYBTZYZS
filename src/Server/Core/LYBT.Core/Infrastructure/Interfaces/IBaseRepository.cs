using System.Linq.Expressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Core.Infrastructure.Interfaces
{
    /// <summary>
    /// 只读仓储接口 - 用于只需要查询功能的场景
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public interface IReadOnlyRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        Task<TEntity?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// 根据条件查找
        /// </summary>
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 获取分页数据
        /// </summary>
        Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// 根据条件获取分页数据
        /// </summary>
        Task<PagedResult<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool ascending = true);

        /// <summary>
        /// 获取单个实体
        /// </summary>
        Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 根据条件检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        Task<long> CountAsync();

        /// <summary>
        /// 根据条件获取记录总数
        /// </summary>
        Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate);
    }

    /// <summary>
    /// 完整仓储接口 - 包含增删改查功能
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public interface IRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class
    {
        // 创建操作
        Task<TEntity> AddAsync(TEntity entity);
        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

        // 更新操作
        Task<TEntity> UpdateAsync(TEntity entity);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities);

        // 删除操作
        Task<bool> DeleteAsync(TEntity entity);
        Task<bool> DeleteAsync(Guid id);
        Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities);
        Task<int> DeleteRangeAsync(IEnumerable<Guid> ids);
    }

    /// <summary>
    /// 基础仓储接口 - 继承自IRepository并添加特定功能
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public interface IBaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        // 高级查询功能
        IQueryable<TEntity> GetQueryable();
        IQueryable<TEntity> GetNoTrackingQueryable();

        // 批量操作
        Task<int> BulkDeleteAsync(List<Guid> ids);

        // 事务支持
        Task<int> SaveChangesAsync();
    }
}

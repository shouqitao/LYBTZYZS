using System.Linq.Expressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Interfaces
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
}

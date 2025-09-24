using System.Linq.Expressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 统一仓储接口 - Solution级架构标准化
    /// 定义所有模块仓储的通用规范，确保数据访问层架构一致性
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// 根据条件查找
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 获取分页数据
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// 根据条件获取分页数据
        /// </summary>
        Task<PagedResult<T>> GetPagedAsync(
            Expression<Func<T, bool>>? predicate,
            int pageNumber,
            int pageSize,
            Expression<Func<T, object>>? orderBy = null,
            bool ascending = true);

        /// <summary>
        /// 获取单个实体
        /// </summary>
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Guid id);

        /// <summary>
        /// 根据条件检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        Task<long> CountAsync();

        /// <summary>
        /// 根据条件获取记录总数
        /// </summary>
        Task<long> CountAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// 添加实体
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 更新实体
        /// </summary>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// 删除实体
        /// </summary>
        Task<bool> DeleteAsync(T entity);

        /// <summary>
        /// 根据ID删除实体
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        Task<int> DeleteRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// 根据ID批量删除
        /// </summary>
        Task<int> DeleteRangeAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
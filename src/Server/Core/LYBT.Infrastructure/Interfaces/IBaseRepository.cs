using LYBT.Shared.Models.Contracts.Common;
using System.Linq.Expressions;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 基础仓储接口 - Solution级架构标准化
    /// 定义所有模块仓储的通用规范，确保数据访问层架构一致性
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public interface IBaseRepository<TEntity> where TEntity : class
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

        /// <summary>
        /// 添加实体
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 更新实体
        /// </summary>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// 删除实体
        /// </summary>
        Task<bool> DeleteAsync(TEntity entity);

        /// <summary>
        /// 根据ID删除实体
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 根据ID批量删除
        /// </summary>
        Task<int> DeleteRangeAsync(IEnumerable<Guid> ids);

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync();
    }

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
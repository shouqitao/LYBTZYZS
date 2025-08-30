using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Repositories.Base
{
    /// <summary>
    /// 通用Repository接口 - UltraThink重构架构
    /// 基于DDD和CQRS模式的Repository接口定义
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IRepository<TEntity, TKey> where TEntity : class
    {
        #region Query Operations (CQRS - Query Side)

        /// <summary>
        /// 根据ID获取实体
        /// </summary>
        Task<TEntity?> GetByIdAsync(TKey id);

        /// <summary>
        /// 根据ID获取实体（只读）
        /// </summary>
        Task<TEntity?> GetByIdAsNoTrackingAsync(TKey id);

        /// <summary>
        /// 获取所有实体
        /// </summary>
        Task<List<TEntity>> GetAllAsync();

        /// <summary>
        /// 根据条件获取实体列表
        /// </summary>
        Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 获取第一个匹配的实体
        /// </summary>
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 分页查询
        /// </summary>
        Task<PagedResult<TEntity>> GetPagedAsync<TDto>(
            IPagedQuery<TDto> query,
            Expression<Func<TEntity, bool>>? predicate = null,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false) where TDto : class;

        /// <summary>
        /// 获取数量
        /// </summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

        /// <summary>
        /// 检查是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 获取查询对象
        /// </summary>
        IQueryable<TEntity> Query();

        /// <summary>
        /// 获取只读查询对象
        /// </summary>
        IQueryable<TEntity> QueryAsNoTracking();

        #endregion

        #region Command Operations (CQRS - Command Side)

        /// <summary>
        /// 添加实体
        /// </summary>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// 批量添加实体
        /// </summary>
        Task<List<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 更新实体
        /// </summary>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// 批量更新实体
        /// </summary>
        Task UpdateRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 删除实体
        /// </summary>
        Task<bool> DeleteAsync(TKey id);

        /// <summary>
        /// 删除实体
        /// </summary>
        Task DeleteAsync(TEntity entity);

        /// <summary>
        /// 批量删除实体
        /// </summary>
        Task DeleteRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 根据条件删除
        /// </summary>
        Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate);

        #endregion

        #region Unit of Work Support

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackTransactionAsync();

        #endregion
    }

    /// <summary>
    /// 简化的Repository接口 - Guid主键
    /// </summary>
    public interface IRepository<TEntity> : IRepository<TEntity, Guid> where TEntity : class
    {
    }

    /// <summary>
    /// 分页查询接口
    /// </summary>
    public interface IPagedQuery<TDto> where TDto : class
    {
        int PageIndex { get; set; }
        int PageSize { get; set; }
        string SearchTerm { get; set; }
        string SortField { get; set; }
        string SortDirection { get; set; }
    }

    /// <summary>
    /// Repository规约接口 - DDD规约模式
    /// </summary>
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> Criteria { get; }
        List<Expression<Func<T, object>>> Includes { get; }
        Expression<Func<T, object>> OrderBy { get; }
        Expression<Func<T, object>> OrderByDescending { get; }
        int Take { get; }
        int Skip { get; }
        bool IsPagingEnabled { get; }
    }
}
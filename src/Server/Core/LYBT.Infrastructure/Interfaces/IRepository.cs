/// <summary>
/// P3-Fix 通用仓储接口 - 解决测试项目编译错误
/// 最小化接口定义，仅用于编译通过
/// </summary>

using System.Linq.Expressions;

namespace LYBT.Infrastructure.Interfaces
{
    /// <summary>
    /// 通用仓储接口
    /// </summary>
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
        /// 添加实体
        /// </summary>
        Task<T> AddAsync(T entity);
        
        /// <summary>
        /// 更新实体
        /// </summary>
        Task<T> UpdateAsync(T entity);
        
        /// <summary>
        /// 删除实体
        /// </summary>
        Task DeleteAsync(T entity);
        
        /// <summary>
        /// 根据ID删除
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}
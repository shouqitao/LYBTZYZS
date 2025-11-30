using System.Linq.Expressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// Repository泛型接口（Infrastructure层）
/// 提供标准CRUD操作，适用于聚合根实体
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
/// <remarks>
/// 设计原则：
/// - 统一共性（14个标准CRUD方法）
/// - 保持特性（各模块可保留特定业务方法）
/// - 使用Guid作为ID类型（对齐BaseEntity设计）
/// - 所有方法均为异步方法（Async后缀）
///
/// 使用示例：
/// <code>
/// public interface IUserRepository : IRepository&lt;User&gt;
/// {
///     // 保留用户模块特定业务方法
///     Task&lt;User?&gt; GetByUsernameAsync(string username);
/// }
/// </code>
/// </remarks>
public interface IRepository<T> where T : class
{
    // ========== 查询方法 (5个) ==========

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体唯一标识（Guid类型）</param>
    /// <returns>实体对象，不存在时返回null</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有实体（仅用于小数据量场景，如下拉列表）
    /// </summary>
    /// <returns>实体集合</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 分页查询实体
    /// </summary>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键字（可选，支持名称/拼音码搜索）</param>
    /// <returns>分页结果对象（包含数据列表、总数、页码等信息）</returns>
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null);

    /// <summary>
    /// 条件查询（谨慎使用，建议使用具体业务方法）
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的实体集合</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 获取单个实体（条件查询，期望唯一结果）
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>单个实体对象，不存在时返回null</returns>
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

    // ========== 写入方法 (6个) ==========

    /// <summary>
    /// 新增实体
    /// </summary>
    /// <param name="entity">待新增的实体对象</param>
    /// <returns>新增后的实体对象（包含生成的ID）</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">待更新的实体对象</param>
    /// <returns>更新后的实体对象</returns>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// 删除实体（软删除或物理删除，由实现决定）
    /// </summary>
    /// <param name="id">实体唯一标识（Guid类型）</param>
    /// <returns>删除成功返回true，否则返回false</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 批量新增实体
    /// </summary>
    /// <param name="entities">待新增的实体集合</param>
    /// <returns>新增后的实体集合</returns>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// 批量删除实体（软删除）
    /// </summary>
    /// <param name="entities">待删除的实体集合</param>
    /// <returns>成功删除的数量</returns>
    Task<int> DeleteRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// 批量删除实体（根据ID集合，软删除）
    /// </summary>
    /// <param name="ids">待删除的实体ID集合</param>
    /// <returns>成功删除的数量</returns>
    Task<int> DeleteRangeAsync(IEnumerable<Guid> ids);

    // ========== 辅助方法 (3个) ==========

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="id">实体唯一标识（Guid类型）</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// 获取实体总数
    /// </summary>
    /// <returns>实体总数</returns>
    Task<int> CountAsync();

    /// <summary>
    /// 保存更改（通常由Service层调用，Repository层实现可选）
    /// </summary>
    /// <returns>受影响的行数</returns>
    Task<int> SaveChangesAsync();
}

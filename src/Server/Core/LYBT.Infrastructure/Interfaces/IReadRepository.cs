using System.Linq.Expressions;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 只读Repository泛型接口（Infrastructure层）
/// 提供5个核心查询方法，不包含写操作
/// 适用于从属实体（Subordinate Entity）
/// </summary>
/// <typeparam name="T">实体类型，必须是引用类型</typeparam>
/// <remarks>
/// 三层架构说明：
/// - Layer 1: IReadRepository&lt;T&gt; - 只读查询，适用于从属实体
/// - Layer 2: IRepository&lt;T&gt; - 完整CRUD，适用于聚合根
/// - Layer 3: 模块特定仓储 - 专门化实现
/// 
/// 从属实体使用此接口：Consultation, Prescription
/// 聚合根实体使用 IRepository&lt;T&gt;：User, Patient, Herb, Formula, MedicalCase
/// 
/// 设计原则：
/// - 遵循DDD聚合根边界（AR-001）
/// - 从属实体通过聚合根完成写操作
/// - 支持软删除模式（IsDeleted标志）
/// - 所有数据库操作必须异步
/// </remarks>
public interface IReadRepository<T> where T : class
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体唯一标识符</param>
    /// <returns>找到的实体，不存在则返回null</returns>
    /// <remarks>
    /// 自动过滤软删除记录（IsDeleted = true）
    /// </remarks>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>所有实体的集合</returns>
    /// <remarks>
    /// 注意：对于大数据集，建议使用分页查询避免性能问题
    /// 自动过滤软删除记录（IsDeleted = true）
    /// </remarks>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 根据条件查询实体
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的实体集合</returns>
    /// <remarks>
    /// 自动过滤软删除记录（IsDeleted = true）
    /// </remarks>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 根据条件获取单个实体
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的实体，不存在则返回null</returns>
    /// <exception cref="InvalidOperationException">找到多个匹配实体时抛出</exception>
    /// <remarks>
    /// 自动过滤软删除记录（IsDeleted = true）
    /// </remarks>
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 统计实体总数量
    /// </summary>
    /// <returns>实体总数</returns>
    /// <remarks>
    /// 自动过滤软删除记录（IsDeleted = true）
    /// </remarks>
    Task<long> CountAsync();
}

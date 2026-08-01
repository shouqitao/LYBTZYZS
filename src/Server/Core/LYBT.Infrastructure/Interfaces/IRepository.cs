using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// Repository泛型接口（Infrastructure层）
/// 提供核心CRUD操作，适用于聚合根实体
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
/// <remarks>
/// 设计原则：
/// - 只保留实际使用的4个核心方法（GetById/Add/Update/Delete）
/// - 复杂查询由各模块 Repository 自定义方法实现（如 WithDetails 系列）
/// - 使用Guid作为ID类型（对齐BaseEntity设计）
/// - 所有方法均为异步方法（Async后缀）
///
/// 使用示例：
/// <code>
/// public interface IPatientRepository : IRepository&lt;Patient&gt;
/// {
///     // 保留模块特定业务方法
///     Task&lt;Patient?&gt; GetByPhoneAsync(string phone);
/// }
/// </code>
/// </remarks>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体唯一标识（Guid类型）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，不存在时返回null</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增实体
    /// </summary>
    /// <param name="entity">待新增的实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新增后的实体对象（包含生成的ID）</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">待更新的实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的实体对象</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体（软删除或物理删除，由实现决定）
    /// </summary>
    /// <param name="id">实体唯一标识（Guid类型）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功返回true，否则返回false</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

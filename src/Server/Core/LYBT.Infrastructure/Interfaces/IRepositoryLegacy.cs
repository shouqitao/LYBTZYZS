using System.Linq.Expressions;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 【已废弃】旧版统一仓储接口 - 仅用于向后兼容
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
/// <remarks>
/// ⚠️ 此接口已废弃，请迁移至新的三层Repository架构：
/// 
/// 迁移指南：
/// - 聚合根实体（User, Patient, Herb, Formula, MedicalCase）
///   → 使用 LYBT.Shared.Models.Interfaces.IRepository&lt;T&gt;
///   
/// - 从属实体（Consultation, Prescription）
///   → 使用 LYBT.Shared.Models.Interfaces.IReadRepository&lt;T&gt;
///   
/// - 详见：docs/explanation/architecture/shared/repository-generic-interface-refactoring-design.md
/// 
/// 预计移除时间：Phase 3 完成后（所有模块迁移完成）
/// 相关Issue：#2016 Repository泛型接口统一重构
/// </remarks>
[Obsolete("此接口已废弃，请使用 LYBT.Shared.Models.Interfaces.IRepository<T> 或 IReadRepository<T>。详见迁移文档。", false)]
public interface IRepositoryLegacy<T> where T : class
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

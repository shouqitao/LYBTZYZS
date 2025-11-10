using System.Linq.Expressions;

namespace LYBT.Infrastructure.Interfaces;

/// <summary>
/// 只读Repository泛型接口 - 用于从属实体模块
/// 提供5个核心查询方法，不包含写操作
/// </summary>
/// <typeparam name="T">实体类型，必须是引用类型</typeparam>
/// <remarks>
/// 适用场景：
/// - 从属实体模块（Consultation, Prescription）
/// - 写操作通过聚合根（MedicalCase）完成
/// - 符合DDD聚合根边界原则（AR-001）
/// </remarks>
public interface IReadRepository<T> where T : class
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体唯一标识符</param>
    /// <returns>找到的实体，不存在则返回null</returns>
    /// <example>
    /// <code>
    /// var consultation = await _repository.GetByIdAsync(consultationId);
    /// if (consultation == null)
    ///     throw new NotFoundException("辨证记录不存在");
    /// </code>
    /// </example>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>所有实体的集合</returns>
    /// <remarks>
    /// ⚠️ 注意：对于大数据集，建议使用分页查询避免性能问题
    /// </remarks>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 根据条件查询实体
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的实体集合</returns>
    /// <example>
    /// <code>
    /// // 查询某个病案的所有辨证记录
    /// var consultations = await _repository.FindAsync(
    ///     c => c.MedicalCaseId == medicalCaseId);
    /// </code>
    /// </example>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 根据条件获取单个实体
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的实体，不存在则返回null</returns>
    /// <exception cref="InvalidOperationException">找到多个匹配实体时抛出</exception>
    /// <example>
    /// <code>
    /// var consultation = await _repository.GetSingleAsync(
    ///     c => c.Id == id);
    /// </code>
    /// </example>
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 统计实体总数量
    /// </summary>
    /// <returns>实体总数</returns>
    Task<long> CountAsync();
}

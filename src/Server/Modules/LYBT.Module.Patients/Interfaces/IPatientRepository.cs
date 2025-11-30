using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Repositories;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 病人仓储接口 - 优化版，包含查询优化方法
    /// </summary>
    /// <summary>
/// 患者仓储接口 - 继承IRepository<Patient>标准接口
/// Task 1.2: PatientRepository重构，适配新的简化Repository设计
/// </summary>
/// <remarks>
/// 设计原则：
/// - ⭐ 继承BaseRepository：复用11个标准CRUD方法
/// - ⭐ 业务扩展：实现患者特定的业务查询方法
/// - ⭐ 接口隔离：职责单一，符合ISP原则
///
/// 特定业务方法说明：
/// - GetByNameAsync: 根据姓名模糊查询患者
/// - ExistsAsync: 检查患者姓名唯一性（支持排除ID）
/// - GetByDateRangeAsync: 按创建日期范围查询患者
/// - GetByPhoneNumberAsync: 手机号重复检查（Epic #1934 BR-004）
/// </remarks>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// 根据姓名获取患者（支持模糊匹配）
    /// </summary>
    /// <param name="name">患者姓名</param>
    /// <returns>患者列表，不存在返回空列表</returns>
    Task<List<Patient>> GetByNameAsync(string name);

    /// <summary>
    /// 检查患者姓名是否已存在
    /// </summary>
    /// <param name="name">患者姓名</param>
    /// <param name="excludeId">排除的患者ID（用于更新时检查）</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ExistsAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// 根据日期范围获取患者（按创建日期）
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>患者列表</returns>
    Task<List<Patient>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号查询患者（Epic #1934 BR-004重复检查）
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>患者对象，不存在返回null</returns>
    Task<Patient?> GetByPhoneNumberAsync(string phoneNumber);
}
}

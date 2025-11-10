using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Repositories;
using LYBT.Shared.Models.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 病人仓储接口 - 优化版，包含查询优化方法
    /// </summary>
    /// <summary>
/// 患者仓储接口 - 继承IBaseRepository<Patient>标准接口
/// Phase 1 Task 1.3: 实现基础数据模块统一Repository规范
/// </summary>
/// <remarks>
/// 设计原则：
/// - ⭐ 统一共性：继承IBaseRepository<Patient>获得11个标准CRUD方法
/// - ⭐ 保持特性：保留患者模块特定业务方法
/// 
/// 特定业务方法说明：
/// - SearchPatientsAsync: 多条件搜索（姓名/拼音码/电话/身份证）
/// - BatchCreateAsync: 批量导入患者（Epic #1934）
/// - GetByPhoneNumberAsync: 手机号重复检查（Epic #1934 BR-004）
/// </remarks>
public interface IPatientRepository : IBaseRepository<Patient>
{
    /// <summary>
    /// 搜索患者（支持多条件和分页）
    /// </summary>
    /// <param name="searchTerm">搜索词（姓名/拼音码/电话/身份证）</param>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>分页搜索结果</returns>
    Task<PaginatedList<Patient>> SearchPatientsAsync(string? searchTerm, int pageIndex, int pageSize);

    /// <summary>
    /// 批量创建患者（Epic #1934 FR-001）
    /// </summary>
    /// <param name="patients">待创建的患者列表</param>
    /// <returns>创建成功的患者列表</returns>
    Task<List<Patient>> BatchCreateAsync(IEnumerable<Patient> patients);

    /// <summary>
    /// 根据手机号查询患者（Epic #1934 BR-004重复检查）
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>患者对象，不存在返回null</returns>
    Task<Patient?> GetByPhoneNumberAsync(string phoneNumber);
}
}

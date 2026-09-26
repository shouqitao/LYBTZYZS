using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Module.Patients.Interfaces;

/// <summary>
/// 患者仓储接口。
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// 分页查询患者（支持状态筛选）。
    /// </summary>
    Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword, CommonStatus? status, CancellationToken ct);

    /// <summary>
    /// 检查患者姓名是否已存在。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>手机号是否已存在（P2 US-PAT-003/004: 需求电话唯一语义——原仅姓名查重）</summary>
    Task<bool> ExistsByPhoneAsync(string phoneNumber, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// 根据姓名精确获取患者（仅 Name 精确匹配，排除已删除）。
    /// </summary>
    Task<Patient?> GetExactByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// 根据身份证号查询患者。
    /// </summary>
    Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken ct);

    /// <summary>
    /// 根据ID获取患者（含已删除）。
    /// </summary>
    Task<Patient?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 开启本模块 DbContext 的显式事务（ADR-0030：同上下文事务——批量导入的原子边界由调用方控制提交/回滚）。
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Interfaces;

/// <summary>
/// 患者仓储接口。
/// </summary>
public interface IPatientRepository
{
    /// <summary>
    /// 根据ID获取患者。
    /// </summary>
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 分页查询患者。
    /// </summary>
    Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);

    /// <summary>
    /// 分页查询患者（支持状态筛选）。
    /// </summary>
    Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword, CommonStatus? status, CancellationToken ct);

    /// <summary>
    /// 检查患者姓名是否已存在。
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default);

    /// <summary>
    /// 新增患者。
    /// </summary>
    Task AddAsync(Patient patient, CancellationToken ct);

    /// <summary>
    /// 更新患者。
    /// </summary>
    Task UpdateAsync(Patient patient, CancellationToken ct);

    /// <summary>
    /// 根据身份证号查询患者。
    /// </summary>
    Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken ct);
}



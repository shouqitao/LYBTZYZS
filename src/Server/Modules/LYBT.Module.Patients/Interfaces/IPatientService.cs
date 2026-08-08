using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces;

/// <summary>
/// 患者服务接口 — 封装简单 CRUD 操作，供 Controller 直接注入。
/// </summary>
public interface IPatientService
{
    Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled, CancellationToken ct);
    Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Result<PatientDetailDto>> GetByIdNumberAsync(string idNumber, CancellationToken ct);
}

using LYBT.Module.Patients.Application.Mappers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Patients.Services;

/// <summary>
/// 患者服务实现 — 读操作直查（写操作已收敛至 MediatR Handler，见蓝图 §2.2）。
/// </summary>
internal class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;

    public PatientService(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, bool filterDisabled, CancellationToken ct)
    {
        var statusFilter = filterDisabled ? CommonStatus.Enabled : (CommonStatus?)null;
        var result = await _patientRepository.GetPagedAsync(page, pageSize, keyword, statusFilter, ct);
        var dtos = result.Items.Select(PatientMapper.ToListDto).ToList();
        var pagedResult = new PagedResult<PatientListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
        return Result<PagedResult<PatientListDto>>.Success(pagedResult);
    }

    public async Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByIdAsync(id, ct);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, ErrorMessages.Get(ErrorCode.PatientNotFound));
        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }

    public async Task<Result<PatientDetailDto>> GetByIdNumberAsync(string idNumber, CancellationToken ct)
    {
        var patient = await _patientRepository.GetByIdNumberAsync(idNumber, ct);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "未找到匹配的患者");
        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}

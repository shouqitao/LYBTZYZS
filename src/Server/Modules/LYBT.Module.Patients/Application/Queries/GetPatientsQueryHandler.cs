using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Common;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 获取患者分页列表查询处理器。
/// </summary>
public class GetPatientsQueryHandler : IRequestHandler<GetPatientsQuery, Result<PagedResult<PatientListDto>>>
{
    private readonly IPatientRepository _patientRepository;

    public GetPatientsQueryHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PagedResult<PatientListDto>>> Handle(
        GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var statusFilter = request.FilterDisabled ? CommonStatus.Enabled : (CommonStatus?)null;
        var result = await _patientRepository.GetPagedAsync(
            request.Page, request.PageSize, request.Keyword, statusFilter, cancellationToken);

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
}



using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Application.Queries;

public class SearchPatientByIdNumberQueryHandler(
    IPatientRepository patientRepository) : IRequestHandler<SearchPatientByIdNumberQuery, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository = patientRepository;

    public async Task<Result<PatientDetailDto>> Handle(
        SearchPatientByIdNumberQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdNumberAsync(request.IdNumber, cancellationToken);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "未找到匹配的患者");

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}

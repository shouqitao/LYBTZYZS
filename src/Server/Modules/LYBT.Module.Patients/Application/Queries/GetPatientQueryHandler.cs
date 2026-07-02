using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Queries;

/// <summary>
/// 根据ID获取患者详情查询处理器。
/// </summary>
public class GetPatientQueryHandler : IRequestHandler<GetPatientQuery, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository;

    public GetPatientQueryHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientDetailDto>> Handle(
        GetPatientQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);

        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "患者不存在");

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}



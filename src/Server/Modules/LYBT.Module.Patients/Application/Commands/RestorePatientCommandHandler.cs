using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 恢复已删除患者命令处理器。
/// </summary>
public class RestorePatientCommandHandler : IRequestHandler<RestorePatientCommand, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository;

    public RestorePatientCommandHandler(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<Result<PatientDetailDto>> Handle(
        RestorePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, ErrorMessages.Get(ErrorCode.PatientNotFound));

        if (!patient.IsDeleted)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotDeleted, ErrorMessages.Get(ErrorCode.PatientNotDeleted));

        patient.Restore(request.CurrentUserId);
        await _patientRepository.UpdateAsync(patient, cancellationToken);
        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}

using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 恢复已软删除的患者命令处理器。
/// </summary>
public class RestorePatientCommandHandler(
    IPatientRepository patientRepository) : IRequestHandler<RestorePatientCommand, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository = patientRepository;

    public async Task<Result<PatientDetailDto>> Handle(
        RestorePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdIncludingDeletedAsync(request.PatientId, cancellationToken);
        if (patient == null)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "患者不存在");

        if (!patient.IsDeleted)
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotDeleted, "该患者未被删除");

        patient.Restore(request.OperatorId);
        await _patientRepository.UpdateAsync(patient, cancellationToken);

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}

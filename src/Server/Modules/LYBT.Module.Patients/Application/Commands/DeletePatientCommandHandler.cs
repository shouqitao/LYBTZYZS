using MediatR;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using LYBT.SharedKernel.Events;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Domain.Events;
using LYBT.Module.Patients.Interfaces;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 删除患者命令处理器（软删除）。
/// </summary>
public class DeletePatientCommandHandler : IRequestHandler<DeletePatientCommand, Result>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeletePatientCommandHandler(
        IPatientRepository patientRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _patientRepository = patientRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        DeletePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (patient == null)
            return Result.Failure(ErrorCode.PatientNotFound, "患者不存在");

        patient.SoftDelete(request.CurrentUserId);

        await _patientRepository.UpdateAsync(patient, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new PatientDeletedEvent(patient.Id, patient.Name, request.CurrentUserId)
        }, cancellationToken);

        return Result.Success();
    }
}



using MediatR;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Events;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Domain.Events;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Application.Mappers;

namespace LYBT.Module.Patients.Application.Commands;

/// <summary>
/// 创建患者命令处理器。
/// </summary>
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientDetailDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreatePatientCommandHandler(
        IPatientRepository patientRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _patientRepository = patientRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result<PatientDetailDto>> Handle(
        CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _patientRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<PatientDetailDto>.Failure(ErrorCode.PatientNotFound, "患者姓名已存在");

        var patient = PatientMapper.ToEntity(dto, request.CurrentUserId);

        await _patientRepository.AddAsync(patient, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new PatientCreatedEvent(patient.Id, patient.Name, patient.Gender, request.CurrentUserId)
        }, cancellationToken);

        return Result<PatientDetailDto>.Success(PatientMapper.ToDetailDto(patient));
    }
}



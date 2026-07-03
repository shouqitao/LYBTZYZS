using LYBT.Entities.Registrations;
using LYBT.Module.Registration.Application.Mappers;
using LYBT.Module.Registration.Domain.Events;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using MediatR;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 创建挂号处理器。
/// </summary>
public sealed class CreateRegistrationCommandHandler
    : IRequestHandler<CreateRegistrationCommand, Result<RegistrationDetailDto>>
{
    private readonly IRegistrationRepository _repository;
    private readonly IPublisher _publisher;

    public CreateRegistrationCommandHandler(
        IRegistrationRepository repository,
        IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<Result<RegistrationDetailDto>> Handle(
        CreateRegistrationCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        var maxQueueNumber = await _repository.GetTodayMaxQueueNumberAsync(cancellationToken);

        var registration = new RegistrationEntity
        {
            Id = Guid.NewGuid(),
            PatientId = dto.PatientId,
            PatientName = dto.PatientName,
            DoctorId = dto.DoctorId,
            DoctorName = dto.DoctorName,
            Source = dto.Source,
            Status = dto.Source == RegistrationSource.Doctor
                ? RegistrationStatus.InProgress
                : RegistrationStatus.Waiting,
            QueueNumber = maxQueueNumber + 1,
            RegistrationFee = dto.RegistrationFee,
            Remark = dto.Remark
        };

        await _repository.AddAsync(registration, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new RegistrationCreatedEvent(
            registration.Id,
            registration.PatientId,
            registration.PatientName,
            registration.DoctorId,
            registration.DoctorName,
            registration.Source,
            registration.Status,
            registration.QueueNumber), cancellationToken);

        return Result<RegistrationDetailDto>.Success(RegistrationMapper.ToDetailDto(registration));
    }
}



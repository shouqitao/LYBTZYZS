using LYBT.Module.Registration.Domain.Events;
using LYBT.Module.Registration.Interfaces;
using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 取消挂号处理器。
/// </summary>
public sealed class CancelRegistrationCommandHandler
    : IRequestHandler<CancelRegistrationCommand>
{
    private readonly IRegistrationRepository _repository;
    private readonly IPublisher _publisher;

    public CancelRegistrationCommandHandler(
        IRegistrationRepository repository,
        IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(
        CancelRegistrationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("挂号记录不存在");

        entity.Cancel();
        await _repository.UpdateAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new RegistrationCancelledEvent(
            entity.Id,
            entity.PatientId,
            entity.PatientName,
            entity.DoctorId), cancellationToken);
    }
}



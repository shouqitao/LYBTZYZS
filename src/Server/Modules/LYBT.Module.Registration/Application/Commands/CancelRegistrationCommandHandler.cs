using LYBT.Module.Registration.Domain.Events;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.SharedKernel.Common;
using MediatR;

namespace LYBT.Module.Registration.Application.Commands;

/// <summary>
/// 取消挂号处理器。
/// </summary>
public sealed class CancelRegistrationCommandHandler
    : IRequestHandler<CancelRegistrationCommand, Result>
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

    public async Task<Result> Handle(
        CancelRegistrationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (entity is null)
            return Result.Failure(ErrorCode.RegistrationNotFound, ErrorMessages.Get(ErrorCode.RegistrationNotFound));

        try
        {
            entity.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            var errorCode = ex.Message.Contains("医案") || ex.Message.Contains("关联")
                ? ErrorCode.RegistrationCancelNotAllowed
                : ErrorCode.RegistrationInvalidStatusTransition;
            return Result.Failure(errorCode, ex.Message);
        }

        await _repository.UpdateAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new RegistrationCancelledEvent(
            entity.Id,
            entity.PatientId,
            entity.PatientName,
            entity.DoctorId), cancellationToken);

        return Result.Success();
    }
}



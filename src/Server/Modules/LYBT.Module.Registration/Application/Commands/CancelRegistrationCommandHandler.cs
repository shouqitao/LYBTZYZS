using LYBT.Infrastructure.SharedKernel.Events;
using LYBT.Module.Registrations.Domain.Events;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Registrations.Application.Commands;

/// <summary>
/// 取消挂号处理器。
/// </summary>
public sealed class CancelRegistrationCommandHandler
    : IRequestHandler<CancelRegistrationCommand, Result>
{
    private readonly IRegistrationRepository _repository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly INotificationService _notificationService;

    public CancelRegistrationCommandHandler(
        IRegistrationRepository repository,
        IDomainEventDispatcher eventDispatcher,
        INotificationService notificationService)
    {
        _repository = repository;
        _eventDispatcher = eventDispatcher;
        _notificationService = notificationService;
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

        await _eventDispatcher.DispatchAsync(new[]
        {
            new RegistrationCancelledEvent(
                entity.Id,
                entity.PatientId,
                entity.PatientName,
                entity.DoctorId)
        }, cancellationToken);

        // US-REG-008: 取消状态变更实时同步到该医生待诊列表
        await _notificationService.NotifyRegistrationStatusChangedAsync(
            entity.DoctorId, entity.Id, entity.Status.ToString(), cancellationToken);

        return Result.Success();
    }
}



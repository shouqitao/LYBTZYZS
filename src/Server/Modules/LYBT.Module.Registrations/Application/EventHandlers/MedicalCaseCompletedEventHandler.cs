using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Registrations.Application.EventHandlers;

/// <summary>
/// 医案完成 → 挂号完成（US-REG-005）。
/// ADR-0018：由同步 IXxxCrossModuleService 直调迁移为领域事件订阅。
/// </summary>
public sealed class MedicalCaseCompletedEventHandler
    : INotificationHandler<MedicalCaseCompletedEvent>
{
    private readonly IRegistrationCrossModuleService _registrationCrossModule;
    private readonly ILogger<MedicalCaseCompletedEventHandler> _logger;

    public MedicalCaseCompletedEventHandler(
        IRegistrationCrossModuleService registrationCrossModule,
        ILogger<MedicalCaseCompletedEventHandler> logger)
    {
        _registrationCrossModule = registrationCrossModule;
        _logger = logger;
    }

    public async Task Handle(MedicalCaseCompletedEvent notification, CancellationToken cancellationToken)
    {
        await _registrationCrossModule.CompleteByMedicalCaseAsync(notification.MedicalCaseId, cancellationToken);
        _logger.LogInformation(
            "[EVT] MedicalCaseCompletedEvent → RegistrationCompleted - EventId={EventId} MedicalCaseId={MedicalCaseId}",
            notification.EventId, notification.MedicalCaseId);
    }
}

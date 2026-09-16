using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Registrations.Application.EventHandlers;

/// <summary>
/// 医案取消 → 挂号回退/取消（G-9 / US-MC-014）。
/// ADR-0018：由同步 IXxxCrossModuleService 直调迁移为领域事件订阅。
/// </summary>
public sealed class MedicalCaseCancelledEventHandler
    : INotificationHandler<MedicalCaseCancelledEvent>
{
    private readonly IRegistrationCrossModuleService _registrationCrossModule;
    private readonly ILogger<MedicalCaseCancelledEventHandler> _logger;

    public MedicalCaseCancelledEventHandler(
        IRegistrationCrossModuleService registrationCrossModule,
        ILogger<MedicalCaseCancelledEventHandler> logger)
    {
        _registrationCrossModule = registrationCrossModule;
        _logger = logger;
    }

    public async Task Handle(MedicalCaseCancelledEvent notification, CancellationToken cancellationToken)
    {
        // 事件处理失败不应回滚已提交的主事务：医案取消已落库，此处失败不应导致客户端 500。
        // Outbox 模式（v2.0）将提供可靠投递与重试；当前阶段仅隔离失败并记录日志。
        try
        {
            await _registrationCrossModule.HandleMedicalCaseCancelledAsync(notification.MedicalCaseId, cancellationToken);
            _logger.LogInformation(
                "[EVT] MedicalCaseCancelledEvent → RegistrationRolledBack - EventId={EventId} MedicalCaseId={MedicalCaseId}",
                notification.EventId, notification.MedicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EVT] MedicalCaseCancelledEvent → HandlerFailed（主事务已提交，不回滚） - EventId={EventId} MedicalCaseId={MedicalCaseId}",
                notification.EventId, notification.MedicalCaseId);
        }
    }
}

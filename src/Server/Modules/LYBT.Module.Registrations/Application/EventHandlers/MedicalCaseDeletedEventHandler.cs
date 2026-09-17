using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Registrations.Application.EventHandlers;

/// <summary>
/// 医案软删除 → 挂号回退（R-10）。
/// ADR-0018：软删路径由 IXxxCrossModuleService 直调迁移为领域事件订阅，
/// 与 MedicalCaseCancelledEventHandler 同构。
/// </summary>
public sealed class MedicalCaseDeletedEventHandler
    : INotificationHandler<MedicalCaseDeletedEvent>
{
    private readonly IRegistrationCrossModuleService _registrationCrossModule;
    private readonly ILogger<MedicalCaseDeletedEventHandler> _logger;

    public MedicalCaseDeletedEventHandler(
        IRegistrationCrossModuleService registrationCrossModule,
        ILogger<MedicalCaseDeletedEventHandler> logger)
    {
        _registrationCrossModule = registrationCrossModule;
        _logger = logger;
    }

    public async Task Handle(MedicalCaseDeletedEvent notification, CancellationToken cancellationToken)
    {
        // 事件处理失败不应回滚已提交的主事务：医案软删已落库，此处失败不应导致客户端 500。
        // Outbox 模式（v2.0）将提供可靠投递与重试；当前阶段仅隔离失败并记录日志。
        try
        {
            await _registrationCrossModule.HandleMedicalCaseCancelledAsync(notification.MedicalCaseId, cancellationToken);
            _logger.LogInformation(
                "[EVT] MedicalCaseDeletedEvent → RegistrationRolledBack - EventId={EventId} MedicalCaseId={MedicalCaseId}",
                notification.EventId, notification.MedicalCaseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EVT] MedicalCaseDeletedEvent → HandlerFailed（主事务已提交，不回滚） - EventId={EventId} MedicalCaseId={MedicalCaseId}",
                notification.EventId, notification.MedicalCaseId);
        }
    }
}

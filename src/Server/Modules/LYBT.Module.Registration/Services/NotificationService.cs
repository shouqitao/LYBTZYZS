using LYBT.Module.Registrations.Hubs;
using LYBT.Module.Registrations.Interfaces;
using LYBT.Shared.Models.Contracts.Registration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Registrations.Services;

/// <summary>
/// 挂号实时通知服务实现 — 通过 IHubContext 推送给指定医生的分组。
/// 推送通道为尽力而为：失败仅记录日志，不影响业务主流程。
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IHubContext<RegistrationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<RegistrationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task NotifyNewRegistrationAsync(
        Guid doctorId, RegistrationDetailDto registration, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty || registration is null)
            return;

        await SendToDoctorAsync(doctorId, "NewRegistration", [registration], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task NotifyRegistrationStatusChangedAsync(
        Guid doctorId, Guid registrationId, string newStatus, CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
            return;

        await SendToDoctorAsync(doctorId, "RegistrationStatusChanged", [registrationId, newStatus], cancellationToken);
    }

    private async Task SendToDoctorAsync(
        Guid doctorId, string method, object?[] args, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients
                .Group(RegistrationHub.GetDoctorGroup(doctorId))
                .SendCoreAsync(method, args, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR 推送失败: Method={Method}, DoctorId={DoctorId}", method, doctorId);
        }
    }
}

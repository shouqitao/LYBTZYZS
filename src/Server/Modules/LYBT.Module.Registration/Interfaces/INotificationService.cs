using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Module.Registration.Interfaces;

/// <summary>
/// 挂号实时通知服务 (US-REG-008)。
/// 通过 SignalR 将挂号创建/状态变更推送给目标医生的工作台。
/// </summary>
public interface INotificationService
{
    /// <summary>新挂号创建后推送给指派医生（仅 Waiting 状态挂号会进入待诊列表）。</summary>
    Task NotifyNewRegistrationAsync(Guid doctorId, RegistrationDetailDto registration, CancellationToken cancellationToken = default);

    /// <summary>挂号状态变更后推送给指派医生，客户端据此刷新待诊列表。</summary>
    Task NotifyRegistrationStatusChangedAsync(Guid doctorId, Guid registrationId, string newStatus, CancellationToken cancellationToken = default);
}

using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LYBT.Module.Registration.Hubs;

/// <summary>
/// 挂号实时通知 Hub — 医生工作台待诊列表实时更新 (US-REG-008)。
/// 医生端通过 query string 携带 doctorId 连接，按医生加入分组，推送仅达目标医生。
/// </summary>
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public sealed class RegistrationHub : Hub
{
    private const string DoctorIdQueryKey = "doctorId";

    private readonly RegistrationConnectionManager _connections;

    public RegistrationHub(RegistrationConnectionManager connections)
    {
        _connections = connections;
    }

    /// <inheritdoc/>
    public override async Task OnConnectedAsync()
    {
        if (TryGetDoctorId(out var doctorId))
        {
            _connections.Add(Context.ConnectionId, doctorId);
            await Groups.AddToGroupAsync(Context.ConnectionId, GetDoctorGroup(doctorId));
        }

        await base.OnConnectedAsync();
    }

    /// <inheritdoc/>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryRemove(Context.ConnectionId, out var doctorId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetDoctorGroup(doctorId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>医生推送分组名，NotificationService 据此按 DoctorId 过滤推送。</summary>
    public static string GetDoctorGroup(Guid doctorId) => $"doctor-{doctorId}";

    private bool TryGetDoctorId(out Guid doctorId)
    {
        doctorId = Guid.Empty;

        var query = Context.GetHttpContext()?.Request.Query;
        if (query is null || !query.TryGetValue(DoctorIdQueryKey, out var raw))
            return false;

        return Guid.TryParse(raw.ToString(), out doctorId) && doctorId != Guid.Empty;
    }
}

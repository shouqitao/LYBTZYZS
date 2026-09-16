using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Registrations.Hubs;

/// <summary>
/// 挂号实时通知 Hub — 医生工作台待诊列表实时更新 (US-REG-008)。
/// 医生端通过 query string 携带 doctorId 连接，按医生加入分组，推送仅达目标医生。
/// </summary>
[Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
public sealed class RegistrationHub : Hub
{
    private const string DoctorIdQueryKey = "doctorId";

    private readonly RegistrationConnectionManager _connections;
    private readonly ILogger<RegistrationHub> _logger;

    public RegistrationHub(RegistrationConnectionManager connections, ILogger<RegistrationHub> logger)
    {
        _connections = connections;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnConnectedAsync()
    {
        if (TryGetDoctorId(out var doctorId))
        {
            // 安全校验（S-03）：doctorId 必须等于当前登录用户，防止伪造他人 ID 订阅其通知
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null && Guid.TryParse(userId, out var currentUserId) && currentUserId == doctorId)
            {
                _connections.Add(Context.ConnectionId, doctorId);
                await Groups.AddToGroupAsync(Context.ConnectionId, GetDoctorGroup(doctorId));
            }
            else
            {
                // doctorId 与登录用户不匹配，拒绝加入分组
                _logger.LogWarning("SignalR connection rejected: doctorId {DoctorId} does not match authenticated user", doctorId);
            }
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

using System.Collections.Concurrent;

namespace LYBT.Module.Registrations.Hubs;

/// <summary>
/// SignalR 连接映射管理 — ConnectionId ↔ DoctorId。
/// 单例，供 RegistrationHub 记录医生端连接，保证按医生过滤推送。
/// P2-10-5 已修复：原 HashSet 非线程安全，现改为 ConcurrentDictionary<string, Guid>，TryRemove 线程安全，无锁操作。
/// </summary>
public sealed class RegistrationConnectionManager
{
    private readonly ConcurrentDictionary<string, Guid> _connections = new();

    /// <summary>记录一条连接映射。</summary>
    public void Add(string connectionId, Guid doctorId)
    {
        _connections[connectionId] = doctorId;
    }

    /// <summary>移除连接映射，返回关联的 DoctorId。</summary>
    public bool TryRemove(string connectionId, out Guid doctorId)
        => _connections.TryRemove(connectionId, out doctorId);
}

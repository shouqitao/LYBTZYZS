using LYBT.Module.Auth.Domain;

namespace LYBT.Module.Auth.Interfaces;

/// <summary>
/// 认证会话仓储接口。
/// </summary>
public interface IAuthSessionRepository
{
    /// <summary>
    /// 根据ID获取会话。
    /// </summary>
    Task<AuthSession?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// 根据令牌哈希获取会话。
    /// </summary>
    Task<AuthSession?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>
    /// 新增会话。
    /// </summary>
    Task AddAsync(AuthSession session, CancellationToken ct);

    /// <summary>
    /// 更新会话。
    /// </summary>
    Task UpdateAsync(AuthSession session, CancellationToken ct);

    /// <summary>
    /// 获取用户的有效会话列表。
    /// </summary>
    Task<IReadOnlyList<AuthSession>> GetActiveSessionsAsync(Guid userId, CancellationToken ct);
}



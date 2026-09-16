using LYBT.Entities.Auth;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Module.Identity.Interfaces;

/// <summary>
/// 认证会话仓储接口。
/// </summary>
public interface IAuthSessionRepository
{
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
    /// 批量撤销用户的全部有效会话（Token 族旋转）。
    /// </summary>
    Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken ct);

    /// <summary>
    /// 开启显式数据库事务（供 Token 旋转等多写操作原子提交）。
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
}

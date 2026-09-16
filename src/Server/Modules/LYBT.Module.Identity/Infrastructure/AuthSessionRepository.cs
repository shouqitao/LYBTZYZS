using LYBT.Entities.Auth;
using LYBT.Module.Identity.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Module.Identity.Infrastructure;

/// <summary>
/// 认证会话仓储实现。封装认证会话数据访问逻辑。
/// </summary>
public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly IdentityDbContext _context;

    public AuthSessionRepository(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<AuthSession?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.AuthSessions
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        await _context.AuthSessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(AuthSession session, CancellationToken cancellationToken = default)
    {
        _context.AuthSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RevokeAllUserSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.AuthSessions
            .Where(s => s.UserId == userId
                && !s.IsRevoked
                && s.LogoutTime == null
                && s.ExpiryTime > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
            return;

        foreach (var session in sessions)
        {
            session.Revoke(reason);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }
}

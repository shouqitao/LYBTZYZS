using LYBT.Module.Auth.Domain;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Auth.Infrastructure;

/// <summary>
/// 认证会话仓储实现。封装认证会话数据访问逻辑。
/// </summary>
public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly AuthDbContext _context;

    public AuthSessionRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<AuthSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuthSessions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
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
    public async Task<IReadOnlyList<AuthSession>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.AuthSessions
            .Where(s => s.UserId == userId
                && !s.IsRevoked
                && s.LogoutTime == null
                && s.ExpiryTime > DateTime.UtcNow)
            .OrderByDescending(s => s.LoginTime)
            .ToListAsync(cancellationToken);
    }
}



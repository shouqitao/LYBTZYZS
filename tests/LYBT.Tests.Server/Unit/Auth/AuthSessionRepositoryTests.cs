using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Module.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// AuthSessionRepository 测试 — 批量撤销（Token 族旋转）。
/// </summary>
public class AuthSessionRepositoryTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly AuthSessionRepository _sut;

    public AuthSessionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new IdentityDbContext(options);
        _sut = new AuthSessionRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private static AuthSession CreateSession(Guid userId, DateTime expiry)
        => AuthSession.Create(userId, Guid.NewGuid().ToString("N"), expiry, "127.0.0.1", "TestAgent");

    [Fact]
    public async Task RevokeAllUserSessionsAsync_WithActiveSessions_RevokesOnlyTargetUsers()
    {
        var userId = Guid.NewGuid();
        var session1 = CreateSession(userId, DateTime.UtcNow.AddHours(2));
        var session2 = CreateSession(userId, DateTime.UtcNow.AddHours(2));
        var otherUserSession = CreateSession(Guid.NewGuid(), DateTime.UtcNow.AddHours(2));
        await _context.AuthSessions.AddRangeAsync(session1, session2, otherUserSession);
        await _context.SaveChangesAsync();

        await _sut.RevokeAllUserSessionsAsync(userId, "新设备登录", CancellationToken.None);

        var all = await _context.AuthSessions.ToListAsync();
        all.Where(s => s.UserId == userId)
            .Should().OnlyContain(s => s.IsRevoked && s.RevokedReason == "新设备登录" && s.LogoutTime.HasValue && !s.IsValid());
        all.Single(s => s.UserId != userId)
            .Should().Match<AuthSession>(s => !s.IsRevoked && s.RevokedReason == null);
    }

    [Fact]
    public async Task RevokeAllUserSessionsAsync_WithNoActiveSessions_LeavesThemUntouched()
    {
        var userId = Guid.NewGuid();
        var alreadyRevoked = CreateSession(userId, DateTime.UtcNow.AddHours(2));
        alreadyRevoked.Revoke("已撤销");
        var expired = CreateSession(userId, DateTime.UtcNow.AddHours(-1));
        var loggedOut = CreateSession(userId, DateTime.UtcNow.AddHours(2));
        loggedOut.Logout();
        await _context.AuthSessions.AddRangeAsync(alreadyRevoked, expired, loggedOut);
        await _context.SaveChangesAsync();

        await _sut.RevokeAllUserSessionsAsync(userId, "新设备登录", CancellationToken.None);

        var all = await _context.AuthSessions.ToListAsync();
        all.Should().OnlyContain(s => s.RevokedReason == "已撤销" || s.RevokedReason == null);
        all.Single(s => s.Id == alreadyRevoked.Id).IsRevoked.Should().BeTrue();
        all.Single(s => s.Id == expired.Id).IsRevoked.Should().BeFalse();
        all.Single(s => s.Id == loggedOut.Id).IsRevoked.Should().BeFalse();
    }
}

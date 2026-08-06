using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Application.Commands;
using LYBT.Module.Auth.Infrastructure;
using LYBT.Module.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// RevokeAllUserTokensCommandHandler 测试 — 撤销全部令牌并记录审计。
/// </summary>
public class RevokeAllUserTokensCommandHandlerTests : IDisposable
{
    private readonly AuthDbContext _authContext;
    private readonly AppDbContext _auditContext;
    private readonly RevokeAllUserTokensCommandHandler _sut;

    public RevokeAllUserTokensCommandHandlerTests()
    {
        var authOptions = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _authContext = new AuthDbContext(authOptions);

        var auditOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _auditContext = new AppDbContext(auditOptions);

        _sut = new RevokeAllUserTokensCommandHandler(
            new AuthSessionRepository(_authContext),
            new SecurityAuditService(
                new SecurityAuditRepository(_auditContext),
                NullLogger<SecurityAuditService>.Instance),
            NullLogger<RevokeAllUserTokensCommandHandler>.Instance);
    }

    public void Dispose()
    {
        _authContext.Dispose();
        _auditContext.Dispose();
    }

    [Fact]
    public async Task Handle_RevokesAllSessionsAndRecordsAudit()
    {
        var userId = Guid.NewGuid();
        var session = AuthSession.Create(userId, Guid.NewGuid().ToString("N"), DateTime.UtcNow.AddHours(2), "127.0.0.1");
        await _authContext.AuthSessions.AddAsync(session);
        await _authContext.SaveChangesAsync();

        var result = await _sut.Handle(new RevokeAllUserTokensCommand(userId, "新设备登录，旧会话已撤销"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _authContext.AuthSessions.SingleAsync())
            .Should().Match<AuthSession>(s => s.IsRevoked && s.RevokedReason == "新设备登录，旧会话已撤销");

        var auditLog = await _auditContext.SecurityAuditLogs.SingleAsync();
        auditLog.EventType.Should().Be("AllTokensRevoked");
        auditLog.UserId.Should().Be(userId);
        auditLog.Details.Should().Be("新设备登录，旧会话已撤销");
        auditLog.IsSuccess.Should().BeTrue();
    }
}

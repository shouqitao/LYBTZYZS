using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Module.Identity.Infrastructure;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// SecurityAuditService 测试 — 安全审计日志记录。
/// ADR-0017: SecurityAuditRepository 注入 IdentityDbContext
/// </summary>
public class SecurityAuditServiceTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly SecurityAuditService _sut;

    public SecurityAuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new IdentityDbContext(options);
        _sut = new SecurityAuditService(
            new SecurityAuditRepository(_context),
            NullLogger<SecurityAuditService>.Instance);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task RecordEventAsync_PersistsAuditLogWithAllFields()
    {
        var userId = Guid.NewGuid();
        var evt = new SecurityAuditEvent
        {
            UserId = userId,
            UserName = "doctor1",
            EventType = "PasswordChanged",
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent",
            Details = "用户修改了自己的密码",
            IsSuccess = true
        };

        await _sut.RecordEventAsync(evt);

        var log = await _context.SecurityAuditLogs.SingleAsync();
        log.EventType.Should().Be("PasswordChanged");
        log.UserId.Should().Be(userId);
        log.UserName.Should().Be("doctor1");
        log.IpAddress.Should().Be("127.0.0.1");
        log.UserAgent.Should().Be("TestAgent");
        log.Details.Should().Be("用户修改了自己的密码");
        log.IsSuccess.Should().BeTrue();
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RecordEventAsync_WithFailure_RecordsFailureReason()
    {
        var evt = new SecurityAuditEvent
        {
            EventType = "LoginFailed",
            IsSuccess = false,
            FailureReason = "密码错误"
        };

        await _sut.RecordEventAsync(evt);

        var log = await _context.SecurityAuditLogs.SingleAsync();
        log.IsSuccess.Should().BeFalse();
        log.FailureReason.Should().Be("密码错误");
    }
}

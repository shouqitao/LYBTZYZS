using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Models;
using LYBT.Module.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// SecurityAuditService 单元测试
/// Issue #1871 - 安全审计服务测试
/// </summary>
public class SecurityAuditServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly SecurityAuditService _sut;

    public SecurityAuditServiceTests()
    {
        // 使用内存数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _logger = new Mock<ILogger<SecurityAuditService>>().Object;
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _sut = new SecurityAuditService(_context, _logger, _httpContextAccessorMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region LogAsync 基础测试

    [Fact]
    public async Task LogAsync_WithValidEvent_ShouldCreateAuditLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            UserId = userId,
            UserType = "user",
            UserName = "testuser",
            Success = true,
            Metadata = "{\"source\":\"web\"}"
        };

        SetupHttpContext("192.168.1.100", "Mozilla/5.0");

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.EventType.Should().Be("Login");
        auditLog.UserId.Should().Be(userId);
        auditLog.UserType.Should().Be("user");
        auditLog.UserName.Should().Be("testuser");
        auditLog.Success.Should().BeTrue();
        auditLog.Metadata.Should().Be("{\"source\":\"web\"}");
        auditLog.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogAsync_WithFailedEvent_ShouldRecordErrorMessage()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "LoginFailed",
            UserName = "testuser",
            Success = false,
            ErrorMessage = "Invalid credentials"
        };

        SetupHttpContext("10.0.0.50", "Chrome/91.0");

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeFalse();
        auditLog.ErrorMessage.Should().Be("Invalid credentials");
    }

    #endregion

    #region IP地址脱敏测试

    [Fact]
    public async Task LogAsync_WithIPv4Address_ShouldMaskLastSegment()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        SetupHttpContext("192.168.1.100", "Mozilla/5.0");

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.IpAddress.Should().Be("192.168.1.*");
    }

    [Fact]
    public async Task LogAsync_WithIPv6Address_ShouldMaskAfterFourthGroup()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        SetupHttpContext("2001:0db8:85a3:0000:0000:8a2e:0370:7334", "Mozilla/5.0");

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        // IPv6地址被.NET自动规范化：2001:0db8:85a3:0000:0000:8a2e:0370:7334 → 2001:db8:85a3::8a2e:370:7334
        auditLog!.IpAddress.Should().Be("2001:db8:85a3::*");
    }

    [Fact]
    public async Task LogAsync_WithNoHttpContext_ShouldHaveNullIpAddress()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.IpAddress.Should().BeNull();
    }

    #endregion

    #region UserAgent截断测试

    [Fact]
    public async Task LogAsync_WithShortUserAgent_ShouldKeepOriginal()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        SetupHttpContext("192.168.1.100", userAgent);

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.UserAgent.Should().Be(userAgent);
    }

    [Fact]
    public async Task LogAsync_WithLongUserAgent_ShouldTruncateTo500Characters()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        var longUserAgent = new string('A', 600); // 600字符
        SetupHttpContext("192.168.1.100", longUserAgent);

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.UserAgent.Should().HaveLength(500);
        auditLog.UserAgent.Should().Be(new string('A', 500));
    }

    [Fact]
    public async Task LogAsync_WithEmptyUserAgent_ShouldHaveNullUserAgent()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        SetupHttpContext("192.168.1.100", "");

        // Act
        await _sut.LogAsync(auditEvent);

        // Assert
        var auditLog = await _context.SecurityAuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.UserAgent.Should().BeNull();
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public async Task LogAsync_WhenDatabaseFails_ShouldNotThrowException()
    {
        // Arrange
        var auditEvent = new SecurityAuditEvent
        {
            EventType = "Login",
            Success = true
        };

        SetupHttpContext("192.168.1.100", "Mozilla/5.0");

        // 创建一个独立的已释放context用于测试
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var disposedContext = new AppDbContext(options);
        disposedContext.Dispose();

        var sutWithDisposedContext = new SecurityAuditService(disposedContext, _logger, _httpContextAccessorMock.Object);

        // Act & Assert - 即使数据库失败也不应抛出异常
        var act = async () => await sutWithDisposedContext.LogAsync(auditEvent);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region 辅助方法

    private void SetupHttpContext(string ipAddress, string userAgent)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        httpContext.Request.Headers["User-Agent"] = userAgent;

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    #endregion
}

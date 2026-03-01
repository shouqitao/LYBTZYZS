using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// TokenRevocationService 单元测试
/// Issue #1870 - Token撤销服务测试
/// </summary>
public class TokenRevocationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ILogger<TokenRevocationService> _logger;
    private readonly TokenRevocationService _sut;

    public TokenRevocationServiceTests()
    {
        // 使用内存数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _logger = Substitute.For<ILogger<TokenRevocationService>>();
        _sut = new TokenRevocationService(_context, _logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region RevokeTokenAsync 测试

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldRevokeSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenString = "test-token-123";
        var reason = "User logout";

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenString,
            UserId = userId,
            UserType = "user",
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RevokeTokenAsync(tokenString, reason);

        // Assert
        result.Should().BeTrue();

        var revokedToken = await _context.RefreshTokens.FirstAsync(t => t.Token == tokenString);
        revokedToken.IsRevoked.Should().BeTrue();
        revokedToken.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedReason.Should().Be(reason);

        // 验证审计日志已创建
        var auditLog = await _context.SecurityAuditLogs
            .FirstOrDefaultAsync(l => l.EventType == "TokenRevoked" && l.UserId == userId);
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenString = "non-existent-token";
        var reason = "Test";

        // Act
        var result = await _sut.RevokeTokenAsync(tokenString, reason);

        // Assert
        result.Should().BeFalse();

        // 验证没有审计日志被创建
        var auditLogs = await _context.SecurityAuditLogs.ToListAsync();
        auditLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithAlreadyRevokedToken_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokenString = "already-revoked-token";
        var reason = "New reason";

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenString,
            UserId = userId,
            UserType = "user",
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            RevokedReason = "Original reason",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RevokeTokenAsync(tokenString, reason);

        // Assert
        result.Should().BeFalse();

        // 验证Token状态未改变
        var unchangedToken = await _context.RefreshTokens.FirstAsync(t => t.Token == tokenString);
        unchangedToken.RevokedReason.Should().Be("Original reason");
    }

    #endregion

    #region IsTokenRevokedAsync 测试

    [Fact]
    public async Task IsTokenRevokedAsync_WithRevokedToken_ShouldReturnTrue()
    {
        // Arrange
        var tokenString = "revoked-token";
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenString,
            UserId = Guid.NewGuid(),
            UserType = "user",
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow,
            RevokedReason = "Test",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsTokenRevokedAsync(tokenString);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenRevokedAsync_WithActiveToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenString = "active-token";
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenString,
            UserId = Guid.NewGuid(),
            UserType = "user",
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsTokenRevokedAsync(tokenString);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsTokenRevokedAsync_WithNonExistentToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenString = "non-existent-token";

        // Act
        var result = await _sut.IsTokenRevokedAsync(tokenString);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

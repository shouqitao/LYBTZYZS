using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Auth;

/// <summary>
/// TokenRevocationService 单元测试
/// Issue #1870 - Token撤销服务测试
/// </summary>
public class TokenRevocationServiceTests
{
    private readonly IRefreshTokenRepository _tokenRepository;
    private readonly ISecurityAuditRepository _auditRepository;
    private readonly TokenRevocationService _sut;

    public TokenRevocationServiceTests()
    {
        _tokenRepository = Substitute.For<IRefreshTokenRepository>();
        _auditRepository = Substitute.For<ISecurityAuditRepository>();
        _sut = new TokenRevocationService(
            _tokenRepository,
            _auditRepository,
            NullLogger<TokenRevocationService>.Instance);
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

        _tokenRepository.GetByTokenAsync(tokenString).Returns(token);

        // Act
        var result = await _sut.RevokeTokenAsync(tokenString, reason);

        // Assert
        result.Should().BeTrue();
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        token.RevokedReason.Should().Be(reason);

        await _tokenRepository.Received(1).SaveChangesAsync();
        await _auditRepository.Received(1).AddAsync(
            Arg.Is<SecurityAuditLog>(l => l.EventType == "TokenRevoked" && l.UserId == userId && l.Success));
        await _auditRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenString = "non-existent-token";
        var reason = "Test";

        _tokenRepository.GetByTokenAsync(tokenString).Returns((RefreshToken?)null);

        // Act
        var result = await _sut.RevokeTokenAsync(tokenString, reason);

        // Assert
        result.Should().BeFalse();
        await _tokenRepository.DidNotReceive().SaveChangesAsync();
        await _auditRepository.DidNotReceive().AddAsync(Arg.Any<SecurityAuditLog>());
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

        _tokenRepository.GetByTokenAsync(tokenString).Returns(token);

        // Act
        var result = await _sut.RevokeTokenAsync(tokenString, reason);

        // Assert
        result.Should().BeFalse();
        token.RevokedReason.Should().Be("Original reason");
        await _tokenRepository.DidNotReceive().SaveChangesAsync();
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

        _tokenRepository.GetByTokenAsync(tokenString).Returns(token);

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

        _tokenRepository.GetByTokenAsync(tokenString).Returns(token);

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
        _tokenRepository.GetByTokenAsync(tokenString).Returns((RefreshToken?)null);

        // Act
        var result = await _sut.IsTokenRevokedAsync(tokenString);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

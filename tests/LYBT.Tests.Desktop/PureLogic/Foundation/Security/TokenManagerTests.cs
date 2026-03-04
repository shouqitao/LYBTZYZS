using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Foundation.Security;

/// <summary>
/// TokenManager单元测试
/// OpenSpec: refactor-login-authentication (TKM-001, TKM-002)
/// </summary>
public class TokenManagerTests
{
    private readonly ILogger<TokenManager> _logger;
    private readonly TokenManager _tokenManager;

    public TokenManagerTests()
    {
        _logger = Substitute.For<ILogger<TokenManager>>();
        _tokenManager = new TokenManager(_logger);
    }

    #region SetTokens测试

    /// <summary>
    /// 测试：设置Token后可以正确获取
    /// </summary>
    [Fact]
    public void SetTokens_ValidTokens_CanRetrieve()
    {
        // Arrange
        var accessToken = "test-access-token";
        var refreshToken = "test-refresh-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        _tokenManager.SetTokens(accessToken, refreshToken, expiry);

        // Assert
        _tokenManager.AccessToken.Should().Be(accessToken);
        _tokenManager.RefreshToken.Should().Be(refreshToken);
        _tokenManager.AccessTokenExpiry.Should().Be(expiry);
    }

    /// <summary>
    /// 测试：空AccessToken抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTokens_EmptyAccessToken_ThrowsArgumentException(string? invalidToken)
    {
        // Arrange
        var refreshToken = "test-refresh-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        var act = () => _tokenManager.SetTokens(invalidToken!, refreshToken, expiry);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AccessToken*");
    }

    /// <summary>
    /// 测试：空RefreshToken抛出异常
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetTokens_EmptyRefreshToken_ThrowsArgumentException(string? invalidToken)
    {
        // Arrange
        var accessToken = "test-access-token";
        var expiry = DateTime.UtcNow.AddHours(1);

        // Act
        var act = () => _tokenManager.SetTokens(accessToken, invalidToken!, expiry);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RefreshToken*");
    }

    #endregion

    #region ClearTokens测试

    /// <summary>
    /// 测试：清除Token后返回null
    /// </summary>
    [Fact]
    public void ClearTokens_AfterSet_ReturnsNull()
    {
        // Arrange
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(1));

        // Act
        _tokenManager.ClearTokens();

        // Assert
        _tokenManager.AccessToken.Should().BeNull();
        _tokenManager.RefreshToken.Should().BeNull();
        _tokenManager.AccessTokenExpiry.Should().BeNull();
    }

    /// <summary>
    /// 测试：未设置Token时清除不报错
    /// </summary>
    [Fact]
    public void ClearTokens_WhenEmpty_DoesNotThrow()
    {
        // Act
        var act = () => _tokenManager.ClearTokens();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region IsTokenValid测试

    /// <summary>
    /// 测试：有效Token返回true
    /// </summary>
    [Fact]
    public void IsTokenValid_ValidToken_ReturnsTrue()
    {
        // Arrange
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(1));

        // Act
        var result = _tokenManager.IsTokenValid();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 测试：未设置Token返回false
    /// </summary>
    [Fact]
    public void IsTokenValid_NoToken_ReturnsFalse()
    {
        // Act
        var result = _tokenManager.IsTokenValid();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 测试：已过期Token返回false
    /// </summary>
    [Fact]
    public void IsTokenValid_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(-1)); // 已过期

        // Act
        var result = _tokenManager.IsTokenValid();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 测试：清除后Token无效
    /// </summary>
    [Fact]
    public void IsTokenValid_AfterClear_ReturnsFalse()
    {
        // Arrange
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(1));
        _tokenManager.ClearTokens();

        // Act
        var result = _tokenManager.IsTokenValid();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsTokenExpiringSoon测试

    /// <summary>
    /// 测试：Token即将过期返回true
    /// </summary>
    [Fact]
    public void IsTokenExpiringSoon_WithinThreshold_ReturnsTrue()
    {
        // Arrange - Token在3分钟后过期
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddMinutes(3));

        // Act - 使用5分钟阈值
        var result = _tokenManager.IsTokenExpiringSoon(TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 测试：Token距离过期还有很长时间返回false
    /// </summary>
    [Fact]
    public void IsTokenExpiringSoon_NotWithinThreshold_ReturnsFalse()
    {
        // Arrange - Token在30分钟后过期
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddMinutes(30));

        // Act - 使用5分钟阈值
        var result = _tokenManager.IsTokenExpiringSoon(TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// 测试：未设置Token返回true（视为即将过期）
    /// </summary>
    [Fact]
    public void IsTokenExpiringSoon_NoToken_ReturnsTrue()
    {
        // Act
        var result = _tokenManager.IsTokenExpiringSoon(TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// 测试：已过期Token返回true
    /// </summary>
    [Fact]
    public void IsTokenExpiringSoon_ExpiredToken_ReturnsTrue()
    {
        // Arrange
        _tokenManager.SetTokens("access", "refresh", DateTime.UtcNow.AddHours(-1));

        // Act
        var result = _tokenManager.IsTokenExpiringSoon(TimeSpan.FromMinutes(5));

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region 线程安全测试

    /// <summary>
    /// 测试：多线程并发访问不会抛出异常
    /// </summary>
    [Fact]
    public async Task TokenManager_ConcurrentAccess_DoesNotThrow()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - 并发执行读写操作
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                if (index % 3 == 0)
                {
                    _tokenManager.SetTokens($"access-{index}", $"refresh-{index}", DateTime.UtcNow.AddHours(1));
                }
                else if (index % 3 == 1)
                {
                    _ = _tokenManager.AccessToken;
                    _ = _tokenManager.IsTokenValid();
                }
                else
                {
                    _tokenManager.ClearTokens();
                }
            }));
        }

        // Assert
        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    #endregion
}

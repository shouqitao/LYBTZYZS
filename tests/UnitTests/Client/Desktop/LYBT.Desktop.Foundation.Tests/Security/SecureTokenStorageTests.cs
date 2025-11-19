using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Desktop.Foundation.Tests.Security;

/// <summary>
/// SecureTokenStorage 单元测试
/// Issue #1907: 测试内存存储（Session级别）
/// </summary>
public class SecureTokenStorageTests : IDisposable
{
    private readonly ILogger<SecureTokenStorage> _logger;
    private readonly SecureTokenStorage _storage;

    public SecureTokenStorageTests()
    {
        _logger = Substitute.For<ILogger<SecureTokenStorage>>();
        _storage = new SecureTokenStorage(_logger);
    }

    public void Dispose()
    {
        // 内存存储不需要清理文件
    }

    /// <summary>
    /// 测试：成功保存Token到内存
    /// </summary>
    [Fact]
    public async Task SaveTokenAsync_Success()
    {
        // Arrange
        var loginResponse = CreateTestLoginResponse();

        // Act
        await _storage.SaveTokenAsync(loginResponse);

        // Assert
        var loadedResponse = await _storage.LoadTokenAsync();
        loadedResponse.Should().NotBeNull("Token应该被成功保存到内存");
    }

    /// <summary>
    /// 测试：成功从内存加载Token
    /// </summary>
    [Fact]
    public async Task LoadTokenAsync_Success()
    {
        // Arrange
        var originalResponse = CreateTestLoginResponse();
        await _storage.SaveTokenAsync(originalResponse);

        // Act
        var loadedResponse = await _storage.LoadTokenAsync();

        // Assert
        loadedResponse.Should().NotBeNull("应该成功加载Token");
        loadedResponse!.Token.Should().Be(originalResponse.Token);
        loadedResponse.RefreshToken.Should().Be(originalResponse.RefreshToken);
        loadedResponse.User.UserName.Should().Be(originalResponse.User.UserName);
    }

    /// <summary>
    /// 测试：内存为空时返回null
    /// </summary>
    [Fact]
    public async Task LoadTokenAsync_EmptyMemory_ReturnsNull()
    {
        // Arrange - 不保存任何Token

        // Act
        var result = await _storage.LoadTokenAsync();

        // Assert
        result.Should().BeNull("内存为空时应返回null");
    }

    /// <summary>
    /// 测试：清除内存中的Token
    /// </summary>
    [Fact]
    public async Task ClearTokenAsync_Success()
    {
        // Arrange
        var loginResponse = CreateTestLoginResponse();
        await _storage.SaveTokenAsync(loginResponse);
        var loadedBefore = await _storage.LoadTokenAsync();
        loadedBefore.Should().NotBeNull("保存后内存中应该有Token");

        // Act
        await _storage.ClearTokenAsync();

        // Assert
        var loadedAfter = await _storage.LoadTokenAsync();
        loadedAfter.Should().BeNull("清除后内存中应该没有Token");
    }

    /// <summary>
    /// 测试：清除空内存不抛异常
    /// </summary>
    [Fact]
    public async Task ClearTokenAsync_EmptyMemory_NoException()
    {
        // Arrange - 不保存任何Token

        // Act & Assert
        var act = async () => await _storage.ClearTokenAsync();
        await act.Should().NotThrowAsync("清除空内存不应抛异常");
    }

    /// <summary>
    /// 测试：SaveTokenAsync抛出ArgumentNullException当参数为null
    /// </summary>
    [Fact]
    public async Task SaveTokenAsync_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        LoginResponse? nullResponse = null;

        // Act & Assert
        var act = async () => await _storage.SaveTokenAsync(nullResponse!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// 创建测试用的 LoginResponse
    /// </summary>
    private LoginResponse CreateTestLoginResponse()
    {
        return new LoginResponse
        {
            Token = "test_access_token_" + Guid.NewGuid().ToString("N"),
            RefreshToken = "test_refresh_token_" + Guid.NewGuid().ToString("N"),
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                UserName = "test_user",
                Role = UserRole.Doctor
            }
        };
    }
}

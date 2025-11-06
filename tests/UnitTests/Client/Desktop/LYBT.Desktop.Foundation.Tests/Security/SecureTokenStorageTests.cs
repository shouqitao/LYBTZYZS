using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IO;

namespace LYBT.Desktop.Foundation.Tests.Security;

/// <summary>
/// SecureTokenStorage 单元测试
/// Issue #1866: 测试DPAPI加密存储、解密、降级策略、文件不存在场景
/// </summary>
public class SecureTokenStorageTests : IDisposable
{
    private readonly ILogger<SecureTokenStorage> _logger;
    private readonly SecureTokenStorage _storage;
    private readonly string _testStorageFilePath;

    public SecureTokenStorageTests()
    {
        _logger = Substitute.For<ILogger<SecureTokenStorage>>();

        // 获取实际存储路径
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _testStorageFilePath = Path.Combine(appDataPath, "LYBTZYZS", "tokens.dat");

        // 确保测试开始前清理旧文件
        try
        {
            if (File.Exists(_testStorageFilePath))
            {
                File.Delete(_testStorageFilePath);
            }
        }
        catch
        {
            // 忽略清理错误
        }

        _storage = new SecureTokenStorage(_logger);
    }

    public void Dispose()
    {
        // 清理测试文件（但保留目录）
        try
        {
            if (File.Exists(_testStorageFilePath))
            {
                File.Delete(_testStorageFilePath);
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 测试：成功加密并保存Token
    /// </summary>
    [Fact]
    public async Task SaveTokenAsync_Success_Encrypted()
    {
        // Arrange
        var loginResponse = CreateTestLoginResponse();

        // Act
        await _storage.SaveTokenAsync(loginResponse);

        // Assert
        // SecureTokenStorage 使用默认路径，我们通过LoadTokenAsync验证保存成功
        var loadedResponse = await _storage.LoadTokenAsync();
        loadedResponse.Should().NotBeNull("Token应该被成功保存");
    }

    /// <summary>
    /// 测试：成功解密并加载Token
    /// </summary>
    [Fact]
    public async Task LoadTokenAsync_Success_Decrypted()
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
    /// 测试：DPAPI加密失败时的降级策略（理论测试，实际DPAPI不易模拟失败）
    /// 注：此测试验证加密逻辑存在，实际DPAPI失败需要集成测试
    /// </summary>
    [Fact]
    public async Task SaveTokenAsync_EncryptionAvailable_UsesEncryption()
    {
        // Arrange
        var loginResponse = CreateTestLoginResponse();

        // Act
        await _storage.SaveTokenAsync(loginResponse);

        // Assert
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var storedFile = Path.Combine(appDataPath, "LYBTZYZS", "tokens.dat");
        var fileContent = await File.ReadAllBytesAsync(storedFile);

        // 验证使用了加密（内容不是有效的JSON）
        try
        {
            var text = System.Text.Encoding.UTF8.GetString(fileContent);
            var _ = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(text);
            Assert.Fail("文件内容应该是加密的二进制数据，而非JSON");
        }
        catch (System.Text.Json.JsonException)
        {
            // 预期异常：文件内容不是有效JSON，说明已加密
            Assert.True(true, "文件已加密");
        }
    }

    /// <summary>
    /// 测试：文件不存在时返回null
    /// </summary>
    [Fact]
    public async Task LoadTokenAsync_FileNotExists_ReturnsNull()
    {
        // Arrange - 不创建任何文件

        // Act
        var result = await _storage.LoadTokenAsync();

        // Assert
        result.Should().BeNull("文件不存在时应返回null");
    }

    /// <summary>
    /// 测试：清除Token文件
    /// </summary>
    [Fact]
    public async Task ClearTokenAsync_Success_DeletesFile()
    {
        // Arrange
        var loginResponse = CreateTestLoginResponse();
        await _storage.SaveTokenAsync(loginResponse);
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var storedFile = Path.Combine(appDataPath, "LYBTZYZS", "tokens.dat");
        File.Exists(storedFile).Should().BeTrue("保存后文件应该存在");

        // Act
        await _storage.ClearTokenAsync();

        // Assert
        File.Exists(storedFile).Should().BeFalse("清除后文件应该被删除");
    }

    /// <summary>
    /// 测试：清除不存在的Token文件不抛异常
    /// </summary>
    [Fact]
    public async Task ClearTokenAsync_FileNotExists_NoException()
    {
        // Arrange - 不创建任何文件

        // Act & Assert
        var act = async () => await _storage.ClearTokenAsync();
        await act.Should().NotThrowAsync("清除不存在的文件不应抛异常");
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

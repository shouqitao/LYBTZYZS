using System.IO;
using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Desktop.Foundation.Tests.Security;

/// <summary>
/// CredentialVault单元测试
/// OpenSpec: refactor-login-authentication (CVT-001, CVT-002)
/// </summary>
public class CredentialVaultTests : IDisposable
{
    private readonly ILogger<CredentialVault> _logger;
    private readonly CredentialVault _vault;
    private readonly string _testVaultPath;

    public CredentialVaultTests()
    {
        _logger = Substitute.For<ILogger<CredentialVault>>();
        _vault = new CredentialVault(_logger);

        // 获取测试用的vault路径
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _testVaultPath = Path.Combine(appDataPath, "LYBT", "Desktop", "vault.dat");
    }

    public void Dispose()
    {
        // 清理测试数据
        if (File.Exists(_testVaultPath))
        {
            try { File.Delete(_testVaultPath); } catch { }
        }
    }

    #region SaveAutoLoginTokenAsync Tests

    [Fact]
    public async Task SaveAutoLoginTokenAsync_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var username = "testuser";
        var token = "test-auto-login-token-12345";

        // Act
        var result = await _vault.SaveAutoLoginTokenAsync(username, token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAutoLoginTokenAsync_WithEmptyUsername_ShouldThrowArgumentException()
    {
        // Arrange
        var token = "test-token";

        // Act
        var act = async () => await _vault.SaveAutoLoginTokenAsync("", token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("username");
    }

    [Fact]
    public async Task SaveAutoLoginTokenAsync_WithEmptyToken_ShouldThrowArgumentException()
    {
        // Arrange
        var username = "testuser";

        // Act
        var act = async () => await _vault.SaveAutoLoginTokenAsync(username, "");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("autoLoginToken");
    }

    [Fact]
    public async Task SaveAutoLoginTokenAsync_WithNullUsername_ShouldThrowArgumentException()
    {
        // Arrange
        var token = "test-token";

        // Act
        var act = async () => await _vault.SaveAutoLoginTokenAsync(null!, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveAutoLoginTokenAsync_UpdateExistingUser_ShouldOverwrite()
    {
        // Arrange
        var username = "testuser";
        var token1 = "token-version-1";
        var token2 = "token-version-2";

        // Act
        await _vault.SaveAutoLoginTokenAsync(username, token1);
        await _vault.SaveAutoLoginTokenAsync(username, token2);
        var retrieved = await _vault.GetAutoLoginTokenAsync(username);

        // Assert
        retrieved.Should().Be(token2);
    }

    #endregion

    #region GetAutoLoginTokenAsync Tests

    [Fact]
    public async Task GetAutoLoginTokenAsync_AfterSave_ShouldReturnSameToken()
    {
        // Arrange
        var username = "testuser";
        var token = "my-secret-auto-login-token";

        // Act
        await _vault.SaveAutoLoginTokenAsync(username, token);
        var retrieved = await _vault.GetAutoLoginTokenAsync(username);

        // Assert
        retrieved.Should().Be(token);
    }

    [Fact]
    public async Task GetAutoLoginTokenAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var username = "nonexistent-user";

        // Act
        var result = await _vault.GetAutoLoginTokenAsync(username);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAutoLoginTokenAsync_WithEmptyUsername_ShouldReturnNull()
    {
        // Act
        var result = await _vault.GetAutoLoginTokenAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAutoLoginTokenAsync_WithNullUsername_ShouldReturnNull()
    {
        // Act
        var result = await _vault.GetAutoLoginTokenAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAutoLoginTokenAsync_CaseInsensitiveUsername_ShouldMatch()
    {
        // Arrange
        var username = "TestUser";
        var token = "test-token";

        // Act
        await _vault.SaveAutoLoginTokenAsync(username, token);
        var result = await _vault.GetAutoLoginTokenAsync("testuser");

        // Assert
        result.Should().Be(token);
    }

    #endregion

    #region ClearCredentialsAsync Tests

    [Fact]
    public async Task ClearCredentialsAsync_WithSpecificUser_ShouldRemoveOnlyThatUser()
    {
        // Arrange
        await _vault.SaveAutoLoginTokenAsync("user1", "token1");
        await _vault.SaveAutoLoginTokenAsync("user2", "token2");

        // Act
        var result = await _vault.ClearCredentialsAsync("user1");

        // Assert
        result.Should().BeTrue();
        (await _vault.GetAutoLoginTokenAsync("user1")).Should().BeNull();
        (await _vault.GetAutoLoginTokenAsync("user2")).Should().Be("token2");
    }

    [Fact]
    public async Task ClearCredentialsAsync_WithNullUsername_ShouldClearAll()
    {
        // Arrange
        await _vault.SaveAutoLoginTokenAsync("user1", "token1");
        await _vault.SaveAutoLoginTokenAsync("user2", "token2");

        // Act
        var result = await _vault.ClearCredentialsAsync(null);

        // Assert
        result.Should().BeTrue();
        (await _vault.GetAutoLoginTokenAsync("user1")).Should().BeNull();
        (await _vault.GetAutoLoginTokenAsync("user2")).Should().BeNull();
    }

    [Fact]
    public async Task ClearCredentialsAsync_WhenNoData_ShouldReturnTrue()
    {
        // Act
        var result = await _vault.ClearCredentialsAsync("anyuser");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region VerifyIntegrityAsync Tests

    [Fact]
    public async Task VerifyIntegrityAsync_AfterSave_ShouldReturnTrue()
    {
        // Arrange
        var username = "testuser";
        await _vault.SaveAutoLoginTokenAsync(username, "token");

        // Act
        var result = await _vault.VerifyIntegrityAsync(username);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyIntegrityAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Act
        var result = await _vault.VerifyIntegrityAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyIntegrityAsync_WithEmptyUsername_ShouldReturnFalse()
    {
        // Act
        var result = await _vault.VerifyIntegrityAsync("");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasValidTokenAsync Tests

    [Fact]
    public async Task HasValidTokenAsync_AfterSave_ShouldReturnTrue()
    {
        // Arrange
        var username = "testuser";
        await _vault.SaveAutoLoginTokenAsync(username, "token");

        // Act
        var result = await _vault.HasValidTokenAsync(username);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasValidTokenAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Act
        var result = await _vault.HasValidTokenAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasValidTokenAsync_AfterClear_ShouldReturnFalse()
    {
        // Arrange
        var username = "testuser";
        await _vault.SaveAutoLoginTokenAsync(username, "token");
        await _vault.ClearCredentialsAsync(username);

        // Act
        var result = await _vault.HasValidTokenAsync(username);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region MigrateOldFormatAsync Tests

    [Fact]
    public async Task MigrateOldFormatAsync_WhenNoOldFile_ShouldNotThrow()
    {
        // Act
        var act = async () => await _vault.MigrateOldFormatAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Multi-User Scenarios

    [Fact]
    public async Task MultipleUsers_ShouldStoreIndependently()
    {
        // Arrange
        var users = new[]
        {
            ("user1", "token1"),
            ("user2", "token2"),
            ("user3", "token3")
        };

        // Act
        foreach (var (username, token) in users)
        {
            await _vault.SaveAutoLoginTokenAsync(username, token);
        }

        // Assert
        foreach (var (username, expectedToken) in users)
        {
            var retrieved = await _vault.GetAutoLoginTokenAsync(username);
            retrieved.Should().Be(expectedToken);
        }
    }

    #endregion

    #region Token Persistence Tests

    [Fact]
    public async Task Token_ShouldPersistAcrossNewInstance()
    {
        // Arrange
        var username = "testuser";
        var token = "persistent-token";
        await _vault.SaveAutoLoginTokenAsync(username, token);

        // Act - Create new instance
        var newVault = new CredentialVault(_logger);
        var retrieved = await newVault.GetAutoLoginTokenAsync(username);

        // Assert
        retrieved.Should().Be(token);
    }

    #endregion
}

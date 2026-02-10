using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Services;
using LYBT.Desktop.LocalData.Tests.TestFixtures;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Tests.Services;

/// <summary>
/// LocalAuthService 单元测试
/// OpenSpec: implement-local-mode Phase 5
/// </summary>
public class LocalAuthServiceTests : IClassFixture<LocalDbContextFixture>
{
    private readonly LocalDbContextFixture _fixture;
    private readonly ILogger<LocalAuthService> _logger;

    public LocalAuthServiceTests(LocalDbContextFixture fixture)
    {
        _fixture = fixture;
        _logger = LocalDbContextFixture.CreateLogger<LocalAuthService>();
    }

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_ValidCredentials_ReturnsUser()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("testuser", "Test@123");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("testuser", "Test@123");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("testuser");
    }

    [Fact]
    public async Task ValidateAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("testuser2", "CorrectPassword");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("testuser2", "WrongPassword");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("nonexistent", "password");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_EmptyUsername_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("", "password");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_EmptyPassword_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("username", "");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_DisabledUser_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("disableduser", "Test@123");
        user.Status = CommonStatus.Disabled;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("disableduser", "Test@123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_LockedUser_ReturnsNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("lockeduser", "Test@123");
        user.LockoutEnd = DateTime.Now.AddMinutes(30); // 锁定30分钟
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("lockeduser", "Test@123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ExpiredLockout_AllowsLogin()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("expiredlock", "Test@123");
        user.LockoutEnd = DateTime.Now.AddMinutes(-5); // 锁定已过期
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("expiredlock", "Test@123");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_SuccessfulLogin_ResetsFailedCount()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("resetcount", "Test@123");
        user.FailedLoginCount = 3;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ValidateAsync("resetcount", "Test@123");

        // Assert
        result.Should().NotBeNull();
        result!.FailedLoginCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidateAsync_SuccessfulLogin_UpdatesLastLoginTime()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("logintime", "Test@123");
        var originalTime = user.LastLoginTime;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var beforeLogin = DateTime.Now;
        var result = await service.ValidateAsync("logintime", "Test@123");
        var afterLogin = DateTime.Now;

        // Assert
        result.Should().NotBeNull();
        result!.LastLoginTime.Should().BeOnOrAfter(beforeLogin);
        result.LastLoginTime.Should().BeOnOrBefore(afterLogin);
    }

    [Fact]
    public async Task ValidateAsync_FailedLogin_IncrementsFailedCount()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("failcount", "CorrectPassword");
        user.FailedLoginCount = 0;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        await service.ValidateAsync("failcount", "WrongPassword");

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginCount.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_FiveFailedAttempts_LocksAccount()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("locktest", "CorrectPassword");
        user.FailedLoginCount = 4; // 已经失败4次
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        await service.ValidateAsync("locktest", "WrongPassword"); // 第5次失败

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginCount.Should().Be(5);
        updatedUser.LockoutEnd.Should().NotBeNull();
        updatedUser.LockoutEnd.Should().BeAfter(DateTime.Now);
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_ValidOldPassword_ChangesPassword()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("changepwd", "OldPassword");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ChangePasswordAsync(user.Id, "OldPassword", "NewPassword");

        // Assert
        result.Should().BeTrue();

        // 验证新密码可以登录
        var loginResult = await service.ValidateAsync("changepwd", "NewPassword");
        loginResult.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidOldPassword_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("changepwd2", "CorrectPassword");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ChangePasswordAsync(user.Id, "WrongPassword", "NewPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistingUser_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ChangePasswordAsync(Guid.NewGuid(), "old", "new");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_EmptyOldPassword_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("emptyold", "Password");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ChangePasswordAsync(user.Id, "", "NewPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_EmptyNewPassword_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var user = CreateTestUser("emptynew", "Password");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new LocalAuthService(context, _logger);

        // Act
        var result = await service.ChangePasswordAsync(user.Id, "Password", "");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(string username, string password)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            RealName = $"测试用户_{username}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = CommonStatus.Enabled,
            Role = UserRole.Admin,
            FailedLoginCount = 0,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    #endregion
}

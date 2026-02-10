using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Initialization;
using LYBT.Desktop.IntegrationTests.LocalMode.Fixtures;
using LYBT.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.IntegrationTests.LocalMode;

/// <summary>
/// 本地模式登录流程集成测试
/// 测试完整的认证流程: 数据库初始化 -> 用户创建 -> 密码验证 -> 登录成功/失败
/// OpenSpec: implement-local-mode Phase 5.2
/// </summary>
public class LoginFlowIntegrationTests : IClassFixture<LocalModeTestFixture>
{
    private readonly LocalModeTestFixture _fixture;

    public LoginFlowIntegrationTests(LocalModeTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region 完整登录流程测试

    [Fact]
    public async Task LocalLogin_WithSeedData_AdminCanLogin()
    {
        // Arrange - 创建完整的服务链
        var serviceProvider = _fixture.CreateServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var authService = serviceProvider.GetRequiredService<ILocalAuthService>();

        // Act - 初始化数据库（创建 admin 用户）
        await initializer.InitializeAsync();

        // 尝试登录
        var result = await authService.ValidateAsync("admin", "Admin@123");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("admin");
        result.Role.Should().Be(LYBT.Shared.Models.Enums.UserRole.SuperAdmin);
    }

    [Fact]
    public async Task LocalLogin_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var authService = serviceProvider.GetRequiredService<ILocalAuthService>();

        await initializer.InitializeAsync();

        // Act
        var result = await authService.ValidateAsync("admin", "WrongPassword");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LocalLogin_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var authService = serviceProvider.GetRequiredService<ILocalAuthService>();

        await initializer.InitializeAsync();

        // Act
        var result = await authService.ValidateAsync("nonexistent", "SomePassword");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region 密码变更流程测试

    [Fact]
    public async Task ChangePassword_ValidOldPassword_Success()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var authService = serviceProvider.GetRequiredService<ILocalAuthService>();

        await initializer.InitializeAsync();

        // 获取 admin 用户 ID
        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        adminUser.Should().NotBeNull();

        // Act - 修改密码
        var changeResult = await authService.ChangePasswordAsync(
            adminUser!.Id,
            "Admin@123",
            "NewPassword@456");

        // Assert
        changeResult.Should().BeTrue();

        // 验证新密码可以登录
        var loginResult = await authService.ValidateAsync("admin", "NewPassword@456");
        loginResult.Should().NotBeNull();

        // 验证旧密码无法登录
        var oldLoginResult = await authService.ValidateAsync("admin", "Admin@123");
        oldLoginResult.Should().BeNull();
    }

    [Fact]
    public async Task ChangePassword_InvalidOldPassword_Fails()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var authService = serviceProvider.GetRequiredService<ILocalAuthService>();

        await initializer.InitializeAsync();

        var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserName == "admin");

        // Act
        var result = await authService.ChangePasswordAsync(
            adminUser!.Id,
            "WrongOldPassword",
            "NewPassword@456");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 账户锁定流程测试

    [Fact]
    public async Task AccountLocking_MultipleFailedAttempts_LocksAccount()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var authService = serviceProvider.GetRequiredService<ILocalAuthService>();

        await initializer.InitializeAsync();

        // Act - 连续失败登录 5 次
        for (int i = 0; i < 5; i++)
        {
            await authService.ValidateAsync("admin", "WrongPassword");
        }

        // Assert - 第 6 次即使密码正确也应该失败（账户锁定）
        var result = await authService.ValidateAsync("admin", "Admin@123");
        result.Should().BeNull("账户应该被锁定");
    }

    #endregion

    #region 数据库初始化测试

    [Fact]
    public async Task DatabaseInitializer_MultipleRuns_Idempotent()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();

        // Act - 运行两次初始化
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        // Assert - 应该只有一个 admin 用户
        var adminCount = await dbContext.Users.CountAsync(u => u.UserName == "admin");
        adminCount.Should().Be(1);
    }

    #endregion
}

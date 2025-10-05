using FluentAssertions;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.IntegrationTests.Infrastructure;
using Xunit;
using static LYBT.WebAPI.IntegrationTests.Infrastructure.TestHelpers;

namespace LYBT.WebAPI.IntegrationTests.Examples;

/// <summary>
/// 使用示例 - 展示测试基础设施的最佳实践
/// </summary>
/// <remarks>
/// 这些示例展示了如何使用：
/// - TestDataSeeder（种子数据）
/// - UserBuilder（构建器模式）
/// - TestHelpers（辅助方法）
/// </remarks>
public class UsageExamples : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsageExamples(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 示例 1: 使用种子数据进行登录测试
    /// </summary>
    [Fact]
    public async Task Example1_LoginWithSeededData()
    {
        // Arrange - 初始化默认用户（admin, doctor, pharmacist）
        await _factory.Seeder.SeedDefaultUsersAsync();

        var client = _factory.CreateClient();

        // Act - 使用种子数据中的 admin 账户登录
        var token = await client.LoginAndGetTokenAsync("admin", "Admin123!");

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 示例 2: 使用 Builder 模式创建自定义用户
    /// </summary>
    [Fact]
    public async Task Example2_CreateCustomUserWithBuilder()
    {
        // Arrange - 使用 Builder 模式创建自定义医生
        var (user, password) = CreateUser()
            .WithUserName("dr_zhang")
            .WithRealName("张医生")
            .AsDoctor()
            .WithPhoneNumber("13800138000")
            .WithEmail("dr.zhang@example.com")
            .BuildWithPassword();

        // 保存到数据库
        await _factory.SaveUserAsync(user);

        var client = _factory.CreateClient();

        // Act - 使用自定义用户登录
        var token = await client.LoginAndGetTokenAsync("dr_zhang", password);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 示例 3: 创建禁用用户并验证登录失败
    /// </summary>
    [Fact]
    public async Task Example3_DisabledUserCannotLogin()
    {
        // Arrange - 创建禁用的用户
        var (user, password) = CreateUser()
            .WithUserName("disabled_user")
            .AsDoctor()
            .Disabled() // 设置为禁用状态
            .BuildWithPassword();

        await _factory.SaveUserAsync(user);

        var client = _factory.CreateClient();

        // Act - 尝试登录
        var loginAction = async () => await client.LoginAndGetTokenAsync("disabled_user", password);

        // Assert - 登录应该失败
        await loginAction.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 示例 4: 批量创建不同角色的用户
    /// </summary>
    [Fact]
    public async Task Example4_CreateMultipleRoles()
    {
        // Arrange - 批量创建不同角色的用户
        var users = new[]
        {
            CreateUser().AsAdmin().Build(),
            CreateUser().AsDoctor().Build(),
            CreateUser().AsPharmacist().Build()
        };

        foreach (var user in users)
        {
            await _factory.SaveUserAsync(user);
        }

        // Assert - 验证角色正确
        users[0].Role.Should().Be(UserRole.Admin);
        users[1].Role.Should().Be(UserRole.Doctor);
        users[2].Role.Should().Be(UserRole.Doctor);
    }

    /// <summary>
    /// 示例 5: 测试数据重置
    /// </summary>
    [Fact]
    public async Task Example5_ResetTestData()
    {
        // Arrange - 创建一些测试用户
        var user = CreateUser().AsDoctor().Build();
        await _factory.SaveUserAsync(user);

        // Act - 重置测试数据（清空 + 初始化默认数据）
        await _factory.Seeder.ResetAsync();

        // Assert - 只剩下默认的 3 个用户
        var client = _factory.CreateClient();
        var adminToken = await client.LoginAndGetTokenAsync("admin", "Admin123!");
        adminToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 示例 6: 组合使用 - 创建用户并登录授权
    /// </summary>
    [Fact]
    public async Task Example6_CreateUserAndAuthorize()
    {
        // Arrange - 创建用户并保存
        var (user, password) = CreateUser()
            .WithUserName("test_doctor")
            .AsDoctor()
            .BuildWithPassword();

        await _factory.SaveUserAsync(user);

        var client = _factory.CreateClient();

        // Act - 登录并设置授权头（一行代码完成）
        await client.LoginAndSetAuthorizationAsync("test_doctor", password);

        // 现在可以调用需要授权的 API
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}

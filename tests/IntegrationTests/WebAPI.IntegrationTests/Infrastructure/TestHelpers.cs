using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Users;

namespace LYBT.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// 集成测试辅助方法
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// 登录并获取访问令牌
    /// </summary>
    /// <param name="client">HTTP 客户端</param>
    /// <param name="username">用户名（默认：admin）</param>
    /// <param name="password">密码（默认：Admin123!）</param>
    /// <returns>访问令牌</returns>
    public static async Task<string> LoginAndGetTokenAsync(
        this HttpClient client,
        string username = "admin",
        string password = "Admin123!")
    {
        // 发送登录请求
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // 验证响应成功
        response.IsSuccessStatusCode.Should().BeTrue(
            $"登录失败，状态码：{response.StatusCode}");

        // 解析响应并获取 Token
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();

        return result.Data.Token;
    }

    /// <summary>
    /// 设置授权头（Bearer Token）
    /// </summary>
    /// <param name="client">HTTP 客户端</param>
    /// <param name="token">访问令牌</param>
    public static void SetAuthorizationHeader(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// 登录并设置授权头
    /// </summary>
    /// <param name="client">HTTP 客户端</param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    public static async Task LoginAndSetAuthorizationAsync(
        this HttpClient client,
        string username = "admin",
        string password = "Admin123!")
    {
        var token = await client.LoginAndGetTokenAsync(username, password);
        client.SetAuthorizationHeader(token);
    }

    /// <summary>
    /// 创建测试用户
    /// </summary>
    /// <param name="factory">Web 应用程序工厂</param>
    /// <param name="username">用户名（默认：自动生成）</param>
    /// <param name="password">密码（默认：Test123!）</param>
    /// <param name="role">角色（默认：医生）</param>
    /// <param name="realName">真实姓名（默认：测试用户）</param>
    /// <returns>创建的用户 DTO</returns>
    public static async Task<UserDto> CreateTestUserAsync(
        this CustomWebApplicationFactory factory,
        string? username = null,
        string password = "Test123!",
        UserRole role = UserRole.Doctor,
        string realName = "测试用户")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 生成唯一的用户名
        username ??= $"test_{Guid.NewGuid():N}";

        // 创建用户实体
        var user = new LYBT.Entities.Users.User
        {
            UserName = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            RealName = realName,
            Role = role,
            Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 返回 DTO
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            RealName = user.RealName,
            Role = user.Role,
            Status = user.Status,
            CreateTime = user.CreatedAt
        };
    }

    /// <summary>
    /// 清理测试数据
    /// </summary>
    /// <param name="factory">Web 应用程序工厂</param>
    public static async Task CleanupTestDataAsync(this CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 删除所有测试数据（除了默认的 admin 用户）
        var testUsers = db.Users.Where(u => u.UserName != "admin");
        db.Users.RemoveRange(testUsers);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 生成唯一的测试数据前缀
    /// </summary>
    /// <param name="prefix">前缀（默认：test）</param>
    /// <returns>带时间戳的唯一前缀</returns>
    public static string GenerateUniquePrefix(string prefix = "test")
    {
        return $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N[..8]}";
    }
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using LYBT.Tests.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Auth;

/// <summary>
/// AuthController集成测试 - Issue #1876
/// 测试Token撤销、审计日志和Token轮换的端到端流程
/// </summary>
public sealed class AuthTokenAdvancedIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private const string TestPassword = "Test123!@#";

    public AuthTokenAdvancedIntegrationTests(ServerFixture fixture, ITestOutputHelper output)
        : base(fixture)
    {
        _output = output;
    }

    /// <summary>
    /// 创建测试用户并返回 (userId, userName) 元组。
    /// 每次调用生成唯一的用户名和邮箱，避免唯一索引冲突。
    /// </summary>
    private async Task<(Guid UserId, string UserName)> SeedTestUserAsync()
    {
        var testUserId = Guid.NewGuid();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var userName = $"inttest_{uniqueSuffix}";
        var email = $"test-{uniqueSuffix}@integration.com";

        await Fixture.WithDbContextAsync(async db =>
        {
            var testUser = new User
            {
                Id = testUserId,
                UserName = userName,
                RealName = "集成测试用户",
                PasswordHash = PasswordHelper.HashPassword(TestPassword),
                Role = UserRole.Doctor,
                Email = email,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Set<User>().Add(testUser);
            await db.SaveChangesAsync();
        });
        return (testUserId, userName);
    }

    #region Token撤销测试

    [Fact]
    public async Task RefreshToken_AfterRevocation_ShouldReturn401()
    {
        // Arrange - 创建测试用户并登录获取Token
        var (testUserId, userName) = await SeedTestUserAsync();
        _output.WriteLine("测试场景: Token撤销后刷新应返回401");

        var loginRequest = new LoginRequest
        {
            UserName = userName,
            Password = TestPassword
        };

        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data.Should().NotBeNull();

        var refreshToken = loginResult.Data!.RefreshToken;
        _output.WriteLine($"获取到RefreshToken: {refreshToken[..Math.Min(20, refreshToken.Length)]}...");

        // 直接在数据库中撤销Token
        await Fixture.WithDbContextAsync(async db =>
        {
            var tokenRecord = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            tokenRecord.Should().NotBeNull();
            tokenRecord!.Revoke("测试撤销", $"IntegrationTest:{testUserId}");
            await db.SaveChangesAsync();

            _output.WriteLine($"已撤销Token (原因: {tokenRecord.RevokedReason})");
        });

        // Act - 尝试使用被撤销的Token刷新
        var refreshRequest = new { refreshToken };
        var refreshResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        _output.WriteLine($"刷新响应状态码: {refreshResponse.StatusCode}");
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var errorResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        errorResult.Should().NotBeNull();
        errorResult!.Success.Should().BeFalse();
        errorResult.Message.Should().Contain("已撤销");

        _output.WriteLine($"验证通过: {errorResult.Message}");
    }

    #endregion

    #region 审计日志测试

    [Fact]
    public async Task Login_Success_ShouldRecordAuditLog()
    {
        // Arrange
        var (testUserId, userName) = await SeedTestUserAsync();
        _output.WriteLine("测试场景: 登录成功应记录审计日志");

        var loginRequest = new LoginRequest
        {
            UserName = userName,
            Password = TestPassword
        };

        // Act - 登录
        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();

        _output.WriteLine($"登录成功: {loginResult.Data!.User.UserName}");

        // Assert - 验证审计日志
        await Fixture.WithDbContextAsync(async db =>
        {
            var auditLog = await db.SecurityAuditLogs
                .Where(log => log.EventType == "Login" &&
                             log.UserId == testUserId &&
                             log.UserName == userName)
                .OrderByDescending(log => log.CreatedAt)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
            auditLog!.Success.Should().BeTrue();
            auditLog.UserName.Should().Be(userName);
            // Note: 集成测试环境中IpAddress可能为null (HttpContext.Connection.RemoteIpAddress不可用)
            // auditLog.IpAddress.Should().NotBeNullOrEmpty();

            _output.WriteLine($"审计日志已记录:");
            _output.WriteLine($"   - EventType: {auditLog.EventType}");
            _output.WriteLine($"   - UserName: {auditLog.UserName}");
            _output.WriteLine($"   - Success: {auditLog.Success}");
            _output.WriteLine($"   - IpAddress: {auditLog.IpAddress}");
            _output.WriteLine($"   - CreatedAt: {auditLog.CreatedAt}");
        });
    }

    #endregion

    #region Token轮换测试

    [Fact]
    public async Task RefreshToken_Success_ShouldRevokeOldToken()
    {
        // Arrange - 创建测试用户并登录获取初始Token
        var (testUserId, userName) = await SeedTestUserAsync();
        _output.WriteLine("测试场景: Token刷新应撤销旧Token并生成新Token");

        var loginRequest = new LoginRequest
        {
            UserName = userName,
            Password = TestPassword
        };

        var loginResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var oldRefreshToken = loginResult!.Data!.RefreshToken;
        var oldAccessToken = loginResult.Data.Token;

        _output.WriteLine($"初始RefreshToken: {oldRefreshToken[..Math.Min(20, oldRefreshToken.Length)]}...");
        _output.WriteLine($"初始AccessToken: {oldAccessToken[..Math.Min(30, oldAccessToken.Length)]}...");

        // Act - 刷新Token
        var refreshRequest = new { refreshToken = oldRefreshToken };
        var refreshResponse = await AnonymousClient.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert - 验证刷新成功
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        refreshResult.Should().NotBeNull();
        refreshResult!.Success.Should().BeTrue();
        refreshResult.Data.Should().NotBeNull();

        var newRefreshToken = refreshResult.Data!.RefreshToken;
        var newAccessToken = refreshResult.Data.Token;

        newRefreshToken.Should().NotBe(oldRefreshToken);
        newAccessToken.Should().NotBe(oldAccessToken);

        _output.WriteLine($"新RefreshToken: {newRefreshToken[..Math.Min(20, newRefreshToken.Length)]}...");
        _output.WriteLine($"新AccessToken: {newAccessToken[..Math.Min(30, newAccessToken.Length)]}...");

        // 验证旧Token已被标记为已使用 (Token轮换使用 MarkAsUsed 而非 Revoke)
        await Fixture.WithDbContextAsync(async db =>
        {
            var oldToken = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == oldRefreshToken);

            oldToken.Should().NotBeNull();
            oldToken!.IsUsed.Should().BeTrue();
            oldToken.UsedAt.Should().NotBeNull();
            oldToken.ReplacedByToken.Should().Be(newRefreshToken);

            _output.WriteLine($"旧Token已标记为已使用:");
            _output.WriteLine($"   - IsUsed: {oldToken.IsUsed}");
            _output.WriteLine($"   - UsedAt: {oldToken.UsedAt}");
            _output.WriteLine($"   - ReplacedByToken: {oldToken.ReplacedByToken?[..Math.Min(20, oldToken.ReplacedByToken.Length)]}...");

            // 验证新Token存在且有效
            var newToken = await db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == newRefreshToken);

            newToken.Should().NotBeNull();
            newToken!.IsRevoked.Should().BeFalse();
            newToken.UserId.Should().Be(testUserId);

            _output.WriteLine($"新Token已创建:");
            _output.WriteLine($"   - IsRevoked: {newToken.IsRevoked}");
            _output.WriteLine($"   - UserId: {newToken.UserId}");
            _output.WriteLine($"   - ExpiresAt: {newToken.ExpiresAt}");
        });
    }

    #endregion
}

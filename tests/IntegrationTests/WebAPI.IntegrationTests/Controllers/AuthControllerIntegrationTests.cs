using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers;

/// <summary>
/// AuthController集成测试 - Issue #1876
/// 测试Token撤销、审计日志和Token轮换的端到端流程
/// </summary>
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private Guid _testUserId;
    private const string TestUserName = "integrationtest";
    private const string TestPassword = "Test123!@#";

    public AuthControllerIntegrationTests(ITestOutputHelper output) : base()
    {
        _output = output;
    }

    /// <summary>
    /// 创建测试用户
    /// </summary>
    protected override void SeedBasicTestData(LYBT.Infrastructure.Data.AppDbContext context)
    {
        base.SeedBasicTestData(context);

        // 创建测试用户
        _testUserId = Guid.NewGuid();
        var testUser = new User
        {
            Id = _testUserId,
            UserName = TestUserName,
            RealName = "集成测试用户",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestPassword),
            Role = UserRole.Doctor,
            Email = "test@integration.com",
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Set<User>().Add(testUser);
        context.SaveChanges();

        // Note: _output可能为null（基类构造函数调用时派生类字段尚未初始化）
        _output?.WriteLine($"✅ 创建测试用户: {TestUserName} (ID: {_testUserId})");
    }

    #region Token撤销测试

    [Fact]
    public async Task RefreshToken_AfterRevocation_ShouldReturn401()
    {
        // Arrange - 登录获取Token
        _output.WriteLine("📝 测试场景: Token撤销后刷新应返回401");

        var loginRequest = new LoginRequest
        {
            UserName = TestUserName,
            Password = TestPassword
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data.Should().NotBeNull();

        var refreshToken = loginResult.Data!.RefreshToken;
        _output.WriteLine($"🔑 获取到RefreshToken: {refreshToken.Substring(0, 20)}...");

        // 直接在数据库中撤销Token
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();
            var tokenRecord = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            tokenRecord.Should().NotBeNull();
            tokenRecord!.Revoke("测试撤销", $"IntegrationTest:{_testUserId}");
            await dbContext.SaveChangesAsync();

            _output.WriteLine($"🚫 已撤销Token (原因: {tokenRecord.RevokedReason})");
        }

        // Act - 尝试使用被撤销的Token刷新
        var refreshRequest = new { refreshToken };
        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        _output.WriteLine($"📡 刷新响应状态码: {refreshResponse.StatusCode}");
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var errorResult = await refreshResponse.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>();
        errorResult.Should().NotBeNull();
        errorResult!.Success.Should().BeFalse();
        errorResult.Message.Should().Contain("已撤销");

        _output.WriteLine($"✅ 验证通过: {errorResult.Message}");
    }

    #endregion

    #region 审计日志测试

    [Fact]
    public async Task Login_Success_ShouldRecordAuditLog()
    {
        // Arrange
        _output.WriteLine("📝 测试场景: 登录成功应记录审计日志");

        var loginRequest = new LoginRequest
        {
            UserName = TestUserName,
            Password = TestPassword
        };

        // Act - 登录
        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();

        _output.WriteLine($"✅ 登录成功: {loginResult.Data!.User.UserName}");

        // Assert - 验证审计日志
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

            var auditLog = await dbContext.SecurityAuditLogs
                .Where(log => log.EventType == "Login" &&
                             log.UserId == _testUserId &&
                             log.UserName == TestUserName)
                .OrderByDescending(log => log.CreatedAt)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
            auditLog!.Success.Should().BeTrue();
            auditLog.UserName.Should().Be(TestUserName);
            // Note: 集成测试环境中IpAddress可能为null（HttpContext.Connection.RemoteIpAddress不可用）
            // auditLog.IpAddress.Should().NotBeNullOrEmpty();

            _output.WriteLine($"📋 审计日志已记录:");
            _output.WriteLine($"   - EventType: {auditLog.EventType}");
            _output.WriteLine($"   - UserName: {auditLog.UserName}");
            _output.WriteLine($"   - Success: {auditLog.Success}");
            _output.WriteLine($"   - IpAddress: {auditLog.IpAddress}");
            _output.WriteLine($"   - CreatedAt: {auditLog.CreatedAt}");
        }
    }

    #endregion

    #region Token轮换测试

    [Fact]
    public async Task RefreshToken_Success_ShouldRevokeOldToken()
    {
        // Arrange - 登录获取初始Token
        _output.WriteLine("📝 测试场景: Token刷新应撤销旧Token并生成新Token");

        var loginRequest = new LoginRequest
        {
            UserName = TestUserName,
            Password = TestPassword
        };

        var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>();
        var oldRefreshToken = loginResult!.Data!.RefreshToken;
        var oldAccessToken = loginResult.Data.Token;

        _output.WriteLine($"🔑 初始RefreshToken: {oldRefreshToken.Substring(0, 20)}...");
        _output.WriteLine($"🔑 初始AccessToken: {oldAccessToken.Substring(0, 30)}...");

        // Act - 刷新Token
        var refreshRequest = new { refreshToken = oldRefreshToken };
        var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert - 验证刷新成功
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>();
        refreshResult.Should().NotBeNull();
        refreshResult!.Success.Should().BeTrue();
        refreshResult.Data.Should().NotBeNull();

        var newRefreshToken = refreshResult.Data!.RefreshToken;
        var newAccessToken = refreshResult.Data.Token;

        newRefreshToken.Should().NotBe(oldRefreshToken);
        newAccessToken.Should().NotBe(oldAccessToken);

        _output.WriteLine($"🔄 新RefreshToken: {newRefreshToken.Substring(0, 20)}...");
        _output.WriteLine($"🔄 新AccessToken: {newAccessToken.Substring(0, 30)}...");

        // 验证旧Token已被撤销
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LYBT.Infrastructure.Data.AppDbContext>();

            var oldToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == oldRefreshToken);

            oldToken.Should().NotBeNull();
            oldToken!.IsRevoked.Should().BeTrue();
            oldToken.RevokedReason.Should().Contain("已被新Token替换");
            oldToken.RevokedAt.Should().NotBeNull();
            oldToken.ReplacedByToken.Should().Be(newRefreshToken);

            _output.WriteLine($"✅ 旧Token已撤销:");
            _output.WriteLine($"   - IsRevoked: {oldToken.IsRevoked}");
            _output.WriteLine($"   - RevokedReason: {oldToken.RevokedReason}");
            _output.WriteLine($"   - RevokedAt: {oldToken.RevokedAt}");
            _output.WriteLine($"   - ReplacedByToken: {oldToken.ReplacedByToken?.Substring(0, 20)}...");

            // 验证新Token存在且有效
            var newToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == newRefreshToken);

            newToken.Should().NotBeNull();
            newToken!.IsRevoked.Should().BeFalse();
            newToken.UserId.Should().Be(_testUserId);

            _output.WriteLine($"✅ 新Token已创建:");
            _output.WriteLine($"   - IsRevoked: {newToken.IsRevoked}");
            _output.WriteLine($"   - UserId: {newToken.UserId}");
            _output.WriteLine($"   - ExpiresAt: {newToken.ExpiresAt}");
        }
    }

    #endregion
}

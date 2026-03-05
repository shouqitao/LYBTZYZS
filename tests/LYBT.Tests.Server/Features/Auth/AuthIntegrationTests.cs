using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Auth;

/// <summary>
/// 认证模块集成测试。
/// 验证完整HTTP管线: Controller -> AuthService -> JwtService -> DB。
/// 不Mock任何组件，测试真实认证流程。
/// </summary>
public sealed class AuthIntegrationTests : IntegrationTestBase
{
    // Test credentials matching ServerFixture seed data
    private const string AdminPassword = "TestAdmin2025@";
    private const string DoctorPassword = "TestDoctor2025@";
    private const string SysAdminPassword = "TestAdmin2025@";

    public AuthIntegrationTests(ServerFixture fixture) : base(fixture) { }

    #region Login - 成功场景

    [Fact]
    public async Task Login_ValidAdminCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = AdminPassword
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace("登录成功应返回JWT Token");
        body.Data.RefreshToken.Should().NotBeNullOrWhiteSpace("登录成功应返回RefreshToken");
        body.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow, "Token过期时间应在当前时间之后");
        body.Data.User.Should().NotBeNull("登录成功应返回用户信息");
    }

    [Fact]
    public async Task Login_ValidDoctorCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "doctor",
            Password = DoctorPassword
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 验证生产种子路径: sysadmin (SuperAdmin) 可正常登录。
    /// 与DatabaseInitializationService.EnsureSystemAdminExistsAsync()对齐，
    /// 确保测试覆盖生产初始化路径。
    /// </summary>
    [Fact]
    public async Task Login_SysAdminCredentials_ReturnsTokenWithSuperAdminRole()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "sysadmin",
            Password = SysAdminPassword
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace("sysadmin登录应返回JWT Token");
        body.Data.User.Should().NotBeNull("登录应返回用户信息");
        body.Data.User!.Role.Should().Be(UserRole.SuperAdmin, "sysadmin应具有SuperAdmin角色");
    }

    #endregion

    #region Login - 失败场景

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = "wrong_password_123"
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "nonexistent_user_xyz",
            Password = "any_password"
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_EmptyUsername_Returns400()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "",
            Password = "some_password"
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - Controller直接返回ValidationFail(400)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "admin",
            Password = ""
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login - 边界场景

    /// <summary>
    /// 验证禁用用户登录返回 403 (UserDisabled)。
    /// 覆盖 AuthService.VerifyCredentialsInternalAsync 的 Status == Disabled 分支。
    /// </summary>
    [Fact]
    public async Task Login_DisabledUser_Returns403()
    {
        // Arrange: 种子一个被禁用的用户
        var disabledUserId = Guid.NewGuid();
        var disabledUsername = $"disabled_user_{Guid.NewGuid():N}"[..24];
        const string disabledPassword = "TestDisabled2025@";

        await Fixture.WithDbContextAsync(async db =>
        {
            db.Set<User>().Add(new User
            {
                Id = disabledUserId,
                UserName = disabledUsername,
                RealName = "禁用测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Disabled,
                PasswordHash = PasswordHelper.HashPassword(disabledPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var request = new LoginRequest
        {
            UserName = disabledUsername,
            Password = disabledPassword
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - AuthService 对禁用用户返回 UserDisabled -> Controller 映射为 403
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "禁用用户登录应返回 403 Forbidden");
    }

    /// <summary>
    /// 验证密码Hash为空字符串的用户登录返回401而非500。
    /// 覆盖 PasswordHelper.VerifyPassword 对空字符串 hash 的防御处理。
    /// 注: 数据库 PasswordHash 列有 NOT NULL 约束，null 场景由 DB 层防御。
    /// </summary>
    [Fact]
    public async Task Login_UserWithEmptyPasswordHash_Returns401()
    {
        // Arrange: 种子一个密码Hash为空字符串的用户
        var emptyHashUserId = Guid.NewGuid();
        var emptyHashUsername = $"emptyhash_user_{Guid.NewGuid():N}"[..24];

        await Fixture.WithDbContextAsync(async db =>
        {
            db.Set<User>().Add(new User
            {
                Id = emptyHashUserId,
                UserName = emptyHashUsername,
                RealName = "空Hash测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var request = new LoginRequest
        {
            UserName = emptyHashUsername,
            Password = "any_password_123"
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert - PasswordHelper.VerifyPassword 对空字符串返回失败
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "密码Hash为空时应返回401 (PasswordHelper 检测到 IsNullOrEmpty 直接返回失败)");
    }

    #endregion

    #region Authorization - 权限控制

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange & Act - GET /api/v1/users 需要AdminOnly策略
        var response = await AnonymousClient
            .GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAdminToken_Returns200()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_WithDoctorToken_Returns403()
    {
        // Arrange
        var doctor = await LoginAsDoctorAsync();

        // Act
        // UsersController标记[Authorize(Policy="AdminOnly")]
        // Doctor角色不在AdminOnly策略("SuperAdmin","Admin")中
        var response = await doctor.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Token验证

    [Fact]
    public async Task ValidateToken_WithLoginToken_ReturnsSuccess()
    {
        // Act
        var admin = await LoginAsAdminAsync();
        var response = await admin.GetAsync("/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidateToken_WithoutToken_Returns401()
    {
        // Arrange & Act
        var response = await AnonymousClient
            .GetAsync("/api/v1/auth/validate");

        // Assert - [Authorize]中间件拦截
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Token刷新

    [Fact]
    public async Task RefreshToken_AfterLogin_ReturnsNewTokens()
    {
        // Arrange: 登录获取RefreshToken
        var loginRequest = new LoginRequest
        {
            UserName = "admin",
            Password = AdminPassword
        };
        var loginResponse = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var originalToken = loginBody!.Data!.Token;
        var refreshToken = loginBody.Data.RefreshToken;
        refreshToken.Should().NotBeNullOrWhiteSpace();

        // Act: 使用RefreshToken刷新
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };
        var refreshResponse = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        refreshBody!.Success.Should().BeTrue();
        refreshBody.Data!.Token.Should().NotBeNullOrWhiteSpace("刷新应返回新Token");
        refreshBody.Data.Token.Should().NotBe(originalToken, "新Token应与原Token不同");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalid_refresh_token_" + Guid.NewGuid()
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithEmptyToken_Returns400()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = ""
        };

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/refresh", request);

        // Assert - Controller验证空Token返回400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_WithRefreshToken_Succeeds()
    {
        // Arrange: 登录获取RefreshToken
        var loginRequest = new LoginRequest
        {
            UserName = "doctor",
            Password = DoctorPassword
        };
        var loginResponse = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
        var refreshToken = loginBody!.Data!.RefreshToken;

        // Act: 登出
        var logoutRequest = new LogoutRequest
        {
            RefreshToken = refreshToken,
            UserName = "doctor"
        };
        var logoutResponse = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutRefreshTokenOrUsername_Returns400()
    {
        // Arrange - 既不提供RefreshToken也不提供UserName
        var request = new LogoutRequest();

        // Act
        var response = await AnonymousClient
            .PostAsJsonAsync("/api/v1/auth/logout", request);

        // Assert - Controller验证"必须提供RefreshToken或用户名"
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace LYBT.Desktop.Foundation.IntegrationTests.Security;

/// <summary>
/// AuthenticationService 集成测试
/// Issue #1867: 测试端到端认证流程、Token刷新
/// Issue #1907: 更新为内存存储模式（医疗系统安全要求）
///
/// 集成测试特点：
/// - 使用真实的TokenStorageService（内存存储，Session级别）
/// - 使用真实的LocalTokenValidator（真实JWT验证）
/// - 仅Mock Server API（IAuthApi）
/// - 测试组件协同工作
///
/// 内存存储设计原则（Issue #1907）：
/// - Token = 会话级数据，应用关闭即失效
/// - 每次启动必须重新登录（医疗合规性要求）
/// - 不支持跨应用重启的Token恢复
/// </summary>
public class AuthenticationIntegrationTests
{
    private const string SecretKey = "your-test-secret-key-at-least-32-characters-long-for-testing";
    private const string Issuer = "LYBT.WebAPI";
    private const string Audience = "LYBT.Desktop";

    /// <summary>
    /// 端到端测试：登录 → 内存存储 → 同会话内恢复
    /// </summary>
    /// <remarks>
    /// Issue #1907: 内存存储模式下，Token仅在同一会话内有效
    /// 不再测试跨应用重启恢复（设计上不支持）
    /// </remarks>
    [Fact]
    public async Task EndToEnd_Login_Store_ValidateInSameSession()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var authService = serviceProvider.GetRequiredService<IAuthenticationService>();

        var testUser = new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Role = UserRole.Doctor
        };

        var loginResponse = new LoginResponse
        {
            Token = GenerateValidToken(testUser.Id, testUser.UserName, testUser.Role.ToString()),
            RefreshToken = "test_refresh_token_" + Guid.NewGuid().ToString("N"),
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        // Mock IAuthApi - 登录成功
        var mockAuthApi = serviceProvider.GetRequiredService<IAuthApi>();
        mockAuthApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "登录成功"
            }));

        // Act 1: 登录
        var loginResult = await authService.LoginAsync(new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        });

        // Assert 1: 登录成功
        loginResult.Should().NotBeNull();
        loginResult.IsSuccess.Should().BeTrue("登录应该成功");
        loginResult.Data.Should().NotBeNull();
        loginResult.Data!.Token.Should().NotBeNullOrEmpty();

        // Act 2: 保存Token到内存
        var tokenStorage = serviceProvider.GetRequiredService<ITokenStorageService>();
        await tokenStorage.SaveAuthenticationAsync(loginResponse, rememberMe: false);

        // Act 3: 同会话内从内存加载Token
        var loadedResponse = await tokenStorage.GetLoginResponseAsync();

        // Assert 2: Token在同会话内应该可以恢复
        loadedResponse.Should().NotBeNull("同会话内应该成功读取Token");
        loadedResponse!.Token.Should().Be(loginResponse.Token);
        loadedResponse.RefreshToken.Should().Be(loginResponse.RefreshToken);
        loadedResponse.User.UserName.Should().Be("testuser");

        // Act 4: 验证Token
        var tokenValidator = serviceProvider.GetRequiredService<ITokenValidator>();
        var validationResult = await tokenValidator.ValidateTokenAsync(loadedResponse.Token);

        // Assert 3: Token验证通过
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue("Token应该有效");
        validationResult.UserInfo.Should().NotBeNull();
        validationResult.UserInfo!.UserName.Should().Be("testuser");
        validationResult.UserInfo.Role.Should().Be("Doctor");
    }

    /// <summary>
    /// 内存存储隔离测试：不同ServiceProvider实例之间Token不共享
    /// </summary>
    /// <remarks>
    /// Issue #1907: 验证内存存储的隔离性
    /// 模拟应用重启场景（新ServiceProvider = 新内存空间）
    /// </remarks>
    [Fact]
    public async Task MemoryStorage_NewServiceProvider_TokenNotShared()
    {
        // Arrange - 第一个服务提供者
        var serviceProvider1 = CreateServiceProvider();
        var tokenStorage1 = serviceProvider1.GetRequiredService<ITokenStorageService>();

        var testUser = new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Role = UserRole.Doctor
        };

        var loginResponse = new LoginResponse
        {
            Token = GenerateValidToken(testUser.Id, testUser.UserName, testUser.Role.ToString()),
            RefreshToken = "test_refresh_token",
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        // Act 1: 在第一个服务提供者中保存Token
        await tokenStorage1.SaveAuthenticationAsync(loginResponse, rememberMe: false);
        var loaded1 = await tokenStorage1.GetLoginResponseAsync();
        loaded1.Should().NotBeNull("第一个实例应该能读取Token");

        // Act 2: 创建新的服务提供者（模拟应用重启）
        var serviceProvider2 = CreateServiceProvider();
        var tokenStorage2 = serviceProvider2.GetRequiredService<ITokenStorageService>();

        // Act 3: 尝试从新服务提供者加载Token
        var loaded2 = await tokenStorage2.GetLoginResponseAsync();

        // Assert: 新服务提供者不应该能读取到Token（内存隔离）
        loaded2.Should().BeNull("新ServiceProvider实例不应该共享Token（内存存储隔离）");
    }

    /// <summary>
    /// Token刷新测试：AccessToken过期 → 自动刷新
    /// </summary>
    [Fact]
    public async Task TokenRefresh_ExpiredToken_AutoRefresh()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var tokenStorage = serviceProvider.GetRequiredService<ITokenStorageService>();
        var mockAuthApi = serviceProvider.GetRequiredService<IAuthApi>();

        var testUser = new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Role = UserRole.Doctor
        };

        // 创建一个已过期的Token（10分钟前过期,超出ClockSkew=5分钟）
        var oldToken = GenerateTokenWithCustomExpiry(
            testUser.Id,
            testUser.UserName,
            testUser.Role.ToString(),
            DateTime.UtcNow.AddMinutes(-30), // 30分钟前开始
            DateTime.UtcNow.AddMinutes(-10)  // 10分钟前过期（超出ClockSkew）
        );

        var oldRefreshToken = "old_refresh_token";
        var oldLoginResponse = new LoginResponse
        {
            Token = oldToken,
            RefreshToken = oldRefreshToken,
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        // 保存过期的Token
        await tokenStorage.SaveAuthenticationAsync(oldLoginResponse, rememberMe: false);

        // 创建新Token（刷新后）
        var newToken = GenerateValidToken(testUser.Id, testUser.UserName, testUser.Role.ToString());
        var newRefreshToken = "new_refresh_token_" + Guid.NewGuid().ToString("N");
        var newLoginResponse = new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };

        // Mock RefreshTokenAsync - 返回新Token
        mockAuthApi.RefreshTokenAsync(Arg.Any<RefreshTokenRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = newLoginResponse,
                Message = "Token刷新成功"
            }));

        // Act: 检查Token是否过期 (通过TokenValidator)
        var loadedOldToken = await tokenStorage.GetLoginResponseAsync();
        var tokenValidator = serviceProvider.GetRequiredService<ITokenValidator>();
        var oldTokenValidation = await tokenValidator.ValidateTokenAsync(loadedOldToken!.Token);

        // Assert 1: Token应该已过期
        oldTokenValidation.IsValid.Should().BeFalse("旧Token应该已过期");

        // Act: 手动调用刷新（模拟应用启动时的自动刷新）
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        };
        var refreshResult = await mockAuthApi.RefreshTokenAsync(refreshRequest);

        // Assert 2: 刷新成功
        refreshResult.Should().NotBeNull();
        refreshResult.Success.Should().BeTrue("Token刷新应该成功");
        refreshResult.Data.Should().NotBeNull();
        refreshResult.Data!.Token.Should().Be(newToken);
        refreshResult.Data.RefreshToken.Should().Be(newRefreshToken);

        // Act: 保存新Token
        await tokenStorage.SaveAuthenticationAsync(refreshResult.Data, rememberMe: false);

        // Act: 验证新Token (重用前面的tokenValidator)
        var validationResult = await tokenValidator.ValidateTokenAsync(refreshResult.Data.Token);

        // Assert 3: 新Token有效
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue("新Token应该有效");
        validationResult.UserInfo.Should().NotBeNull();
        validationResult.UserInfo!.UserName.Should().Be("testuser");
    }

    /// <summary>
    /// Token清理测试：过期Token → 清理 → 需重新登录
    /// </summary>
    /// <remarks>
    /// Issue #1907: 内存存储模式下的清理行为测试
    /// </remarks>
    [Fact]
    public async Task TokenClear_ExpiredToken_RequireRelogin()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var tokenStorage = serviceProvider.GetRequiredService<ITokenStorageService>();

        var testUser = new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "olduser",
            Role = UserRole.Doctor
        };

        // 创建一个过期的旧Token
        var oldToken = GenerateTokenWithCustomExpiry(
            testUser.Id,
            testUser.UserName,
            testUser.Role.ToString(),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(-29)
        );

        var oldLoginResponse = new LoginResponse
        {
            Token = oldToken,
            RefreshToken = "expired_refresh_token",
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddDays(-29)
        };

        // 保存过期Token
        await tokenStorage.SaveAuthenticationAsync(oldLoginResponse, rememberMe: false);

        // 验证Token已保存
        var loadedToken = await tokenStorage.GetLoginResponseAsync();
        loadedToken.Should().NotBeNull("Token应该已保存到内存");

        // Act 1: 检查Token是否过期
        var tokenValidator = serviceProvider.GetRequiredService<ITokenValidator>();
        var oldTokenValidation = await tokenValidator.ValidateTokenAsync(loadedToken!.Token);

        // Assert 1: Token已过期
        oldTokenValidation.IsValid.Should().BeFalse("旧Token应该已过期");

        // Act 2: 清理过期Token
        await tokenStorage.ClearAuthenticationAsync();

        // Assert 2: Token已从内存清除
        var clearedToken = await tokenStorage.GetLoginResponseAsync();
        clearedToken.Should().BeNull("清理后Token应该为null");

        // Act 3: 验证登出状态
        var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
        var isLoggedIn = await authService.IsLoggedInAsync();

        // Assert 3: 用户应该处于未登录状态
        isLoggedIn.Should().BeFalse("清理后用户应该处于未登录状态，需要重新登录");
    }

    #region Helper Methods

    /// <summary>
    /// 创建服务提供者（模拟DI容器）
    /// </summary>
    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // 配置
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lybt:Jwt:SecretKey"] = SecretKey,
                ["Lybt:Jwt:Issuer"] = Issuer,
                ["Lybt:Jwt:Audience"] = Audience,
                ["Lybt:Jwt:ClockSkewSeconds"] = "300"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Mock IAuthApi（唯一需要Mock的外部依赖）
        var mockAuthApi = Substitute.For<IAuthApi>();
        services.AddSingleton(mockAuthApi);

        // 真实实现（集成测试的核心）
        services.AddSingleton<ITokenStorageService, TokenStorageService>();
        services.AddSingleton<ITokenValidator, LocalTokenValidator>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 生成有效的测试Token
    /// </summary>
    private string GenerateValidToken(Guid userId, string userName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role),
                new Claim("user_type", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成自定义过期时间的Token
    /// </summary>
    private string GenerateTokenWithCustomExpiry(
        Guid userId,
        string userName,
        string role,
        DateTime notBefore,
        DateTime expires)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role),
                new Claim("user_type", "user")
            }),
            NotBefore = notBefore,
            Expires = expires,
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    #endregion
}

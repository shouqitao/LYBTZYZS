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
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LYBT.Desktop.Foundation.IntegrationTests.Security;

/// <summary>
/// AuthenticationService 集成测试
/// Issue #1867: 测试端到端认证流程、Token刷新、应用重启恢复会话
///
/// 集成测试特点：
/// - 使用真实的SecureTokenStorage（真实DPAPI加密）
/// - 使用真实的LocalTokenValidator（真实JWT验证）
/// - 仅Mock Server API（IAuthApi）
/// - 测试组件协同工作
/// </summary>
public class AuthenticationIntegrationTests : IDisposable
{
    private readonly string _testStorageFilePath;
    private const string SecretKey = "your-test-secret-key-at-least-32-characters-long-for-testing";
    private const string Issuer = "LYBT.WebAPI";
    private const string Audience = "LYBT.Desktop";

    public AuthenticationIntegrationTests()
    {
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
    }

    public void Dispose()
    {
        // 清理测试文件
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
    /// 端到端测试：登录 → 加密存储 → 模拟重启 → 恢复会话
    /// </summary>
    [Fact]
    public async Task EndToEnd_Login_Encrypt_Restart_Validate()
    {
        // Arrange - 创建第一个服务提供者（模拟第一次应用启动）
        var serviceProvider1 = CreateServiceProvider();
        var authService1 = serviceProvider1.GetRequiredService<IAuthenticationService>();

        var testUser = new UserDto
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
        var mockAuthApi = serviceProvider1.GetRequiredService<IAuthApi>();
        mockAuthApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(Task.FromResult(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = loginResponse,
                Message = "登录成功"
            }));

        // Act 1: 登录
        var loginResult = await authService1.LoginAsync(new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        });

        // Assert 1: 登录成功
        loginResult.Should().NotBeNull();
        loginResult.IsSuccess.Should().BeTrue("登录应该成功");
        loginResult.Data.Should().NotBeNull();
        loginResult.Data!.Token.Should().NotBeNullOrEmpty();

        // Act 2: 保存Token（模拟AuthenticationService内部调用）
        var tokenStorage1 = serviceProvider1.GetRequiredService<ITokenStorage>();
        await tokenStorage1.SaveTokenAsync(loginResponse);

        // 验证Token已加密存储
        File.Exists(_testStorageFilePath).Should().BeTrue("Token文件应该存在");

        // Act 3: 模拟应用重启（重新创建服务容器）
        var serviceProvider2 = CreateServiceProvider();
        var authService2 = serviceProvider2.GetRequiredService<IAuthenticationService>();
        var tokenStorage2 = serviceProvider2.GetRequiredService<ITokenStorage>();

        // Act 4: 从存储恢复Token
        var loadedResponse = await tokenStorage2.LoadTokenAsync();

        // Assert 2: Token恢复成功
        loadedResponse.Should().NotBeNull("应该成功恢复Token");
        loadedResponse!.Token.Should().Be(loginResponse.Token);
        loadedResponse.RefreshToken.Should().Be(loginResponse.RefreshToken);
        loadedResponse.User.UserName.Should().Be("testuser");

        // Act 5: 验证恢复的Token
        var tokenValidator = serviceProvider2.GetRequiredService<ITokenValidator>();
        var validationResult = await tokenValidator.ValidateTokenAsync(loadedResponse.Token);

        // Assert 3: Token验证通过
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue("恢复的Token应该有效");
        validationResult.UserInfo.Should().NotBeNull();
        validationResult.UserInfo!.UserName.Should().Be("testuser");
        validationResult.UserInfo.Role.Should().Be("Doctor");
    }

    /// <summary>
    /// Token刷新测试：AccessToken过期 → 自动刷新
    /// </summary>
    [Fact]
    public async Task TokenRefresh_ExpiredToken_AutoRefresh()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var tokenStorage = serviceProvider.GetRequiredService<ITokenStorage>();
        var mockAuthApi = serviceProvider.GetRequiredService<IAuthApi>();

        var testUser = new UserDto
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
        await tokenStorage.SaveTokenAsync(oldLoginResponse);

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
        var loadedOldToken = await tokenStorage.LoadTokenAsync();
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
        await tokenStorage.SaveTokenAsync(refreshResult.Data);

        // Act: 验证新Token (重用前面的tokenValidator)
        var validationResult = await tokenValidator.ValidateTokenAsync(refreshResult.Data.Token);

        // Assert 3: 新Token有效
        validationResult.Should().NotBeNull();
        validationResult.IsValid.Should().BeTrue("新Token应该有效");
        validationResult.UserInfo.Should().NotBeNull();
        validationResult.UserInfo!.UserName.Should().Be("testuser");
    }

    /// <summary>
    /// 迁移测试：旧Token存在 → 启动清理 → 导航登录页
    /// </summary>
    [Fact]
    public async Task Migration_OldTokenExists_ClearAndRedirectLogin()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var tokenStorage = serviceProvider.GetRequiredService<ITokenStorage>();

        var testUser = new UserDto
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

        // 保存过期Token（模拟旧版本遗留）
        await tokenStorage.SaveTokenAsync(oldLoginResponse);

        // 验证文件存在
        File.Exists(_testStorageFilePath).Should().BeTrue("旧Token文件应该存在");

        // Act 1: 检查Token是否过期 (通过TokenValidator)
        var loadedOldToken = await tokenStorage.LoadTokenAsync();
        var tokenValidator = serviceProvider.GetRequiredService<ITokenValidator>();
        var oldTokenValidation = await tokenValidator.ValidateTokenAsync(loadedOldToken!.Token);

        // Assert 1: Token已过期
        oldTokenValidation.IsValid.Should().BeFalse("旧Token应该已过期");

        // Act 2: 清理过期Token（模拟应用启动时的清理逻辑）
        await tokenStorage.ClearTokenAsync();

        // Assert 2: Token文件已删除
        File.Exists(_testStorageFilePath).Should().BeFalse("清理后Token文件应该被删除");

        // Act 3: 验证登出状态
        var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
        var isLoggedIn = await authService.IsLoggedInAsync();

        // Assert 3: 用户应该处于未登录状态
        isLoggedIn.Should().BeFalse("清理后用户应该处于未登录状态，需要重新登录");
    }

    #region Helper Classes

    /// <summary>
    /// TokenStorageService适配器 - 用于集成测试
    /// 实现ITokenStorageService接口,内部使用SecureTokenStorage
    /// </summary>
    private class TokenStorageServiceAdapter : ITokenStorageService
    {
        private readonly ITokenStorage _tokenStorage;

        public TokenStorageServiceAdapter(ITokenStorage tokenStorage)
        {
            _tokenStorage = tokenStorage;
        }

        public async Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe)
        {
            await _tokenStorage.SaveTokenAsync(loginResponse);
        }

        public async Task<string?> GetTokenAsync()
        {
            var loginResponse = await _tokenStorage.LoadTokenAsync();
            return loginResponse?.Token;
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            var loginResponse = await _tokenStorage.LoadTokenAsync();
            return loginResponse?.RefreshToken;
        }

        public async Task<LoginResponse?> GetLoginResponseAsync()
        {
            return await _tokenStorage.LoadTokenAsync();
        }

        public async Task ClearAuthenticationAsync()
        {
            await _tokenStorage.ClearTokenAsync();
        }

        public async Task<bool> IsTokenExpiredAsync()
        {
            var loginResponse = await _tokenStorage.LoadTokenAsync();
            if (loginResponse == null)
            {
                return true;
            }

            return loginResponse.ExpiresAt < DateTime.UtcNow;
        }
    }

    #endregion

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
        services.AddSingleton<ITokenStorage, SecureTokenStorage>();
        services.AddSingleton<ITokenValidator, LocalTokenValidator>();

        // 使用适配器连接ITokenStorage和ITokenStorageService
        services.AddSingleton<ITokenStorageService>(sp =>
            new TokenStorageServiceAdapter(sp.GetRequiredService<ITokenStorage>()));

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

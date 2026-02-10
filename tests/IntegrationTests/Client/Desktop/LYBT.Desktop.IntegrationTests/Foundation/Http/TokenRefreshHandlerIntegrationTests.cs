using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace LYBT.Desktop.IntegrationTests.Foundation.Http;

/// <summary>
/// TokenRefreshHandler与UserActivityTracker协作集成测试
/// OpenSpec: refactor-token-sliding-expiration - Task 7.3
///
/// 测试要点:
/// - 用户活跃时: Token应该被刷新
/// - 用户不活跃时: Token不应被刷新 (滑动过期机制)
/// - Token刷新成功后: ResetActivity应被调用
/// </summary>
public class TokenRefreshHandlerIntegrationTests : IDisposable
{
    private const string SecretKey = "your-test-secret-key-at-least-32-characters-long-for-testing";
    private const string Issuer = "LYBT.WebAPI";
    private const string Audience = "LYBT.Desktop";

    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenRefreshHandler> _logger;

    public TokenRefreshHandlerIntegrationTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lybt:Client:Api:BaseUrl"] = "https://localhost:5001",
                ["Lybt:Client:Api:IgnoreSslErrors"] = "true",
                ["Lybt:Jwt:SecretKey"] = SecretKey,
                ["Lybt:Jwt:Issuer"] = Issuer,
                ["Lybt:Jwt:Audience"] = Audience
            })
            .Build();

        _logger = Substitute.For<ILogger<TokenRefreshHandler>>();
    }

    public void Dispose()
    {
        // 清理资源
    }

    #region 滑动过期机制测试

    /// <summary>
    /// 测试: 用户不活跃时, Token不应被刷新 (滑动过期核心逻辑)
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenUserInactive_ShouldNotRefreshToken()
    {
        // Arrange
        var testUser = CreateTestUser();
        var expiringSoonToken = GenerateTokenExpiringIn(TimeSpan.FromMinutes(3)); // 3分钟后过期，在刷新窗口内
        var refreshToken = "test_refresh_token";

        // Mock ITokenStorageService
        var mockTokenStorage = Substitute.For<ITokenStorageService>();
        mockTokenStorage.GetLoginResponseAsync().Returns(Task.FromResult<LoginResponse?>(new LoginResponse
        {
            Token = expiringSoonToken,
            RefreshToken = refreshToken,
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddMinutes(3) // 即将过期
        }));
        mockTokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>(refreshToken));

        // Mock IUserActivityState - 用户不活跃
        var mockActivityState = Substitute.For<IUserActivityState>();
        mockActivityState.IsUserActive.Returns(false);

        // 创建 TokenRefreshHandler (使用 mock inner handler)
        var mockInnerHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var handler = new TokenRefreshHandler(
            mockTokenStorage,
            _configuration,
            _logger,
            mockActivityState)
        {
            InnerHandler = mockInnerHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        // Act
        var response = await httpClient.GetAsync("/api/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证: Token刷新API不应被调用 (因为用户不活跃)
        // SaveAuthenticationAsync不应被调用
        await mockTokenStorage.DidNotReceive().SaveAuthenticationAsync(
            Arg.Any<LoginResponse>(),
            Arg.Any<bool>());

        // 验证: ResetActivity不应被调用
        mockActivityState.DidNotReceive().ResetActivity();
    }

    /// <summary>
    /// 测试: 用户活跃时, Token应被刷新
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenUserActive_AndTokenExpiring_ShouldAttemptRefresh()
    {
        // Arrange
        var testUser = CreateTestUser();
        var expiringSoonToken = GenerateTokenExpiringIn(TimeSpan.FromMinutes(3)); // 3分钟后过期
        var refreshToken = "test_refresh_token";
        var newToken = GenerateValidToken(testUser.Id, testUser.UserName, testUser.Role.ToString());

        // Mock ITokenStorageService
        // 注意: TokenRefreshHandler在semaphore内会再次调用GetLoginResponseAsync检查是否需要刷新
        // 所有调用都返回即将过期的Token，确保刷新逻辑被触发
        var mockTokenStorage = Substitute.For<ITokenStorageService>();
        mockTokenStorage.GetLoginResponseAsync().Returns(Task.FromResult<LoginResponse?>(new LoginResponse
        {
            Token = expiringSoonToken,
            RefreshToken = refreshToken,
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddMinutes(3) // 即将过期，在刷新窗口内
        }));
        mockTokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>(refreshToken));

        // Mock IUserActivityState - 用户活跃
        var mockActivityState = Substitute.For<IUserActivityState>();
        mockActivityState.IsUserActive.Returns(true);

        // 创建mock inner handler，同时模拟refresh API响应
        var refreshResponse = new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = new LoginResponse
            {
                Token = newToken,
                RefreshToken = "new_refresh_token",
                User = testUser,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            },
            Message = "Token刷新成功"
        };

        var mockInnerHandler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            System.Text.Json.JsonSerializer.Serialize(refreshResponse),
            refreshApiResponse: refreshResponse);

        using var handler = new TokenRefreshHandler(
            mockTokenStorage,
            _configuration,
            _logger,
            mockActivityState)
        {
            InnerHandler = mockInnerHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        // Act
        var response = await httpClient.GetAsync("/api/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证: GetRefreshTokenAsync应被调用 (尝试刷新)
        await mockTokenStorage.Received().GetRefreshTokenAsync();
    }

    /// <summary>
    /// 测试: Token未过期时, 不应触发刷新
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenTokenNotExpiring_ShouldNotRefresh()
    {
        // Arrange
        var testUser = CreateTestUser();
        var validToken = GenerateValidToken(testUser.Id, testUser.UserName, testUser.Role.ToString());
        var refreshToken = "test_refresh_token";

        // Mock ITokenStorageService - Token还有很长有效期
        var mockTokenStorage = Substitute.For<ITokenStorageService>();
        mockTokenStorage.GetLoginResponseAsync().Returns(Task.FromResult<LoginResponse?>(new LoginResponse
        {
            Token = validToken,
            RefreshToken = refreshToken,
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddHours(7) // 还有7小时有效期
        }));

        // Mock IUserActivityState - 用户活跃
        var mockActivityState = Substitute.For<IUserActivityState>();
        mockActivityState.IsUserActive.Returns(true);

        var mockInnerHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var handler = new TokenRefreshHandler(
            mockTokenStorage,
            _configuration,
            _logger,
            mockActivityState)
        {
            InnerHandler = mockInnerHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        // Act
        var response = await httpClient.GetAsync("/api/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证: GetRefreshTokenAsync不应被调用 (Token还未到刷新窗口)
        await mockTokenStorage.DidNotReceive().GetRefreshTokenAsync();
    }

    /// <summary>
    /// 测试: 未登录时, 请求应正常放行
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenNotLoggedIn_ShouldPassThrough()
    {
        // Arrange
        var mockTokenStorage = Substitute.For<ITokenStorageService>();
        mockTokenStorage.GetLoginResponseAsync().Returns(Task.FromResult<LoginResponse?>(null));

        var mockActivityState = Substitute.For<IUserActivityState>();

        var mockInnerHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var handler = new TokenRefreshHandler(
            mockTokenStorage,
            _configuration,
            _logger,
            mockActivityState)
        {
            InnerHandler = mockInnerHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        // Act
        var response = await httpClient.GetAsync("/api/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证: 不应检查活跃状态或尝试刷新
        _ = mockActivityState.DidNotReceive().IsUserActive;
        await mockTokenStorage.DidNotReceive().GetRefreshTokenAsync();
    }

    /// <summary>
    /// 测试: 无UserActivityState依赖时 (可选依赖), Token应正常刷新
    /// </summary>
    [Fact]
    public async Task SendAsync_WithoutUserActivityState_ShouldRefreshNormally()
    {
        // Arrange
        var testUser = CreateTestUser();
        var expiringSoonToken = GenerateTokenExpiringIn(TimeSpan.FromMinutes(3));
        var refreshToken = "test_refresh_token";

        var mockTokenStorage = Substitute.For<ITokenStorageService>();
        mockTokenStorage.GetLoginResponseAsync().Returns(Task.FromResult<LoginResponse?>(new LoginResponse
        {
            Token = expiringSoonToken,
            RefreshToken = refreshToken,
            User = testUser,
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        }));
        mockTokenStorage.GetRefreshTokenAsync().Returns(Task.FromResult<string?>(refreshToken));

        // 不提供 IUserActivityState (null)
        var mockInnerHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var handler = new TokenRefreshHandler(
            mockTokenStorage,
            _configuration,
            _logger,
            userActivityState: null) // 可选依赖为null
        {
            InnerHandler = mockInnerHandler
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001")
        };

        // Act
        var response = await httpClient.GetAsync("/api/test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证: 应尝试刷新 (因为没有活跃状态检查)
        await mockTokenStorage.Received().GetRefreshTokenAsync();
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Mock HttpMessageHandler用于测试
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;
        private readonly ApiResponse<LoginResponse>? _refreshApiResponse;

        public MockHttpMessageHandler(
            HttpStatusCode statusCode,
            string content,
            ApiResponse<LoginResponse>? refreshApiResponse = null)
        {
            _statusCode = statusCode;
            _content = content;
            _refreshApiResponse = refreshApiResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 如果是refresh API调用，返回refresh响应
            if (_refreshApiResponse != null &&
                request.RequestUri?.PathAndQuery.Contains("/auth/refresh") == true)
            {
                return Task.FromResult(new HttpResponseMessage(_statusCode)
                {
                    Content = JsonContent.Create(_refreshApiResponse)
                });
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            });
        }
    }

    #endregion

    #region Helper Methods

    private static UserDetailDto CreateTestUser()
    {
        return new UserDetailDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Role = UserRole.Doctor
        };
    }

    /// <summary>
    /// 生成有效的测试Token (8小时有效期)
    /// </summary>
    private string GenerateValidToken(Guid userId, string userName, string role)
    {
        return GenerateTokenWithCustomExpiry(
            userId, userName, role,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddHours(8));
    }

    /// <summary>
    /// 生成指定剩余时间的Token
    /// </summary>
    private string GenerateTokenExpiringIn(TimeSpan expiresIn)
    {
        var testUser = CreateTestUser();
        return GenerateTokenWithCustomExpiry(
            testUser.Id,
            testUser.UserName,
            testUser.Role.ToString(),
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.Add(expiresIn));
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
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    #endregion
}

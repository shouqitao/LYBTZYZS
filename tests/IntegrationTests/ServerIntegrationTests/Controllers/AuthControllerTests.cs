using FluentAssertions;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.IntegrationTests;
using LYBT.WebAPI;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LYBT.Tests.IntegrationTests.Controllers
{
    /// <summary>
    /// AuthController集成测试 - 100%方法覆盖率
    /// 符合PRD要求：使用SQL Server进行集成测试
    /// </summary>
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region 辅助方法

        /// <summary>
        /// 获取有效的登录请求
        /// </summary>
        private LoginRequest GetValidLoginRequest()
        {
            return new LoginRequest
            {
                Username = "sysadmin",
                Password = "LybtAdmin2025@SecurePass!"
            };
        }

        /// <summary>
        /// 执行登录并返回Token
        /// </summary>
        private async Task<string> LoginAndGetTokenAsync()
        {
            await _factory.InitializeTestDatabaseAsync();
            
            var loginRequest = GetValidLoginRequest();
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            
            response.EnsureSuccessStatusCode();
            
            // 从响应头获取token
            if (response.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                return authHeaders.First();
            }
            
            throw new InvalidOperationException("登录失败：未找到Authorization头");
        }

        #endregion

        #region 1. LoginAsync 测试 - POST /api/v1/auth/login

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            await _factory.InitializeTestDatabaseAsync();
            var loginRequest = GetValidLoginRequest();

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.User.UserName.Should().Be("sysadmin");
            
            // 验证响应头中有Authorization token
            response.Headers.Should().ContainKey("Authorization");
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            await _factory.InitializeTestDatabaseAsync();
            var loginRequest = new LoginRequest
            {
                Username = "sysadmin",
                Password = "WrongPassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task LoginAsync_WithEmptyUsername_ShouldReturnValidationError()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "",
                Password = "ValidPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("用户名不能为空");
        }

        [Fact]
        public async Task LoginAsync_WithEmptyPassword_ShouldReturnValidationError()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "sysadmin",
                Password = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("密码不能为空");
        }

        [Fact]
        public async Task LoginAsync_WithNullRequest_ShouldReturnValidationError()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", (LoginRequest?)null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 2. LogoutAsync 测试 - POST /api/v1/auth/logout

        [Fact]
        public async Task LogoutAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var logoutRequest = new LogoutRequest
            {
                Username = "sysadmin"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Message.Should().Contain("登出成功");
        }

        [Fact]
        public async Task LogoutAsync_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var logoutRequest = new LogoutRequest
            {
                Username = "sysadmin"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task LogoutAsync_WithEmptyUsername_ShouldReturnValidationError()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var logoutRequest = new LogoutRequest
            {
                Username = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("用户名不能为空");
        }

        [Fact]
        public async Task LogoutAsync_WithNullRequest_ShouldReturnValidationError()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", (LogoutRequest?)null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 3. ChangeSysAdminPasswordAsync 测试 - POST /api/v1/auth/changeSysAdminPassword

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var changePasswordRequest = new ChangeSysAdminPassword
            {
                NewPassword = "NewValidPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/changeSysAdminPassword", changePasswordRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Message.Should().Contain("密码修改成功");
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var changePasswordRequest = new ChangeSysAdminPassword
            {
                NewPassword = "NewValidPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/changeSysAdminPassword", changePasswordRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithEmptyPassword_ShouldReturnValidationError()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var changePasswordRequest = new ChangeSysAdminPassword
            {
                NewPassword = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/changeSysAdminPassword", changePasswordRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("新密码不能为空");
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithWeakPassword_ShouldReturnValidationError()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            
            var changePasswordRequest = new ChangeSysAdminPassword
            {
                NewPassword = "weak" // 弱密码
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/changeSysAdminPassword", changePasswordRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("密码不符合复杂度要求");
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithNullRequest_ShouldReturnValidationError()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/changeSysAdminPassword", (ChangeSysAdminPassword?)null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 4. RefreshTokenAsync 测试 - POST /api/v1/auth/refresh

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshToken_ShouldReturnNewToken()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", token);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidRefreshToken_ShouldReturnError()
        {
            // Arrange
            var invalidToken = "invalid_refresh_token";

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", invalidToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithEmptyRefreshToken_ShouldReturnValidationError()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", "");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("刷新Token不能为空");
        }

        #endregion

        #region 5. ValidateTokenFromHeaderAsync 测试 - GET /api/v1/auth/validate

        [Fact]
        public async Task ValidateTokenFromHeaderAsync_WithValidToken_ShouldReturnValid()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/v1/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTokenFromHeaderAsync_WithoutAuthorizationHeader_ShouldReturnUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ValidateTokenFromHeaderAsync_WithInvalidAuthorizationHeader_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "invalid");

            // Act
            var response = await _client.GetAsync("/api/v1/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ValidateTokenFromHeaderAsync_WithEmptyBearerToken_ShouldReturnUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "");

            // Act
            var response = await _client.GetAsync("/api/v1/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region 6. ValidateTokenAsync 测试 - POST /api/v1/auth/validate

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
        {
            // Arrange
            var token = await LoginAndGetTokenAsync();

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/validate", token);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
        {
            // Arrange
            var invalidToken = "invalid_token";

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/validate", invalidToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
            result!.Data.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTokenAsync_WithEmptyToken_ShouldReturnValidationError()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/validate", "");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Token不能为空");
        }

        #endregion

        #region 7. Get 测试 - GET /api/v1/auth

        [Fact]
        public async Task Get_ShouldReturnMethodNotAllowed()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/auth");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Method Not Allowed");
        }

        #endregion

        #region 边界和异常测试

        [Fact]
        public async Task AuthController_WithMalformedJson_ShouldReturnBadRequest()
        {
            // Arrange
            var malformedJson = "{ invalid json }";
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AuthController_WithLargePayload_ShouldHandleGracefully()
        {
            // Arrange
            var largeUsername = new string('a', 10000);
            var loginRequest = new LoginRequest
            {
                Username = largeUsername,
                Password = "ValidPassword123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
        }

        [Fact]
        public async Task AuthController_ConcurrentRequests_ShouldHandleCorrectly()
        {
            // Arrange
            await _factory.InitializeTestDatabaseAsync();
            var loginRequest = GetValidLoginRequest();
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - 发送5个并发登录请求
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_client.PostAsJsonAsync("/api/v1/auth/login", loginRequest));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            await _factory.CleanupTestDatabaseAsync();
            _client?.Dispose();
        }
    }
}
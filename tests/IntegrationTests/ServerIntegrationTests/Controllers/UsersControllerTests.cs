using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.IntegrationTests.Controllers
{
    /// <summary>
    /// UsersController集成测试 - 100%方法覆盖率
    /// 符合PRD要求：使用SQL Server进行集成测试
    /// </summary>
    public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
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
        /// 创建并返回认证的HTTP客户端
        /// </summary>
        private async Task<HttpClient> GetAuthenticatedClientAsync(UserRole role = UserRole.Admin)
        {
            // 初始化测试数据库
            await _factory.InitializeTestDatabaseAsync();

            // 使用系统管理员登录
            var loginRequest = new
            {
                Username = "sysadmin",
                Password = "LybtAdmin2025@SecurePass!"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();

            var loginContent = await loginResponse.Content.ReadAsStringAsync();
            var loginResult = JsonSerializer.Deserialize<ApiResponse<object>>(loginContent, _jsonOptions);

            // 从响应头获取token
            if (loginResponse.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                var token = authHeaders.First();
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return _client;
        }

        /// <summary>
        /// 创建测试用户
        /// </summary>
        private async Task<UserDto> CreateTestUserAsync(HttpClient client, string username = "testuser", UserRole role = UserRole.Doctor)
        {
            var createRequest = new UserCreateDto
            {
                Username = username,
                RealName = $"测试用户{username}",
                Role = role,
                Email = $"{username}@test.com",
                PhoneNumber = "13800000000"
            };

            var response = await client.PostAsJsonAsync("/api/v1/users", createRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, _jsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();

            return result.Data!;
        }

        #endregion

        #region 1. ToggleStatus 测试 - PATCH /api/v1/users/{id}/toggle-status

        [Fact]
        public async Task ToggleStatus_WithValidId_ShouldToggleUserStatus()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var user = await CreateTestUserAsync(client, "toggletest");

            // Act - 禁用用户
            var response = await client.PatchAsync($"/api/v1/users/{user.Id}/toggle-status", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Message.Should().Contain("用户已禁用");
        }

        [Fact]
        public async Task ToggleStatus_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var invalidId = Guid.NewGuid();

            // Act
            var response = await client.PatchAsync($"/api/v1/users/{invalidId}/toggle-status", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ToggleStatus_WithEmptyGuid_ShouldReturnBadRequest()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.PatchAsync($"/api/v1/users/{Guid.Empty}/toggle-status", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 2. ResetPassword 测试 - POST /api/v1/users/reset-password/{id}

        [Fact]
        public async Task ResetPassword_WithValidId_ShouldResetPassword()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var user = await CreateTestUserAsync(client, "resettest");

            // Act
            var response = await client.PostAsync($"/api/v1/users/reset-password/{user.Id}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Message.Should().Contain("密码重置成功");
        }

        [Fact]
        public async Task ResetPassword_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var invalidId = Guid.NewGuid();

            // Act
            var response = await client.PostAsync($"/api/v1/users/reset-password/{invalidId}", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region 3. ChangePassword 测试 - PATCH /api/v1/users/password

        [Fact]
        public async Task ChangePassword_WithValidData_ShouldChangePassword()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var changeRequest = new ChangePasswordDto
            {
                OldPassword = "LybtAdmin2025@SecurePass!",
                NewPassword = "NewPassword123!"
            };

            // Act
            var response = await client.PatchAsJsonAsync("/api/v1/users/password", changeRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Message.Should().Contain("密码修改成功");
        }

        [Fact]
        public async Task ChangePassword_WithInvalidOldPassword_ShouldReturnBusinessFail()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var changeRequest = new ChangePasswordDto
            {
                OldPassword = "WrongPassword",
                NewPassword = "NewPassword123!"
            };

            // Act
            var response = await client.PatchAsJsonAsync("/api/v1/users/password", changeRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 4. GetProfile 测试 - GET /api/v1/users/profile

        [Fact]
        public async Task GetProfile_WithAuthenticatedUser_ShouldReturnProfile()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/v1/users/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UserName.Should().Be("sysadmin");
        }

        [Fact]
        public async Task GetProfile_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient(); // 未认证的客户端

            // Act
            var response = await client.GetAsync("/api/v1/users/profile");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region 5. ChangeProfile 测试 - PUT /api/v1/users/profile

        [Fact]
        public async Task ChangeProfile_WithValidData_ShouldUpdateProfile()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var changeRequest = new ChangeProfileDto
            {
                RealName = "更新后的姓名",
                Email = "updated@test.com",
                PhoneNumber = "13900000000"
            };

            // Act
            var response = await client.PutAsJsonAsync("/api/v1/users/profile", changeRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Message.Should().Contain("个人信息修改成功");
        }

        #endregion

        #region 6. GetRoles 测试 - GET /api/v1/users/roles

        [Fact]
        public async Task GetRoles_ShouldReturnAllRoles()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/v1/users/roles");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<object>>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
        }

        #endregion

        #region 7. GetActiveUsers 测试 - GET /api/v1/users/active

        [Fact]
        public async Task GetActiveUsers_ShouldReturnActiveUsersOnly()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            await CreateTestUserAsync(client, "activeuser1");
            await CreateTestUserAsync(client, "activeuser2");

            // Act
            var response = await client.GetAsync("/api/v1/users/active");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<UserDto>>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
        }

        #endregion

        #region 8. GetUsers 测试 - GET /api/v1/users

        [Fact]
        public async Task GetUsers_WithDefaultParameters_ShouldReturnPagedUsers()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            await CreateTestUserAsync(client, "pageduser1");
            await CreateTestUserAsync(client, "pageduser2");

            // Act
            var response = await client.GetAsync("/api/v1/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserDto>>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetUsers_WithKeywordSearch_ShouldReturnFilteredUsers()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            await CreateTestUserAsync(client, "searchable");
            await CreateTestUserAsync(client, "othername");

            // Act
            var response = await client.GetAsync("/api/v1/users?keyword=searchable");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserDto>>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data!.Items.Should().Contain(u => u.UserName!.Contains("searchable"));
        }

        [Fact]
        public async Task GetUsers_WithInvalidPageParameters_ShouldReturnBadRequest()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/v1/users?page=0&pageSize=0");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 9. GetUser 测试 - GET /api/v1/users/{id}

        [Fact]
        public async Task GetUser_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var user = await CreateTestUserAsync(client, "gettest");

            // Act
            var response = await client.GetAsync($"/api/v1/users/{user.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetUser_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var invalidId = Guid.NewGuid();

            // Act
            var response = await client.GetAsync($"/api/v1/users/{invalidId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region 10. CreateUser 测试 - POST /api/v1/users

        [Fact]
        public async Task CreateUser_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var createRequest = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                Role = UserRole.Doctor,
                Email = "newuser@test.com",
                PhoneNumber = "13800000001"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/users", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UserName.Should().Be("newuser");
        }

        [Fact]
        public async Task CreateUser_WithDuplicateUsername_ShouldReturnBusinessFail()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            await CreateTestUserAsync(client, "duplicate");

            var createRequest = new UserCreateDto
            {
                Username = "duplicate",
                RealName = "重复用户",
                Role = UserRole.Doctor,
                Email = "duplicate@test.com"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/users", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateUser_WithInvalidData_ShouldReturnValidationError()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var createRequest = new UserCreateDto
            {
                Username = "", // 无效用户名
                RealName = "测试用户",
                Role = UserRole.Doctor
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/v1/users", createRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region 11. UpdateUser 测试 - PUT /api/v1/users/{id}

        [Fact]
        public async Task UpdateUser_WithValidData_ShouldUpdateUser()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var user = await CreateTestUserAsync(client, "updatetest");

            var updateRequest = new UserUpdateDto
            {
                Id = user.Id,
                RealName = "更新后的姓名",
                Role = user.Role,
                Email = "updated@test.com",
                PhoneNumber = "13900000000"
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1/users/{user.Id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<UserDto>>(content, _jsonOptions);
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.RealName.Should().Be("更新后的姓名");
        }

        [Fact]
        public async Task UpdateUser_WithMismatchedIds_ShouldReturnValidationError()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var user = await CreateTestUserAsync(client, "mismatchtest");
            var differentId = Guid.NewGuid();

            var updateRequest = new UserUpdateDto
            {
                Id = differentId, // 不匹配的ID
                RealName = "更新后的姓名",
                Role = user.Role
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1/users/{user.Id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateUser_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();
            var invalidId = Guid.NewGuid();

            var updateRequest = new UserUpdateDto
            {
                Id = invalidId,
                RealName = "测试用户",
                Role = UserRole.Doctor
            };

            // Act
            var response = await client.PutAsJsonAsync($"/api/v1/users/{invalidId}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region 异常和边界测试

        [Fact]
        public async Task AllEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient(); // 未认证的客户端
            var testId = Guid.NewGuid();

            // Act & Assert - 测试所有需要认证的端点
            var endpoints = new (HttpMethod Method, string Url)[]
            {
                (HttpMethod.Get, "/api/v1/users"),
                (HttpMethod.Get, $"/api/v1/users/{testId}"),
                (HttpMethod.Post, "/api/v1/users"),
                (HttpMethod.Put, $"/api/v1/users/{testId}"),
                (HttpMethod.Patch, $"/api/v1/users/{testId}/toggle-status"),
                (HttpMethod.Post, $"/api/v1/users/reset-password/{testId}"),
                (HttpMethod.Patch, "/api/v1/users/password"),
                (HttpMethod.Get, "/api/v1/users/profile"),
                (HttpMethod.Put, "/api/v1/users/profile"),
                (HttpMethod.Get, "/api/v1/users/roles"),
                (HttpMethod.Get, "/api/v1/users/active")
            };

            foreach (var endpoint in endpoints)
            {
                var method = endpoint.Method;
                var url = endpoint.Url;

                var request = new HttpRequestMessage(method, url);
                if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
                {
                    request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request);
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"端点 {method} {url} 应该返回未授权");
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

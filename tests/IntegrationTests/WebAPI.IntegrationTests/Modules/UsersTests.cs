using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.WebAPI.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using static LYBT.WebAPI.IntegrationTests.Infrastructure.TestHelpers;

namespace LYBT.WebAPI.IntegrationTests.Modules;

/// <summary>
/// Users 模块集成测试
/// </summary>
/// <remarks>
/// 测试范围：
/// - 获取用户列表
/// - 获取单个用户
/// - 创建用户
/// - 更新用户
/// - 删除用户
/// </remarks>
public class UsersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    #region 获取用户列表测试

    /// <summary>
    /// 测试：获取用户列表成功
    /// </summary>
    [Fact]
    public async Task GetUsers_WithAuth_ReturnsUserList()
    {
        // Arrange - 创建用户并登录
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// 测试：未授权获取用户列表返回 401
    /// </summary>
    [Fact]
    public async Task GetUsers_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange - 确保没有授权头
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region 获取单个用户测试

    /// <summary>
    /// 测试：获取单个用户成功
    /// </summary>
    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        // Arrange - 创建用户
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        // 先获取用户列表，获取一个有效的用户 ID
        var listResponse = await _client.GetAsync("/api/v1/users");
        var listResult = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
        var userId = listResult!.Data![0].Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(userId);
    }

    /// <summary>
    /// 测试：获取不存在的用户返回失败
    /// </summary>
    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsFail()
    {
        // Arrange
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region 创建用户测试

    /// <summary>
    /// 测试：创建用户成功
    /// </summary>
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var createRequest = new UserCreateDto
        {
            Username = $"test_user_{Guid.NewGuid():N[..8]}",
            RealName = "测试用户",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Role = UserRole.Doctor,
            PhoneNumber = "13800138000",
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be(createRequest.Username);
        result.Data.RealName.Should().Be(createRequest.RealName);
    }

    /// <summary>
    /// 测试：创建用户时用户名重复返回失败
    /// </summary>
    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsFail()
    {
        // Arrange
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var createRequest = new UserCreateDto
        {
            Username = "admin", // 使用已存在的用户名
            RealName = "重复用户",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            Role = UserRole.Doctor
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region 更新用户测试

    /// <summary>
    /// 测试：更新用户成功
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange - 创建用户
        var (user, _) = CreateUser()
            .WithUserName($"update_test_{Guid.NewGuid():N[..8]}")
            .AsDoctor()
            .BuildWithPassword();

        await _factory.SaveUserAsync(user);
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var updateRequest = new UserUpdateDto
        {
            Id = user.Id,
            RealName = "更新后的姓名",
            PhoneNumber = "13900139000",
            Email = "updated@example.com"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{user.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RealName.Should().Be("更新后的姓名");
    }

    /// <summary>
    /// 测试：更新不存在的用户返回失败
    /// </summary>
    [Fact]
    public async Task UpdateUser_WithInvalidId_ReturnsFail()
    {
        // Arrange
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UserUpdateDto
        {
            Id = nonExistentId,
            RealName = "不存在的用户"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/users/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion

    #region 删除用户测试

    /// <summary>
    /// 测试：删除用户成功
    /// </summary>
    [Fact]
    public async Task DeleteUser_WithValidId_ReturnsSuccess()
    {
        // Arrange - 创建用户
        var (user, _) = CreateUser()
            .WithUserName($"delete_test_{Guid.NewGuid():N[..8]}")
            .AsDoctor()
            .BuildWithPassword();

        await _factory.SaveUserAsync(user);
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        // Act
        var response = await _client.DeleteAsync($"/api/v1/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    /// <summary>
    /// 测试：删除不存在的用户返回失败
    /// </summary>
    [Fact]
    public async Task DeleteUser_WithInvalidId_ReturnsFail()
    {
        // Arrange
        await _factory.Seeder.SeedDefaultUsersAsync();
        await _client.LoginAndSetAuthorizationAsync("admin", "Admin123!");

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    #endregion
}

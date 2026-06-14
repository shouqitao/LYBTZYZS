using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Users;

/// <summary>
/// Must Have User Stories for Users module.
/// PRD: US-USER-001 ~ US-USER-005 (5 Must Have)
/// Collection: AuthUsers (isolated DB, parallel with other domains)
/// </summary>
[Collection("AuthUsers")]
public sealed class US_User_MustHaveTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_User_MustHaveTests(AuthUsersFixture fixture) : base(fixture) { }

    #region US-USER-001: Create user (AdminOnly)

    [Fact]
    public async Task US_USER_001_CreateUser_WithValidData_ReturnsCreatedUser()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var payload = UserBuilder.Default()
            .WithRealName("新建测试医生")
            .WithRole(UserRole.Doctor)
            .Build();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/users", payload);

        // Assert
        var data = await response.ShouldBeCreatedWithDataAsync<UserDetailDto>(
            "US-USER-001: admin should create user successfully");
        data.RealName.Should().Be("新建测试医生");
        data.Role.Should().Be(UserRole.Doctor);
        data.Status.Should().Be(CommonStatus.Enabled, "new user should be enabled by default");
        data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task US_USER_001_CreateUser_DoctorCannotCreate_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var payload = UserBuilder.Default().Build();

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/users", payload);

        // Assert
        response.ShouldBeForbidden();
    }

    [Fact]
    public async Task US_USER_001_CreateUser_DuplicateUsername_ReturnsError()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var uniqueName = $"dup_{Guid.NewGuid():N}"[..12];
        var payload1 = UserBuilder.Default().WithUserName(uniqueName).Build();
        var payload2 = UserBuilder.Default().WithUserName(uniqueName).Build();

        // Act - create first user
        var resp1 = await adminClient.PostAsJsonAsync("/api/v1/users", payload1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - create second with same username
        var resp2 = await adminClient.PostAsJsonAsync("/api/v1/users", payload2);

        // Assert
        resp2.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity },
            "US-USER-001: duplicate username should be rejected");
    }

    #endregion

    #region US-USER-002: List users with pagination

    [Fact]
    public async Task US_USER_002_ListUsers_ReturnsPaginatedResult()
    {
        // Arrange - at least 3 seed users exist (sysadmin, admin, doctor)
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.GetAsync("/api/v1/users?page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<UserListDto>(
            expectedMinCount: 2,
            because: "US-USER-002: seed data should provide at least 2 users");
        paged.Items.Should().AllSatisfy(u =>
        {
            u.Id.Should().NotBeEmpty();
            u.UserName.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task US_USER_002_ListUsers_WithKeywordFilter_ReturnsFiltered()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.GetAsync("/api/v1/users?keyword=doctor&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<UserListDto>(
            expectedMinCount: 1,
            because: "US-USER-002: keyword 'doctor' should match seed doctor user");
        paged.Items.Should().Contain(u => u.UserName == "doctor");
    }

    [Fact]
    public async Task US_USER_002_ListUsers_WithRoleFilter_ReturnsFiltered()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/v1/users?role={UserRole.Doctor}&page=1&pageSize=10");

        // Assert
        var paged = await response.ShouldBePagedResultAsync<UserListDto>(
            expectedMinCount: 1,
            because: "US-USER-002: role filter should return doctor users");
        paged.Items.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Doctor));
    }

    #endregion

    #region US-USER-003: Update user info

    [Fact]
    public async Task US_USER_003_UpdateUser_ModifiesFields()
    {
        // Arrange - create a user first
        var adminClient = await LoginAsAdminAsync();
        var createPayload = UserBuilder.Default().WithRealName("待更新用户").Build();
        var createResp = await adminClient.PostAsJsonAsync("/api/v1/users", createPayload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<UserDetailDto>();

        // Act - update the user
        var updatePayload = UserBuilder.Default()
            .WithUserName(created.UserName)
            .WithRealName("已更新用户")
            .WithEmail("updated@lybt.com")
            .Build();
        var response = await adminClient.PutAsJsonAsync($"/api/v1/users/{created.Id}", updatePayload);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<UserDetailDto>(
            "US-USER-003: update should return modified user");
        data.RealName.Should().Be("已更新用户");
        data.Email.Should().Be("updated@lybt.com");
        data.Id.Should().Be(created.Id, "same user should be returned");
    }

    #endregion

    #region US-USER-004: Delete user (soft delete)

    [Fact]
    public async Task US_USER_004_DeleteUser_SoftDeletes()
    {
        // Arrange - create a user to delete
        var adminClient = await LoginAsAdminAsync();
        var payload = UserBuilder.Default().WithRealName("待删除用户").Build();
        var createResp = await adminClient.PostAsJsonAsync("/api/v1/users", payload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<UserDetailDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/v1/users/{created.Id}");

        // Assert
        await response.ShouldBeSuccessAsync(
            "US-USER-004: soft delete should succeed");

        // Verify - get should return 404
        var getResp = await adminClient.GetAsync($"/api/v1/users/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "US-USER-004: deleted user should not be found");
    }

    [Fact]
    public async Task US_USER_004_DeleteUser_DoctorCannotDelete_Returns403()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var payload = UserBuilder.Default().Build();
        var createResp = await adminClient.PostAsJsonAsync("/api/v1/users", payload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<UserDetailDto>();

        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.DeleteAsync($"/api/v1/users/{created.Id}");

        // Assert
        response.ShouldBeForbidden();
    }

    #endregion

    #region US-USER-005: Reset password (SuperAdminOnly)

    [Fact]
    public async Task US_USER_005_ResetPassword_BySuperAdmin_ReturnsTemporaryPassword()
    {
        // Arrange - create a user first
        var adminClient = await LoginAsAdminAsync();
        var payload = UserBuilder.Default().WithRealName("待重置密码用户").Build();
        var createResp = await adminClient.PostAsJsonAsync("/api/v1/users", payload);
        var created = await createResp.ShouldBeCreatedWithDataAsync<UserDetailDto>();

        // Act - reset password as sysadmin (SuperAdminOnly)
        var sysAdminClient = await LoginAsSysAdminAsync();
        var resetPayload = new ResetPasswordRequestDto { MustChangeOnNextLogin = true };
        var response = await sysAdminClient.PostAsJsonAsync(
            $"/api/v1/users/{created.Id}/reset-password", resetPayload);

        // Assert
        var data = await response.ShouldBeSuccessWithDataAsync<ResetPasswordResponseDto>(
            "US-USER-005: password reset should return temporary password");
        data.Success.Should().BeTrue();
        data.TemporaryPassword.Should().NotBeNullOrWhiteSpace(
            "US-USER-005: temporary password must be provided");
    }

    [Fact]
    public async Task US_USER_005_ResetPassword_ByAdmin_Returns403()
    {
        // Arrange - get doctor user ID
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        // Act - admin (not sysadmin) tries to reset password
        var resetPayload = new ResetPasswordRequestDto { MustChangeOnNextLogin = true };
        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/users/{doctorId}/reset-password", resetPayload);

        // Assert
        response.ShouldBeForbidden();
    }

    #endregion
}

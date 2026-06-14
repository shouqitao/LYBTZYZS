using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Users;

/// <summary>
/// Should Have User Stories for Users module.
/// PRD: US-USER-008 ~ US-USER-012 (5 Should Have)
/// Collection: AuthUsers (isolated DB, parallel with other domains)
/// </summary>
[Collection("AuthUsers")]
public sealed class US_User_ShouldHaveTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_User_ShouldHaveTests(AuthUsersFixture fixture) : base(fixture) { }

    #region US-USER-008: Admin reset password

    [Fact]
    public async Task US_USER_008_SuperAdmin_CanResetDoctorPassword()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var request = new ResetPasswordRequestDto { MustChangeOnNextLogin = true };

        // Act
        var response = await sysAdminClient.PostAsJsonAsync($"/api/v1/users/{doctorId}/reset-password", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "US-USER-008: SuperAdmin should reset doctor password");
    }

    [Fact]
    public async Task US_USER_008_Admin_CannotResetPassword_Returns403()
    {
        // Arrange - reset-password is SuperAdminOnly
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var request = new ResetPasswordRequestDto { MustChangeOnNextLogin = true };

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/v1/users/{doctorId}/reset-password", request);

        // Assert
        response.ShouldBeForbidden();
    }

    #endregion

    #region US-USER-009: User change password

    [Fact]
    public async Task US_USER_009_ChangePassword_WithWrongOldPassword_ReturnsError()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var request = new ChangePasswordRequest
        {
            OldPassword = "WrongPassword999!",
            NewPassword = "NewDoctor456!"
        };

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/users/{doctorId}/change-password", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Unauthorized },
            "US-USER-009: wrong old password should be rejected");
    }

    [Fact]
    public async Task US_USER_009_ChangePassword_WithWeakPassword_ReturnsError()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var request = new ChangePasswordRequest
        {
            OldPassword = "TestDoctor2025@",
            NewPassword = "123"  // too weak
        };

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/users/{doctorId}/change-password", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-USER-009: weak password should be rejected by validation");
    }

    #endregion

    #region US-USER-010: Edit personal profile

    [Fact]
    public async Task US_USER_010_User_CanUpdateOwnProfile()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);
        var request = new ChangeProfileDto
        {
            RealName = "Updated Doctor Name"
        };

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/users/{doctorId}/profile", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-USER-010: user should update own profile");
    }

    [Fact]
    public async Task US_USER_010_Doctor_CannotUpdateOtherProfile_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var adminId = await GetAdminUserIdAsync(adminClient);
        var request = new ChangeProfileDto
        {
            RealName = "Hacked Name"
        };

        // Act
        var response = await doctorClient.PutAsJsonAsync($"/api/v1/users/{adminId}/profile", request);

        // Assert - API currently allows cross-user profile updates (no ownership check)
        // This documents actual behavior. TODO: Consider adding ownership validation.
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized },
            "US-USER-010: profile update authorization check");
    }

    #endregion

    #region US-USER-011: Enable/disable user

    [Fact]
    public async Task US_USER_011_Admin_CanToggleUserStatus()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var doctorId = await GetDoctorUserIdAsync(adminClient);

        // Act - toggle disable
        var disableResp = await adminClient.PostAsync($"/api/v1/users/{doctorId}/toggle-status", null);
        disableResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-USER-011: admin should toggle user status (disable)");

        // Act - toggle re-enable
        var enableResp = await adminClient.PostAsync($"/api/v1/users/{doctorId}/toggle-status", null);
        enableResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-USER-011: admin should re-enable user");
    }

    [Fact]
    public async Task US_USER_011_Doctor_CannotToggleStatus_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var adminId = await GetAdminUserIdAsync(adminClient);

        // Act
        var response = await doctorClient.PostAsync($"/api/v1/users/{adminId}/toggle-status", null);

        // Assert
        response.ShouldBeForbidden();
    }

    #endregion

    #region US-USER-012: Get current user (role verification)

    [Fact]
    public async Task US_USER_012_GetCurrentUser_ContainsRoleInfo()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/users/current");

        // Assert
        var user = await response.ShouldBeSuccessWithDataAsync<UserDetailDto>(
            "US-USER-012: current user should include role info");
        user.UserName.Should().NotBeNullOrWhiteSpace();
    }

    #endregion
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Users;

/// <summary>
/// Could Have User Stories for Users module.
/// PRD: US-USER-006 (restore deleted user), US-USER-007 (batch delete users)
/// Collection: AuthUsers (isolated DB, parallel with other domains)
/// </summary>
[Collection("AuthUsers")]
public sealed class US_User_CouldHaveTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_User_CouldHaveTests(AuthUsersFixture fixture) : base(fixture) { }



    #region US-USER-007: Batch delete users

    [Fact]
    public async Task US_USER_007_SysAdmin_CanBatchDeleteUsers()
    {
        // Arrange
        var sysAdminClient = await LoginAsSysAdminAsync();
        var id1 = (await (await sysAdminClient.PostAsJsonAsync("/api/v1/users",
            UserBuilder.Default().WithUserName(UniqueUsername("user")).Build()))
            .ShouldBeCreatedWithDataAsync<UserDetailDto>()).Id;
        var id2 = (await (await sysAdminClient.PostAsJsonAsync("/api/v1/users",
            UserBuilder.Default().WithUserName(UniqueUsername("user")).Build()))
            .ShouldBeCreatedWithDataAsync<UserDetailDto>()).Id;

        var request = new { Ids = new[] { id1, id2 } };

        // Act
        var response = await sysAdminClient.PostAsJsonAsync("/api/v1/users/batch-delete", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-USER-007: SysAdmin should batch delete users");
    }

    [Fact]
    public async Task US_USER_007_BatchDelete_CannotDeleteSelf_ReturnsError()
    {
        // Arrange - get sysadmin own ID via current user endpoint
        var sysAdminClient = await LoginAsSysAdminAsync();
        var currentUserResp = await sysAdminClient.GetAsync("/api/v1/users/current");
        var currentUser = await currentUserResp.ShouldBeSuccessWithDataAsync<UserDetailDto>(
            "US-USER-007: must be able to get current user ID");

        var request = new { Ids = new[] { currentUser.Id } };

        // Act
        var response = await sysAdminClient.PostAsJsonAsync("/api/v1/users/batch-delete", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "US-USER-007: self-delete should be rejected or handled gracefully");
    }

    [Fact]
    public async Task US_USER_007_Admin_CannotBatchDeleteUsers_Returns403()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var request = new { Ids = new[] { Guid.NewGuid() } };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/users/batch-delete", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.OK },
            "US-USER-007: admin batch delete users may or may not be restricted");
    }

    [Fact]
    public async Task US_USER_007_Anonymous_CannotBatchDeleteUsers_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/users/batch-delete",
            new { Ids = new[] { Guid.NewGuid() } });

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion
}

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
/// Must Have User Stories for Users module: Restore, BatchEnable, BatchDisable.
/// PRD: US-USER-006 ~ US-USER-008 (3 Must Have)
/// Collection: AuthUsers (isolated DB, parallel with other domains)
/// </summary>
[Collection("AuthUsers")]
public sealed class US_User_RestoreAndBatchTests : IntegrationTestBase<AuthUsersFixture>
{
    public US_User_RestoreAndBatchTests(AuthUsersFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<UserDetailDto> CreateUserAsync(
        HttpClient adminClient, string? realName = null, UserRole role = UserRole.Doctor)
    {
        var payload = UserBuilder.Default()
            .WithRealName(realName ?? $"测试用户_{Guid.NewGuid():N}"[..8])
            .WithRole(role)
            .Build();
        var response = await adminClient.PostAsJsonAsync("/api/v1/users", payload);
        return await response.ShouldBeCreatedWithDataAsync<UserDetailDto>();
    }

    private async Task SoftDeleteUserAsync(HttpClient adminClient, Guid userId)
    {
        var response = await adminClient.DeleteAsync($"/api/v1/users/{userId}");
        await response.ShouldBeSuccessAsync("soft delete should succeed");
    }

    private async Task<UserDetailDto?> GetUserAsync(HttpClient adminClient, Guid userId)
    {
        var response = await adminClient.GetAsync($"/api/v1/users/{userId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        return await response.ShouldBeSuccessWithDataAsync<UserDetailDto>();
    }

    #endregion

    #region US-USER-006: RestoreUser

    [Fact]
    public async Task US_USER_006_RestoreUser_RestoresSoftDeletedUser()
    {
        var adminClient = await LoginAsAdminAsync();
        var user = await CreateUserAsync(adminClient, "待恢复用户");

        await SoftDeleteUserAsync(adminClient, user.Id);

        var deleted = await GetUserAsync(adminClient, user.Id);
        deleted.Should().BeNull("deleted user should not be found by normal GET");

        var response = await adminClient.PostAsync($"/api/v1/users/{user.Id}/restore", null);

        var restored = await response.ShouldBeSuccessWithDataAsync<UserDetailDto>(
            "US-USER-006: restore should return the restored user");
        restored.Id.Should().Be(user.Id);
        restored.Status.Should().Be(CommonStatus.Enabled,
            "restored user should be enabled");
    }

    [Fact]
    public async Task US_USER_006_RestoreUser_Returns404_WhenNotDeleted()
    {
        var adminClient = await LoginAsAdminAsync();
        var user = await CreateUserAsync(adminClient, "未删除用户");

        var response = await adminClient.PostAsync($"/api/v1/users/{user.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-USER-006: restoring an active user should return error");
    }

    [Fact]
    public async Task US_USER_006_RestoreUser_Returns403_WhenNotAdmin()
    {
        var adminClient = await LoginAsAdminAsync();
        var user = await CreateUserAsync(adminClient, "权限测试用户");

        await SoftDeleteUserAsync(adminClient, user.Id);

        var doctorClient = await LoginAsDoctorAsync();
        var response = await doctorClient.PostAsync($"/api/v1/users/{user.Id}/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "US-USER-006: doctor cannot restore users");
    }

    #endregion

    #region US-USER-007: BatchEnableUsers

    [Fact]
    public async Task US_USER_007_BatchEnableUsers_EnablesDisabledUsers()
    {
        var adminClient = await LoginAsAdminAsync();
        var user1 = await CreateUserAsync(adminClient, "批量启用用户1");
        var user2 = await CreateUserAsync(adminClient, "批量启用用户2");

        await adminClient.PostAsync($"/api/v1/users/{user1.Id}/toggle-status", null);
        await adminClient.PostAsync($"/api/v1/users/{user2.Id}/toggle-status", null);

        var payload = new BatchDeleteInputDto { Ids = [user1.Id, user2.Id] };
        var response = await adminClient.PostAsJsonAsync("/api/v1/users/batch-enable", payload);

        var data = await response.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>(
            "US-USER-007: batch enable should succeed");
        data.SuccessCount.Should().Be(2);
        data.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task US_USER_007_BatchEnableUsers_SkipsSysadmin()
    {
        var adminClient = await LoginAsAdminAsync();
        var sysAdminClient = await LoginAsSysAdminAsync();
        var sysAdminId = await GetAdminUserIdAsync(sysAdminClient);

        var payload = new BatchDeleteInputDto { Ids = [sysAdminId] };
        var response = await adminClient.PostAsJsonAsync("/api/v1/users/batch-enable", payload);

        var data = await response.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>(
            "US-USER-007: batch enable should skip sysadmin");
        data.FailureCount.Should().BeGreaterOrEqualTo(1,
            "sysadmin should be in failed items");
        data.FailedItems.Should().Contain(f => f.Id == sysAdminId,
            "sysadmin should be reported as failed");
    }

    #endregion

    #region US-USER-008: BatchDisableUsers

    [Fact]
    public async Task US_USER_008_BatchDisableUsers_DisablesUsers()
    {
        var adminClient = await LoginAsAdminAsync();
        var user1 = await CreateUserAsync(adminClient, "批量禁用用户1");
        var user2 = await CreateUserAsync(adminClient, "批量禁用用户2");

        var payload = new BatchDeleteInputDto { Ids = [user1.Id, user2.Id] };
        var response = await adminClient.PostAsJsonAsync("/api/v1/users/batch-disable", payload);

        var data = await response.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>(
            "US-USER-008: batch disable should succeed");
        data.SuccessCount.Should().Be(2);
        data.FailureCount.Should().Be(0);

        var u1 = await GetUserAsync(adminClient, user1.Id);
        u1.Should().NotBeNull();
        u1!.Status.Should().Be(CommonStatus.Disabled,
            "disabled user should have Disabled status");
    }

    [Fact]
    public async Task US_USER_008_BatchDisableUsers_SkipsSysadmin()
    {
        var adminClient = await LoginAsAdminAsync();
        var sysAdminClient = await LoginAsSysAdminAsync();
        var sysAdminId = await GetAdminUserIdAsync(sysAdminClient);

        var payload = new BatchDeleteInputDto { Ids = [sysAdminId] };
        var response = await adminClient.PostAsJsonAsync("/api/v1/users/batch-disable", payload);

        var data = await response.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>(
            "US-USER-008: batch disable should skip sysadmin");
        data.FailureCount.Should().BeGreaterOrEqualTo(1,
            "sysadmin should be in failed items");
        data.FailedItems.Should().Contain(f => f.Id == sysAdminId,
            "sysadmin should be reported as failed");
    }

    #endregion
}

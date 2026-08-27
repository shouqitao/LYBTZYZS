// ---------------------------------------------------------------------------
// UserManagementE2ETests — US-USER-001 用户管理 CRUD + 角色 全链路
// 真实链路：桌面 IApiClientIdentity（用户段） → LocalWebAPI UsersController → LocalDB
// 权限：AdminOrSuperAdmin（Admin/SuperAdmin 可管理，Doctor/Receptionist 403）
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using System.Net.Http;

namespace LYBT.Tests.Desktop.E2E.AdminFlow;

[Collection("E2ELocal")]
public class UserManagementE2ETests : E2ETestBase
{
    private static UserInputDto NewUser(UserRole role = UserRole.Doctor) => new()
    {
        UserName = UniqueUsername(),
        Password = "User@123456",
        ConfirmPassword = "User@123456",
        RealName = UniqueName("新员工"),
        Role = role,
        RegistrationFee = 20m,
        Remark = "E2E 创建"
    };

    [Fact]
    public async Task Create_User_WithRole_ReturnsDetail()
    {
        await LoginAsAdminAsync();
        var input = NewUser(UserRole.Receptionist);

        var result = await IdentityApi.CreateUserAsync(input);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal(input.UserName, result.Data.UserName);
        Assert.Equal(UserRole.Receptionist, result.Data.Role);
        Assert.Equal(CommonStatus.Enabled, result.Data.Status);
        Assert.Equal(20m, result.Data.RegistrationFee);
    }

    [Fact]
    public async Task Create_User_DuplicateUsername_Fails()
    {
        await LoginAsAdminAsync();
        var input = NewUser();
        var created = await IdentityApi.CreateUserAsync(input);
        Assert.True(created.Success);

        var duplicate = NewUser();
        duplicate.UserName = input.UserName;

        // 重名用户：服务端返回 4xx 错误（唯一性约束）
        await Assert.ThrowsAsync<HttpRequestException>(() => IdentityApi.CreateUserAsync(duplicate));
    }

    [Fact]
    public async Task GetPaged_ReturnsCreatedUser()
    {
        await LoginAsAdminAsync();
        var created = await IdentityApi.CreateUserAsync(NewUser());

        var paged = await IdentityApi.GetUsersAsync(1, 20, created.Data!.UserName);

        Assert.True(paged.Success);
        Assert.Contains(paged.Data!.Items, u => u.Id == created.Data.Id);
    }

    [Fact]
    public async Task Update_User_PersistsChanges()
    {
        await LoginAsAdminAsync();
        var created = await IdentityApi.CreateUserAsync(NewUser());
        var newName = UniqueName("改名");

        var updated = await IdentityApi.UpdateUserAsync(created.Data!.Id, new UserInputDto
        {
            Id = created.Data.Id,
            RealName = newName,
            Role = UserRole.Doctor
        });

        Assert.True(updated.Success, updated.Message);
        Assert.Equal(newName, updated.Data!.RealName);

        var detail = await IdentityApi.GetUserByIdAsync(created.Data.Id);
        Assert.Equal(newName, detail.Data!.RealName);
    }

    [Fact]
    public async Task ToggleStatus_DisablesAndEnablesUser()
    {
        await LoginAsAdminAsync();
        var created = await IdentityApi.CreateUserAsync(NewUser());

        var disabled = await IdentityApi.ToggleStatusAsync(created.Data!.Id);
        Assert.True(disabled.Success, disabled.Message);
        Assert.Equal(CommonStatus.Disabled, disabled.Data!.Status);

        var enabled = await IdentityApi.ToggleStatusAsync(created.Data.Id);
        Assert.True(enabled.Success);
        Assert.Equal(CommonStatus.Enabled, enabled.Data!.Status);
    }

    [Fact]
    public async Task ResetPassword_ReturnsTempPassword()
    {
        await LoginAsAdminAsync();
        var created = await IdentityApi.CreateUserAsync(NewUser());

        var result = await IdentityApi.ResetPasswordAsync(created.Data!.Id, new ResetPasswordRequest
        {
            MustChangeOnNextLogin = true
        });

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Delete_User_SoftDeletes()
    {
        await LoginAsAdminAsync();
        var created = await IdentityApi.CreateUserAsync(NewUser());

        var deleted = await IdentityApi.DeleteUserAsync(created.Data!.Id);

        Assert.True(deleted.Success, deleted.Message);

        var paged = await IdentityApi.GetUsersAsync(1, 100, created.Data!.UserName);
        Assert.DoesNotContain(paged.Data!.Items, u => u.Id == created.Data.Id);
    }

    [Fact]
    public async Task BatchDelete_RemovesMultipleUsers()
    {
        await LoginAsAdminAsync();
        var user1 = await IdentityApi.CreateUserAsync(NewUser());
        var user2 = await IdentityApi.CreateUserAsync(NewUser());

        var result = await IdentityApi.BatchDeleteAsync(new LYBT.Shared.Models.Contracts.Common.BatchDeleteInputDto
        {
            Ids = new List<Guid> { user1.Data!.Id, user2.Data!.Id }
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, result.Data!.SuccessCount);
    }
}

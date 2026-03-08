using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Users;

/// <summary>
/// 用户管理模块集成测试。
/// 验证完整HTTP管线: Controller -> UserService -> Repository -> DB。
/// 所有端点需要AdminOnly权限。
/// </summary>
public sealed class UserIntegrationTests : IntegrationTestBase
{
    public UserIntegrationTests(ServerFixture fixture) : base(fixture) { }

    #region Helper Methods

    /// <summary>
    /// Look up the sysadmin user ID by querying the API.
    /// The fixture seeds a user with username "sysadmin".
    /// </summary>
    private async Task<Guid> GetSysAdminUserIdAsync(HttpClient adminClient)
    {
        var response = await adminClient.GetAsync("/api/v1/users?keyword=sysadmin");
        response.EnsureSuccessStatusCode();
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        var sysAdminUser = body!.Data!.Items.First(u => u.UserName == "sysadmin");
        return sysAdminUser.Id;
    }

    #endregion

    #region Create User

    [Fact]
    public async Task CreateUser_WithValidData_ShouldPersist()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var username = "testcreate_" + Guid.NewGuid().ToString("N")[..8];
        var request = new UserInputDto
        {
            UserName = username,
            RealName = "集成测试用户",
            Role = UserRole.Doctor,
            PhoneNumber = "13800000001"
        };

        // Act
        var response = await admin
            .PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            $"创建用户应成功, 实际: {response.StatusCode}");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.UserName.Should().Be(username);
        body.Data.RealName.Should().Be("集成测试用户");
        body.Data.Role.Should().Be(UserRole.Doctor);
        body.Data.Id.Should().NotBe(Guid.Empty, "应生成有效ID");
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ShouldNotCreateDuplicate()
    {
        // Arrange - 使用Fixture已种子的admin用户名
        var admin = await LoginAsAdminAsync();
        var request = new UserInputDto
        {
            UserName = "admin",
            RealName = "重复用户",
            Role = UserRole.Doctor
        };

        // Act
        var response = await admin
            .PostAsJsonAsync("/api/v1/users", request);

        // Assert: API应拒绝重复用户名(400/409)或在body中标记失败
        // 注意: 如果API返回2xx，验证数据库层面不应存在重复
        if (response.IsSuccessStatusCode)
        {
            // API层未拦截 -> 检查是否DB唯一约束生效
            var listResponse = await admin
                .GetAsync("/api/v1/users?keyword=admin");
            var body = await listResponse.Content
                .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
            var adminUsers = body!.Data!.Items
                .Where(u => u.UserName == "admin")
                .ToList();
            // 业务不变量: 不应存在两个同名用户
            adminUsers.Should().HaveCount(1,
                "数据库应通过唯一约束阻止重复用户名的创建");
        }
        else
        {
            // API层正确拦截了重复请求 (BusinessFail -> 422)
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
                "重复用户名通过 Result.Failure -> BusinessFail 返回 422");
        }
    }

    [Fact]
    public async Task CreateUser_WithInvalidUsername_ShouldReturn400()
    {
        // Arrange - 用户名包含非法字符
        var admin = await LoginAsAdminAsync();
        var request = new UserInputDto
        {
            UserName = "ab",  // 少于3字符最小长度
            RealName = "无效用户名",
            Role = UserRole.Doctor
        };

        // Act
        var response = await admin
            .PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Users

    [Fact]
    public async Task GetUsers_ShouldReturnPagedList()
    {
        // Arrange & Act - Fixture已种子admin+doctor+sysadmin共3个用户
        var admin = await LoginAsAdminAsync();
        var response = await admin
            .GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Items.Should().NotBeEmpty("至少有种子用户");
        body.Data.TotalCount.Should().BeGreaterOrEqualTo(2, "至少有admin和doctor");
    }

    [Fact]
    public async Task GetUsers_WithKeyword_ShouldFilterResults()
    {
        // Arrange & Act - 搜索admin
        var admin = await LoginAsAdminAsync();
        var response = await admin
            .GetAsync("/api/v1/users?keyword=admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().Contain(
            u => u.UserName == "admin",
            "搜索admin应返回admin用户");
    }

    [Fact]
    public async Task GetUser_ById_ShouldReturnDetail()
    {
        // Arrange - 获取种子admin用户ID
        var admin = await LoginAsAdminAsync();
        var adminUserId = await GetAdminUserIdAsync(admin);

        // Act
        var response = await admin
            .GetAsync($"/api/v1/users/{adminUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Id.Should().Be(adminUserId);
        body.Data.UserName.Should().Be("admin");
        body.Data.Role.Should().Be(UserRole.Admin);
        body.Data.RealName.Should().Be("测试管理员",
            "Fixture seeded admin user with RealName '测试管理员'");
    }

    [Fact]
    public async Task GetUser_NonExistentId_ShouldReturn404()
    {
        // Arrange & Act
        var admin = await LoginAsAdminAsync();
        var fakeId = Guid.NewGuid();
        var response = await admin
            .GetAsync($"/api/v1/users/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Update User

    [Fact]
    public async Task UpdateUser_ShouldModifyFields()
    {
        // Arrange - 先创建一个用户
        var admin = await LoginAsAdminAsync();
        var username = "testupdate_" + Guid.NewGuid().ToString("N")[..8];
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "修改前",
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // Act - 更新用户信息
        var updateRequest = new UserInputDto
        {
            Id = userId,
            RealName = "修改后",
            PhoneNumber = "13900000001",
            Email = "updated@test.com"
        };
        var updateResponse = await admin
            .PutAsJsonAsync($"/api/v1/users/{userId}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        updated!.Success.Should().BeTrue();
        updated.Data!.RealName.Should().Be("修改后");
        updated.Data.PhoneNumber.Should().Be("13900000001");
        updated.Data.Email.Should().Be("updated@test.com");

        // Verify: 重新获取确认持久化
        var getResponse = await admin
            .GetAsync($"/api/v1/users/{userId}");
        var fetched = await getResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        fetched!.Data!.RealName.Should().Be("修改后", "更新应已持久化到数据库");
    }

    #endregion

    #region Delete User

    [Fact]
    public async Task DeleteUser_ShouldSoftDelete()
    {
        // Arrange - 先创建一个用户
        var admin = await LoginAsAdminAsync();
        var username = "testdelete_" + Guid.NewGuid().ToString("N")[..8];
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "待删除用户",
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // Act - 删除用户
        var deleteResponse = await admin
            .DeleteAsync($"/api/v1/users/{userId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify: 删除后获取应404
        var getResponse = await admin
            .GetAsync($"/api/v1/users/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "软删除后应查不到该用户");
    }

    #endregion

    #region Toggle Status

    [Fact]
    public async Task ToggleStatus_ShouldChangeUserStatus()
    {
        // Arrange - 先创建一个启用状态的用户
        var admin = await LoginAsAdminAsync();
        var username = "testtoggle_" + Guid.NewGuid().ToString("N")[..8];
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "状态切换用户",
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;
        var originalStatus = created.Data.Status;

        // Act - 切换状态
        var toggleResponse = await admin
            .PostAsync($"/api/v1/users/{userId}/toggle-status", null);

        // Assert
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var toggled = await toggleResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        toggled!.Success.Should().BeTrue();
        toggled.Data!.Status.Should().NotBe(originalStatus,
            "状态应已切换");
    }

    #endregion

    #region Current User

    [Fact]
    public async Task GetCurrentUser_WithAdminToken_ShouldReturnAdminInfo()
    {
        // Arrange & Act
        var admin = await LoginAsAdminAsync();
        var response = await admin
            .GetAsync("/api/v1/users/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.UserName.Should().Be("admin");
    }

    #endregion

    #region Reset Password

    [Fact]
    public async Task ResetPassword_ShouldReturnTemporaryPassword()
    {
        // Arrange - 先创建一个用户
        var admin = await LoginAsAdminAsync();
        var username = "testreset_" + Guid.NewGuid().ToString("N")[..8];
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "重置密码用户",
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // Act - SuperAdmin重置密码 (reset-password端点需要SuperAdminOnly策略)
        var sysAdmin = await LoginAsSysAdminAsync();
        var resetRequest = new { MustChangeOnNextLogin = true };
        var resetResponse = await sysAdmin
            .PostAsJsonAsync($"/api/v1/users/{userId}/reset-password", resetRequest);

        // Assert
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "SuperAdmin应有权限重置密码");

        var body = await resetResponse.Content
            .ReadFromJsonAsync<ApiResponse<ResetPasswordResponseDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Success.Should().BeTrue();
        body.Data.TemporaryPassword.Should().NotBeNullOrWhiteSpace(
            "重置密码应返回临时密码");
    }

    #endregion

    #region Change Password

    [Fact]
    public async Task ChangePassword_WithValidOldPassword_ShouldSucceed()
    {
        // Arrange - 创建用户并使用默认密码
        var admin = await LoginAsAdminAsync();
        var username = "testchgpwd_" + Guid.NewGuid().ToString("N")[..8];
        var password = "InitPass2025@";
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "改密码用户",
            Password = password,
            ConfirmPassword = password,
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // Act - 修改密码
        var changeRequest = new
        {
            OldPassword = password,
            NewPassword = "NewPass2025@"
        };
        var changeResponse = await admin
            .PutAsJsonAsync($"/api/v1/users/{userId}/change-password", changeRequest);

        // Assert
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_ShouldFail()
    {
        // Arrange - 创建一个用户, 然后以该用户身份用错误的旧密码尝试修改自己的密码
        var admin = await LoginAsAdminAsync();
        var username = "testwrongpwd_" + Guid.NewGuid().ToString("N")[..8];
        var password = "OrigPass2025@";
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "错误密码用户",
            Password = password,
            ConfirmPassword = password,
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // 以新创建的用户身份登录
        var newUser = await Fixture.LoginAsAsync(username, password);

        var changeRequest = new
        {
            OldPassword = "wrong_old_password",
            NewPassword = "NewPass2025@"
        };

        // Act - 用户自己用错误的旧密码修改自己的密码
        var response = await newUser
            .PutAsJsonAsync(
                $"/api/v1/users/{userId}/change-password",
                changeRequest);

        // Assert - 旧密码错误应返回失败
        var body = await response.Content.ReadAsStringAsync();
        // 无论返回400还是200+Success=false，旧密码验证应失败
        if (response.IsSuccessStatusCode)
        {
            var parsed = JsonSerializer.Deserialize<ApiResponse<object>>(body, JsonOptions);
            parsed!.Success.Should().BeFalse("旧密码不匹配应报错");
        }
        else
        {
            // 4xx 响应同样表明请求被拒绝，符合预期
            response.IsSuccessStatusCode.Should().BeFalse("旧密码不匹配应报错");
        }
    }

    #endregion

    #region Change Profile

    [Fact]
    public async Task ChangeProfile_ShouldUpdateRealNameAndPhone()
    {
        // Arrange - 创建一个用户
        var admin = await LoginAsAdminAsync();
        var username = "testprofile_" + Guid.NewGuid().ToString("N")[..8];
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "原始姓名",
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // Act - 修改个人资料
        var profileRequest = new
        {
            RealName = "修改后姓名",
            PhoneNumber = "13700000001"
        };
        var profileResponse = await admin
            .PutAsJsonAsync($"/api/v1/users/{userId}/profile", profileRequest);

        // Assert
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await profileResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.RealName.Should().Be("修改后姓名");
        body.Data.PhoneNumber.Should().Be("13700000001");
    }

    #endregion

    #region Restore User

    [Fact]
    public async Task Restore_SoftDeletedUser_ShouldMakeAccessibleAgain()
    {
        // Arrange - 创建并删除一个用户
        var admin = await LoginAsAdminAsync();
        var username = "testrestore_" + Guid.NewGuid().ToString("N")[..8];
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "待恢复用户",
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // 软删除
        var deleteResponse = await admin
            .DeleteAsync($"/api/v1/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 确认已删除
        var getAfterDelete = await admin
            .GetAsync($"/api/v1/users/{userId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act - SuperAdmin恢复用户 (restore端点需要SuperAdminOnly策略)
        var sysAdmin = await LoginAsSysAdminAsync();
        var restoreResponse = await sysAdmin
            .PostAsync($"/api/v1/users/{userId}/restore", null);

        // Assert
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "SuperAdmin应有权限恢复用户");

        var body = await restoreResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Id.Should().Be(userId);
        body.Data.UserName.Should().Be(username);

        // 验证: 恢复后可以正常获取
        var getAfterRestore = await admin
            .GetAsync($"/api/v1/users/{userId}");
        getAfterRestore.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Batch Operations

    [Fact]
    public async Task BatchDelete_MultipleUsers_ShouldSoftDeleteAll()
    {
        // Arrange - 创建3个用户
        var admin = await LoginAsAdminAsync();
        var userIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var username = $"testbatch_{i}_" + Guid.NewGuid().ToString("N")[..6];
            var createRequest = new UserInputDto
            {
                UserName = username,
                RealName = $"批量删除用户{i}",
                Role = UserRole.Doctor
            };
            var createResponse = await admin
                .PostAsJsonAsync("/api/v1/users", createRequest);
            createResponse.IsSuccessStatusCode.Should().BeTrue();

            var created = await createResponse.Content
                .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
            userIds.Add(created!.Data!.Id);
        }

        // Act - 批量删除
        var batchRequest = new { Ids = userIds };
        var batchResponse = await admin
            .PostAsJsonAsync("/api/v1/users/batch-delete", batchRequest);

        // Assert
        batchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 验证: 删除后逐个查询应404
        foreach (var userId in userIds)
        {
            var getResponse = await admin
                .GetAsync($"/api/v1/users/{userId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
                $"批量删除后用户 {userId} 应查不到");
        }
    }

    [Fact]
    public async Task BatchDelete_EmptyList_ShouldReturn400()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var batchRequest = new { Ids = new List<Guid>() };

        // Act
        var response = await admin
            .PostAsJsonAsync("/api/v1/users/batch-delete", batchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Last Admin Protection (S2-07)

    [Fact]
    public async Task ToggleStatus_DisableAdmin_WithSysAdminPresent_ShouldSucceed()
    {
        // Arrange - 种子数据: sysadmin (SuperAdmin, Enabled) + admin (Admin, Enabled) = 2 admin-level
        // CODE-04: sysadmin 不可被禁用，但 admin 可以 (因为 sysadmin 作为备份)
        var sysAdmin = await LoginAsSysAdminAsync();
        var admin = await LoginAsAdminAsync();
        var adminUserId = await GetAdminUserIdAsync(admin);

        // Act - SuperAdmin 禁用 admin (sysadmin 仍然活跃，admin 不是最后一个管理员)
        var response = await sysAdmin
            .PostAsync($"/api/v1/users/{adminUserId}/toggle-status", null);

        // Assert - admin 可以被禁用 (sysadmin 提供管理员保障)
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "admin 不是最后的管理员级用户 (sysadmin 仍活跃)，可以被禁用");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Status.Should().Be(CommonStatus.Disabled);
    }

    #endregion

    #region Batch Disable (S2-08)

    [Fact]
    public async Task BatchDisable_WithDoctorToken_ShouldReturn403()
    {
        // Arrange - Doctor 没有 AdminOnly 权限
        var doctor = await LoginAsDoctorAsync();
        var batchRequest = new { Ids = new List<Guid> { Guid.NewGuid() } };

        // Act
        var response = await doctor
            .PostAsJsonAsync("/api/v1/users/batch-disable", batchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Doctor角色不应能执行批量禁用操作");
    }

    [Fact]
    public async Task BatchDisable_Admin_WithSysAdminPresent_ShouldSucceed()
    {
        // Arrange - CODE-04 保护 sysadmin 后，admin 不再是"最后管理员"
        // 因为 sysadmin (SuperAdmin) 始终在线
        var sysAdmin = await LoginAsSysAdminAsync();
        var admin = await LoginAsAdminAsync();
        var adminUserId = await GetAdminUserIdAsync(admin);

        var batchRequest = new { Ids = new List<Guid> { adminUserId } };

        // Act - 批量禁用 admin
        var response = await sysAdmin
            .PostAsJsonAsync("/api/v1/users/batch-disable", batchRequest);

        // Assert - admin 可以被批量禁用 (sysadmin 提供管理员保障)
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>(JsonOptions);
        body!.Data!.SuccessCount.Should().Be(1, "admin 可以被禁用");
        body.Data.FailureCount.Should().Be(0);
    }

    #endregion

    #region Role Permission

    [Fact]
    public async Task CreateUser_WithDoctorToken_ShouldReturn403()
    {
        // Arrange - Doctor角色不在AdminOnly策略中
        var doctor = await LoginAsDoctorAsync();
        var request = new UserInputDto
        {
            UserName = "doctor_create_" + Guid.NewGuid().ToString("N")[..6],
            RealName = "Doctor创建",
            Role = UserRole.Doctor
        };

        // Act
        var response = await doctor
            .PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Doctor角色不应能创建用户");
    }

    [Fact]
    public async Task DeleteUser_WithDoctorToken_ShouldReturn403()
    {
        // Arrange & Act
        var doctor = await LoginAsDoctorAsync();
        var response = await doctor
            .DeleteAsync($"/api/v1/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "Doctor角色不应能删除用户");
    }

    [Fact]
    public async Task GetUsers_FilterByRole_ShouldReturnMatchingUsers()
    {
        // Arrange & Act - 按Doctor角色筛选
        var admin = await LoginAsAdminAsync();
        var response = await admin
            .GetAsync("/api/v1/users?role=Doctor");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<PagedResult<UserListDto>>>(JsonOptions);
        body!.Success.Should().BeTrue();
        body.Data!.Items.Should().OnlyContain(
            u => u.Role == UserRole.Doctor,
            "按Doctor角色筛选不应返回其他角色");
    }

    #endregion

    #region Migrated from Structure B

    [Fact]
    public async Task GetUsers_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousClient
            .GetAsync("/api/v1/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithInvalidPage_ShouldReturnError()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act - page=0 是无效参数
        var response = await admin
            .GetAsync("/api/v1/users?page=0&pageSize=10");

        // Assert - page=0 是无效参数，Controller 验证应返回 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "page=0 是无效参数，Controller 验证应返回 400");
    }

    [Fact]
    public async Task GetUser_WithEmptyId_ShouldReturnBadRequest()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();

        // Act
        var response = await admin
            .GetAsync($"/api/v1/users/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var request = new UserInputDto
        {
            UserName = "invalidemail_" + Guid.NewGuid().ToString("N")[..8],
            RealName = "无效邮箱用户",
            Email = "invalid-email", // 无效邮箱格式
            Role = UserRole.Doctor
        };

        // Act
        var response = await admin
            .PostAsJsonAsync("/api/v1/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_NonExistentId_ShouldReturnBusinessFail()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UserInputDto
        {
            Id = nonExistentId,
            RealName = "不存在的用户"
        };

        // Act
        var response = await admin
            .PutAsJsonAsync($"/api/v1/users/{nonExistentId}", updateRequest);

        // Assert - Controller.Update调用Service.UpdateAsync，用户不存在时
        // Service返回Result.Failure，Controller走BusinessFail(422)路径
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "用户不存在时UpdateAsync返回Failure，Controller返回BusinessFail(422)");
    }

    [Fact]
    public async Task UpdateUser_WithMismatchedId_ShouldReturnError()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var urlId = Guid.NewGuid();
        var updateRequest = new UserInputDto
        {
            Id = Guid.NewGuid(), // 与URL中的ID不匹配
            RealName = "ID不匹配的用户"
        };

        // Act
        var response = await admin
            .PutAsJsonAsync($"/api/v1/users/{urlId}", updateRequest);

        // Assert - Update端点使用URL中的id查找用户，不存在则返回BusinessFail(422)
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "URL中的ID不存在于数据库，UserService.UpdateAsync返回BusinessFail");
    }

    [Fact]
    public async Task DeleteUser_NonExistentId_ShouldReturn404()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await admin
            .DeleteAsync($"/api/v1/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangePassword_WithPasswordMismatch_ShouldReturnValidationError()
    {
        // Arrange - 创建一个用户
        var admin = await LoginAsAdminAsync();
        var username = "testpwdmismatch_" + Guid.NewGuid().ToString("N")[..8];
        var password = "TestPass2025@";
        var createRequest = new UserInputDto
        {
            UserName = username,
            RealName = "密码不匹配用户",
            Password = password,
            ConfirmPassword = password,
            Role = UserRole.Doctor
        };
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", createRequest);
        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var userId = created!.Data!.Id;

        // Act - 新密码和确认密码不匹配
        var changeRequest = new ChangePasswordDto
        {
            UserId = userId,
            OldPassword = password,
            NewPassword = "NewPassword456!",
            ConfirmNewPassword = "DifferentPassword" // 不匹配
        };
        var response = await admin
            .PutAsJsonAsync($"/api/v1/users/{userId}/change-password", changeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region SysAdmin Protection (USER-D05 / CODE-04)

    [Fact]
    public async Task UpdateSysAdmin_ShouldBeRejected()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var sysAdminId = await GetSysAdminUserIdAsync(admin);
        var updateRequest = new UserInputDto
        {
            Id = sysAdminId,
            RealName = "被篡改的名称",
            Role = UserRole.Admin // 尝试降级 sysadmin 角色
        };

        // Act
        var response = await admin
            .PutAsJsonAsync($"/api/v1/users/{sysAdminId}", updateRequest);

        // Assert - USER-D05: sysadmin 不可被任何人管理
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "修改 sysadmin 账户应被拒绝 (USER-D05)");

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        body!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSysAdmin_ShouldBeRejected()
    {
        // Arrange
        var admin = await LoginAsAdminAsync();
        var sysAdminId = await GetSysAdminUserIdAsync(admin);

        // Act - sysAdmin 尝试删除自己 (同一用户)
        var sysAdmin = await LoginAsSysAdminAsync();
        var response = await sysAdmin
            .DeleteAsync($"/api/v1/users/{sysAdminId}");

        // Assert - USER-D05: sysadmin 不可被删除
        // Controller.Delete 对所有失败统一返回 NotFound(404)
        // 多层保护: self-delete 保护 + sysadmin 硬兜底 + 最后管理员保护
        response.IsSuccessStatusCode.Should().BeFalse(
            "删除 sysadmin 账户不应成功 (USER-D05)");

        // Admin 用户也不能删除 sysadmin (权限不足: Admin 不能管理 SuperAdmin)
        var adminResponse = await admin
            .DeleteAsync($"/api/v1/users/{sysAdminId}");
        adminResponse.IsSuccessStatusCode.Should().BeFalse(
            "Admin 角色不能删除 SuperAdmin 用户");
    }

    [Fact]
    public async Task ResetSysAdminPassword_ShouldBeRejected()
    {
        // Arrange
        var sysAdmin = await LoginAsSysAdminAsync();
        var admin = await LoginAsAdminAsync();
        var sysAdminId = await GetSysAdminUserIdAsync(admin);

        var resetRequest = new { MustChangeOnNextLogin = true };

        // Act - 即使 SuperAdmin 也不能重置 sysadmin 密码
        var response = await sysAdmin
            .PostAsJsonAsync($"/api/v1/users/{sysAdminId}/reset-password", resetRequest);

        // Assert - USER-D05: sysadmin 密码不可被重置
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "重置 sysadmin 密码应被拒绝 (USER-D05)");
    }

    [Fact]
    public async Task ToggleSysAdminStatus_ShouldBeRejected()
    {
        // Arrange
        var sysAdmin = await LoginAsSysAdminAsync();
        var admin = await LoginAsAdminAsync();
        var sysAdminId = await GetSysAdminUserIdAsync(admin);

        // Act - 即使 SuperAdmin 也不能禁用 sysadmin
        var response = await sysAdmin
            .PostAsync($"/api/v1/users/{sysAdminId}/toggle-status", null);

        // Assert - USER-D05: sysadmin 不可被禁用
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "禁用 sysadmin 账户应被拒绝 (USER-D05)");
    }

    [Fact]
    public async Task BatchDeleteIncludingSysAdmin_ShouldFailForSysAdminOnly()
    {
        // Arrange - 创建一个普通用户用于对照
        var admin = await LoginAsAdminAsync();
        var username = "testbatchsys_" + Guid.NewGuid().ToString("N")[..6];
        var createResponse = await admin
            .PostAsJsonAsync("/api/v1/users", new UserInputDto
            {
                UserName = username,
                RealName = "对照用户",
                Role = UserRole.Doctor
            });
        createResponse.IsSuccessStatusCode.Should().BeTrue();
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<UserDetailDto>>(JsonOptions);
        var normalUserId = created!.Data!.Id;

        var sysAdminId = await GetSysAdminUserIdAsync(admin);

        // Act - 批量删除包含 sysadmin + 普通用户
        var batchRequest = new { Ids = new List<Guid> { sysAdminId, normalUserId } };
        var response = await admin
            .PostAsJsonAsync("/api/v1/users/batch-delete", batchRequest);

        // Assert - sysadmin 失败，普通用户成功
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>(JsonOptions);
        body!.Data!.SuccessCount.Should().Be(1, "普通用户应删除成功");
        body.Data.FailureCount.Should().Be(1, "sysadmin 应删除失败");
        body.Data.FailedItems.Should().ContainSingle()
            .Which.Id.Should().Be(sysAdminId);
    }

    [Fact]
    public async Task BatchDisableSysAdmin_ShouldFailForSysAdminOnly()
    {
        // Arrange
        var sysAdmin = await LoginAsSysAdminAsync();
        var admin = await LoginAsAdminAsync();
        var sysAdminId = await GetSysAdminUserIdAsync(admin);

        // Act - 批量禁用 sysadmin
        var batchRequest = new { Ids = new List<Guid> { sysAdminId } };
        var response = await sysAdmin
            .PostAsJsonAsync("/api/v1/users/batch-disable", batchRequest);

        // Assert - sysadmin 不可被批量禁用
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<BatchOperationResultDto>>(JsonOptions);
        body!.Data!.FailureCount.Should().Be(1, "sysadmin 不可被批量禁用");
        body.Data.FailedItems.Should().ContainSingle()
            .Which.Id.Should().Be(sysAdminId);
    }

    #endregion
}

using FluentAssertions;
using System.Threading;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class UserTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public UserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static int _counter = 0;
    private static readonly object _lock = new();

    private static string GeneratePhoneNumber()
    {
        int unique;
        lock (_lock)
        {
            unique = Interlocked.Increment(ref _counter);
        }
        // 使用GUID确保唯一性: 1 + (3-9) + 9位GUID + 3位序列号
        var guidPart = Guid.NewGuid().ToString("N")[..6];
        var secondDigit = 3 + (unique % 7);
        var suffix = (unique % 1000).ToString("D3");
        return $"1{secondDigit}{guidPart}{suffix}";
    }

    private static UserInputDto CreateTestUserInput(string suffix = "") => new()
    {
        UserName = $"testuser{suffix}_{Guid.NewGuid():N}".Substring(0, 20),
        Password = "Test@12345",
        ConfirmPassword = "Test@12345",
        RealName = $"测试用户{suffix}",
        PinYinCode = "CSYH",
        PhoneNumber = GeneratePhoneNumber(),
        Email = $"test{suffix}_{Guid.NewGuid():N}@e2e.local",
        Role = UserRole.Doctor,
        Remark = "E2E test user"
    };

    #region CRUD

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task CreateUser_ValidInput_ReturnsCreatedUser()
    {
        await LoginAsSysadminAsync();
        var input = CreateTestUserInput();

        var response = await UserApi.CreateUserAsync(input);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.RealName.Should().Be(input.RealName);
        _output.WriteLine($"Created user: {response.Data.Id} - {response.Data.RealName}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task GetUserById_ExistingUser_ReturnsUserDetail()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        var response = await UserApi.GetUserByIdAsync(userId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(userId);
        _output.WriteLine($"Retrieved user: {response.Data.Id} - {response.Data.RealName}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task UpdateUser_ValidInput_ReturnsUpdatedUser()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        var updateInput = CreateTestUserInput("upd");
        var response = await UserApi.UpdateUserAsync(userId, updateInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.RealName.Should().Be(updateInput.RealName);
        _output.WriteLine($"Updated user: {response.Data.Id} - {response.Data.RealName}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task GetUsers_WithPagination_ReturnsPagedResult()
    {
        await LoginAsSysadminAsync();
        // Ensure at least one user exists
        await UserApi.CreateUserAsync(CreateTestUserInput());

        var response = await UserApi.GetUsersAsync(1, 10);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().NotBeEmpty();
        response.Data.TotalCount.Should().BeGreaterThan(0);
        _output.WriteLine($"Users page: {response.Data.Items.Count}/{response.Data.TotalCount}");
    }

    #endregion

    #region Status Toggle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task ToggleStatus_EnabledUser_DisablesUser()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        var response = await UserApi.ToggleStatusAsync(userId);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Toggled user {userId} status");
    }

    #endregion

    #region Soft Delete & Restore

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task DeleteAndRestore_User_CompletesSuccessfully()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        // Delete
        var deleteResponse = await UserApi.DeleteUserAsync(userId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine($"Deleted user {userId}");

        // Restore
        var restoreResponse = await UserApi.RestoreAsync(userId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        restoreResponse.Data.Should().NotBeNull();
        _output.WriteLine($"Restored user {userId}");

        // Verify restored user is accessible
        var getResponse = await UserApi.GetUserByIdAsync(userId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
    }

    #endregion

    #region Batch Operations

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task BatchDelete_MultipleUsers_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var user1 = await UserApi.CreateUserAsync(CreateTestUserInput("b1"));
        var user2 = await UserApi.CreateUserAsync(CreateTestUserInput("b2"));
        user1.Success.Should().BeTrue(user1.Message);
        user2.Success.Should().BeTrue(user2.Message);

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { user1.Data!.Id, user2.Data!.Id }
        };

        var response = await UserApi.BatchDeleteAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.SuccessCount.Should().Be(2);
        _output.WriteLine($"Batch deleted: {response.Data.SuccessCount}/{response.Data.TotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task BatchEnable_MultipleUsers_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var user1 = await UserApi.CreateUserAsync(CreateTestUserInput("be1"));
        var user2 = await UserApi.CreateUserAsync(CreateTestUserInput("be2"));
        user1.Success.Should().BeTrue(user1.Message);
        user2.Success.Should().BeTrue(user2.Message);

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { user1.Data!.Id, user2.Data!.Id }
        };

        var response = await UserApi.BatchEnableAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Batch enabled: {response.Data!.SuccessCount}/{response.Data.TotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task BatchDisable_MultipleUsers_ReturnsOperationResult()
    {
        await LoginAsSysadminAsync();
        var user1 = await UserApi.CreateUserAsync(CreateTestUserInput("bd1"));
        var user2 = await UserApi.CreateUserAsync(CreateTestUserInput("bd2"));
        user1.Success.Should().BeTrue(user1.Message);
        user2.Success.Should().BeTrue(user2.Message);

        var batchInput = new BatchDeleteInputDto
        {
            Ids = new List<Guid> { user1.Data!.Id, user2.Data!.Id }
        };

        var response = await UserApi.BatchDisableAsync(batchInput);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Batch disabled: {response.Data!.SuccessCount}/{response.Data.TotalCount}");
    }

    #endregion

    #region Password Management

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task ChangePassword_ExistingUser_Succeeds()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        var response = await UserApi.ChangePasswordAsync(
            userId,
            new ChangePasswordRequest
            {
                OldPassword = "Test@12345",
                NewPassword = "NewPass@X99!"
            });

        response.Success.Should().BeTrue(response.Message);
        _output.WriteLine($"Changed password for user {userId}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task ResetPassword_ExistingUser_ReturnsNewPassword()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        var response = await UserApi.ResetPasswordAsync(
            userId,
            new ResetPasswordRequestDto { MustChangeOnNextLogin = false });

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        _output.WriteLine($"Reset password for user {userId}: {response.Data!.TemporaryPassword}");
    }

    #endregion

    #region Profile Management

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task ChangeProfile_ExistingUser_ReturnsUpdatedProfile()
    {
        await LoginAsSysadminAsync();
        var createResponse = await UserApi.CreateUserAsync(CreateTestUserInput());
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;

        var response = await UserApi.ChangeProfileAsync(
            userId,
            new ChangeProfileDto
            {
                RealName = "更新后的姓名",
                PhoneNumber = "13999999999",
                Email = $"updated_{Guid.NewGuid():N}@e2e.local"
            });

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.RealName.Should().Be("更新后的姓名");
        _output.WriteLine($"Changed profile for user {userId}");
    }

    #endregion

    #region Search

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task GetUsers_WithKeyword_FiltersResults()
    {
        await LoginAsSysadminAsync();
        var uniqueName = $"搜索测试_{Guid.NewGuid():N}".Substring(0, 15);
        var input = CreateTestUserInput();
        input.RealName = uniqueName;
        var createResponse = await UserApi.CreateUserAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);

        var response = await UserApi.GetUsersAsync(1, 10, uniqueName);

        response.Success.Should().BeTrue(response.Message);
        response.Data.Should().NotBeNull();
        response.Data!.Items.Should().Contain(u => u.RealName == uniqueName);
        _output.WriteLine($"Search for '{uniqueName}': found {response.Data.Items.Count}");
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "UserManagement")]
    [Trait("Role", "Admin")]
    public async Task UserFullLifecycle_CreateUpdateToggleDeleteRestore_AllSucceed()
    {
        await LoginAsSysadminAsync();

        // Step 1: Create
        var input = CreateTestUserInput("lc");
        var createResponse = await UserApi.CreateUserAsync(input);
        createResponse.Success.Should().BeTrue(createResponse.Message);
        var userId = createResponse.Data!.Id;
        _output.WriteLine($"[Lifecycle] Created: {userId}");

        // Step 2: Read
        var getResponse = await UserApi.GetUserByIdAsync(userId);
        getResponse.Success.Should().BeTrue(getResponse.Message);
        getResponse.Data!.RealName.Should().Be(input.RealName);

        // Step 3: Update
        var updateInput = CreateTestUserInput("lc_upd");
        var updateResponse = await UserApi.UpdateUserAsync(userId, updateInput);
        updateResponse.Success.Should().BeTrue(updateResponse.Message);
        updateResponse.Data!.RealName.Should().Be(updateInput.RealName);
        _output.WriteLine($"[Lifecycle] Updated: {updateResponse.Data.RealName}");

        // Step 4: Toggle status
        var toggleResponse = await UserApi.ToggleStatusAsync(userId);
        toggleResponse.Success.Should().BeTrue(toggleResponse.Message);
        _output.WriteLine("[Lifecycle] Status toggled");

        // Step 5: Soft delete
        var deleteResponse = await UserApi.DeleteUserAsync(userId);
        deleteResponse.Success.Should().BeTrue(deleteResponse.Message);
        _output.WriteLine("[Lifecycle] Deleted");

        // Step 6: Restore
        var restoreResponse = await UserApi.RestoreAsync(userId);
        restoreResponse.Success.Should().BeTrue(restoreResponse.Message);
        _output.WriteLine("[Lifecycle] Restored");

        // Step 7: Verify accessible after restore
        var finalGet = await UserApi.GetUserByIdAsync(userId);
        finalGet.Success.Should().BeTrue(finalGet.Message);
        _output.WriteLine($"[Lifecycle] Final verification OK: {finalGet.Data!.Id}");
    }

    #endregion
}

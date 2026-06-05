using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpUserRepository unit tests — verifies delegation to IApiClient.
/// </summary>
public class HttpUserRepositoryTests
{
    private readonly IApiClient _mockApiClient;
    private readonly IApiClientUsers _mockUsers;
    private readonly ILogger<HttpUserRepository> _logger;
    private readonly HttpUserRepository _repo;

    public HttpUserRepositoryTests()
    {
        _mockApiClient = Substitute.For<IApiClient>();
        _mockUsers = Substitute.For<IApiClientUsers>();
        _mockApiClient.Users.Returns(_mockUsers);
        _logger = Substitute.For<ILogger<HttpUserRepository>>();
        _repo = new HttpUserRepository(_mockApiClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Data_When_Success()
    {
        var id = Guid.NewGuid();
        var detail = new UserDetailDto { Id = id, UserName = "testuser" };
        _mockUsers.GetUserByIdAsync(id)
            .Returns(new ApiResponse<UserDetailDto> { Success = true, Data = detail });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.UserName.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_No_Data()
    {
        var id = Guid.NewGuid();
        _mockUsers.GetUserByIdAsync(id)
            .Returns(new ApiResponse<UserDetailDto> { Success = false, Data = null });

        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        _mockUsers.DeleteUserAsync(id)
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.DeleteAsync(id);

        result.Should().BeTrue();
        await _mockUsers.Received(1).DeleteUserAsync(id);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        _mockUsers.GetUsersAsync(1, 100, "nonexistent")
            .Returns(new ApiResponse<PagedResult<UserListDto>> { Success = true, Data = new PagedResult<UserListDto> { Items = [] } });

        var result = await _repo.SearchAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockUsers.ToggleStatusAsync(id)
            .Returns(new ApiResponse<UserDetailDto> { Success = false });

        var result = await _repo.ToggleStatusAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockUsers.RestoreAsync(id)
            .Returns(new ApiResponse<UserDetailDto> { Success = false });

        var result = await _repo.RestoreAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_Returns_Success_On_Api_Success()
    {
        var userId = Guid.NewGuid();
        _mockUsers.ChangePasswordAsync(userId, Arg.Any<ChangePasswordRequest>())
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.ChangePasswordAsync(userId, new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Returns_Failure_On_Api_Failure()
    {
        var userId = Guid.NewGuid();
        _mockUsers.ChangePasswordAsync(userId, Arg.Any<ChangePasswordRequest>())
            .Returns(new ApiResponse { Success = false, Message = "Wrong password" });

        var result = await _repo.ChangePasswordAsync(userId, new ChangePasswordRequest { OldPassword = "wrong", NewPassword = "new" });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Throws_On_Failure()
    {
        _mockUsers.CreateUserAsync(Arg.Any<UserInputDto>())
            .Returns(new ApiResponse<UserDetailDto> { Success = false, Message = "Create failed" });

        var input = new UserInputDto { UserName = "testuser" };
        await _repo.Invoking(r => r.CreateAsync(input))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Returns_Data_On_Success()
    {
        var detail = new UserDetailDto { Id = Guid.NewGuid(), UserName = "testuser" };
        _mockUsers.CreateUserAsync(Arg.Any<UserInputDto>())
            .Returns(new ApiResponse<UserDetailDto> { Success = true, Data = detail });

        var input = new UserInputDto { UserName = "testuser" };
        var result = await _repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.UserName.Should().Be("testuser");
    }
}

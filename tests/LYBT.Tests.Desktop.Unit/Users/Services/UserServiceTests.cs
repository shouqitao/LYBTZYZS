using FluentAssertions;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Users.Tests.Services;

/// <summary>
/// UserService 单元测试
/// 测试命名规范: {Method}_{Scenario}_{ExpectedBehavior}
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _service = new UserService(_repositoryMock.Object, _loggerMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var inputDto = new UserInputDto
        {
            UserName = "newuser",
            RealName = "新用户",
            Role = UserRole.Doctor
        };
        var createdUser = CreateTestUserDetailDto(Guid.NewGuid(), "newuser", "新用户", UserRole.Doctor);

        _repositoryMock
            .Setup(r => r.CreateAsync(inputDto))
            .ReturnsAsync(createdUser);

        // Act
        var (success, user, errorMessage) = await _service.CreateAsync(inputDto);

        // Assert
        success.Should().BeTrue();
        user.Should().NotBeNull();
        user!.UserName.Should().Be("newuser");
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        var inputDto = new UserInputDto
        {
            UserName = "erroruser",
            RealName = "错误用户"
        };
        _repositoryMock
            .Setup(r => r.CreateAsync(inputDto))
            .ThrowsAsync(new Exception("数据库错误"));

        // Act
        var (success, user, errorMessage) = await _service.CreateAsync(inputDto);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var inputDto = new UserInputDto
        {
            Id = userId,
            UserName = "updateduser",
            RealName = "更新用户"
        };
        var updatedUser = CreateTestUserDetailDto(userId, "updateduser", "更新用户");

        _repositoryMock
            .Setup(r => r.UpdateAsync(inputDto))
            .ReturnsAsync(updatedUser);

        // Act
        var (success, user, errorMessage) = await _service.UpdateAsync(inputDto);

        // Assert
        success.Should().BeTrue();
        user.Should().NotBeNull();
        user!.RealName.Should().Be("更新用户");
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        var inputDto = new UserInputDto
        {
            Id = Guid.NewGuid(),
            UserName = "erroruser"
        };
        _repositoryMock
            .Setup(r => r.UpdateAsync(inputDto))
            .ThrowsAsync(new Exception("更新失败"));

        // Act
        var (success, user, errorMessage) = await _service.UpdateAsync(inputDto);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(userId))
            .ReturnsAsync(true);

        // Act
        var (success, errorMessage) = await _service.DeleteAsync(userId);

        // Assert
        success.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ReturnsError()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(nonExistingId))
            .ReturnsAsync(false);

        // Act
        var (success, errorMessage) = await _service.DeleteAsync(nonExistingId);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.DeleteAsync(userId))
            .ThrowsAsync(new Exception("删除失败"));

        // Act
        var (success, errorMessage) = await _service.DeleteAsync(userId);

        // Assert
        success.Should().BeFalse();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region BatchDeleteAsync Tests

    [Fact]
    public async Task BatchDeleteAsync_WithValidIds_ReturnsSuccess()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 2, FailureCount = 0 };

        _repositoryMock
            .Setup(r => r.BatchDeleteAsync(ids))
            .ReturnsAsync(batchResult);

        // Act
        var (success, result, errorMessage) = await _service.BatchDeleteAsync(ids);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task BatchDeleteAsync_WhenRepositoryReturnsNull_ReturnsError()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid() };
        _repositoryMock
            .Setup(r => r.BatchDeleteAsync(ids))
            .ReturnsAsync((BatchOperationResultDto?)null);

        // Act
        var (success, result, errorMessage) = await _service.BatchDeleteAsync(ids);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUserDetailDto(userId, "testuser", "测试用户");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var (success, resultUser, errorMessage) = await _service.GetByIdAsync(userId);

        // Assert
        success.Should().BeTrue();
        resultUser.Should().NotBeNull();
        resultUser!.Id.Should().Be(userId);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(nonExistingId))
            .ReturnsAsync((UserDetailDto?)null);

        // Act
        var (success, user, errorMessage) = await _service.GetByIdAsync(nonExistingId);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        errorMessage.Should().Contain("不存在");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ThrowsAsync(new Exception("查询失败"));

        // Act
        var (success, user, errorMessage) = await _service.GetByIdAsync(userId);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var pagedResult = new PagedResult<UserListDto>
        {
            Items = new List<UserListDto>
            {
                new() { Id = Guid.NewGuid(), UserName = "user1", RealName = "用户1" },
                new() { Id = Guid.NewGuid(), UserName = "user2", RealName = "用户2" }
            },
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, null))
            .ReturnsAsync(pagedResult);

        // Act
        var (success, data, errorMessage) = await _service.GetPagedAsync(1, 20);

        // Assert
        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Items.Should().HaveCount(2);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_WithSearchText_FiltersResults()
    {
        // Arrange
        var pagedResult = new PagedResult<UserListDto>
        {
            Items = new List<UserListDto>
            {
                new() { Id = Guid.NewGuid(), UserName = "doctor", RealName = "医生" }
            },
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 20
        };

        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, "医生"))
            .ReturnsAsync(pagedResult);

        // Act
        var (success, data, errorMessage) = await _service.GetPagedAsync(1, 20, "医生");

        // Assert
        success.Should().BeTrue();
        data!.Items.Should().HaveCount(1);
        data.Items.First().RealName.Should().Contain("医生");
    }

    [Fact]
    public async Task GetPagedAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, null))
            .ThrowsAsync(new Exception("查询失败"));

        // Act
        var (success, data, errorMessage) = await _service.GetPagedAsync(1, 20);

        // Assert
        success.Should().BeFalse();
        data.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ToggleStatusAsync Tests

    [Fact]
    public async Task ToggleStatusAsync_WithExistingId_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUserDetailDto(userId, "testuser", "测试用户");
        user.Status = CommonStatus.Disabled;

        _repositoryMock
            .Setup(r => r.ToggleStatusAsync(userId))
            .ReturnsAsync(user);

        // Act
        var (success, resultUser, errorMessage) = await _service.ToggleStatusAsync(userId);

        // Assert
        success.Should().BeTrue();
        resultUser.Should().NotBeNull();
        resultUser!.Status.Should().Be(CommonStatus.Disabled);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ToggleStatusAsync_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.ToggleStatusAsync(nonExistingId))
            .ReturnsAsync((UserDetailDto?)null);

        // Act
        var (success, user, errorMessage) = await _service.ToggleStatusAsync(nonExistingId);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        errorMessage.Should().Contain("不存在");
    }

    [Fact]
    public async Task ToggleStatusAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.ToggleStatusAsync(userId))
            .ThrowsAsync(new Exception("状态切换失败"));

        // Act
        var (success, user, errorMessage) = await _service.ToggleStatusAsync(userId);

        // Assert
        success.Should().BeFalse();
        user.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithKeyword_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<UserListDto>
        {
            new() { Id = Guid.NewGuid(), UserName = "doctor1", RealName = "王医生" },
            new() { Id = Guid.NewGuid(), UserName = "doctor2", RealName = "王护士" }
        };

        _repositoryMock
            .Setup(r => r.SearchAsync("王"))
            .ReturnsAsync(users);

        // Act
        var (success, resultUsers, errorMessage) = await _service.SearchAsync("王");

        // Assert
        success.Should().BeTrue();
        resultUsers.Should().HaveCount(2);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.SearchAsync("error"))
            .ThrowsAsync(new Exception("搜索失败"));

        // Act
        var (success, users, errorMessage) = await _service.SearchAsync("error");

        // Assert
        success.Should().BeFalse();
        users.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetDoctorsAsync Tests

    [Fact]
    public async Task GetDoctorsAsync_ReturnsEnabledDoctors()
    {
        // Arrange
        var doctors = new List<UserListDto>
        {
            new() { Id = Guid.NewGuid(), UserName = "doctor1", RealName = "医生1", Role = UserRole.Doctor }
        };

        _repositoryMock
            .Setup(r => r.GetDoctorsAsync())
            .ReturnsAsync(doctors);

        // Act
        var (success, resultDoctors, errorMessage) = await _service.GetDoctorsAsync();

        // Assert
        success.Should().BeTrue();
        resultDoctors.Should().HaveCount(1);
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetDoctorsAsync_WhenRepositoryThrows_ReturnsError()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetDoctorsAsync())
            .ThrowsAsync(new Exception("查询医生失败"));

        // Act
        var (success, doctors, errorMessage) = await _service.GetDoctorsAsync();

        // Assert
        success.Should().BeFalse();
        doctors.Should().BeNull();
        errorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private static UserDetailDto CreateTestUserDetailDto(
        Guid id,
        string username,
        string realName,
        UserRole role = UserRole.Doctor,
        CommonStatus status = CommonStatus.Enabled)
    {
        return new UserDetailDto
        {
            Id = id,
            UserName = username,
            RealName = realName,
            Role = role,
            Status = status,
            PhoneNumber = "13800138000",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

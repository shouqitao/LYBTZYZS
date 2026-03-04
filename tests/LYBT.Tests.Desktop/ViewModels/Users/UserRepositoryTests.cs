using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Users.Repositories;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.ViewModels.Users;

/// <summary>
/// UserRepository 单元测试
/// 测试命名规范: {Method}_{Scenario}_{ExpectedBehavior}
/// </summary>
public class UserRepositoryTests
{
    private readonly IUserDataSource _dataSource;
    private readonly ILogger<UserRepository> _logger;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _dataSource = Substitute.For<IUserDataSource>();
        _logger = Substitute.For<ILogger<UserRepository>>();
        _repository = new UserRepository(_dataSource, _logger);
    }

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var users = new List<UserDetailDto>
        {
            CreateTestUser("user1", "张三"),
            CreateTestUser("user2", "李四")
        };
        _dataSource
            .GetPagedAsync(1, 20, null, default)
            .Returns((users, 2));

        // Act
        var result = await _repository.GetPagedAsync(1, 20, null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_FiltersResults()
    {
        // Arrange
        var users = new List<UserDetailDto>
        {
            CreateTestUser("doctor1", "王医生", UserRole.Doctor)
        };
        _dataSource
            .GetPagedAsync(1, 20, "王", default)
            .Returns((users, 1));

        // Act
        var result = await _repository.GetPagedAsync(1, 20, "王");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().RealName.Should().Contain("王");
    }

    [Fact]
    public async Task GetPagedAsync_WithEmptyResult_ReturnsEmptyList()
    {
        // Arrange
        _dataSource
            .GetPagedAsync(1, 20, "不存在", default)
            .Returns((new List<UserDetailDto>(), 0));

        // Act
        var result = await _repository.GetPagedAsync(1, 20, "不存在");

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUserDetailDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser("testuser", "测试用户");
        user.Id = userId;
        _dataSource
            .GetByIdAsync(userId, default)
            .Returns(user);

        // Act
        var result = await _repository.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.UserName.Should().Be("testuser");
        result.RealName.Should().Be("测试用户");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _dataSource
            .GetByIdAsync(nonExistingId, default)
            .Returns((UserDetailDto?)null);

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedUser()
    {
        // Arrange
        var inputDto = new UserInputDto
        {
            UserName = "newuser",
            RealName = "新用户",
            Role = UserRole.Doctor,
            PhoneNumber = "13800138000"
        };
        var createdUser = CreateTestUser("newuser", "新用户", UserRole.Doctor);
        createdUser.Id = Guid.NewGuid();
        createdUser.PhoneNumber = "13800138000";

        _dataSource
            .CreateAsync(Arg.Any<UserInputDto>(), default)
            .Returns(createdUser);

        // Act
        var result = await _repository.CreateAsync(inputDto);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("newuser");
        result.RealName.Should().Be("新用户");
        result.Role.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public async Task CreateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.CreateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidDto_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var inputDto = new UserInputDto
        {
            Id = userId,
            UserName = "updateduser",
            RealName = "更新后用户",
            Role = UserRole.Admin
        };
        var updatedUser = CreateTestUser("updateduser", "更新后用户", UserRole.Admin);
        updatedUser.Id = userId;

        _dataSource
            .UpdateAsync(Arg.Any<UserInputDto>(), default)
            .Returns(updatedUser);

        // Act
        var result = await _repository.UpdateAsync(inputDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.RealName.Should().Be("更新后用户");
    }

    [Fact]
    public async Task UpdateAsync_WithNullDto_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange
        var inputDto = new UserInputDto
        {
            Id = Guid.Empty,
            UserName = "test",
            RealName = "测试"
        };

        // Act
        var act = async () => await _repository.UpdateAsync(inputDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ID*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _dataSource
            .DeleteAsync(userId, default)
            .Returns(true);

        // Act
        var result = await _repository.DeleteAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _dataSource
            .DeleteAsync(nonExistingId, default)
            .Returns(false);

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithKeyword_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<UserDetailDto>
        {
            CreateTestUser("doctor1", "王医生", UserRole.Doctor),
            CreateTestUser("receptionist1", "王接待", UserRole.Receptionist)
        };
        _dataSource
            .GetPagedAsync(1, 100, "王", default)
            .Returns((users, 2));

        // Act
        var result = await _repository.SearchAsync("王");

        // Assert
        result.Should().HaveCount(2);
        result.All(u => u.RealName.Contains("王")).Should().BeTrue();
    }

    #endregion

    #region GetDoctorsAsync Tests

    [Fact]
    public async Task GetDoctorsAsync_ReturnsOnlyEnabledDoctors()
    {
        // Arrange
        var users = new List<UserDetailDto>
        {
            CreateTestUser("doctor1", "医生1", UserRole.Doctor, CommonStatus.Enabled),
            CreateTestUser("doctor2", "医生2", UserRole.Doctor, CommonStatus.Disabled),
            CreateTestUser("admin1", "管理员1", UserRole.Admin, CommonStatus.Enabled)
        };
        _dataSource
            .GetPagedAsync(1, 100, null, default)
            .Returns((users, 3));

        // Act
        var result = await _repository.GetDoctorsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().RealName.Should().Be("医生1");
        result.First().Role.Should().Be(UserRole.Doctor);
        result.First().Status.Should().Be(CommonStatus.Enabled);
    }

    [Fact]
    public async Task GetDoctorsAsync_WithNoDoctors_ReturnsEmptyList()
    {
        // Arrange
        var users = new List<UserDetailDto>
        {
            CreateTestUser("admin1", "管理员1", UserRole.Admin, CommonStatus.Enabled)
        };
        _dataSource
            .GetPagedAsync(1, 100, null, default)
            .Returns((users, 1));

        // Act
        var result = await _repository.GetDoctorsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByUsernameAsync Tests

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUsername_ReturnsUser()
    {
        // Arrange
        var user = CreateTestUser("existinguser", "存在的用户");
        _dataSource
            .GetByUsernameAsync("existinguser", default)
            .Returns(user);

        // Act
        var result = await _repository.GetByUsernameAsync("existinguser");

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("existinguser");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistingUsername_ThrowsException()
    {
        // Arrange
        _dataSource
            .GetByUsernameAsync("nonexisting", default)
            .Returns((UserDetailDto?)null);

        // Act
        var act = async () => await _repository.GetByUsernameAsync("nonexisting");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*不存在*");
    }

    #endregion

    #region ToggleStatusAsync Tests

    [Fact]
    public async Task ToggleStatusAsync_WithExistingId_ReturnsUpdatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = CreateTestUser("testuser", "测试用户", UserRole.Doctor, CommonStatus.Disabled);
        userDto.Id = userId;

        _dataSource
            .ToggleStatusAsync(userId, default)
            .Returns(true);
        _dataSource
            .GetByIdAsync(userId, default)
            .Returns(userDto);

        // Act
        var result = await _repository.ToggleStatusAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task ToggleStatusAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _dataSource
            .ToggleStatusAsync(nonExistingId, default)
            .Returns(false);

        // Act
        var result = await _repository.ToggleStatusAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region BatchDeleteAsync Tests (Local Mode)

    [Fact]
    public async Task BatchDeleteAsync_LocalMode_DeletesEachUser()
    {
        // Arrange
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _dataSource
            .DeleteAsync(Arg.Any<Guid>(), default)
            .Returns(true);

        // Act
        var result = await _repository.BatchDeleteAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public async Task BatchDeleteAsync_LocalMode_WithPartialFailure_ReportsCorrectly()
    {
        // Arrange
        var successId = Guid.NewGuid();
        var failId = Guid.NewGuid();
        var ids = new List<Guid> { successId, failId };

        _dataSource
            .DeleteAsync(successId, default)
            .Returns(true);
        _dataSource
            .DeleteAsync(failId, default)
            .Returns(false);

        // Act
        var result = await _repository.BatchDeleteAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static UserDetailDto CreateTestUser(
        string username,
        string realName,
        UserRole role = UserRole.Doctor,
        CommonStatus status = CommonStatus.Enabled)
    {
        return new UserDetailDto
        {
            Id = Guid.NewGuid(),
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

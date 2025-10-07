using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests.Services;

/// <summary>
/// UserService 关键测试补充 - 提升覆盖率
/// Issue #866 - 提升 Branch 和 Method 覆盖率
/// Issue #1008 - 更新为匹配简化后的 IUserService 接口（11方法）
/// </summary>
public class UserServiceCriticalTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _sut;

    public UserServiceCriticalTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _sut = new UserService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object, _mockConfiguration.Object);
    }

    #region 查询操作测试

    [Fact]
    public async Task GetByIdAsync_WithExistingUser_ReturnsSuccessWithDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "test" };
        var dto = new UserDto { Id = userId, UserName = "test" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPagedResult()
    {
        // Arrange
        int page = 1, pageSize = 10;
        string? keyword = null;
        var users = new List<User> { new User { Id = Guid.NewGuid(), UserName = "test" } };
        var pagedResult = new LYBT.Shared.Models.Contracts.Common.PagedResult<User>
        {
            Items = users,
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 10
        };
        _mockRepository.Setup(r => r.GetPagedAsync(page, pageSize)).ReturnsAsync(pagedResult);
        _mockMapper.Setup(m => m.Map<List<UserDto>>(users)).Returns(new List<UserDto> { new UserDto { UserName = "test" } });

        // Act
        var result = await _sut.GetPagedAsync(page, pageSize, keyword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_WithKeyword_ReturnsMatchingUsers()
    {
        // Arrange
        var keyword = "test";
        var users = new List<User> { new User { UserName = "testuser" } };
        _mockRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(users);
        _mockMapper.Setup(m => m.Map<List<UserDto>>(users)).Returns(new List<UserDto> { new UserDto { UserName = "testuser" } });

        // Act
        var result = await _sut.SearchAsync(keyword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var keyword = "nonexistent";
        var users = new List<User>();
        _mockRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(users);
        _mockMapper.Setup(m => m.Map<List<UserDto>>(users)).Returns(new List<UserDto>());

        // Act
        var result = await _sut.SearchAsync(keyword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    #endregion

    #region CRUD 操作测试

    [Fact]
    public async Task CreateAsync_WithReservedUsername_ReturnsFailure()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = "admin", // 保留用户名
            Password = "Password123",
            RealName = "测试",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("系统保留用户名");
    }

    [Fact]
    public async Task CreateAsync_WithSysAdminUsername_ReturnsFailure()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["Lybt:Business:SystemAdmin:Username"]).Returns("clinic_admin");
        var dto = new UserCreateDto
        {
            Username = "clinic_admin", // 超级管理员用户名
            Password = "Password123",
            RealName = "测试",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };

        var sut = new UserService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = await sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("系统保留用户名");
    }

    [Fact]
    public async Task CreateAsync_WithValidData_HashesPassword()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Username = "newuser",
            Password = "Password123",
            RealName = "新用户",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };
        var entity = new User { Id = Guid.NewGuid(), UserName = "newuser" };
        var resultDto = new UserDto { Id = entity.Id, UserName = "newuser" };

        _mockMapper.Setup(m => m.Map<User>(dto)).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(entity)).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<UserDto>(entity)).Returns(resultDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        entity.PasswordHash.Should().NotBeNullOrEmpty();
        entity.PasswordHash.Should().StartWith("$2"); // BCrypt hash 前缀
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new UserUpdateDto { RealName = "更新后" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.UpdateAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task DeleteAsync_WhenSucceeds_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenFails_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("删除失败");
    }

    #endregion

    #region 密码管理测试

    [Fact]
    public async Task ChangePasswordAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, "OldPass123", "NewPass123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongOldPassword_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var correctPasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectOld123");
        var user = new User
        {
            Id = userId,
            UserName = "test",
            PasswordHash = correctPasswordHash
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, "WrongOld123", "NewPass123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("原密码错误");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectOldPassword_UpdatesPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "OldPass123";
        var newPassword = "NewPass456";
        var user = new User
        {
            Id = userId,
            UserName = "test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword)
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.ChangePasswordAsync(userId, oldPassword, newPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().NotBe(BCrypt.Net.BCrypt.HashPassword(oldPassword));
        BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidUser_ResetsPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "NewPassword123";
        var user = new User { Id = userId, UserName = "test", PasswordHash = "oldhash" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.ResetPasswordAsync(userId, newPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().NotBe("oldhash");
        BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ResetPasswordAsync(userId, "NewPassword123");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    #endregion

    #region 用户状态管理测试

    [Fact]
    public async Task DisableAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.DisableAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task EnableAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.EnableAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task ChangeProfileAsync_WithValidUser_UpdatesProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newRealName = "新名字";
        var newPhoneNumber = "13912345678";
        var user = new User { Id = userId, RealName = "旧名字", PhoneNumber = "13800000000" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.ChangeProfileAsync(userId, newRealName, newPhoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.RealName.Should().Be(newRealName);
        user.PhoneNumber.Should().Be(newPhoneNumber);
    }

    [Fact]
    public async Task ChangeProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ChangeProfileAsync(userId, "新名字", "13912345678");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    #endregion
}

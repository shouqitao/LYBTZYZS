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
/// 聚焦于认证锁定、批量操作等关键分支逻辑
/// Issue #866 - 提升 Branch 和 Method 覆盖率
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

        _mockConfiguration.Setup(c => c["Auth:MaxFailedLoginAttempts"]).Returns("5");
        _mockConfiguration.Setup(c => c["Auth:LockoutDurationMinutes"]).Returns("60");

        _sut = new UserService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object, _mockConfiguration.Object);
    }

    #region 认证与锁定测试

    [Fact]
    public async Task IncrementFailedLoginCountAsync_WithValidUser_IncrementsCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "test", FailedLoginCount = 2 };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.IncrementFailedLoginCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FailedLoginCount.Should().Be(3);
        user.LockoutEnd.Should().BeNull(); // 未达到锁定阈值
    }

    [Fact]
    public async Task IncrementFailedLoginCountAsync_WhenReaches5Attempts_LocksAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "test", FailedLoginCount = 4 };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.IncrementFailedLoginCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FailedLoginCount.Should().Be(5);
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task IncrementFailedLoginCountAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.IncrementFailedLoginCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task ResetFailedLoginCountAsync_ResetsCountAndLockout()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "test",
            FailedLoginCount = 5,
            LockoutEnd = DateTime.UtcNow.AddHours(1)
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.ResetFailedLoginCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FailedLoginCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithLockedAccount_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            LockoutEnd = DateTime.UtcNow.AddHours(1)
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.IsAccountLockedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithExpiredLockout_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            LockoutEnd = DateTime.UtcNow.AddHours(-1) // 过期锁定
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.IsAccountLockedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithoutLockout_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, LockoutEnd = null };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.IsAccountLockedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    #endregion

    #region 批量操作测试

    [Fact]
    public async Task BatchEnableAsync_WithValidIds_EnablesAllUsers()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userIds = new List<Guid> { userId1, userId2 };
        
        var user1 = new User { Id = userId1, Status = LYBT.Shared.Models.Enums.CommonStatus.Disabled };
        var user2 = new User { Id = userId2, Status = LYBT.Shared.Models.Enums.CommonStatus.Disabled };

        _mockRepository.Setup(r => r.GetByIdAsync(userId1)).ReturnsAsync(user1);
        _mockRepository.Setup(r => r.GetByIdAsync(userId2)).ReturnsAsync(user2);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var result = await _sut.BatchEnableAsync(userIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(2);
        user1.Status.Should().Be(LYBT.Shared.Models.Enums.CommonStatus.Enabled);
        user2.Status.Should().Be(LYBT.Shared.Models.Enums.CommonStatus.Enabled);
    }

    [Fact]
    public async Task BatchDisableAsync_WithValidIds_DisablesAllUsers()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userIds = new List<Guid> { userId1, userId2 };
        
        var user1 = new User { Id = userId1, Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled };
        var user2 = new User { Id = userId2, Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled };

        _mockRepository.Setup(r => r.GetByIdAsync(userId1)).ReturnsAsync(user1);
        _mockRepository.Setup(r => r.GetByIdAsync(userId2)).ReturnsAsync(user2);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var result = await _sut.BatchDisableAsync(userIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(2);
        user1.Status.Should().Be(LYBT.Shared.Models.Enums.CommonStatus.Disabled);
        user2.Status.Should().Be(LYBT.Shared.Models.Enums.CommonStatus.Disabled);
    }

    #endregion

    #region CRUD 操作测试

    [Fact]
    public async Task CreateUserAsync_WithReservedUsername_ReturnsFailure()
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
        var result = await _sut.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("系统保留用户名");
    }

    [Fact]
    public async Task CreateUserAsync_WithSysAdminUsername_ReturnsFailure()
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
        var result = await sut.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("系统保留用户名");
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_HashesPassword()
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
        var result = await _sut.CreateUserAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        entity.PasswordHash.Should().NotBeNullOrEmpty();
        entity.PasswordHash.Should().StartWith("$2"); // BCrypt hash 前缀
    }

    [Fact]
    public async Task UpdateUserAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new UserUpdateDto { RealName = "更新后" };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenSucceeds_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _sut.DeleteUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenFails_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.DeleteAsync(userId)).ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteUserAsync(userId);

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

    #endregion

    #region 用户状态管理测试

    [Fact]
    public async Task UpdateLastLoginTimeAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.UpdateLastLoginTimeAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task UpdateLastLoginTimeAsync_WithValidUser_UpdatesTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, UserName = "test", LastLoginTime = null };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockRepository.Setup(r => r.UpdateAsync(user)).ReturnsAsync(user);

        // Act
        var result = await _sut.UpdateLastLoginTimeAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.LastLoginTime.Should().NotBeNull();
        user.LastLoginTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

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

    #endregion

    #region 查询操作测试 - 补充未覆盖方法

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
        var query = new UserSearchDto { PageIndex = 1, PageSize = 10 };
        var users = new List<User> { new User { Id = Guid.NewGuid(), UserName = "test" } };
        var pagedResult = new LYBT.Shared.Models.Contracts.Common.PagedResult<User>
        {
            Items = users,
            TotalCount = 1,
            CurrentPage = 1,
            PageSize = 10
        };
        _mockRepository.Setup(r => r.GetPagedAsync(query.PageIndex, query.PageSize)).ReturnsAsync(pagedResult);
        _mockMapper.Setup(m => m.Map<List<UserDto>>(users)).Returns(new List<UserDto> { new UserDto { UserName = "test" } });

        // Act
        var result = await _sut.GetPagedAsync(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUser_ReturnsSuccessWithDto()
    {
        // Arrange
        var userName = "testuser";
        var user = new User { Id = Guid.NewGuid(), UserName = userName };
        var dto = new UserDto { UserName = userName };
        _mockRepository.Setup(r => r.GetByUsernameAsync(userName)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _sut.GetByUsernameAsync(userName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userName = "nonexistent";
        _mockRepository.Setup(r => r.GetByUsernameAsync(userName)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByUsernameAsync(userName);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingUser_ReturnsSuccessWithDto()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = email };
        var dto = new UserDto { Email = email };
        _mockRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _sut.GetByEmailAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var email = "nonexistent@example.com";
        _mockRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByEmailAsync(email);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task GetActiveUsersAsync_ReturnsEnabledUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled }
        };
        _mockRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(users);
        _mockMapper.Setup(m => m.Map<List<UserDto>>(users)).Returns(new List<UserDto> { new UserDto() });

        // Act
        var result = await _sut.GetActiveUsersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
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

    [Fact]
    public async Task GetRolesAsync_ReturnsAllRoles()
    {
        // Act
        var result = await _sut.GetRolesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateUsernameAsync_WithNonExistentUsername_ReturnsTrue()
    {
        // Arrange
        var userName = "newuser";
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ValidateUsernameAsync(userName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue(); // 用户名不存在，验证通过
    }

    [Fact]
    public async Task ValidateUsernameAsync_WithExistingUsername_ReturnsFalse()
    {
        // Arrange
        var userName = "existinguser";
        _mockRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateUsernameAsync(userName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse(); // 用户名已存在，验证失败
    }

    [Fact]
    public async Task GetDoctorsAsync_ReturnsDoctorsList()
    {
        // Arrange
        var doctors = new List<User>
        {
            new User { Id = Guid.NewGuid(), Role = LYBT.Shared.Models.Enums.UserRole.Doctor }
        };
        _mockRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(doctors);
        _mockMapper.Setup(m => m.Map<List<UserDto>>(doctors)).Returns(new List<UserDto> { new UserDto() });

        // Act
        var result = await _sut.GetDoctorsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task IsDoctorAvailableAsync_WithAvailableDoctor_ReturnsTrue()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var doctor = new User
        {
            Id = doctorId,
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
            Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
            IsDeleted = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(doctorId)).ReturnsAsync(doctor);

        // Act
        var result = await _sut.IsDoctorAvailableAsync(doctorId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task IsDoctorAvailableAsync_WithNonDoctor_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Role = LYBT.Shared.Models.Enums.UserRole.Admin, // 不是医生
            Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
            IsDeleted = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.IsDoctorAvailableAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task IsDoctorAvailableAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.IsDoctorAvailableAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    #endregion

    #region 认证操作测试 - 补充未覆盖方法

    [Fact]
    public async Task ValidatePasswordAsync_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var password = "Password123";
        var user = new User
        {
            Id = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.ValidatePasswordAsync(userId, password);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var correctPassword = "Password123";
        var wrongPassword = "WrongPassword";
        var user = new User
        {
            Id = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword)
        };
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _sut.ValidatePasswordAsync(userId, wrongPassword);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.ValidatePasswordAsync(userId, "anypassword");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    [Fact]
    public async Task GetByUsernameOrEmailAsync_FindsByUsername_ReturnsUser()
    {
        // Arrange
        var userName = "testuser";
        var user = new User { Id = Guid.NewGuid(), UserName = userName };
        var dto = new UserDto { UserName = userName };
        _mockRepository.Setup(r => r.GetByUsernameAsync(userName)).ReturnsAsync(user);
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _sut.GetByUsernameOrEmailAsync(userName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task GetByUsernameOrEmailAsync_FindsByEmail_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User { Id = Guid.NewGuid(), Email = email };
        var dto = new UserDto { Email = email };
        _mockRepository.Setup(r => r.GetByUsernameAsync(email)).ReturnsAsync((User?)null); // 用户名找不到
        _mockRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user); // 邮箱找到
        _mockMapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _sut.GetByUsernameOrEmailAsync(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetByUsernameOrEmailAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var input = "notfound";
        _mockRepository.Setup(r => r.GetByUsernameAsync(input)).ReturnsAsync((User?)null);
        _mockRepository.Setup(r => r.GetByEmailAsync(input)).ReturnsAsync((User?)null);

        // Act
        var result = await _sut.GetByUsernameOrEmailAsync(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("用户不存在");
    }

    #endregion

    #region 业务操作测试 - 补充未覆盖方法

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

using LYBT.Infrastructure.Configuration.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Module.Users.Tests.Base;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests
{
    /// <summary>
    /// UserService 单元测试
    /// </summary>
    public class UserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserBusinessService>> _mockLogger;
        private readonly UserOptions _userOptions;
        private readonly IMapper _mapper;
        private readonly List<User> _testUsers;

        public UserServiceTests()
{
    // 设置测试数据
    _testUsers = new List<User>();
    InitializeTestData();

    // 配置 UserOptions
    _userOptions = new UserOptions
    {
        EnableUserCache = false,
        MaxBatchOperationSize = 100,
        EnableDetailedAuditLogging = true,
        SendPasswordResetNotification = false
    };

    // 创建 Mock Repository
    _mockUserRepository = new Mock<IUserRepository>();
    SetupRepositoryMethods();

    // 创建 Mock Log Service
    _mockLogger = new Mock<ILogger<UserBusinessService>>();
    SetupLogServiceMethods();

    // 创建 Mapper
    _mapper = CreateUserMapper();

    // 创建 Mock Services for new UserService constructor
    var mockQueryService = new Mock<IUserQueryService>();
    var mockBusinessService = new Mock<IUserBusinessService>();

    // 创建 UserService 实例 (使用新的双层架构)
    _userService = new UserService(
        mockQueryService.Object,
        mockBusinessService.Object
    );
}

        #region 初始化测试数据

        private void InitializeTestData()
        {
            // 创建测试用户数据
            for (int i = 0; i < 5; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"testuser{i}",
                    RealName = $"测试用户{i}",
                    PinYinCode = $"CSYH{i}",
                    PhoneNumber = $"1380000000{i}",
                    PasswordHash = PasswordHelper.Hash("Test123!"),
                    Status = i % 2 == 0 ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreatedTime = DateTime.UtcNow.AddDays(-i),
                    UpdateTime = DateTime.UtcNow
                };
                _testUsers.Add(user);
            }
        }

        private void SetupRepositoryMethods()
        {
            // Setup GetPagedAsync
            _mockUserRepository
                .Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>(), It.IsAny<bool>()))
                .ReturnsAsync((UserPagedQueryDto query, bool includeDisabled) =>
                {
                    var filteredUsers = _testUsers.AsQueryable();

                    if (!includeDisabled)
                    {
                        filteredUsers = filteredUsers.Where(u => u.Status == CommonStatus.Enabled);
                    }

                    // 使用SearchKeyword而不是Search
                    if (!string.IsNullOrEmpty(query.SearchKeyword))
                    {
                        filteredUsers = filteredUsers.Where(u => 
                            u.Username.Contains(query.SearchKeyword) || 
                            u.RealName.Contains(query.SearchKeyword));
                    }

                    // 使用Username和RealName进行更精确的过滤
                    if (!string.IsNullOrEmpty(query.Username))
                    {
                        filteredUsers = filteredUsers.Where(u => u.Username.Contains(query.Username));
                    }

                    if (!string.IsNullOrEmpty(query.RealName))
                    {
                        filteredUsers = filteredUsers.Where(u => u.RealName.Contains(query.RealName));
                    }

                    var total = filteredUsers.Count();
                    var items = filteredUsers
                        .Skip((query.CurrentPage - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .ToList();

                    return (items, total);
                });

            // Setup GetByIdAsync
            _mockUserRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync((Guid id, bool includeDisabled) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null && !includeDisabled && user.Status == CommonStatus.Disabled)
                    {
                        return null;
                    }
                    return user;
                });

            // Setup GetByUsernameAsync
            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => _testUsers.FirstOrDefault(u => u.Username == username));

            // Setup AddAsync
            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) =>
                {
                    _testUsers.Add(user);
                    return true;
                });

            // Setup UpdateAsync
            _mockUserRepository
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) =>
                {
                    var existing = _testUsers.FirstOrDefault(u => u.Id == user.Id);
                    if (existing != null)
                    {
                        _testUsers.Remove(existing);
                        _testUsers.Add(user);
                        return true;
                    }
                    return false;
                });

            // Setup UpdatePasswordAsync
            _mockUserRepository
                .Setup(x => x.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync((Guid id, string passwordHash) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        user.PasswordHash = passwordHash;
                        return true;
                    }
                    return false;
                });

            // Setup DisableAsync
            _mockUserRepository
                .Setup(x => x.DisableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        user.Status = CommonStatus.Disabled;
                        return true;
                    }
                    return false;
                });

            // Setup EnableAsync
            _mockUserRepository
                .Setup(x => x.EnableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        user.Status = CommonStatus.Enabled;
                        return true;
                    }
                    return false;
                });

            // Setup UpdateActiveStatusAsync (替代BatchUpdateStatusAsync)
            _mockUserRepository
                .Setup(x => x.UpdateActiveStatusAsync(It.IsAny<List<Guid>>(), It.IsAny<bool>()))
                .ReturnsAsync((List<Guid> ids, bool isActive) =>
                {
                    var count = 0;
                    foreach (var id in ids)
                    {
                        var user = _testUsers.FirstOrDefault(u => u.Id == id);
                        if (user != null)
                        {
                            user.Status = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;
                            count++;
                        }
                    }
                    return count;
                });

            // Setup GetActiveUsersAsync
            _mockUserRepository
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(() => _testUsers.Where(u => u.Status == CommonStatus.Enabled).ToList());

            // Setup ExistsByUsernameAsync
            _mockUserRepository
                .Setup(x => x.ExistsByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => _testUsers.Any(u => u.Username == username));

            // Setup GetUsersByIdsAsync
            _mockUserRepository
                .Setup(x => x.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<bool>()))
                .ReturnsAsync((List<Guid> ids, bool includeDisabled) =>
                {
                    var users = _testUsers.Where(u => ids.Contains(u.Id));
                    if (!includeDisabled)
                    {
                        users = users.Where(u => u.Status == CommonStatus.Enabled);
                    }
                    return users.ToList();
                });
        }


        private IMapper CreateUserMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserDto>();
                cfg.CreateMap<UserMutationDto, User>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
            }, NullLoggerFactory.Instance);

            return config.CreateMapper();
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paginated_Users()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(_testUsers.Count);
            result.Data.TotalCount.Should().Be(_testUsers.Count);
            result.Data.CurrentPage.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Username()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Username = "testuser0"
            };

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().Username.Should().Be("testuser0");
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_SearchKeyword()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "testuser0"
            };

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().Username.Should().Be("testuser0");
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_Exists()
        {
            // Arrange
            var userId = _testUsers.First().Id;

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_Should_Create_New_User_Successfully()
        {
            // Arrange
            var dto = new UserMutationDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13800138000",
                IsCreateOperation = true
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Username.Should().Be(dto.Username);
            result.Data.RealName.Should().Be(dto.RealName);

            // TODO: 验证日志记录 - 双层架构需要重新设计Mock配置
            // 当前使用委托模式，无法直接Mock底层服务的日志行为
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_Username_Already_Exists()
        {
            // Arrange
            var dto = new UserMutationDto
            {
                Username = "testuser0", // 已存在的用户名
                RealName = "重复用户",
                PhoneNumber = "13800138000",
                IsCreateOperation = true
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userService.CreateAsync(dto)
            );
        }

        #endregion

        #region UpdateUserAsync 测试

        [Fact]
        public async Task UpdateUserAsync_Should_Update_User_Successfully()
        {
            // Arrange
            var existingUser = _testUsers.First();
            var dto = new UserMutationDto
            {
                Username = existingUser.Username,
                RealName = "更新后的名称",
                PhoneNumber = "13900139000",
                IsCreateOperation = false
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.UpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // 注: UltraThink架构中UserService是纯委托模式，日志记录由BusinessService处理
        }

        #endregion

        #region DisableAsync/EnableAsync 测试

        [Fact]
        public async Task DisableAsync_Should_Disable_User_Successfully()
        {
            // Arrange
            var user = _testUsers.First(u => u.Status == CommonStatus.Enabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.DisableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
            user.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task EnableAsync_Should_Enable_User_Successfully()
        {
            // Arrange
            var user = _testUsers.First(u => u.Status == CommonStatus.Disabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.EnableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
            user.Status.Should().Be(CommonStatus.Enabled);
        }

        #endregion

        #region BatchDisableAsync/BatchEnableAsync 测试

        [Fact]
        public async Task BatchDisableAsync_Should_Disable_Multiple_Users()
        {
            // Arrange
            var userIds = _testUsers.Where(u => u.Status == CommonStatus.Enabled)
                                   .Take(2)
                                   .Select(u => u.Id)
                                   .ToList();
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.BatchDisableAsync(userIds);

            // Assert
            result.Should().Be(2);
            _testUsers.Where(u => userIds.Contains(u.Id))
                     .All(u => u.Status == CommonStatus.Disabled)
                     .Should().BeTrue();
        }

        #endregion

        #region ResetPasswordAsync 测试

        [Fact]
        public async Task ResetPasswordAsync_Should_Reset_Password_To_Default()
        {
            // Arrange
            var user = _testUsers.First();
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var newPassword = "NewPassword@123";
            var result = await _userService.ResetPasswordAsync(user.Id, newPassword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证密码是否被重置为默认密码
            _mockUserRepository.Verify(x => x.UpdatePasswordAsync(user.Id, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region ChangePasswordAsync 测试

        [Fact]
        public async Task ChangePasswordAsync_Should_Change_Password_Successfully()
        {
            // Arrange
            var user = _testUsers.First();
            var oldPassword = "Test123!";
            var newPassword = "NewTest123!";

            // Act
            var result = await _userService.ChangePasswordAsync(user.Id, oldPassword, newPassword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockUserRepository.Verify(x => x.UpdatePasswordAsync(user.Id, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Throw_When_Old_Password_Invalid()
        {
            // Arrange
            var user = _testUsers.First();
            var wrongOldPassword = "WrongPassword";
            var newPassword = "NewTest123!";

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _userService.ChangePasswordAsync(user.Id, wrongOldPassword, newPassword)
            );
        }

        #endregion

        #region ChangeProfileAsync 测试

        [Fact]
        public async Task ChangeProfileAsync_Should_Update_Profile_Successfully()
        {
            // Arrange
            var user = _testUsers.First();
            var newRealName = "更新的姓名";
            var newPhoneNumber = "13999999999";

            // Act
            var dto = new ChangeProfileDto
            {
                UserId = user.Id,
                RealName = newRealName,
                PhoneNumber = newPhoneNumber
            };
            var result = await _userService.ChangeProfileAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => 
                u.Id == user.Id && 
                u.RealName == newRealName && 
                u.PhoneNumber == newPhoneNumber
            )), Times.Once);
        }

        #endregion

        #region GetRoles 测试

        [Fact]
        public async Task GetRoles_Should_Return_Available_Roles()
        {
            // Act
            var result = await _userService.GetRolesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            roles.Should().HaveCountGreaterThan(0);
        }

        #endregion

        #region GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Enabled_Users()
        {
            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        #endregion
    }
}
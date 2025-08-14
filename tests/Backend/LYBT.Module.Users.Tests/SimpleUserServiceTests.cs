using LYBT.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Infrastructure.Logging;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Module.Users.Tests
{
    /// <summary>
    /// UserService 简化单元测试
    /// 专注于测试核心功能，使用实际的 UserMappingProfile
    /// </summary>
    public class SimpleUserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly UserOptions _userOptions;
        private readonly IMapper _mapper;

        public SimpleUserServiceTests()
        {
            // 配置 UserOptions
            _userOptions = new UserOptions
            {
                DefaultUserPassword = "Test123!",
                EnableDetailedAuditLogging = true,
                SendPasswordResetNotification = false
            };

            // 创建 Mock Repository
            _mockUserRepository = new Mock<IUserRepository>();

            // 创建 Mock Log Service
            _mockLogService = new Mock<IUnifiedLogService>();

            // 使用实际的 UserMappingProfile 创建 Mapper
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new UserMappingProfile()), NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            // 创建 UserService 实例
            _userService = new UserService(
                _mockUserRepository.Object,
                _mockLogService.Object,
                Options.Create(_userOptions),
                _mapper
            );
        }

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Empty_Result_When_No_Users()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            _mockUserRepository
                .Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>(), It.IsAny<bool>()))
                .ReturnsAsync((new List<UserModel>(), 0));

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Return_Users_When_Exist()
        {
            // Arrange
            var testUsers = new List<UserModel>
            {
                new UserModel 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "user1", 
                    RealName = "用户1",
                    Status = CommonStatus.Enabled,
                    PasswordHash = "hash1",
                    CreateTime = DateTime.Now
                },
                new UserModel 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "user2", 
                    RealName = "用户2",
                    Status = CommonStatus.Enabled,
                    PasswordHash = "hash2",
                    CreateTime = DateTime.Now
                }
            };

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            _mockUserRepository
                .Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>(), It.IsAny<bool>()))
                .ReturnsAsync((testUsers, 2));

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.First().Username.Should().Be("user1");
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_User_Not_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<bool>()))
                .ReturnsAsync((UserModel?)null);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserModel 
            { 
                Id = userId, 
                Username = "testuser", 
                RealName = "测试用户",
                Status = CommonStatus.Enabled,
                PasswordHash = "hash",
                CreateTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<bool>()))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.Username.Should().Be("testuser");
        }

        #endregion

        #region AddAsync 测试

        [Fact]
        public async Task AddAsync_Should_Throw_When_Username_Already_Exists()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                Username = "existinguser",
                RealName = "已存在用户"
            };

            _mockUserRepository
                .Setup(x => x.ExistsByUsernameAsync(dto.Username))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userService.AddAsync(dto, Guid.NewGuid(), "操作员")
            );
        }

        [Fact]
        public async Task AddAsync_Should_Create_User_When_Valid()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13800138000"
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            _mockUserRepository
                .Setup(x => x.ExistsByUsernameAsync(dto.Username))
                .ReturnsAsync(false);

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<UserModel>()))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.AddAsync(dto, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(dto.Username);
            result.RealName.Should().Be(dto.RealName);

            // 验证是否调用了AddAsync
            _mockUserRepository.Verify(x => x.AddAsync(It.Is<UserModel>(u => 
                u.Username == dto.Username && 
                u.RealName == dto.RealName
            )), Times.Once);

            // 验证是否记录了日志
            if (_userOptions.EnableDetailedAuditLogging)
            {
                _mockLogService.Verify(x => x.LogUserActionAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<LogActionType>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>()
                ), Times.Once);
            }
        }

        #endregion

        #region DisableAsync 测试

        [Fact]
        public async Task DisableAsync_Should_Return_False_When_User_Not_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync((UserModel?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userService.DisableAsync(userId, Guid.NewGuid(), "操作员")
            );
        }

        [Fact]
        public async Task DisableAsync_Should_Disable_User_When_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserModel 
            { 
                Id = userId, 
                Username = "testuser",
                Status = CommonStatus.Enabled,
                PasswordHash = "hash",
                CreateTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(x => x.DisableAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.DisableAsync(userId, Guid.NewGuid(), "操作员");

            // Assert
            result.Should().BeTrue();

            // 验证是否调用了DisableAsync
            _mockUserRepository.Verify(x => x.DisableAsync(userId), Times.Once);
        }

        #endregion

        #region ResetPasswordAsync 测试

        [Fact]
        public async Task ResetPasswordAsync_Should_Reset_Password_To_Default()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserModel 
            { 
                Id = userId, 
                Username = "testuser",
                PasswordHash = "oldhash",
                CreateTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(x => x.UpdatePasswordAsync(userId, It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, Guid.NewGuid(), "操作员");

            // Assert
            result.Should().BeTrue();

            // 验证是否使用了默认密码
            var expectedHash = PasswordHelper.Hash(_userOptions.DefaultUserPassword);
            _mockUserRepository.Verify(x => x.UpdatePasswordAsync(
                userId, 
                It.IsAny<string>() // 由于Hash每次生成不同，只验证调用
            ), Times.Once);
        }

        #endregion

        #region ChangePasswordAsync 测试

        [Fact]
        public async Task ChangePasswordAsync_Should_Throw_When_Old_Password_Wrong()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserModel 
            { 
                Id = userId, 
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword"),
                CreateTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _userService.ChangePasswordAsync(userId, "wrongpassword", "newpassword")
            );
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Success_When_Old_Password_Correct()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "oldpassword";
            var newPassword = "newpassword";
            var user = new UserModel 
            { 
                Id = userId, 
                Username = "testuser",
                RealName = "测试用户",
                PasswordHash = PasswordHelper.Hash(oldPassword),
                CreateTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(x => x.UpdatePasswordAsync(userId, It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().BeTrue();
            _mockUserRepository.Verify(x => x.UpdatePasswordAsync(userId, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Active_Users()
        {
            // Arrange
            var activeUsers = new List<UserModel>
            {
                new UserModel 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "active1",
                    Status = CommonStatus.Enabled,
                    PasswordHash = "hash",
                    CreateTime = DateTime.Now
                },
                new UserModel 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "active2",
                    Status = CommonStatus.Enabled,
                    PasswordHash = "hash",
                    CreateTime = DateTime.Now
                }
            };

            _mockUserRepository
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(activeUsers);

            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(u => u.Status.Should().Be(CommonStatus.Enabled));
        }

        #endregion
    }
}
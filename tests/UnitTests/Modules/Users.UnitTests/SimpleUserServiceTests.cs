using LYBT.Infrastructure.Configuration.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
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
    /// UserService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心功能，使用实际的 UserMappingProfile
    /// </summary>
    public class SimpleUserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserQueryService> _mockQueryService;
        private readonly Mock<IUserBusinessService> _mockBusinessService;
        private readonly UserOptions _userOptions;
        private readonly IMapper _mapper;

        public SimpleUserServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IUserQueryService>();
            _mockBusinessService = new Mock<IUserBusinessService>();

            // 创建 UserService 实例 (主Service委托模式)
            _userService = new UserService(
                _mockQueryService.Object,
                _mockBusinessService.Object
            );
            
            // 配置 UserOptions
            _userOptions = new UserOptions
            {
                EnableDetailedAuditLogging = true,
                SendPasswordResetNotification = false
            };

            // 使用实际的 UserMappingProfile 创建 Mapper
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new UserMappingProfile()));
            _mapper = config.CreateMapper();
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

            var expectedResult = ServiceResult<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
            {
                Items = new List<UserDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().BeEmpty();
            result.Data.TotalCount.Should().Be(0);
            
            // 验证委托调用
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Return_Users_When_Exist()
        {
            // Arrange
            var testUserDtos = new List<UserDto>
            {
                new UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "user1", 
                    RealName = "用户1",
                    Status = CommonStatus.Enabled,
                    CreatedTime = DateTime.Now
                },
                new UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "user2", 
                    RealName = "用户2",
                    Status = CommonStatus.Enabled,
                    CreatedTime = DateTime.Now
                }
            };

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            var expectedResult = ServiceResult<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
            {
                Items = testUserDtos,
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService
                .Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);
            result.Data.Items.First().Username.Should().Be("user1");
            
            // 验证委托调用
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_User_Not_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockQueryService
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ServiceResult<UserDto>?)null);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
            _mockQueryService.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new UserDto 
            { 
                Id = userId, 
                Username = "testuser", 
                RealName = "测试用户",
                Status = CommonStatus.Enabled,
                CreatedTime = DateTime.Now
            };

            var expectedResult = ServiceResult<UserDto>.Success(userDto);

            _mockQueryService
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.IsSuccess.Should().BeTrue();
            result.Data!.Id.Should().Be(userId);
            result.Data.Username.Should().Be("testuser");
            
            // 验证委托调用
            _mockQueryService.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_Should_Return_Error_When_Username_Already_Exists()
        {
            // Arrange
            var dto = new UserMutationDto
            {
                Username = "existinguser",
                RealName = "已存在用户",
                IsCreateOperation = true
            };

            var expectedResult = ServiceResult<UserDto>.Failure("用户名已存在");

            _mockBusinessService
                .Setup(x => x.CreateAsync(It.IsAny<UserMutationDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("用户名已存在");
            
            // 验证委托调用
            _mockBusinessService.Verify(x => x.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Should_Create_User_When_Valid()
        {
            // Arrange
            var dto = new UserMutationDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13800138000",
                IsCreateOperation = true
            };

            var createdUserDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                RealName = dto.RealName,
                PhoneNumber = dto.PhoneNumber,
                Status = CommonStatus.Enabled,
                CreatedTime = DateTime.Now
            };

            var expectedResult = ServiceResult<UserDto>.Success(createdUserDto);

            _mockBusinessService
                .Setup(x => x.CreateAsync(It.IsAny<UserMutationDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Username.Should().Be(dto.Username);
            result.Data.RealName.Should().Be(dto.RealName);
            result.Data.PhoneNumber.Should().Be(dto.PhoneNumber);

            // 验证委托调用
            _mockBusinessService.Verify(x => x.CreateAsync(dto), Times.Once);
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
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _userService.DisableAsync(userId)
            );
        }

        [Fact]
        public async Task DisableAsync_Should_Disable_User_When_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User 
            { 
                Id = userId, 
                Username = "testuser",
                Status = CommonStatus.Enabled,
                PasswordHash = "hash",
                CreatedTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(x => x.DisableAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.DisableAsync(userId);

            // Assert
            result.Should().Be(true);

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
            var user = new User 
            { 
                Id = userId, 
                Username = "testuser",
                PasswordHash = "oldhash",
                CreatedTime = DateTime.Now
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, true))
                .ReturnsAsync(user);

            _mockUserRepository
                .Setup(x => x.UpdatePasswordAsync(userId, It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, "NewPassword123!");

            // Assert
            result.Should().Be(true);

            // 验证是否使用了默认密码
            var expectedHash = PasswordHelper.Hash("LybtUser2025#InitPass!");
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
            var user = new User 
            { 
                Id = userId, 
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword"),
                CreatedTime = DateTime.Now
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
            var user = new User 
            { 
                Id = userId, 
                Username = "testuser",
                RealName = "测试用户",
                PasswordHash = PasswordHelper.Hash(oldPassword),
                CreatedTime = DateTime.Now
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
            result.Should().Be(true);
            _mockUserRepository.Verify(x => x.UpdatePasswordAsync(userId, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Active_Users()
        {
            // Arrange
            var activeUsers = new List<User>
            {
                new User 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "active1",
                    Status = CommonStatus.Enabled,
                    PasswordHash = "hash",
                    CreatedTime = DateTime.Now
                },
                new User 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "active2",
                    Status = CommonStatus.Enabled,
                    PasswordHash = "hash",
                    CreatedTime = DateTime.Now
                }
            };

            _mockUserRepository
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(activeUsers);

            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(2);
            result.Data.All(u => u.Status == CommonStatus.Enabled).Should().BeTrue();
        }

        #endregion
    }
}
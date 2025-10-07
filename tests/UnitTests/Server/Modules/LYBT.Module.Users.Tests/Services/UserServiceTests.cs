using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests.Services
{
    /// <summary>
    /// 用户服务单元测试
    /// 测试用户CRUD操作的所有场景
    /// </summary>
    public class UserServiceTests : TestBase
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _repositoryMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public UserServiceTests()
        {
            _repositoryMock = CreateMock<IUserRepository>();
            _loggerMock = CreateLoggerMock<UserService>();
            _configurationMock = CreateMock<IConfiguration>();

            // 创建UserService实例，使用基类提供的Mapper
            _userService = new UserService(
                _repositoryMock.Object,
                Mapper,
                _loggerMock.Object,
                _configurationMock.Object);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 注册测试服务
            services.AddSingleton(_userService);
        }

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var users = CreateTestUsers(5);
            var pagedResult = new PagedResult<User>
            {
                Items = users,
                TotalCount = 5,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .Setup(x => x.GetPagedAsync(1, 20))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _userService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(5);
            result.Data!.TotalCount.Should().Be(5);
            result.Data!.CurrentPage.Should().Be(1);
            result.Data!.PageSize.Should().Be(20);

            _repositoryMock.Verify(x => x.GetPagedAsync(1, 20), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("数据库错误");
            _repositoryMock
                .Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _userService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取用户列表失败");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取用户列表失败")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<User>
            {
                Items = new List<User>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .Setup(x => x.GetPagedAsync(1, 20))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _userService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().BeEmpty();
            result.Data!.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);

            _repositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId);
            result.Data!.UserName.Should().Be(user.UserName);
            result.Data!.RealName.Should().Be(user.RealName);

            _repositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ThrowsAsync(exception);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取用户详情失败");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取用户详情失败")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                Email = "newuser@test.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor
            };

            var createdUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = createDto.Username,
                RealName = createDto.RealName,
                Email = createDto.Email,
                PhoneNumber = createDto.PhoneNumber,
                Role = createDto.Role,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow
            };

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _userService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UserName.Should().Be(createDto.Username);
            result.Data!.RealName.Should().Be(createDto.RealName);
            result.Data!.Email.Should().Be(createDto.Email);

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                Email = "newuser@test.com"
            };

            var exception = new Exception("数据库错误");

            _repositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _userService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("创建用户失败");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("创建用户失败")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_WithExistingUser_ShouldUpdateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(userId);
            
            var updateDto = new UserUpdateDto
            {
                RealName = "更新的名字",
                Email = "updated@test.com",
                PhoneNumber = "13900139000"
            };

            var updatedUser = new User
            {
                Id = userId,
                UserName = existingUser.UserName,
                RealName = updateDto.RealName,
                Email = updateDto.Email,
                PhoneNumber = updateDto.PhoneNumber,
                Role = existingUser.Role,
                Status = existingUser.Status,
                UpdatedAt = DateTime.UtcNow
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _userService.UpdateAsync(userId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId);
            result.Data!.RealName.Should().Be(updateDto.RealName);
            result.Data!.Email.Should().Be(updateDto.Email);

            _repositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserUpdateDto
            {
                RealName = "更新的名字"
            };

            _repositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateAsync(userId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");

            _repositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(userId);
            var updateDto = new UserUpdateDto
            {
                RealName = "更新的名字"
            };

            var exception = new Exception("数据库错误");

            _repositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _userService.UpdateAsync(userId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("更新用户失败");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("更新用户失败")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_WithExistingUser_ShouldDeleteSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.DeleteAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            _repositoryMock.Verify(x => x.DeleteAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _repositoryMock
                .Setup(x => x.DeleteAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("删除失败");
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock
                .Setup(x => x.DeleteAsync(userId))
                .ThrowsAsync(exception);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("删除用户失败");

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("删除用户失败")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region 辅助方法

        private User CreateTestUser(Guid? id = null)
        {
            var userId = id ?? Guid.NewGuid();
            return new User
            {
                Id = userId,
                UserName = $"user_{userId.ToString().Substring(0, 8)}",
                RealName = $"测试用户_{userId.ToString().Substring(0, 8)}",
                Email = $"user_{userId.ToString().Substring(0, 8)}@test.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private List<User> CreateTestUsers(int count)
        {
            var users = new List<User>();
            for (int i = 0; i < count; i++)
            {
                users.Add(CreateTestUser());
            }
            return users;
        }

        #endregion
    }
}
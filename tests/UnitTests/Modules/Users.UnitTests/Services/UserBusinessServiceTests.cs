using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using AutoMapper;

namespace LYBT.Module.Users.Tests.Services
{
    public class UserBusinessServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserBusinessService _service;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserBusinessService>> _mockLogger;
        private readonly DefaultPasswordService _defaultPasswordService;
        private readonly Mock<IOptions<UserOptions>> _mockOptions;
        private readonly UserOptions _userOptions;

        public UserBusinessServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserBusinessService>>();
            _mockOptions = new Mock<IOptions<UserOptions>>();

            _userOptions = new UserOptions
            {
                EnableUserCache = true,
                UserCacheExpirationMinutes = 30,
                MaxBatchOperationSize = 100,
                EnableDetailedAuditLogging = true,
                SendPasswordResetNotification = false,
                SessionTimeoutMinutes = 480,
                EnableOnlineStatusTracking = true
            };
            _mockOptions.Setup(x => x.Value).Returns(_userOptions);

            // 创建 DefaultPasswordService
            var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            mockWebHostEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

            var defaultPasswordOptions = Options.Create(new DefaultPasswordOptions
            {
                SystemAdmin = "AdminPass123!",
                NewUser = "DefaultPass123!",
                EnableInDevelopment = true,
                OnlyWhenDatabaseEmpty = false,
                ExpiryDays = 30
            });

            _defaultPasswordService = new DefaultPasswordService(defaultPasswordOptions, mockWebHostEnvironment.Object);

            _service = new UserBusinessService(
                _context,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockOptions.Object,
                _defaultPasswordService);

            SetupMockMapper();
        }

        private void SetupMockMapper()
        {
            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns((User user) => new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreateTime = user.CreatedTime
                });
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region DisableAsync Tests

        [Fact]
        public async Task DisableAsync_Should_Disable_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DisableAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task DisableAsync_Should_Return_Failure_When_User_Not_Found()
        {
            // Act
            var result = await _service.DisableAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        #endregion

        #region EnableAsync Tests

        [Fact]
        public async Task EnableAsync_Should_Enable_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                Status = CommonStatus.Disabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.EnableAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Status.Should().Be(CommonStatus.Enabled);
        }

        #endregion

        #region ResetPasswordAsync Tests

        [Fact]
        public async Task ResetPasswordAsync_Should_Reset_Password_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "old_hash"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var newPassword = "NewPassword123!";

            // Act
            var result = await _service.ResetPasswordAsync(user.Id, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.PasswordHash.Should().NotBe("old_hash");
        }

        [Fact]
        public async Task ResetPasswordAsync_Should_Return_Failure_For_Invalid_Password()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ResetPasswordAsync(user.Id, "123"); // Too short

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("密码");
        }

        #endregion

        #region BatchDisableAsync Tests

        [Fact]
        public async Task BatchDisableAsync_Should_Disable_Multiple_Users()
        {
            // Arrange
            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Username = "user1", Status = CommonStatus.Enabled },
                new User { Id = Guid.NewGuid(), Username = "user2", Status = CommonStatus.Enabled },
                new User { Id = Guid.NewGuid(), Username = "user3", Status = CommonStatus.Enabled }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var ids = users.Select(u => u.Id).ToList();

            // Act
            var result = await _service.BatchDisableAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(3);

            var updatedUsers = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            updatedUsers.Should().AllSatisfy(u => u.Status.Should().Be(CommonStatus.Disabled));
        }

        #endregion

        #region BatchEnableAsync Tests

        [Fact]
        public async Task BatchEnableAsync_Should_Enable_Multiple_Users()
        {
            // Arrange
            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Username = "user1", Status = CommonStatus.Disabled },
                new User { Id = Guid.NewGuid(), Username = "user2", Status = CommonStatus.Disabled },
                new User { Id = Guid.NewGuid(), Username = "user3", Status = CommonStatus.Disabled }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var ids = users.Select(u => u.Id).ToList();

            // Act
            var result = await _service.BatchEnableAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(3);

            var updatedUsers = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            updatedUsers.Should().AllSatisfy(u => u.Status.Should().Be(CommonStatus.Enabled));
        }

        #endregion

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_Should_Create_User_Successfully()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "New User",
                Email = "new@example.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor
            };

            _mockMapper.Setup(x => x.Map<User>(It.IsAny<UserCreateDto>()))
                .Returns(new User
                {
                    Username = dto.Username,
                    RealName = dto.RealName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Role = dto.Role
                });

            // Act
            var result = await _service.CreateUserAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("newuser");

            var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            createdUser.Should().NotBeNull();
        }

        #endregion

        #region UpdateUserAsync Tests

        [Fact]
        public async Task UpdateUserAsync_Should_Update_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Old Name",
                Email = "old@example.com"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var dto = new UserUpdateDto
            {
                RealName = "New Name",
                Email = "new@example.com",
                PhoneNumber = "13900139000"
            };

            // Act
            var result = await _service.UpdateUserAsync(user.Id, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.RealName.Should().Be("New Name");
            updatedUser.Email.Should().Be("new@example.com");
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_Should_Delete_User_Successfully()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteUserAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            var deletedUser = await _context.Users.FindAsync(user.Id);
            deletedUser.Should().BeNull();
        }

        #endregion
    }
}
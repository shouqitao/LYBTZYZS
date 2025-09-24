using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests.Services
{
    public class UserQueryServiceTests
    {
        private readonly UserQueryService _service;
        private readonly Mock<IUserReadRepository> _mockReadRepository;
        private readonly Mock<ILogger<UserQueryService>> _mockLogger;

        public UserQueryServiceTests()
        {
            _mockReadRepository = new Mock<IUserReadRepository>();
            _mockLogger = new Mock<ILogger<UserQueryService>>();

            _service = new UserQueryService(
                _mockReadRepository.Object,
                _mockLogger.Object);
        }

        #region GetRolesAsync Tests

        [Fact]
        public async Task GetRolesAsync_Should_Return_All_User_Roles()
        {
            // Arrange
            // GetRolesAsync 现在返回固定的角色列表，不依赖于数据库中的用户

            // Act
            var result = await _service.GetRolesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2); // Admin and Doctor

            // 验证角色数据结构（避免使用 dynamic）
            // GetRolesAsync 返回固定的匿名类型列表 { Value = int, Text = string }
            var rolesJson = System.Text.Json.JsonSerializer.Serialize(result.Data);
            rolesJson.Should().Contain("\"Value\":" + (int)UserRole.Admin);
            rolesJson.Should().Contain("\"Value\":" + (int)UserRole.Doctor);
            rolesJson.Should().Contain("\"Text\":\"管理员\"");
            rolesJson.Should().Contain("\"Text\":\"医生\"");
        }

        [Fact]
        public async Task GetRolesAsync_Should_Always_Return_Fixed_Roles()
        {
            // Arrange
            // GetRolesAsync 返回固定的角色列表，不管数据库中是否有用户

            // Act
            var result = await _service.GetRolesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2); // 总是返回 Admin 和 Doctor
        }

        #endregion



        #region Edge Case Tests

        [Fact]
        public async Task GetByIdAsync_Should_Handle_Empty_Guid()
        {
            // Arrange
            _mockReadRepository.Setup(x => x.GetUserDtoByIdAsync(Guid.Empty))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _service.GetByIdAsync(Guid.Empty);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetByUsernameAsync_Should_Handle_Invalid_Input(string invalidUsername)
        {
            // Act
            var result = await _service.GetByUsernameAsync(invalidUsername);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ValidateUsernameAsync_Should_Return_True_For_Invalid_Input(string invalidUsername)
        {
            // Act
            var result = await _service.ValidateUsernameAsync(invalidUsername);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse(); // Invalid usernames return failure
            result.ErrorMessage.Should().Contain("用户名不能为空");
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_False_For_Non_Doctor()
        {
            // Arrange
            var adminUserId = Guid.NewGuid();
            _mockReadRepository.Setup(x => x.IsDoctorAvailableAsync(adminUserId))
                .ReturnsAsync(false); // Admin is not a doctor

            // Act
            var result = await _service.IsDoctorAvailableAsync(adminUserId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse(); // Admin is not a doctor
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_Found()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new UserDto
            {
                Id = userId,
                Username = "testuser",
                RealName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor
            };

            _mockReadRepository.Setup(x => x.GetUserDtoByIdAsync(userId))
                .ReturnsAsync(userDto);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId);
            result.Data.Username.Should().Be("testuser");
            result.Data.RealName.Should().Be("Test User");
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Not_Found()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockReadRepository.Setup(x => x.GetUserDtoByIdAsync(userId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        #endregion

        #region GetByUsernameAsync Tests

        [Fact]
        public async Task GetByUsernameAsync_Should_Return_User_When_Found()
        {
            // Arrange
            var userDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "13800138000"
            };

            _mockReadRepository.Setup(x => x.GetUserDtoByUsernameAsync("testuser"))
                .ReturnsAsync(userDto);

            // Act
            var result = await _service.GetByUsernameAsync("testuser");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_Return_Failure_When_Not_Found()
        {
            // Arrange
            _mockReadRepository.Setup(x => x.GetUserDtoByUsernameAsync("nonexistent"))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _service.GetByUsernameAsync("nonexistent");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetByUsernameAsync_Should_Return_Failure_When_Username_Invalid(string username)
        {
            // Act
            var result = await _service.GetByUsernameAsync(username);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户名");
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_Should_Return_All_Users_When_No_Criteria()
        {
            // Arrange
            var criteria = new UserSearchDto { PageIndex = 1, PageSize = 10 };
            var pagedResult = new PagedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Username = "user1", RealName = "User One" },
                    new UserDto { Username = "user2", RealName = "User Two" },
                    new UserDto { Username = "user3", RealName = "User Three" }
                },
                TotalCount = 3,
                CurrentPage = 1,
                PageSize = 10
            };

            _mockReadRepository.Setup(x => x.GetPagedUserDtosAsync(criteria))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPagedAsync(criteria);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(3);
            result.Data.Items.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Username()
        {
            // Arrange
            var criteria = new UserSearchDto
            {
                Username = "doctor",
                PageIndex = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Username = "doctor1", RealName = "Doctor One" },
                    new UserDto { Username = "doctor2", RealName = "Doctor Two" }
                },
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            };

            _mockReadRepository.Setup(x => x.GetPagedUserDtosAsync(criteria))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPagedAsync(criteria);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().AllSatisfy(u => u.Username.Should().Contain("doctor"));
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Role()
        {
            // Arrange
            var criteria = new UserSearchDto
            {
                Role = UserRole.Doctor,
                PageIndex = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Username = "doctor1", Role = UserRole.Doctor },
                    new UserDto { Username = "doctor2", Role = UserRole.Doctor }
                },
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 10
            };

            _mockReadRepository.Setup(x => x.GetPagedUserDtosAsync(criteria))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _service.GetPagedAsync(criteria);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Doctor));
        }

        #endregion

        #region GetActiveUsersAsync Tests

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Active_Users()
        {
            // Arrange
            var activeUsers = new List<UserDto>
            {
                new UserDto { Username = "active1", Status = CommonStatus.Enabled },
                new UserDto { Username = "active2", Status = CommonStatus.Enabled }
            };

            _mockReadRepository.Setup(x => x.GetActiveUserDtosAsync())
                .ReturnsAsync(activeUsers);

            // Act
            var result = await _service.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(u => u.Username.Should().Match(name => name == "active1" || name == "active2"));
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_Should_Find_Users_By_Username()
        {
            // Arrange
            var searchResults = new List<UserDto>
            {
                new UserDto { Username = "john_doe", RealName = "John Doe" }
            };

            _mockReadRepository.Setup(x => x.SearchUserDtosAsync("john", 50))
                .ReturnsAsync(searchResults);

            // Act
            var result = await _service.SearchAsync("john");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data!.First().Username.Should().Be("john_doe");
        }

        [Fact]
        public async Task SearchAsync_Should_Find_Users_By_RealName()
        {
            // Arrange
            var searchResults = new List<UserDto>
            {
                new UserDto { Username = "user1", RealName = "张三" },
                new UserDto { Username = "user3", RealName = "张五" }
            };

            _mockReadRepository.Setup(x => x.SearchUserDtosAsync("张", 50))
                .ReturnsAsync(searchResults);

            // Act
            var result = await _service.SearchAsync("张");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region GetDoctorsAsync Tests

        [Fact]
        public async Task GetDoctorsAsync_Should_Return_Only_Doctors()
        {
            // Arrange
            var doctors = new List<UserDto>
            {
                new UserDto { Username = "doctor1", Role = UserRole.Doctor, Status = CommonStatus.Enabled },
                new UserDto { Username = "doctor2", Role = UserRole.Doctor, Status = CommonStatus.Enabled }
            };

            _mockReadRepository.Setup(x => x.GetDoctorDtosAsync())
                .ReturnsAsync(doctors);

            // Act
            var result = await _service.GetDoctorsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Doctor));
        }

        #endregion

        #region ValidateUsernameAsync Tests

        [Fact]
        public async Task ValidateUsernameAsync_Should_Return_False_When_Username_Exists()
        {
            // Arrange
            _mockReadRepository.Setup(x => x.IsUsernameAvailableAsync("existinguser"))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ValidateUsernameAsync("existinguser");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUsernameAsync_Should_Return_True_When_Username_Available()
        {
            // Arrange
            _mockReadRepository.Setup(x => x.IsUsernameAvailableAsync("newuser"))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ValidateUsernameAsync("newuser");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region IsDoctorAvailableAsync Tests

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_True_For_Available_Doctor()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            _mockReadRepository.Setup(x => x.IsDoctorAvailableAsync(doctorId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_False_For_Disabled_Doctor()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            _mockReadRepository.Setup(x => x.IsDoctorAvailableAsync(doctorId))
                .ReturnsAsync(false);

            // Act
            var result = await _service.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        #endregion
    }
}
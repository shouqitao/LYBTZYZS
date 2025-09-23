using FluentAssertions;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Tests.UnitTests.ServerServices
{
    /// <summary>
    /// UserService单元测试 - 100%方法覆盖率
    /// 测试UltraThink三层架构纯委托模式
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IUserQueryService> _mockQueryService;
        private readonly Mock<IUserBusinessService> _mockBusinessService;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockQueryService = new Mock<IUserQueryService>();
            _mockBusinessService = new Mock<IUserBusinessService>();
            _userService = new UserService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullQueryService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new UserService(null!, _mockBusinessService.Object));
            
            exception.ParamName.Should().Be("queryService");
        }

        [Fact]
        public void Constructor_WithNullBusinessService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new UserService(_mockQueryService.Object, null!));
            
            exception.ParamName.Should().Be("businessService");
        }

        [Fact]
        public void Constructor_WithValidServices_ShouldCreateInstance()
        {
            // Act & Assert
            _userService.Should().NotBeNull();
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var query = new UserSearchDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<UserDto>>.Success(
                new PagedResult<UserDto> { Items = [], Total = 0 });
            
            _mockQueryService.Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<UserDto>.Success(CreateTestUserDto());
            
            _mockQueryService.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByUsernameAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var username = "testuser";
            var expectedResult = ServiceResult<UserDto>.Success(CreateTestUserDto());
            
            _mockQueryService.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetByUsernameAsync(username);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByUsernameAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetActiveUsersAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var expectedResult = ServiceResult<List<UserDto>>.Success([CreateTestUserDto()]);
            
            _mockQueryService.Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetActiveUsersAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var keyword = "search";
            var expectedResult = ServiceResult<List<UserDto>>.Success([CreateTestUserDto()]);
            
            _mockQueryService.Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.SearchAsync(keyword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task GetRolesAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var expectedResult = ServiceResult<List<object>>.Success([
                new { Value = "Admin", Label = "管理员" },
                new { Value = "Doctor", Label = "医生" }
            ]);
            
            _mockQueryService.Setup(x => x.GetRolesAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetRolesAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetRolesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateUsernameAsync_ShouldDelegateToQueryService()
        {
            // Arrange
            var username = "testuser";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockQueryService.Setup(x => x.ValidateUsernameAsync(username))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ValidateUsernameAsync(username);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.ValidateUsernameAsync(username), Times.Once);
        }

        #endregion

        #region Core Operations测试

        [Fact]
        public async Task CreateAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                Role = UserRole.Doctor
            };
            var expectedResult = ServiceResult<UserDto>.Success(CreateTestUserDto());
            
            _mockBusinessService.Setup(x => x.CreateUserAsync(createDto))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateUserAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserUpdateDto
            {
                Id = userId,
                Username = "updateduser",
                RealName = "更新用户",
                Role = UserRole.Doctor
            };
            var expectedResult = ServiceResult<UserDto>.Success(CreateTestUserDto());
            
            _mockBusinessService.Setup(x => x.UpdateUserAsync(userId, updateDto))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.UpdateAsync(updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateUserAsync(userId, updateDto), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }

        #endregion

        #region Status Management测试

        [Fact]
        public async Task DisableAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.DisableAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DisableAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DisableAsync(userId), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.EnableAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.EnableAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.EnableAsync(userId), Times.Once);
        }

        [Fact]
        public async Task BatchDisableAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userIds = [Guid.NewGuid(), Guid.NewGuid()];
            var expectedResult = ServiceResult<int>.Success(2);
            
            _mockBusinessService.Setup(x => x.BatchDisableAsync(userIds))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.BatchDisableAsync(userIds);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.BatchDisableAsync(userIds), Times.Once);
        }

        [Fact]
        public async Task BatchEnableAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userIds = [Guid.NewGuid(), Guid.NewGuid()];
            var expectedResult = ServiceResult<int>.Success(2);
            
            _mockBusinessService.Setup(x => x.BatchEnableAsync(userIds))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.BatchEnableAsync(userIds);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.BatchEnableAsync(userIds), Times.Once);
        }

        #endregion

        #region Password Management测试

        [Fact]
        public async Task ResetPasswordAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var newPassword = "NewPassword123!";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ResetPasswordAsync(userId, newPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, newPassword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ResetPasswordAsync(userId, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "OldPassword123!";
            var newPassword = "NewPassword123!";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ChangePasswordAsync(userId, oldPassword, newPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangePasswordAsync(userId, oldPassword, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangeProfileAsync_ShouldDelegateToBusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var changeProfileDto = new ChangeProfileDto
            {
                UserId = userId,
                RealName = "更新姓名",
                PhoneNumber = "13900000000"
            };
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ChangeProfileAsync(userId, "更新姓名", "13900000000"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangeProfileAsync(changeProfileDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeProfileAsync(userId, "更新姓名", "13900000000"), Times.Once);
        }

        [Fact]
        public async Task ChangeProfileAsync_WithNullPhoneNumber_ShouldUseEmptyString()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var changeProfileDto = new ChangeProfileDto
            {
                UserId = userId,
                RealName = "更新姓名",
                PhoneNumber = null
            };
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ChangeProfileAsync(userId, "更新姓名", string.Empty))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangeProfileAsync(changeProfileDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeProfileAsync(userId, "更新姓名", string.Empty), Times.Once);
        }

        #endregion

        #region Doctor Compatibility测试

        [Fact]
        public async Task GetDoctorsAsync_WithSuccessResult_ShouldReturnDoctors()
        {
            // Arrange
            var doctors = [CreateTestUserDto(), CreateTestUserDto()];
            var expectedResult = ServiceResult<List<UserDto>>.Success(doctors);
            
            _mockQueryService.Setup(x => x.GetDoctorsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetDoctorsAsync();

            // Assert
            result.Should().BeSameAs(doctors);
            _mockQueryService.Verify(x => x.GetDoctorsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDoctorsAsync_WithFailureResult_ShouldReturnEmptyList()
        {
            // Arrange
            var expectedResult = ServiceResult<List<UserDto>>.Fail("Error occurred");
            
            _mockQueryService.Setup(x => x.GetDoctorsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetDoctorsAsync();

            // Assert
            result.Should().BeEmpty();
            _mockQueryService.Verify(x => x.GetDoctorsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDoctorsAsync_WithNullData_ShouldReturnEmptyList()
        {
            // Arrange
            var expectedResult = ServiceResult<List<UserDto>>.Success(null);
            
            _mockQueryService.Setup(x => x.GetDoctorsAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetDoctorsAsync();

            // Assert
            result.Should().BeEmpty();
            _mockQueryService.Verify(x => x.GetDoctorsAsync(), Times.Once);
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_WithSuccessResultTrue_ShouldReturnTrue()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockQueryService.Setup(x => x.IsDoctorAvailableAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().BeTrue();
            _mockQueryService.Verify(x => x.IsDoctorAvailableAsync(doctorId), Times.Once);
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_WithSuccessResultFalse_ShouldReturnFalse()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(false);
            
            _mockQueryService.Setup(x => x.IsDoctorAvailableAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().BeFalse();
            _mockQueryService.Verify(x => x.IsDoctorAvailableAsync(doctorId), Times.Once);
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_WithFailureResult_ShouldReturnFalse()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Fail("Error occurred");
            
            _mockQueryService.Setup(x => x.IsDoctorAvailableAsync(doctorId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().BeFalse();
            _mockQueryService.Verify(x => x.IsDoctorAvailableAsync(doctorId), Times.Once);
        }

        #endregion

        #region 辅助方法

        private static UserDto CreateTestUserDto()
        {
            return new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                Email = "test@example.com",
                PhoneNumber = "13800000000"
            };
        }

        #endregion
    }
}
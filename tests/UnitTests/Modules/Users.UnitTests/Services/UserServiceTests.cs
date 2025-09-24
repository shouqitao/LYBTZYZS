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
using LYBT.Shared.Interfaces.Services;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests.Services
{
    /// <summary>
    /// UserService 完整单元测试 - UltraThink双层架构
    /// 主Service委托模式测试，验证所有委托调用的正确性
    /// </summary>
    public class UserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserQueryService> _mockQueryService;
        private readonly Mock<IUserBusinessService> _mockBusinessService;

        #region Edge Cases and Error Handling Tests

        [Fact]
        public async Task GetByIdAsync_Should_Handle_Empty_Guid()
        {
            // Arrange
            _mockQueryService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(ServiceResult<UserDto>.Failure("用户不存在"));

            // Act
            var result = await _userService.GetByIdAsync(Guid.Empty);

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
            // Arrange
            _mockQueryService.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<UserDto>.Failure("用户名不能为空"));

            // Act
            var result = await _userService.GetByUsernameAsync(invalidUsername);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_Handle_Null_Input()
        {
            // Arrange
            _mockQueryService.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<UserDto>.Failure("用户名不能为空"));

            // Act
            var result = await _userService.GetByUsernameAsync(null!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_Should_Handle_Null_Dto()
        {
            // Arrange
            _mockBusinessService.Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _userService.CreateAsync(null!));
        }

        [Fact]
        public async Task UpdateAsync_Should_Handle_Null_Dto()
        {
            // Arrange
            _mockBusinessService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UserUpdateDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentNullException());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _userService.UpdateAsync(null!));
        }

        [Fact]
        public async Task BatchDisableAsync_Should_Handle_Empty_List()
        {
            // Arrange
            var emptyList = new List<Guid>();
            _mockBusinessService.Setup(x => x.BatchDisableAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(ServiceResult<int>.Success(0));

            // Act
            var result = await _userService.BatchDisableAsync(emptyList);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Fact]
        public async Task BatchEnableAsync_Should_Handle_Empty_List()
        {
            // Arrange
            var emptyList = new List<Guid>();
            _mockBusinessService.Setup(x => x.BatchEnableAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync(ServiceResult<int>.Success(0));

            // Act
            var result = await _userService.BatchEnableAsync(emptyList);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Fact]
        public async Task SearchAsync_Should_Handle_Complex_Search_Criteria()
        {
            // Arrange
            var searchDto = new UserSearchDto
            {
                Keyword = "test",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now,
                PageIndex = 1,
                PageSize = 20
            };

            var pagedResult = new PagedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Id = Guid.NewGuid(), Username = "testdoctor", Role = UserRole.Doctor }
                },
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20
            };

            _mockQueryService.Setup(x => x.SearchAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<List<UserDto>>.Success(pagedResult.Items.ToList()));

            // Act
            var result = await _userService.SearchAsync(searchDto.Keyword!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data![0].Role.Should().Be(UserRole.Doctor);
        }

        [Fact]
        public async Task ResetPasswordAsync_Should_Handle_Weak_Password()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var weakPassword = "123"; // Too weak

            _mockBusinessService.Setup(x => x.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("密码强度不足"));

            // Act
            var result = await _userService.ResetPasswordAsync(userId, weakPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("密码强度不足");
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Handle_Same_Old_And_New_Password()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var samePassword = "Pass@word1!";

            _mockBusinessService.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<bool>.Failure("新密码不能与旧密码相同"));

            // Act
            var result = await _userService.ChangePasswordAsync(userId, samePassword, samePassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("新密码不能与旧密码相同");
        }

        #endregion

        public UserServiceTests()
        {
            _mockQueryService = new Mock<IUserQueryService>();
            _mockBusinessService = new Mock<IUserBusinessService>();
            _userService = new UserService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            // Act & Assert
            var action = () => new UserService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            // Act & Assert
            var action = () => new UserService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("businessService");
        }

        [Fact]
        public void Constructor_Should_Create_Instance_When_Dependencies_Are_Valid()
        {
            // Act
            var service = new UserService(_mockQueryService.Object, _mockBusinessService.Object);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region 查询操作测试

        [Fact]
        public async Task GetPagedAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var query = new UserSearchDto { PageIndex = 1, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
            {
                Items = new List<UserDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 10
            });

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new UserDto { Id = userId, Username = "testuser" };
            var expectedResult = ServiceResult<UserDto>.Success(userDto);

            _mockQueryService.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var username = "testuser";
            var userDto = new UserDto { Username = username };
            var expectedResult = ServiceResult<UserDto>.Success(userDto);

            _mockQueryService.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetByUsernameAsync(username);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetByUsernameAsync(username), Times.Once);
        }

        [Fact]
        public async Task GetActiveUsersAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var activeUsers = new List<UserDto>
            {
                new() { Id = Guid.NewGuid(), Username = "user1", Status = CommonStatus.Enabled },
                new() { Id = Guid.NewGuid(), Username = "user2", Status = CommonStatus.Enabled }
            };
            var expectedResult = ServiceResult<List<UserDto>>.Success(activeUsers);

            _mockQueryService.Setup(x => x.GetActiveUsersAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetActiveUsersAsync(), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var keyword = "test";
            var searchResults = new List<UserDto>
            {
                new() { Username = "testuser1" },
                new() { Username = "testuser2" }
            };
            var expectedResult = ServiceResult<List<UserDto>>.Success(searchResults);

            _mockQueryService.Setup(x => x.SearchAsync(keyword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.SearchAsync(keyword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        [Fact]
        public async Task GetRolesAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var roles = new List<object> { "Doctor", "Admin" };
            var expectedResult = ServiceResult<List<object>>.Success(roles);

            _mockQueryService.Setup(x => x.GetRolesAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetRolesAsync();

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetRolesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateUsernameAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var username = "newuser";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockQueryService.Setup(x => x.ValidateUsernameAsync(username)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ValidateUsernameAsync(username);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.ValidateUsernameAsync(username), Times.Once);
        }

        #endregion

        #region Core Operations 测试

        [Fact]
        public async Task CreateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                Role = UserRole.Doctor,
                Password = "password123"
            };
            var createdUser = new UserDto { Id = Guid.NewGuid(), Username = "newuser" };
            var expectedResult = ServiceResult<UserDto>.Success(createdUser);

            _mockBusinessService.Setup(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserUpdateDto
            {
                Id = userId,
                RealName = "更新用户",
                Role = UserRole.Doctor
            };
            var updatedUser = new UserDto { Id = userId, RealName = "更新用户" };
            var expectedResult = ServiceResult<UserDto>.Success(updatedUser);

            _mockBusinessService.Setup(x => x.UpdateUserAsync(userId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.UpdateAsync(updateDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.UpdateUserAsync(userId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }

        #endregion

        #region Status Management 测试

        [Fact]
        public async Task DisableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.DisableAsync(userId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DisableAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.DisableAsync(userId), Times.Once);
        }

        [Fact]
        public async Task EnableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.EnableAsync(userId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.EnableAsync(userId);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.EnableAsync(userId), Times.Once);
        }

        [Fact]
        public async Task BatchDisableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var expectedResult = ServiceResult<int>.Success(2);

            _mockBusinessService.Setup(x => x.BatchDisableAsync(userIds)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.BatchDisableAsync(userIds);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.BatchDisableAsync(userIds), Times.Once);
        }

        [Fact]
        public async Task BatchEnableAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var expectedResult = ServiceResult<int>.Success(2);

            _mockBusinessService.Setup(x => x.BatchEnableAsync(userIds)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.BatchEnableAsync(userIds);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.BatchEnableAsync(userIds), Times.Once);
        }

        #endregion

        #region Password Management 测试

        [Fact]
        public async Task ResetPasswordAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var newPassword = "newpassword123";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.ResetPasswordAsync(userId, newPassword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, newPassword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ResetPasswordAsync(userId, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "oldpassword";
            var newPassword = "newpassword123";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.ChangePasswordAsync(userId, oldPassword, newPassword)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangePasswordAsync(userId, oldPassword, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangeProfileAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var changeProfileDto = new ChangeProfileDto
            {
                UserId = Guid.NewGuid(),
                RealName = "新姓名",
                PhoneNumber = "13800138000"
            };
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.ChangeProfileAsync(
                changeProfileDto.UserId,
                changeProfileDto.RealName,
                changeProfileDto.PhoneNumber ?? string.Empty))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangeProfileAsync(changeProfileDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeProfileAsync(
                changeProfileDto.UserId,
                changeProfileDto.RealName,
                changeProfileDto.PhoneNumber ?? string.Empty), Times.Once);
        }

        [Fact]
        public async Task ChangeProfileAsync_Should_Handle_Null_PhoneNumber()
        {
            // Arrange
            var changeProfileDto = new ChangeProfileDto
            {
                UserId = Guid.NewGuid(),
                RealName = "新姓名",
                PhoneNumber = null
            };
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService.Setup(x => x.ChangeProfileAsync(
                changeProfileDto.UserId,
                changeProfileDto.RealName,
                string.Empty))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangeProfileAsync(changeProfileDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeProfileAsync(
                changeProfileDto.UserId,
                changeProfileDto.RealName,
                string.Empty), Times.Once);
        }

        #endregion

        #region Doctor Compatibility 测试

        [Fact]
        public async Task GetDoctorsAsync_Should_Return_Doctors_When_QueryService_Success()
        {
            // Arrange
            var doctors = new List<UserDto>
            {
                new() { Id = Guid.NewGuid(), Username = "doctor1", Role = UserRole.Doctor },
                new() { Id = Guid.NewGuid(), Username = "doctor2", Role = UserRole.Doctor }
            };
            var expectedResult = ServiceResult<List<UserDto>>.Success(doctors);

            _mockQueryService.Setup(x => x.GetDoctorsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetDoctorsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(doctors);
            _mockQueryService.Verify(x => x.GetDoctorsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDoctorsAsync_Should_Return_Empty_List_When_QueryService_Fails()
        {
            // Arrange
            var expectedResult = ServiceResult<List<UserDto>>.Failure("获取医生列表失败");

            _mockQueryService.Setup(x => x.GetDoctorsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetDoctorsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetDoctorsAsync_Should_Return_Empty_List_When_QueryService_Returns_Null_Data()
        {
            // Arrange
            var expectedResult = ServiceResult<List<UserDto>>.Success(null);

            _mockQueryService.Setup(x => x.GetDoctorsAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetDoctorsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_True_When_Doctor_Is_Available()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockQueryService.Setup(x => x.IsDoctorAvailableAsync(doctorId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().BeTrue();
            _mockQueryService.Verify(x => x.IsDoctorAvailableAsync(doctorId), Times.Once);
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_False_When_Doctor_Is_Not_Available()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(false);

            _mockQueryService.Setup(x => x.IsDoctorAvailableAsync(doctorId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_False_When_QueryService_Fails()
        {
            // Arrange
            var doctorId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Failure("查询医生状态失败");

            _mockQueryService.Setup(x => x.IsDoctorAvailableAsync(doctorId)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.IsDoctorAvailableAsync(doctorId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 边界值和异常测试

        [Fact]
        public async Task All_Methods_Should_Handle_Service_Dependencies_Correctly()
        {
            // Arrange - 确保所有依赖都正确注入
            var userId = Guid.NewGuid();
            var searchDto = new UserSearchDto();

            _mockQueryService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(ServiceResult<UserDto>.Success(new UserDto()));
            _mockBusinessService.Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ServiceResult<UserDto>.Success(new UserDto()));

            // Act & Assert - 所有方法都应该能正常调用
            await _userService.GetByIdAsync(userId);
            await _userService.CreateAsync(new UserCreateDto());

            // Verify
            _mockQueryService.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockBusinessService.Verify(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void UserService_Should_Implement_IUserService()
        {
            // Assert
            _userService.Should().BeAssignableTo<LYBT.Shared.Interfaces.Services.IUserService>();
        }

        [Fact]
        public async Task Query_Methods_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange
            var query = new UserSearchDto();
            var expectedResult = ServiceResult<PagedResult<UserDto>>.Failure("查询失败");

            _mockQueryService.Setup(x => x.GetPagedAsync(query)).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Business_Methods_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var createDto = new UserCreateDto { Username = "testuser" };
            var expectedResult = ServiceResult<UserDto>.Failure("创建失败");

            _mockBusinessService.Setup(x => x.CreateUserAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(createDto);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 用户管理服务前端单元测试
    /// 测试用户管理的核心功能
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IUserApiService> _mockUserApiService;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockUserApiService = new Mock<IUserApiService>();
            // UserService需要IApiService，但我们的测试不会调用它，传null即可
            _service = new UserService(null!, _mockUserApiService.Object);
        }

        #region Test Data Factory Methods

        private UserDto CreateTestUserDto(Guid? id = null)
        {
            return new UserDto
            {
                Id = id ?? Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                Status = CommonStatus.Enabled,
                PhoneNumber = "13800138000",
                CreateTime = DateTime.Now.AddDays(-30),
                LastLoginTime = DateTime.Now.AddHours(-2)
            };
        }

        private UserCreateDto CreateTestUserCreateDto()
        {
            return new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                Password = "TestPassword123!",
                PhoneNumber = "13900139000"
            };
        }

        private UserUpdateDto CreateTestUserUpdateDto(Guid? id = null)
        {
            return new UserUpdateDto
            {
                Id = id ?? Guid.NewGuid(),
                RealName = "更新的用户",
                PhoneNumber = "13700137000"
            };
        }

        private UserPagedQueryDto CreateTestUserPagedQueryDto()
        {
            return new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 20,
                SearchKeyword = "test",
                Username = "test",
                RealName = "测试",
                Status = CommonStatus.Enabled
            };
        }

        private PaginatedResult<UserDto> CreateTestPaginatedResult()
        {
            return new PaginatedResult<UserDto>
            {
                Items = new List<UserDto> 
                { 
                    CreateTestUserDto(),
                    CreateTestUserDto() 
                },
                TotalCount = 2,
                CurrentPage = 1,
                PageSize = 20
            };
        }

        private ChangePasswordDto CreateTestChangePasswordDto()
        {
            return new ChangePasswordDto
            {
                OldPassword = "OldPassword123!",
                NewPassword = "NewPassword123!"
            };
        }

        private ChangeProfileDto CreateTestChangeProfileDto()
        {
            return new ChangeProfileDto
            {
                RealName = "更新的名称",
                PhoneNumber = "13600136000"
            };
        }

        private BatchIdsDto CreateTestBatchIdsDto()
        {
            return new BatchIdsDto
            {
                Ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };
        }

        /// <summary>
        /// 创建成功的 ApiResponse
        /// </summary>
        private ApiResponse<T> CreateSuccessApiResponse<T>(T content)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return new ApiResponse<T>(response, content, new RefitSettings());
        }

        /// <summary>
        /// 创建失败的 ApiResponse
        /// </summary>
        private ApiResponse<T> CreateFailureApiResponse<T>()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            return new ApiResponse<T>(response, default(T), new RefitSettings());
        }

        #endregion

        #region SearchUsersAsync Tests

        [Fact]
        public async Task SearchUsersAsync_WithValidQuery_ReturnsPagedResult()
        {
            // Arrange
            var query = CreateTestUserPagedQueryDto();
            var paginatedResult = CreateTestPaginatedResult();
            var apiResponse = CreateSuccessApiResponse(paginatedResult);

            _mockUserApiService
                .Setup(x => x.GetUsersAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchUsersAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(20);
        }

        [Fact]
        public async Task SearchUsersAsync_WhenApiCallFails_ReturnsEmptyResult()
        {
            // Arrange
            var query = CreateTestUserPagedQueryDto();
            var apiResponse = CreateFailureApiResponse<PaginatedResult<UserDto>>();

            _mockUserApiService
                .Setup(x => x.GetUsersAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.SearchUsersAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task SearchUsersAsync_WhenExceptionThrown_ThrowsInvalidOperationException()
        {
            // Arrange
            var query = CreateTestUserPagedQueryDto();

            _mockUserApiService
                .Setup(x => x.GetUsersAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                .ThrowsAsync(new Exception("API错误"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.SearchUsersAsync(query));
            exception.Message.Should().Contain("搜索用户失败");
        }

        #endregion

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_WithValidDto_ReturnsSuccess()
        {
            // Arrange
            var createDto = CreateTestUserCreateDto();
            var createdUserDto = CreateTestUserDto();
            var apiResponse = CreateSuccessApiResponse(createdUserDto);

            _mockUserApiService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.CreateUserAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.CreateUserAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestUserCreateDto();

            _mockUserApiService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>()))
                .ThrowsAsync(new Exception("创建失败"));

            // Act
            var result = await _service.CreateUserAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateUsername_ReturnsFailure()
        {
            // Arrange
            var createDto = CreateTestUserCreateDto();
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.Conflict),
                new RefitSettings());

            _mockUserApiService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.CreateUserAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region UpdateUserAsync Tests

        [Fact]
        public async Task UpdateUserAsync_WithValidDto_ReturnsSuccess()
        {
            // Arrange
            var updateDto = CreateTestUserUpdateDto();
            var updatedUserDto = CreateTestUserDto(updateDto.Id);
            var apiResponse = CreateSuccessApiResponse(updatedUserDto);

            _mockUserApiService
                .Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UserUpdateDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.UpdateUserAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.UpdateUserAsync(updateDto.Id, updateDto), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var updateDto = CreateTestUserUpdateDto();

            _mockUserApiService
                .Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UserUpdateDto>()))
                .ThrowsAsync(new Exception("更新失败"));

            // Act
            var result = await _service.UpdateUserAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region DisableUserAsync Tests

        [Fact]
        public async Task DisableUserAsync_WithValidId_CallsToggleStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DisableUserAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.ToggleStatusAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DisableUserAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("禁用失败"));

            // Act
            var result = await _service.DisableUserAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region EnableUserAsync Tests

        [Fact]
        public async Task EnableUserAsync_WithValidId_CallsToggleStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.ToggleStatusAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.EnableUserAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.ToggleStatusAsync(userId), Times.Once);
        }

        #endregion

        #region ResetPasswordAsync Tests

        [Fact]
        public async Task ResetPasswordAsync_WithValidId_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.ResetPasswordAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ResetPasswordAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.ResetPasswordAsync(userId), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WhenApiCallFails_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserApiService
                .Setup(x => x.ResetPasswordAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("重置密码失败"));

            // Act
            var result = await _service.ResetPasswordAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region GetRolesAsync Tests

        [Fact]
        public async Task GetRolesAsync_ReturnsSystemRoles()
        {
            // Act
            var result = await _service.GetRolesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().Contain("系统管理员");
            result.Should().Contain("普通用户");
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUserInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = CreateTestUserDto(userId);
            var apiResponse = CreateSuccessApiResponse(userDto);

            _mockUserApiService
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId);
            result.Data.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetUserByIdAsync_WhenNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiResponse = CreateFailureApiResponse<UserDto>();

            _mockUserApiService
                .Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
        }

        #endregion

        #region GetActiveUsersAsync Tests

        [Fact]
        public async Task GetActiveUsersAsync_ReturnsActiveUsersList()
        {
            // Arrange
            var userList = new List<UserDto> { CreateTestUserDto(), CreateTestUserDto() };
            var apiResponse = CreateSuccessApiResponse<IEnumerable<UserDto>>(userList);

            _mockUserApiService
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region BatchDisableUsersAsync Tests

        [Fact]
        public async Task BatchDisableUsersAsync_WithValidIds_ReturnsSuccess()
        {
            // Arrange
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.BatchDisableAsync(It.IsAny<BatchIdsDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.BatchDisableUsersAsync(userIds);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.BatchDisableAsync(It.Is<BatchIdsDto>(dto => 
                dto.Ids.Count == 2)), Times.Once);
        }

        #endregion

        #region BatchEnableUsersAsync Tests

        [Fact]
        public async Task BatchEnableUsersAsync_WithValidIds_ReturnsSuccess()
        {
            // Arrange
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.BatchEnableAsync(It.IsAny<BatchIdsDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.BatchEnableUsersAsync(userIds);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.BatchEnableAsync(It.Is<BatchIdsDto>(dto => 
                dto.Ids.Count == 2)), Times.Once);
        }

        #endregion

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_WithValidPasswords_ReturnsSuccess()
        {
            // Arrange
            var oldPassword = "OldPassword123!";
            var newPassword = "NewPassword123!";
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ChangePasswordAsync(oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.ChangePasswordAsync(It.Is<ChangePasswordDto>(dto => 
                dto.OldPassword == oldPassword && dto.NewPassword == newPassword)), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WithIncorrectOldPassword_ReturnsFailure()
        {
            // Arrange
            var oldPassword = "WrongPassword123!";
            var newPassword = "NewPassword123!";
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.BadRequest),
                new RefitSettings());

            _mockUserApiService
                .Setup(x => x.ChangePasswordAsync(It.IsAny<ChangePasswordDto>()))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.ChangePasswordAsync(oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
        }

        #endregion

        #region ChangeProfileAsync Tests

        [Fact]
        public async Task ChangeProfileAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var realName = "新名称";
            var phoneNumber = "13500135000";
            var apiResponse = CreateSuccessApiResponse(new object());

            _mockUserApiService
                .Setup(x => x.ChangeProfileAsync(It.IsAny<ChangeProfileDto>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ChangeProfileAsync(realName, phoneNumber);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockUserApiService.Verify(x => x.ChangeProfileAsync(It.Is<ChangeProfileDto>(dto => 
                dto.RealName == realName && dto.PhoneNumber == phoneNumber)), Times.Once);
        }

        #endregion

        #region GetUsersAsync Tests

        [Fact]
        public async Task GetUsersAsync_WhenActiveUsersExist_ReturnsList()
        {
            // Arrange
            var userList = new List<UserDto> { CreateTestUserDto(), CreateTestUserDto() };
            var apiResponse = CreateSuccessApiResponse<IEnumerable<UserDto>>(userList);

            _mockUserApiService
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUsersAsync_WhenNoActiveUsers_ReturnsEmptyList()
        {
            // Arrange
            var apiResponse = CreateFailureApiResponse<IEnumerable<UserDto>>();

            _mockUserApiService
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion
    }
}
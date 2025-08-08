using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Shared.Models.ApiResponses;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Dtos;
using LYBT.WPF.Client.Core.Common;
using LYBT.WPF.Client.Core.Interfaces.Services.System;
using LYBT.WPF.Client.Services.System;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Xunit;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 用户服务前端单元测试
    /// 测试服务层对API调用的封装和错误处理
    /// </summary>
    public class UserServiceTests
    {
        private readonly Mock<IUserApiService> _mockApiService;
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockApiService = new Mock<IUserApiService>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _service = new UserService(_mockApiService.Object, _mockLogger.Object);
        }

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithValidRequest_ReturnsPagedResult()
        {
            // Arrange
            var request = new PaginationRequest
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "admin"
            };

            var apiResponse = new PaginatedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Id = Guid.NewGuid(), Username = "admin", RealName = "管理员" },
                    new UserDto { Id = Guid.NewGuid(), Username = "admin2", RealName = "管理员" }
                },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockApiService.Setup(x => x.GetUsersAsync(
                request.CurrentPage,
                request.PageSize,
                request.SearchKeyword,
                It.IsAny<Guid?>(),
                It.IsAny<bool?>())
            ).ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.CurrentPage.Should().Be(1);
            result.TotalPages.Should().Be(1);
            result.Total.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_ApiThrowsException_ReturnsEmptyResult()
        {
            // Arrange
            var request = new PaginationRequest { CurrentPage = 1, PageSize = 10 };

            _mockApiService.Setup(x => x.GetUsersAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>())
            ).ThrowsAsync(new Exception("网络错误"));

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.Total.Should().Be(0);
            
            // 验证日志记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("获取用户列表失败")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ReturnsSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new UserDto
            {
                Id = userId,
                Username = "testuser",
                RealName = "测试用户"
            };

            _mockApiService.Setup(x => x.GetUserAsync(userId))
                .ReturnsAsync(userDto);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.Id.Should().Be(userId);
            result.Content.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetByIdAsync_UserNotFound_ReturnsFailureResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                System.Net.Http.HttpMethod.Get,
                new HttpResponseMessage(HttpStatusCode.NotFound),
                new RefitSettings());

            _mockApiService.Setup(x => x.GetUserAsync(userId))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.Content.Should().BeNull();
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidData_ReturnsSuccessResult()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13800138000",
                RoleId = Guid.NewGuid()
            };

            var createdUser = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = createDto.Username,
                RealName = createDto.RealName
            };

            _mockApiService.Setup(x => x.CreateUserAsync(createDto))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.Username.Should().Be("newuser");
        }

        [Fact]
        public async Task CreateAsync_DuplicateUsername_ReturnsFailureResult()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "existinguser",
                RealName = "已存在用户"
            };

            var errorContent = new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "用户名已存在",
                Status = 400
            };

            var apiException = await ApiException.Create(
                new HttpRequestMessage(),
                System.Net.Http.HttpMethod.Post,
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new System.Net.Http.StringContent(
                        System.Text.Json.JsonSerializer.Serialize(errorContent))
                },
                new RefitSettings());

            _mockApiService.Setup(x => x.CreateUserAsync(createDto))
                .ThrowsAsync(apiException);

            // Act
            var result = await _service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Error?.Detail.Should().Contain("用户名已存在");
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ReturnsSuccessResult()
        {
            // Arrange
            var updateDto = new UserEditDto
            {
                Id = Guid.NewGuid(),
                RealName = "更新后的用户",
                PhoneNumber = "13900139000"
            };

            var updatedUser = new UserDto
            {
                Id = updateDto.Id,
                Username = "testuser",
                RealName = updateDto.RealName
            };

            _mockApiService.Setup(x => x.UpdateUserAsync(updateDto.Id, updateDto))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _service.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.RealName.Should().Be("更新后的用户");
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithExistingUser_ReturnsSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiResponse = new ApiResponse<object> { Message = "用户删除成功" };

            _mockApiService.Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.DeleteAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion

        #region ToggleStatusAsync Tests

        [Fact]
        public async Task ToggleStatusAsync_WithActiveUser_ReturnsInactiveUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var toggledUser = new UserDto
            {
                Id = userId,
                Username = "testuser",
                IsActive = false
            };

            _mockApiService.Setup(x => x.ToggleStatusAsync(userId))
                .ReturnsAsync(toggledUser);

            // Act
            var result = await _service.ToggleStatusAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
            result.Content.Should().NotBeNull();
            result.Content!.IsActive.Should().BeFalse();
        }

        #endregion

        #region ResetPasswordAsync Tests

        [Fact]
        public async Task ResetPasswordAsync_WithValidUser_ReturnsSuccessResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var apiResponse = new ApiResponse<object> { Message = "密码重置成功" };

            _mockApiService.Setup(x => x.ResetPasswordAsync(userId))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _service.ResetPasswordAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeTrue();
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task GetPagedAsync_WithNullRequest_UsesDefaultValues()
        {
            // Arrange
            PaginationRequest? request = null;

            var apiResponse = new PaginatedResult<UserDto>
            {
                Items = new List<UserDto>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 10
            };

            _mockApiService.Setup(x => x.GetUsersAsync(
                1, // 默认页码
                10, // 默认页大小
                null,
                null,
                null)
            ).ReturnsAsync(apiResponse);

            // Act
            var result = await _service.GetPagedAsync(request);

            // Assert
            result.Should().NotBeNull();
            _mockApiService.Verify(x => x.GetUsersAsync(1, 10, null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NetworkTimeout_ReturnsTimeoutError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var timeoutException = new TaskCanceledException("请求超时");

            _mockApiService.Setup(x => x.GetUserAsync(userId))
                .ThrowsAsync(timeoutException);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessStatusCode.Should().BeFalse();
            result.Error?.Detail.Should().Contain("请求超时");
        }

        #endregion
    }
}
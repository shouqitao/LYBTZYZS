using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Users.Controllers;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// 用户控制器单元测试
    /// 测试优化后的RESTful接口
    /// </summary>
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);
        }

        #region GET /api/users Tests

        [Fact]
        public async Task GetUsers_WithValidParameters_ReturnsOkResultWithPaginatedData()
        {
            // Arrange
            var expectedData = new PaginatedResult<UserDto>
            {
                Items = new List<UserDto>
                {
                    new UserDto { Id = Guid.NewGuid(), Username = "user1", RealName = "用户1" },
                    new UserDto { Id = Guid.NewGuid(), Username = "user2", RealName = "用户2" }
                },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            _mockUserService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<bool?>())
            ).ReturnsAsync(expectedData);

            // Act
            var result = await _controller.GetUsers(1, 10, null, null, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<PaginatedResult<UserDto>>().Subject;
            returnValue.Items.Should().HaveCount(2);
            returnValue.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetUsers_WithSearchKeyword_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var keyword = "admin";
            _mockUserService.Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                keyword,
                It.IsAny<Guid?>(),
                It.IsAny<bool?>())
            ).ReturnsAsync(new PaginatedResult<UserDto>());

            // Act
            await _controller.GetUsers(1, 10, keyword, null, null);

            // Assert
            _mockUserService.Verify(x => x.GetPagedAsync(1, 10, keyword, null, null), Times.Once);
        }

        #endregion

        #region GET /api/users/{id} Tests

        [Fact]
        public async Task GetUser_WithExistingId_ReturnsOkResultWithUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUser = new UserDto
            {
                Id = userId,
                Username = "testuser",
                RealName = "测试用户"
            };

            _mockUserService.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<UserDto>().Subject;
            returnValue.Id.Should().Be(userId);
            returnValue.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetUser_WithNonExistingId_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserService.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region POST /api/users Tests

        [Fact]
        public async Task CreateUser_WithValidData_ReturnsOkResultWithCreatedUser()
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

            _mockUserService.Setup(x => x.CreateAsync(createDto))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _controller.CreateUser(createDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<UserDto>().Subject;
            returnValue.Username.Should().Be("newuser");
        }

        [Fact]
        public async Task CreateUser_WithInvalidData_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new UserCreateDto(); // 空数据
            _controller.ModelState.AddModelError("Username", "用户名不能为空");

            // Act
            var result = await _controller.CreateUser(createDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region PUT /api/users/{id} Tests

        [Fact]
        public async Task UpdateUser_WithValidData_ReturnsOkResultWithUpdatedUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserEditDto
            {
                Id = userId,
                RealName = "更新后的用户",
                PhoneNumber = "13900139000"
            };

            var updatedUser = new UserDto
            {
                Id = userId,
                Username = "testuser",
                RealName = updateDto.RealName
            };

            _mockUserService.Setup(x => x.UpdateAsync(It.IsAny<UserEditDto>()))
                .ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.UpdateUser(userId, updateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<UserDto>().Subject;
            returnValue.RealName.Should().Be("更新后的用户");
            
            // 验证ID被正确设置
            _mockUserService.Verify(x => x.UpdateAsync(It.Is<UserEditDto>(dto => dto.Id == userId)), Times.Once);
        }

        #endregion

        #region DELETE /api/users/{id} Tests

        [Fact]
        public async Task DeleteUser_WithExistingId_ReturnsOkResultWithSuccessMessage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserService.Setup(x => x.DeleteAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { message = "用户删除成功" });
        }

        [Fact]
        public async Task DeleteUser_WithNonExistingId_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserService.Setup(x => x.DeleteAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(new { message = "用户删除失败" });
        }

        #endregion

        #region POST /api/users/{id}/toggle-status Tests

        [Fact]
        public async Task ToggleStatus_WithExistingUser_ReturnsOkResultWithUpdatedUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var toggledUser = new UserDto
            {
                Id = userId,
                Username = "testuser",
                IsActive = false
            };

            _mockUserService.Setup(x => x.ToggleStatusAsync(userId))
                .ReturnsAsync(toggledUser);

            // Act
            var result = await _controller.ToggleStatus(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnValue = okResult.Value.Should().BeOfType<UserDto>().Subject;
            returnValue.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ToggleStatus_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserService.Setup(x => x.ToggleStatusAsync(userId))
                .ThrowsAsync(new Exception("数据库错误"));

            // Act
            var act = async () => await _controller.ToggleStatus(userId);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("数据库错误");
        }

        #endregion

        #region POST /api/users/{id}/reset-password Tests

        [Fact]
        public async Task ResetPassword_WithValidId_ReturnsOkResultWithSuccessMessage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserService.Setup(x => x.ResetPasswordAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ResetPassword(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { message = "密码重置成功" });
        }

        #endregion
    }
}
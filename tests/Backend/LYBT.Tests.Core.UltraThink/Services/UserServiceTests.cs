using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Services.Core;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Core.UltraThink.Base;
using LYBT.Infrastructure.Data;

namespace LYBT.Tests.Core.UltraThink.Services
{
    /// <summary>
    /// UserService测试 - UltraThink三层架构测试模式（接口Mock版本）
    /// 测试主Service的纯委托逻辑，确保正确调用子服务层接口
    /// </summary>
    public class UserServiceTests : UltraThinkTestBase
    {
        private readonly Mock<IUserServiceCore> _mockCoreService;
        private readonly Mock<IUserQueryService> _mockQueryService; 
        private readonly Mock<IUserBusinessService> _mockBusinessService;
        private readonly Mock<ILogger<UserService>> _mockUserServiceLogger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // 为UserService创建专用的泛型Logger Mock
            _mockUserServiceLogger = new Mock<ILogger<UserService>>();

            // Mock三层服务接口（不再Mock具体类）
            _mockCoreService = new Mock<IUserServiceCore>();
            _mockQueryService = new Mock<IUserQueryService>();
            _mockBusinessService = new Mock<IUserBusinessService>();

            // 创建被测试的主Service - 使用接口依赖
            _userService = new UserService(
                DbContext, 
                Mapper, 
                _mockUserServiceLogger.Object,
                _mockCoreService.Object,
                _mockQueryService.Object,
                _mockBusinessService.Object);
        }

        #region 查询操作委托测试

        [Fact]
        public async Task GetByIdAsync_ShouldDelegate_To_QueryService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<UserDto>.Success(new UserDto { Id = userId });
            
            _mockQueryService
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockQueryService.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetPagedAsync_ShouldDelegate_To_QueryService()
        {
            // Arrange
            var query = new UserPagedQueryDto { PageIndex = 0, PageSize = 10 };
            var expectedResult = ServiceResult<PagedResult<UserDto>>.Success(
                new PagedResult<UserDto> { Items = new List<UserDto>(), TotalCount = 0 });
            
            _mockQueryService
                .Setup(x => x.GetPagedAsync(query))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_ShouldDelegate_To_QueryService()
        {
            // Arrange
            var keyword = "test";
            var expectedResult = ServiceResult<List<UserDto>>.Success(new List<UserDto>());
            
            _mockQueryService
                .Setup(x => x.SearchAsync(keyword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.SearchAsync(keyword);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockQueryService.Verify(x => x.SearchAsync(keyword), Times.Once);
        }

        #endregion

        #region CRUD操作委托测试

        [Fact]
        public async Task CreateAsync_ShouldDelegate_To_BusinessService()
        {
            // Arrange
            var mutationDto = new UserMutationDto 
            { 
                Username = "testuser", 
                RealName = "测试用户",
                Password = "test123",
                IsCreateOperation = true
            };
            var expectedResult = ServiceResult<UserDto>.Success(new UserDto());
            
            _mockBusinessService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserMutationDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(mutationDto);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockBusinessService.Verify(x => x.CreateUserAsync(It.IsAny<UserMutationDto>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldDelegate_To_BusinessService()
        {
            // Arrange
            var mutationDto = new UserMutationDto 
            { 
                Id = Guid.NewGuid(),
                RealName = "更新用户",
                IsCreateOperation = false
            };
            var expectedResult = ServiceResult<UserDto>.Success(new UserDto());
            
            _mockBusinessService
                .Setup(x => x.UpdateUserAsync(mutationDto.Id, It.IsAny<UserMutationDto>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.UpdateAsync(mutationDto);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockBusinessService.Verify(x => x.UpdateUserAsync(mutationDto.Id, It.IsAny<UserMutationDto>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDelegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService
                .Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockBusinessService.Verify(x => x.DeleteUserAsync(userId), Times.Once);
        }

        #endregion

        #region 状态管理委托测试

        [Fact]
        public async Task DisableAsync_ShouldDelegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService
                .Setup(x => x.DisableAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DisableAsync(userId);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockBusinessService.Verify(x => x.DisableAsync(userId), Times.Once);
        }

        [Fact]
        public async Task BatchEnableAsync_ShouldDelegate_To_BusinessService()
        {
            // Arrange
            var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var expectedResult = ServiceResult<int>.Success(2);
            
            _mockBusinessService
                .Setup(x => x.BatchEnableAsync(userIds))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.BatchEnableAsync(userIds);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockBusinessService.Verify(x => x.BatchEnableAsync(userIds), Times.Once);
        }

        #endregion

        #region 密码管理委托测试

        [Fact]
        public async Task ChangePasswordAsync_ShouldDelegate_To_BusinessService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "old123";
            var newPassword = "new123";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService
                .Setup(x => x.ChangePasswordAsync(userId, oldPassword, newPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            Assert.Equal(expectedResult, result);
            _mockBusinessService.Verify(x => x.ChangePasswordAsync(userId, oldPassword, newPassword), Times.Once);
        }

        #endregion

        #region DTO传递测试

        [Fact]
        public async Task CreateAsync_ShouldPass_MutationDto_WithCreateFlag()
        {
            // Arrange
            var mutationDto = new UserMutationDto
            {
                Username = "testuser",
                Password = "test123",
                ConfirmPassword = "test123",
                RealName = "测试用户",
                Email = "test@example.com",
                IsCreateOperation = true
            };

            UserMutationDto? capturedDto = null;
            _mockBusinessService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserMutationDto>()))
                .Callback<UserMutationDto>(dto => capturedDto = dto)
                .ReturnsAsync(ServiceResult<UserDto>.Success(new UserDto()));

            // Act
            await _userService.CreateAsync(mutationDto);

            // Assert
            Assert.NotNull(capturedDto);
            Assert.Equal(mutationDto.Username, capturedDto.Username);
            Assert.Equal(mutationDto.RealName, capturedDto.RealName);
            Assert.Equal(mutationDto.Email, capturedDto.Email);
            Assert.True(capturedDto.IsCreateOperation);
        }

        #endregion

        #region 构造函数参数验证测试

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_CoreService_IsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new UserService(DbContext, Mapper, _mockUserServiceLogger.Object, null, _mockQueryService.Object, _mockBusinessService.Object));
            
            Assert.Equal("coreService", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_QueryService_IsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new UserService(DbContext, Mapper, _mockUserServiceLogger.Object, _mockCoreService.Object, null, _mockBusinessService.Object));
            
            Assert.Equal("queryService", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_BusinessService_IsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new UserService(DbContext, Mapper, _mockUserServiceLogger.Object, _mockCoreService.Object, _mockQueryService.Object, null));
            
            Assert.Equal("businessService", exception.ParamName);
        }

        #endregion
    }
}
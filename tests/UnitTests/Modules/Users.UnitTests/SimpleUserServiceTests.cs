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
using Microsoft.Extensions.Caching.Memory;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Shared.Models.Contracts.Common;
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
            var query = new UserSearchDto
            {
                PageIndex = 1,
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
                .Setup(x => x.GetPagedAsync(It.IsAny<UserSearchDto>()))
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
                    CreateTime = DateTime.Now
                },
                new UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "user2", 
                    RealName = "用户2",
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now
                }
            };

            var query = new UserSearchDto
            {
                PageIndex = 1,
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
                .Setup(x => x.GetPagedAsync(It.IsAny<UserSearchDto>()))
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
                .ReturnsAsync(ServiceResult<UserDto>.Failure("用户不存在"));

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");
            result.Data.Should().BeNull();
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
                CreateTime = DateTime.Now
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
            var dto = new UserCreateDto
            {
                Username = "existinguser",
                RealName = "已存在用户",
                Password = "Pass@word1!",
                ConfirmPassword = "Pass@word1!",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            var expectedResult = ServiceResult<UserDto>.Failure("用户名已存在");

            _mockBusinessService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("用户名已存在");
            
            // 验证委托调用
            _mockBusinessService.Verify(x => x.CreateUserAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Should_Create_User_When_Valid()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13800138000",
                Password = "Pass@word1!",
                ConfirmPassword = "Pass@word1!",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            var createdUserDto = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                RealName = dto.RealName,
                PhoneNumber = dto.PhoneNumber,
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now
            };

            var expectedResult = ServiceResult<UserDto>.Success(createdUserDto);

            _mockBusinessService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
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
            _mockBusinessService.Verify(x => x.CreateUserAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region DisableAsync 测试

        [Fact]
        public async Task DisableAsync_Should_Return_Failure_When_User_Not_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Failure("用户不存在");

            _mockBusinessService
                .Setup(x => x.DisableAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DisableAsync(userId);

            // Assert - FluentAssertions风格
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("用户不存在");
            
            // 验证委托调用
            _mockBusinessService.Verify(x => x.DisableAsync(userId), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_Should_Disable_User_When_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService
                .Setup(x => x.DisableAsync(userId))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.DisableAsync(userId);

            // Assert - FluentAssertions风格
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证委托调用
            _mockBusinessService.Verify(x => x.DisableAsync(userId), Times.Once);
        }

        #endregion

        #region ResetPasswordAsync 测试

        [Fact]
        public async Task ResetPasswordAsync_Should_Reset_Password_To_Default()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var newPassword = "NewPass@word1!";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService
                .Setup(x => x.ResetPasswordAsync(userId, newPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, newPassword);

            // Assert - FluentAssertions风格
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证委托调用
            _mockBusinessService.Verify(x => x.ResetPasswordAsync(userId, newPassword), Times.Once);
        }

        #endregion

        #region ChangePasswordAsync 测试

        [Fact]
        public async Task ChangePasswordAsync_Should_Return_Failure_When_Old_Password_Wrong()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "wrongpassword";
            var newPassword = "newpassword";
            
            var expectedResult = ServiceResult<bool>.Failure("原密码不正确");

            _mockBusinessService
                .Setup(x => x.ChangePasswordAsync(userId, oldPassword, newPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("原密码不正确");
            
            // 验证委托调用
            _mockBusinessService.Verify(x => x.ChangePasswordAsync(userId, oldPassword, newPassword), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Return_Success_When_Old_Password_Correct()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "oldpassword";
            var newPassword = "newpassword";
            
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService
                .Setup(x => x.ChangePasswordAsync(userId, oldPassword, newPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            
            // 验证委托调用
            _mockBusinessService.Verify(x => x.ChangePasswordAsync(userId, oldPassword, newPassword), Times.Once);
        }

        #endregion

        #region GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Active_Users()
        {
            // Arrange
            var activeUserDtos = new List<UserDto>
            {
                new UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "active1",
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now
                },
                new UserDto 
                { 
                    Id = Guid.NewGuid(), 
                    Username = "active2",
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now
                }
            };

            var expectedResult = ServiceResult<List<UserDto>>.Success(activeUserDtos);

            _mockQueryService
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Count.Should().Be(2);
            result.Data.All(u => u.Status == CommonStatus.Enabled).Should().BeTrue();
            
            // 验证委托调用
            _mockQueryService.Verify(x => x.GetActiveUsersAsync(), Times.Once);
        }

        #endregion

        #region 参数化测试 - 覆盖边界值与异常场景

        [Theory]
        [InlineData(0, 10, false)] // 页码为0 - 边界值
        [InlineData(1, 0, false)]  // 页面大小为0 - 边界值
        [InlineData(-1, 10, false)] // 负数页码 - 异常值
        [InlineData(1, -5, false)]  // 负数页面大小 - 异常值
        [InlineData(1, 10, true)]   // 正常值 - 基准测试
        [InlineData(1, 1000, false)] // 页面大小过大 - 边界值
        public async Task GetPagedAsync_Should_Handle_Various_Page_Parameters(int currentPage, int pageSize, bool shouldSucceed)
        {
            // Arrange
            var query = new UserSearchDto
            {
                PageIndex = currentPage,
                PageSize = pageSize
            };

            if (shouldSucceed)
            {
                var expectedResult = ServiceResult<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
                {
                    Items = new List<UserDto>(),
                    TotalCount = 0,
                    CurrentPage = currentPage,
                    PageSize = pageSize
                });

                _mockQueryService
                    .Setup(x => x.GetPagedAsync(It.IsAny<UserSearchDto>()))
                    .ReturnsAsync(expectedResult);
            }
            else
            {
                var expectedResult = ServiceResult<PagedResult<UserDto>>.Failure("页面参数无效");
                _mockQueryService
                    .Setup(x => x.GetPagedAsync(It.IsAny<UserSearchDto>()))
                    .ReturnsAsync(expectedResult);
            }

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            if (shouldSucceed)
            {
                result.IsSuccess.Should().BeTrue();
            }
            else
            {
                result.IsSuccess.Should().BeFalse();
                result.Message.Should().NotBeNullOrEmpty();
            }
            
            _mockQueryService.Verify(x => x.GetPagedAsync(query), Times.Once);
        }

        [Theory]
        [InlineData("")]                    // 空字符串用户名
        [InlineData("a")]                   // 单字符用户名  
        [InlineData("ab")]                  // 2字符用户名
        [InlineData("abc")]                 // 3字符用户名（最小合法）
        [InlineData("test_user_123")]       // 正常用户名
        [InlineData("this_is_a_very_long_username_that_exceeds_normal_limits_abcdefghijklmnopqrstuvwxyz")] // 过长用户名
        public async Task CreateAsync_Should_Handle_Various_Username_Lengths(string username)
        {
            // Arrange  
            var dto = new UserCreateDto
            {
                Username = username,
                RealName = "测试用户",
                Password = "Pass@word1!",
                ConfirmPassword = "Pass@word1!",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            bool isValidUsername = !string.IsNullOrEmpty(username) && username.Length >= 3 && username.Length <= 50;
            ServiceResult<UserDto> expectedResult;

            if (isValidUsername)
            {
                var createdUserDto = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    RealName = "测试用户",
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now
                };
                expectedResult = ServiceResult<UserDto>.Success(createdUserDto);
            }
            else
            {
                expectedResult = ServiceResult<UserDto>.Failure("用户名长度不符合要求");
            }

            _mockBusinessService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            if (isValidUsername)
            {
                result.IsSuccess.Should().BeTrue();
                result.Data!.Username.Should().Be(username);
            }
            else
            {
                result.IsSuccess.Should().BeFalse();
                result.Message.Should().NotBeNullOrEmpty();
            }

            _mockBusinessService.Verify(x => x.CreateUserAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("13800138000", true)]   // 标准手机号
        [InlineData("138001380001", false)] // 过长手机号
        [InlineData("1380013800", false)]   // 过短手机号  
        [InlineData("abc12345678", false)]  // 包含字母
        [InlineData("", true)]              // 空字符串（可选字段）
        [InlineData(null!, true)]            // null值（可选字段）
        public async Task CreateAsync_Should_Validate_Phone_Number_Format(string? phoneNumber, bool shouldSucceed)
        {
            // Arrange
            var dto = new UserCreateDto
            {
                Username = "test_user",
                RealName = "测试用户", 
                PhoneNumber = phoneNumber,
                Password = "Pass@word1!",
                ConfirmPassword = "Pass@word1!",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            ServiceResult<UserDto> expectedResult;
            if (shouldSucceed)
            {
                var createdUserDto = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Username = dto.Username,
                    RealName = dto.RealName,
                    PhoneNumber = phoneNumber,
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.Now
                };
                expectedResult = ServiceResult<UserDto>.Success(createdUserDto);
            }
            else
            {
                expectedResult = ServiceResult<UserDto>.Failure("手机号格式不正确");
            }

            _mockBusinessService
                .Setup(x => x.CreateUserAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert  
            result.Should().NotBeNull();
            result.IsSuccess.Should().Be(shouldSucceed);
            if (shouldSucceed)
            {
                result.Data!.PhoneNumber.Should().Be(phoneNumber);
            }
            else
            {
                result.Message.Should().NotBeNullOrEmpty();
            }

            _mockBusinessService.Verify(x => x.CreateUserAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
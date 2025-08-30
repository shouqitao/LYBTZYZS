using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using LYBT.Module.Users.Helpers;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Options;

namespace LYBT.Module.Users.Tests.Helpers
{
    /// <summary>
    /// UserValidationHelper单元测试
    /// 测试重构后的用户验证助手类，确保使用BaseValidationHelper基类方法的正确性
    /// </summary>
    public class UserValidationHelperTests : IDisposable
    {
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserValidationHelper> _logger;
        private readonly Mock<IOptions<UserOptions>> _mockOptions;
        private readonly UserValidationHelper _validationHelper;

        public UserValidationHelperTests()
        {
            _mockRepository = new Mock<IUserRepository>();
            
            // 配置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                // 添加基本的映射配置用于测试
                cfg.CreateMap<UserCreateDto, UserDto>();
                cfg.CreateMap<UserUpdateDto, UserDto>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _logger = NullLogger<UserValidationHelper>.Instance;
            
            // 配置UserOptions
            var userOptions = new UserOptions
            {
                MinPasswordLength = 8,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonAlphanumeric = false
            };
            _mockOptions = new Mock<IOptions<UserOptions>>();
            _mockOptions.Setup(x => x.Value).Returns(userOptions);

            _validationHelper = new UserValidationHelper(
                _mockRepository.Object,
                _mapper,
                _logger,
                _mockOptions.Object);
        }

        public void Dispose()
        {
            // 清理资源
        }

        #region ValidateUserCreationAsync Tests

        [Fact]
        public async Task ValidateUserCreationAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = "testuser",
                RealName = "测试用户",
                Email = "test@example.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor
            };

            _mockRepository
                .Setup(x => x.ExistsAsync(dto.UserName, dto.Email, dto.PhoneNumber))
                .ReturnsAsync(false);

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateUserCreationAsync_WithInvalidUserName_ReturnsFailure(string invalidUserName)
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = invalidUserName,
                RealName = "测试用户",
                Email = "test@example.com",
                Role = UserRole.Doctor
            };

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户名");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateUserCreationAsync_WithInvalidRealName_ReturnsFailure(string invalidRealName)
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = "testuser",
                RealName = invalidRealName,
                Email = "test@example.com",
                Role = UserRole.Doctor
            };

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("真实姓名");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@example.com")]
        [InlineData("test@")]
        public async Task ValidateUserCreationAsync_WithInvalidEmail_ReturnsFailure(string invalidEmail)
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = "testuser",
                RealName = "测试用户",
                Email = invalidEmail,
                Role = UserRole.Doctor
            };

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("邮箱");
            result.ErrorMessage.Should().Contain("格式不正确");
        }

        [Fact]
        public async Task ValidateUserCreationAsync_WithInvalidPhoneNumber_ReturnsFailure()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = "testuser",
                RealName = "测试用户",
                Email = "test@example.com",
                PhoneNumber = "12345", // 无效手机号
                Role = UserRole.Doctor
            };

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("手机号码");
            result.ErrorMessage.Should().Contain("格式不正确");
        }

        [Fact]
        public async Task ValidateUserCreationAsync_WithDuplicateUser_ReturnsFailure()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = "existinguser",
                RealName = "测试用户",
                Email = "test@example.com",
                Role = UserRole.Doctor
            };

            _mockRepository
                .Setup(x => x.ExistsAsync(dto.UserName, dto.Email, dto.PhoneNumber))
                .ReturnsAsync(true);

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户名、邮箱或手机号已被使用");
        }

        [Fact]
        public async Task ValidateUserCreationAsync_WithInvalidRole_ReturnsFailure()
        {
            // Arrange
            var dto = new UserCreateDto
            {
                UserName = "testuser",
                RealName = "测试用户",
                Email = "test@example.com",
                Role = (UserRole)999 // 无效角色
            };

            // Act
            var result = await _validationHelper.ValidateUserCreationAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户角色无效");
        }

        #endregion

        #region ValidateUserUpdateAsync Tests

        [Fact]
        public async Task ValidateUserUpdateAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserUpdateDto
            {
                RealName = "更新用户",
                Email = "updated@example.com",
                PhoneNumber = "13900139000"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User 
                { 
                    Id = userId, 
                    UserName = "testuser",
                    RealName = "原始用户",
                    Email = "original@example.com"
                });

            _mockRepository
                .Setup(x => x.IsEmailExistsAsync(dto.Email, userId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(x => x.IsPhoneNumberExistsAsync(dto.PhoneNumber, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _validationHelper.ValidateUserUpdateAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateUserUpdateAsync_WithNonExistentUser_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserUpdateDto
            {
                RealName = "更新用户",
                Email = "updated@example.com"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((LYBT.Entities.Users.User)null);

            // Act
            var result = await _validationHelper.ValidateUserUpdateAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        [Fact]
        public async Task ValidateUserUpdateAsync_WithDuplicateEmail_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserUpdateDto
            {
                RealName = "更新用户",
                Email = "duplicate@example.com"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User 
                { 
                    Id = userId, 
                    UserName = "testuser",
                    Email = "original@example.com"
                });

            _mockRepository
                .Setup(x => x.IsEmailExistsAsync(dto.Email, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _validationHelper.ValidateUserUpdateAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("邮箱已被其他用户使用");
        }

        #endregion

        #region ValidateUsernameAsync Tests

        [Theory]
        [InlineData("validuser")]
        [InlineData("user123")]
        [InlineData("test_user")]
        public async Task ValidateUsernameAsync_WithValidUsername_ReturnsSuccess(string username)
        {
            // Arrange
            _mockRepository
                .Setup(x => x.IsUsernameExistsAsync(username))
                .ReturnsAsync(false);

            // Act
            var result = await _validationHelper.ValidateUsernameAsync(username);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateUsernameAsync_WithInvalidUsername_ReturnsFailure(string invalidUsername)
        {
            // Act
            var result = await _validationHelper.ValidateUsernameAsync(invalidUsername);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户名");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        [Fact]
        public async Task ValidateUsernameAsync_WithDuplicateUsername_ReturnsFailure()
        {
            // Arrange
            var username = "existinguser";
            
            _mockRepository
                .Setup(x => x.IsUsernameExistsAsync(username))
                .ReturnsAsync(true);

            // Act
            var result = await _validationHelper.ValidateUsernameAsync(username);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户名已存在");
        }

        #endregion

        #region ValidatePasswordResetAsync Tests

        [Fact]
        public async Task ValidatePasswordResetAsync_WithExistingUser_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserPasswordResetDto
            {
                NewPassword = "NewPassword123"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User { Id = userId });

            // Act
            var result = await _validationHelper.ValidatePasswordResetAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePasswordResetAsync_WithNonExistentUser_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserPasswordResetDto
            {
                NewPassword = "NewPassword123"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((LYBT.Entities.Users.User)null);

            // Act
            var result = await _validationHelper.ValidatePasswordResetAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidatePasswordResetAsync_WithInvalidPassword_ReturnsFailure(string invalidPassword)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserPasswordResetDto
            {
                NewPassword = invalidPassword
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User { Id = userId });

            // Act
            var result = await _validationHelper.ValidatePasswordResetAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("新密码");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        #endregion

        #region ValidatePasswordChangeAsync Tests

        [Fact]
        public async Task ValidatePasswordChangeAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserPasswordChangeDto
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User { Id = userId });

            // Act
            var result = await _validationHelper.ValidatePasswordChangeAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidatePasswordChangeAsync_WithInvalidCurrentPassword_ReturnsFailure(string invalidCurrentPassword)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserPasswordChangeDto
            {
                CurrentPassword = invalidCurrentPassword,
                NewPassword = "NewPassword123"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User { Id = userId });

            // Act
            var result = await _validationHelper.ValidatePasswordChangeAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("当前密码");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        #endregion

        #region ValidateProfileChangeAsync Tests

        [Fact]
        public async Task ValidateProfileChangeAsync_WithValidData_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserProfileChangeDto
            {
                RealName = "新真实姓名",
                Email = "newemail@example.com",
                PhoneNumber = "13700137000"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User { Id = userId, Email = "old@example.com" });

            _mockRepository
                .Setup(x => x.IsEmailExistsAsync(dto.Email, userId))
                .ReturnsAsync(false);

            _mockRepository
                .Setup(x => x.IsPhoneNumberExistsAsync(dto.PhoneNumber, userId))
                .ReturnsAsync(false);

            // Act
            var result = await _validationHelper.ValidateProfileChangeAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidateProfileChangeAsync_WithInvalidRealName_ReturnsFailure(string invalidRealName)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new UserProfileChangeDto
            {
                RealName = invalidRealName,
                Email = "newemail@example.com"
            };

            _mockRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new LYBT.Entities.Users.User { Id = userId });

            // Act
            var result = await _validationHelper.ValidateProfileChangeAsync(userId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("真实姓名");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        #endregion

        #region ValidateBatchOperation Tests

        [Fact]
        public void ValidateBatchOperation_WithValidIds_ReturnsSuccess()
        {
            // Arrange
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var result = _validationHelper.ValidateBatchOperation(ids, "测试操作");

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidateBatchOperation_WithEmptyIds_ReturnsFailure()
        {
            // Arrange
            var ids = new List<Guid>();

            // Act
            var result = _validationHelper.ValidateBatchOperation(ids, "测试操作");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("至少选择一个用户进行测试操作");
        }

        [Fact]
        public void ValidateBatchOperation_WithNullIds_ReturnsFailure()
        {
            // Arrange
            List<Guid> ids = null;

            // Act
            var result = _validationHelper.ValidateBatchOperation(ids, "测试操作");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("至少选择一个用户进行测试操作");
        }

        [Fact]
        public void ValidateBatchOperation_WithTooManyIds_ReturnsFailure()
        {
            // Arrange
            var ids = new List<Guid>();
            for (int i = 0; i < 101; i++) // 超过100个
            {
                ids.Add(Guid.NewGuid());
            }

            // Act
            var result = _validationHelper.ValidateBatchOperation(ids, "测试操作");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("一次最多只能对100个用户进行测试操作");
        }

        #endregion

        #region ValidatePagedQuery Tests

        [Fact]
        public void ValidatePagedQuery_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                Keyword = "test",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidatePagedQuery_WithInvalidPageIndex_ReturnsFailure(int pageIndex)
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                PageIndex = pageIndex,
                PageSize = 20
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("页码必须大于0");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public void ValidatePagedQuery_WithInvalidPageSize_ReturnsFailure(int pageSize)
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                PageIndex = 1,
                PageSize = pageSize
            };

            // Act
            var result = _validationHelper.ValidatePagedQuery(query);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("页大小");
        }

        #endregion

        #region ValidatePasswordStrength Tests

        [Theory]
        [InlineData("Password123")]
        [InlineData("MyStrongPass1")]
        public void ValidatePasswordStrength_WithStrongPassword_ReturnsSuccess(string strongPassword)
        {
            // Act
            var result = _validationHelper.ValidatePasswordStrength(strongPassword);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("weak")]       // 太短
        [InlineData("password")]   // 没有大写字母和数字
        [InlineData("PASSWORD")]   // 没有小写字母和数字
        [InlineData("12345678")]   // 没有字母
        public void ValidatePasswordStrength_WithWeakPassword_ReturnsFailure(string weakPassword)
        {
            // Act
            var result = _validationHelper.ValidatePasswordStrength(weakPassword);

            // Assert
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidatePasswordStrength_WithInvalidPassword_ReturnsFailure(string invalidPassword)
        {
            // Act
            var result = _validationHelper.ValidatePasswordStrength(invalidPassword);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("密码");
            result.ErrorMessage.Should().Contain("不能为空");
        }

        #endregion

        #region Utility Methods Tests

        [Theory]
        [InlineData("validkeyword", true)]
        [InlineData("a", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void IsValidSearchKeyword_ReturnsExpectedResult(string keyword, bool expected)
        {
            // Act
            var result = _validationHelper.IsValidSearchKeyword(keyword);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(CommonStatus.Enabled, true)]
        [InlineData(CommonStatus.Disabled, true)]
        public void IsValidUserStatus_WithValidStatus_ReturnsTrue(CommonStatus status, bool expected)
        {
            // Act
            var result = _validationHelper.IsValidUserStatus(status);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void IsValidUserStatus_WithInvalidStatus_ReturnsFalse()
        {
            // Arrange
            var invalidStatus = (CommonStatus)999;

            // Act
            var result = _validationHelper.IsValidUserStatus(invalidStatus);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidGuid_WithValidGuid_ReturnsTrue()
        {
            // Arrange
            var validGuid = Guid.NewGuid();

            // Act
            var result = _validationHelper.IsValidGuid(validGuid);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidGuid_WithEmptyGuid_ReturnsFalse()
        {
            // Arrange
            var emptyGuid = Guid.Empty;

            // Act
            var result = _validationHelper.IsValidGuid(emptyGuid);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
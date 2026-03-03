using System.Security.Claims;
using BCrypt.Net;
using FluentAssertions;
using FluentValidation;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Unit.Modules.Users.Services
{
    /// <summary>
    /// 用户服务单元测试
    /// 测试用户CRUD操作的所有场景
    /// </summary>
    public class UserServiceTests : TestBase
    {
        private readonly UserService _userService;
        private readonly IUserRepository _repositoryMock;
        private readonly ILogger<UserService> _loggerMock;
        private readonly IConfiguration _configurationMock;
        private readonly IHttpContextAccessor _httpContextAccessorMock;
        private readonly IValidator<UserInputDto> _validatorMock;
        private readonly ICrossModuleAuthService _authServiceMock;
        private readonly IUserBatchOperationService _batchServiceMock;

        public UserServiceTests()
        {
            _repositoryMock = CreateMock<IUserRepository>();
            _loggerMock = CreateLoggerMock<UserService>();
            _configurationMock = CreateMock<IConfiguration>();
            _httpContextAccessorMock = CreateMock<IHttpContextAccessor>();
            _validatorMock = CreateMock<IValidator<UserInputDto>>();
            _authServiceMock = CreateMock<ICrossModuleAuthService>();
            _batchServiceMock = CreateMock<IUserBatchOperationService>();

            // 设置默认密码配置（通过IConfiguration）
            _configurationMock["Lybt:DefaultPasswords:NewUserPassword"]
                .Returns("Lybt2025@TempPass#");
            _configurationMock["Lybt:DefaultPasswords:SysAdminPassword"]
                .Returns("LybtAdmin2025@SecurePass#");

            // Phase 1 Task 1.6: 默认设置 validator 返回成功
            _validatorMock.ValidateAsync(Arg.Any<UserInputDto>(), Arg.Any<CancellationToken>())
                .Returns(new FluentValidation.Results.ValidationResult());

            // X3: 默认设置 token 撤销为成功
            _authServiceMock.RevokeUserTokensAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns(Task.CompletedTask);

            // 创建UserService实例
            _userService = new UserService(
                _repositoryMock,
                _loggerMock,
                _configurationMock,
                _httpContextAccessorMock,
                _validatorMock,
                _authServiceMock,
                _batchServiceMock);

            // Issue #1909: 默认设置为SuperAdmin角色，允许所有操作
            SetupUserRole(UserRole.SuperAdmin);
        }

        #region 辅助方法 - Issue #1909: 权限测试支持

        /// <summary>
        /// 设置当前用户角色（用于权限测试）
        /// </summary>
        private void SetupUserRole(UserRole role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            _httpContextAccessorMock.HttpContext.Returns(httpContext);
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResult()
        {
            // Arrange
            var users = CreateTestUsers(5);
            var pagedResult = new PagedResult<User>
            {
                Items = users,
                TotalCount = 5,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .GetPagedAsync(1, 20, Arg.Any<string?>(), Arg.Any<UserRole?>(), Arg.Any<CommonStatus?>())
                .Returns(pagedResult);

            // Act
            var result = await _userService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCount(5);
            result.Data!.TotalCount.Should().Be(5);
            result.Data!.CurrentPage.Should().Be(1);
            result.Data!.PageSize.Should().Be(20);

            await _repositoryMock.Received(1).GetPagedAsync(1, 20, Arg.Any<string?>(), Arg.Any<UserRole?>(), Arg.Any<CommonStatus?>());
        }

        [Fact]
        public async Task GetPagedAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var exception = new Exception("数据库错误");
            _repositoryMock
                .GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<UserRole?>(), Arg.Any<CommonStatus?>())
                .Returns<PagedResult<User>>(x => throw exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _userService.GetPagedAsync(1, 20, null));

            thrownException.Message.Should().Be("数据库错误");
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var pagedResult = new PagedResult<User>
            {
                Items = new List<User>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            };

            _repositoryMock
                .GetPagedAsync(1, 20, Arg.Any<string?>(), Arg.Any<UserRole?>(), Arg.Any<CommonStatus?>())
                .Returns(pagedResult);

            // Act
            var result = await _userService.GetPagedAsync(1, 20, null);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().BeEmpty();
            result.Data!.TotalCount.Should().Be(0);
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);

            _repositoryMock.GetByIdAsync(userId).Returns(user);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId);
            result.Data!.UserName.Should().Be(user.UserName);
            result.Data!.RealName.Should().Be(user.RealName);

            await _repositoryMock.Received(1).GetByIdAsync(userId);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _repositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var exception = new Exception("数据库错误");

            _repositoryMock.GetByIdAsync(userId).Returns<User?>(x => throw exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _userService.GetByIdAsync(userId));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var createDto = new UserInputDto
            {
                UserName = "newuser",
                RealName = "新用户",
                Email = "newuser@test.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor
            };

            var createdUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = createDto.UserName,
                RealName = createDto.RealName,
                Email = createDto.Email,
                PhoneNumber = createDto.PhoneNumber,
                Role = createDto.Role ?? UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow
            };

            _repositoryMock.AddAsync(Arg.Any<User>()).Returns(createdUser);

            // Act
            var result = await _userService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UserName.Should().Be(createDto.UserName);
            result.Data!.RealName.Should().Be(createDto.RealName);
            result.Data!.Email.Should().Be(createDto.Email);

            await _repositoryMock.Received(1).AddAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task CreateAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var createDto = new UserInputDto
            {
                UserName = "newuser",
                RealName = "新用户",
                Email = "newuser@test.com"
            };

            var exception = new Exception("数据库错误");

            _repositoryMock.AddAsync(Arg.Any<User>()).Returns<User>(x => throw exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _userService.CreateAsync(createDto));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_WithExistingUser_ShouldUpdateUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(userId);

            var updateDto = new UserInputDto
            {
                RealName = "更新的名字",
                Email = "updated@test.com",
                PhoneNumber = "13900139000"
            };

            var updatedUser = new User
            {
                Id = userId,
                UserName = existingUser.UserName,
                RealName = updateDto.RealName,
                Email = updateDto.Email,
                PhoneNumber = updateDto.PhoneNumber,
                Role = existingUser.Role,
                Status = existingUser.Status,
                UpdatedAt = DateTime.UtcNow
            };

            _repositoryMock.GetByIdAsync(userId).Returns(existingUser);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(updatedUser);

            // Act
            var result = await _userService.UpdateAsync(userId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(userId);
            result.Data!.RealName.Should().Be(updateDto.RealName);
            result.Data!.Email.Should().Be(updateDto.Email);

            await _repositoryMock.Received(1).GetByIdAsync(userId);
            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = new UserInputDto { RealName = "更新的名字" };

            _repositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.UpdateAsync(userId, updateDto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");

            await _repositoryMock.Received(1).GetByIdAsync(userId);
            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task UpdateAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(userId);
            var updateDto = new UserInputDto { RealName = "更新的名字" };

            var exception = new Exception("数据库错误");

            _repositoryMock.GetByIdAsync(userId).Returns(existingUser);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns<User>(x => throw exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _userService.UpdateAsync(userId, updateDto));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_WithExistingUser_ShouldDeleteSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var targetUser = new User
            {
                Id = userId,
                UserName = "testuser",
                PasswordHash = "hash",
                Role = UserRole.Doctor,
                RealName = "Test User"
            };

            _repositoryMock.GetByIdAsync(userId).Returns(targetUser);
            _repositoryMock.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>())
                .Returns(new List<User> { new User(), new User() });
            _repositoryMock.DeleteAsync(userId).Returns(true);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            await _repositoryMock.Received(1).DeleteAsync(userId);
        }

        [Fact]
        public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var targetUser = new User
            {
                Id = userId,
                UserName = "testuser",
                PasswordHash = "hash",
                Role = UserRole.Doctor,
                RealName = "Test User"
            };

            _repositoryMock.GetByIdAsync(userId).Returns(targetUser);
            _repositoryMock.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>())
                .Returns(new List<User> { new User(), new User() });
            _repositoryMock.DeleteAsync(userId).Returns(false);

            // Act
            var result = await _userService.DeleteAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("删除失败");
        }

        [Fact]
        public async Task DeleteAsync_WhenRepositoryThrowsException_ShouldThrowException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var targetUser = new User
            {
                Id = userId,
                UserName = "testuser",
                PasswordHash = "hash",
                Role = UserRole.Doctor,
                RealName = "Test User"
            };
            var exception = new Exception("数据库错误");

            _repositoryMock.GetByIdAsync(userId).Returns(targetUser);
            _repositoryMock.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>())
                .Returns(new List<User> { new User(), new User() });
            _repositoryMock.DeleteAsync(userId).Returns<bool>(x => throw exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<Exception>(
                () => _userService.DeleteAsync(userId));

            thrownException.Message.Should().Be("数据库错误");
        }

        #endregion

        #region ResetPasswordAsync 测试

        [Fact]
        public async Task ResetPasswordAsync_WithExistingUser_ShouldResetPassword()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var request = new ResetPasswordRequestDto();

            _repositoryMock.GetByIdAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(user);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TemporaryPassword.Should().NotBeNullOrEmpty();

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task ResetPasswordAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new ResetPasswordRequestDto();

            _repositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.ResetPasswordAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");

            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        #endregion

        #region ChangePasswordAsync 测试

        [Fact]
        public async Task ChangePasswordAsync_WithValidOldPassword_ShouldChangePassword()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "OldPass@1x";
            var newPassword = "NewPass@7x";
            var user = CreateTestUser(userId);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);

            _repositoryMock.GetByIdAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(user);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task ChangePasswordAsync_WithInvalidOldPassword_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "WrongPass@1x";
            var newPassword = "NewPass@7x";
            var user = CreateTestUser(userId);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass@1x");

            _repositoryMock.GetByIdAsync(userId).Returns(user);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("原密码");

            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task ChangePasswordAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            // 密码必须满足策略要求，否则在用户查找前就被拒绝
            var oldPassword = "OldPass@1x";
            var newPassword = "NewPass@7x";

            _repositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        /// <summary>
        /// S1-fix: 验证哈希升级场景下，新密码确实被使用而非旧密码的 rehash
        /// </summary>
        [Fact]
        public async Task ChangePasswordAsync_WithHashUpgradeNeeded_ShouldUseNewPasswordHash()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var oldPassword = "OldPass@1x";
            var newPassword = "NewPass@7x";
            var user = CreateTestUser(userId);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword);

            User? capturedUser = null;
            _repositoryMock.GetByIdAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Do<User>(u => capturedUser = u))
                .Returns(user);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            // 关键验证: 保存的哈希必须能验证 newPassword，而非 oldPassword
            capturedUser.Should().NotBeNull();
            BCrypt.Net.BCrypt.Verify(newPassword, capturedUser!.PasswordHash).Should().BeTrue(
                "保存的密码哈希应该能验证新密码");
            BCrypt.Net.BCrypt.Verify(oldPassword, capturedUser.PasswordHash).Should().BeFalse(
                "保存的密码哈希不应该能验证旧密码");
        }

        #endregion

        #region ChangeProfileAsync 测试

        [Fact]
        public async Task ChangeProfileAsync_WithValidInput_ShouldUpdateProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            var dto = new ChangeProfileDto
            {
                RealName = "更新的名字",
                PhoneNumber = "13900139000"
            };

            _repositoryMock.GetByIdAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(user);

            // Act
            var result = await _userService.ChangeProfileAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task ChangeProfileAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new ChangeProfileDto { RealName = "Test" };

            _repositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.ChangeProfileAsync(userId, dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        #endregion

        #region ToggleStatusAsync 测试

        [Fact]
        public async Task ToggleStatusAsync_EnabledToDisabled_ShouldToggle()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            user.Status = CommonStatus.Enabled;

            _repositoryMock.GetByIdAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(user);

            // Act
            var result = await _userService.ToggleStatusAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Status.Should().Be(CommonStatus.Disabled);

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task ToggleStatusAsync_DisabledToEnabled_ShouldToggle()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            user.Status = CommonStatus.Disabled;

            _repositoryMock.GetByIdAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(user);

            // Act
            var result = await _userService.ToggleStatusAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task ToggleStatusAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _repositoryMock.GetByIdAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.ToggleStatusAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");

            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        #endregion

        #region RestoreAsync 测试

        [Fact]
        public async Task RestoreAsync_WithDeletedUser_ShouldRestore()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            user.IsDeleted = true;

            _repositoryMock.GetByIdIncludingDeletedAsync(userId).Returns(user);
            _repositoryMock.UpdateAsync(Arg.Any<User>()).Returns(user);

            // Act
            var result = await _userService.RestoreAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            user.IsDeleted.Should().BeFalse();

            await _repositoryMock.Received(1).UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task RestoreAsync_WithNonDeletedUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(userId);
            user.IsDeleted = false;

            _repositoryMock.GetByIdIncludingDeletedAsync(userId).Returns(user);

            // Act
            var result = await _userService.RestoreAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("未被删除");

            await _repositoryMock.DidNotReceive().UpdateAsync(Arg.Any<User>());
        }

        [Fact]
        public async Task RestoreAsync_WithNonExistingUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _repositoryMock.GetByIdIncludingDeletedAsync(userId).Returns((User?)null);

            // Act
            var result = await _userService.RestoreAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        #endregion

        #region BatchDeleteAsync 测试

        [Fact]
        public async Task BatchDeleteAsync_WithValidIds_ShouldDeleteAll()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var batchResult = new BatchOperationResultDto
            {
                TotalCount = 2,
                SuccessCount = 2,
                FailureCount = 0,
                IsSuccess = true
            };

            _batchServiceMock.BatchDeleteAsync(ids, Arg.Any<Guid?>())
                .Returns(Result<BatchOperationResultDto>.Success(batchResult));

            // Act
            var result = await _userService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(2);
            result.Data!.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchDeleteAsync_WithEmptyList_ShouldReturnFailure()
        {
            // Arrange
            var ids = new List<Guid>();

            _batchServiceMock.BatchDeleteAsync(ids, Arg.Any<Guid?>())
                .Returns(Result<BatchOperationResultDto>.Failure("至少选择一个用户"));

            // Act
            var result = await _userService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("至少选择一个");
        }

        [Fact]
        public async Task BatchDeleteAsync_WithSomeNonExistent_ShouldReportPartial()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var batchResult = new BatchOperationResultDto
            {
                TotalCount = 2,
                SuccessCount = 1,
                FailureCount = 1,
                IsSuccess = true
            };

            _batchServiceMock.BatchDeleteAsync(ids, Arg.Any<Guid?>())
                .Returns(Result<BatchOperationResultDto>.Success(batchResult));

            // Act
            var result = await _userService.BatchDeleteAsync(ids);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data!.FailureCount.Should().Be(1);
        }

        #endregion

        #region BatchUpdateStatusAsync 测试

        [Fact]
        public async Task BatchUpdateStatusAsync_WithValidIds_ShouldUpdateAll()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };
            var targetStatus = CommonStatus.Disabled;

            var batchResult = new BatchOperationResultDto
            {
                TotalCount = 2,
                SuccessCount = 2,
                FailureCount = 0,
                IsSuccess = true
            };

            _batchServiceMock.BatchUpdateStatusAsync(ids, targetStatus, Arg.Any<Guid?>())
                .Returns(Result<BatchOperationResultDto>.Success(batchResult));

            // Act
            var result = await _userService.BatchUpdateStatusAsync(ids, targetStatus);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(2);
            result.Data!.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_WithEmptyList_ShouldReturnEmptyResult()
        {
            // Arrange
            var ids = new List<Guid>();
            var targetStatus = CommonStatus.Disabled;

            var batchResult = new BatchOperationResultDto
            {
                TotalCount = 0,
                SuccessCount = 0,
                FailureCount = 0,
                IsSuccess = true
            };

            _batchServiceMock.BatchUpdateStatusAsync(ids, targetStatus, Arg.Any<Guid?>())
                .Returns(Result<BatchOperationResultDto>.Success(batchResult));

            // Act
            var result = await _userService.BatchUpdateStatusAsync(ids, targetStatus);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(0);
            result.Data!.FailureCount.Should().Be(0);
        }

        [Fact]
        public async Task BatchUpdateStatusAsync_WithMixedResults_ShouldReportPartial()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };
            var targetStatus = CommonStatus.Disabled;

            var batchResult = new BatchOperationResultDto
            {
                TotalCount = 2,
                SuccessCount = 1,
                FailureCount = 1,
                IsSuccess = true
            };

            _batchServiceMock.BatchUpdateStatusAsync(ids, targetStatus, Arg.Any<Guid?>())
                .Returns(Result<BatchOperationResultDto>.Success(batchResult));

            // Act
            var result = await _userService.BatchUpdateStatusAsync(ids, targetStatus);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();
            result.Data!.SuccessCount.Should().Be(1);
            result.Data!.FailureCount.Should().Be(1);
        }

        #endregion

        #region 辅助方法

        private User CreateTestUser(Guid? id = null)
        {
            var userId = id ?? Guid.NewGuid();
            return new User
            {
                Id = userId,
                UserName = $"user_{userId.ToString().Substring(0, 8)}",
                RealName = $"测试用户_{userId.ToString().Substring(0, 8)}",
                Email = $"user_{userId.ToString().Substring(0, 8)}@test.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private List<User> CreateTestUsers(int count)
        {
            var users = new List<User>();
            for (int i = 0; i < count; i++)
            {
                users.Add(CreateTestUser());
            }
            return users;
        }

        #endregion
    }
}

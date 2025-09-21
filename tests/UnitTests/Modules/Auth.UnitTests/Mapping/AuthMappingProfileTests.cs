using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Mapping;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Auth.Tests.Mapping
{
    /// <summary>
    /// Auth模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// </summary>
    public class AuthMappingProfileTests
    {
        private readonly IMapper _mapper;

        public AuthMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AuthMappingProfile());
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AuthMappingProfile());
            }, NullLoggerFactory.Instance);

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_User_To_UserDto_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                PhoneNumber = "13812345678",
                Status = UserStatus.Active
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Id.Should().Be(user.Id);
            userDto.Username.Should().Be(user.Username);
            userDto.RealName.Should().Be(user.RealName);
            userDto.PhoneNumber.Should().Be(user.PhoneNumber);
            userDto.Status.Should().Be(user.Status);
        }

        [Fact]
        public void Map_ChangePasswordRequest_Should_Success_Bidirectional()
        {
            // Arrange
            var request = new ChangePasswordRequest
            {
                OldPassword = "oldPassword123",
                NewPassword = "newPassword456"
            };

            // Act
            var mapped = _mapper.Map<ChangePasswordRequest>(request);
            var mappedBack = _mapper.Map<ChangePasswordRequest>(mapped);

            // Assert
            mapped.Should().NotBeNull();
            mapped.OldPassword.Should().Be(request.OldPassword);
            mapped.NewPassword.Should().Be(request.NewPassword);

            mappedBack.Should().NotBeNull();
            mappedBack.OldPassword.Should().Be(request.OldPassword);
            mappedBack.NewPassword.Should().Be(request.NewPassword);
        }

        [Fact]
        public void Map_ChangeSysAdminPassword_Should_Success_Bidirectional()
        {
            // Arrange
            var changeSysAdminPassword = new ChangeSysAdminPassword
            {
                NewPassword = "newSysAdminPassword123",
                SecretKey = "secretKey456"
            };

            // Act
            var mapped = _mapper.Map<ChangeSysAdminPassword>(changeSysAdminPassword);
            var mappedBack = _mapper.Map<ChangeSysAdminPassword>(mapped);

            // Assert
            mapped.Should().NotBeNull();
            mapped.NewPassword.Should().Be(changeSysAdminPassword.NewPassword);
            mapped.SecretKey.Should().Be(changeSysAdminPassword.SecretKey);

            mappedBack.Should().NotBeNull();
            mappedBack.NewPassword.Should().Be(changeSysAdminPassword.NewPassword);
            mappedBack.SecretKey.Should().Be(changeSysAdminPassword.SecretKey);
        }

        [Fact]
        public void Map_AdminSecretModel_Should_Success_Bidirectional()
        {
            // Arrange
            var adminSecret = new AdminSecretModel
            {
                SecretKey = "adminSecretKey789"
            };

            // Act
            var mapped = _mapper.Map<AdminSecretModel>(adminSecret);
            var mappedBack = _mapper.Map<AdminSecretModel>(mapped);

            // Assert
            mapped.Should().NotBeNull();
            mapped.SecretKey.Should().Be(adminSecret.SecretKey);

            mappedBack.Should().NotBeNull();
            mappedBack.SecretKey.Should().Be(adminSecret.SecretKey);
        }

        [Fact]
        public void Map_BaseAuthSession_To_AuthSession_Should_Success()
        {
            // Arrange
            var baseAuthSession = new BaseAuthSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "test-token-123",
                ExpiryTime = DateTime.UtcNow.AddHours(8),
                IsActive = true
            };

            // Act
            var authSession = _mapper.Map<AuthSession>(baseAuthSession);

            // Assert
            authSession.Should().NotBeNull();
            authSession.Id.Should().Be(baseAuthSession.Id);
            authSession.UserId.Should().Be(baseAuthSession.UserId);
            authSession.Token.Should().Be(baseAuthSession.Token);
            authSession.ExpiryTime.Should().Be(baseAuthSession.ExpiryTime);
            authSession.IsActive.Should().Be(baseAuthSession.IsActive);
        }

        [Fact]
        public void Map_AuthSession_To_BaseAuthSession_Should_Success()
        {
            // Arrange
            var authSession = new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "test-token-456",
                ExpiryTime = DateTime.UtcNow.AddHours(8),
                IsActive = true
            };

            // Act
            var baseAuthSession = _mapper.Map<BaseAuthSession>(authSession);

            // Assert
            baseAuthSession.Should().NotBeNull();
            baseAuthSession.Id.Should().Be(authSession.Id);
            baseAuthSession.UserId.Should().Be(authSession.UserId);
            baseAuthSession.Token.Should().Be(authSession.Token);
            baseAuthSession.ExpiryTime.Should().Be(authSession.ExpiryTime);
            baseAuthSession.IsActive.Should().Be(authSession.IsActive);
        }

        [Fact]
        public void Map_User_With_Null_Properties_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = null,
                PhoneNumber = null,
                Status = UserStatus.Active
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Id.Should().Be(user.Id);
            userDto.Username.Should().Be(user.Username);
            userDto.RealName.Should().BeNull();
            userDto.PhoneNumber.Should().BeNull();
            userDto.Status.Should().Be(user.Status);
        }

        [Fact]
        public void Map_User_With_Different_UserStatus_Should_Success()
        {
            // Arrange
            var inactiveUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "inactiveuser",
                Status = UserStatus.Inactive
            };

            var lockedUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "lockeduser",
                Status = UserStatus.Locked
            };

            // Act
            var inactiveDto = _mapper.Map<UserDto>(inactiveUser);
            var lockedDto = _mapper.Map<UserDto>(lockedUser);

            // Assert
            inactiveDto.Status.Should().Be(UserStatus.Inactive);
            lockedDto.Status.Should().Be(UserStatus.Locked);
        }
    }
}
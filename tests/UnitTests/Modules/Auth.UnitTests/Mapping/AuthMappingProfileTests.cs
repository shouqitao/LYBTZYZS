using System;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Mapping;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
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
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AuthMappingProfile());
            });

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
                UsernName = "testuser",
                RealName = "测试用户",
                PhoneNumber = "13812345678",
                Status = CommonStatus.Enabled
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Id.Should().Be(user.Id);
            userDto.UserName.Should().Be(user.UsernName);
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
                OldPassword = "oldSysAdminPassword123",
                NewPassword = "newSysAdminPassword456"
            };

            // Act
            var mapped = _mapper.Map<ChangeSysAdminPassword>(changeSysAdminPassword);
            var mappedBack = _mapper.Map<ChangeSysAdminPassword>(mapped);

            // Assert
            mapped.Should().NotBeNull();
            mapped.OldPassword.Should().Be(changeSysAdminPassword.OldPassword);
            mapped.NewPassword.Should().Be(changeSysAdminPassword.NewPassword);

            mappedBack.Should().NotBeNull();
            mappedBack.OldPassword.Should().Be(changeSysAdminPassword.OldPassword);
            mappedBack.NewPassword.Should().Be(changeSysAdminPassword.NewPassword);
        }

        [Fact]
        public void Map_AdminSecretModel_Should_Success_Bidirectional()
        {
            // Arrange
            var adminSecret = new AdminSecretModel
            {
                Id = Guid.NewGuid(),
                PasswordHash = "hashedPasswordValue123456"
            };

            // Act
            var mapped = _mapper.Map<AdminSecretModel>(adminSecret);
            var mappedBack = _mapper.Map<AdminSecretModel>(mapped);

            // Assert
            mapped.Should().NotBeNull();
            mapped.Id.Should().Be(adminSecret.Id);
            mapped.PasswordHash.Should().Be(adminSecret.PasswordHash);

            mappedBack.Should().NotBeNull();
            mappedBack.Id.Should().Be(adminSecret.Id);
            mappedBack.PasswordHash.Should().Be(adminSecret.PasswordHash);
        }

        [Fact]
        public void Map_BaseAuthSession_To_AuthSession_Should_Success()
        {
            // Arrange
            var baseAuthSession = new BaseAuthSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Username = "testuser",
                LoginTime = DateTime.Now,
                Status = AuthSessionStatus.Active,
                RememberMe = false
            };

            // Act
            var authSession = _mapper.Map<AuthSession>(baseAuthSession);

            // Assert
            authSession.Should().NotBeNull();
            authSession.Id.Should().Be(baseAuthSession.Id);
            authSession.UserId.Should().Be(baseAuthSession.UserId.Value);
            authSession.LoginTime.Should().BeCloseTo(baseAuthSession.LoginTime, TimeSpan.FromSeconds(1));
            authSession.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Map_AuthSession_To_BaseAuthSession_Should_Success()
        {
            // Arrange
            var authSession = new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TokenHash = "hashedToken456",
                LoginTime = DateTime.Now,
                ExpiryTime = DateTime.Now.AddHours(8),
                IpAddress = "192.168.1.1",
                IsRevoked = false,
                Status = CommonStatus.Enabled
            };

            // Act
            var baseAuthSession = _mapper.Map<BaseAuthSession>(authSession);

            // Assert
            baseAuthSession.Should().NotBeNull();
            baseAuthSession.Id.Should().Be(authSession.Id);
            baseAuthSession.UserId.Should().Be(authSession.UserId);
            baseAuthSession.LoginTime.Should().BeCloseTo(authSession.LoginTime, TimeSpan.FromSeconds(1));
            baseAuthSession.Status.Should().Be(AuthSessionStatus.Active);
        }

        [Fact]
        public void Map_User_With_Null_Properties_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UsernName = "testuser",
                RealName = null,
                PhoneNumber = null,
                Status = CommonStatus.Enabled
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Id.Should().Be(user.Id);
            userDto.UserName.Should().Be(user.UsernName);
            userDto.RealName.Should().BeNull();
            userDto.PhoneNumber.Should().BeNull();
            userDto.Status.Should().Be(user.Status);
        }

        [Fact]
        public void Map_User_With_Different_Status_Should_Success()
        {
            // Arrange
            var disabledUser = new User
            {
                Id = Guid.NewGuid(),
                UsernName = "disableduser",
                Status = CommonStatus.Disabled
            };

            var enabledUser = new User
            {
                Id = Guid.NewGuid(),
                UsernName = "enableduser",
                Status = CommonStatus.Enabled
            };

            // Act
            var disabledDto = _mapper.Map<UserDto>(disabledUser);
            var enabledDto = _mapper.Map<UserDto>(enabledUser);

            // Assert
            disabledDto.Status.Should().Be(CommonStatus.Disabled);
            enabledDto.Status.Should().Be(CommonStatus.Enabled);
        }
    }
}
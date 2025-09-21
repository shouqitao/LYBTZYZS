using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Module.Users.Tests.Mapping
{
    /// <summary>
    /// Users模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确性
    /// </summary>
    public class UserMappingProfileTests
    {
        private readonly IMapper _mapper;

        public UserMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new UserMappingProfile());
            }, NullLoggerFactory.Instance);

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new UserMappingProfile());
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
                Role = UserRole.Doctor,
                Status = UserStatus.Active,
                CreatedTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                PinYinCode = "CSYH"
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Id.Should().Be(user.Id);
            userDto.Username.Should().Be(user.Username);
            userDto.RealName.Should().Be(user.RealName);
            userDto.PhoneNumber.Should().Be(user.PhoneNumber);
            userDto.Role.Should().Be(user.Role);
            userDto.Status.Should().Be(user.Status);
        }

        [Fact]
        public void Map_UserCreateDto_To_User_Should_Success()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                Password = "password123",
                RealName = "新用户",
                PhoneNumber = "13987654321",
                Role = UserRole.Doctor,
                Status = UserStatus.Active
            };

            // Act
            var user = _mapper.Map<User>(createDto);

            // Assert
            user.Should().NotBeNull();
            user.Username.Should().Be(createDto.Username);
            user.RealName.Should().Be(createDto.RealName);
            user.PhoneNumber.Should().Be(createDto.PhoneNumber);
            user.Role.Should().Be(createDto.Role);
            user.Status.Should().Be(createDto.Status);

            // 验证忽略字段
            user.Id.Should().Be(Guid.Empty);
            user.PasswordHash.Should().BeNull();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
            user.CreatedTime.Should().Be(default);
            user.UpdateTime.Should().Be(default);
            user.PinYinCode.Should().BeNull();
        }

        [Fact]
        public void Map_UserUpdateDto_To_User_Should_Success()
        {
            // Arrange
            var updateDto = new UserUpdateDto
            {
                Id = Guid.NewGuid(),
                RealName = "更新用户",
                PhoneNumber = "13911111111",
                Role = UserRole.Admin,
                Status = UserStatus.Inactive
            };

            // Act
            var user = _mapper.Map<User>(updateDto);

            // Assert
            user.Should().NotBeNull();
            user.RealName.Should().Be(updateDto.RealName);
            user.PhoneNumber.Should().Be(updateDto.PhoneNumber);
            user.Role.Should().Be(updateDto.Role);
            user.Status.Should().Be(updateDto.Status);

            // 验证忽略字段
            user.Id.Should().Be(Guid.Empty);
            user.Username.Should().BeNull();
            user.PasswordHash.Should().BeNull();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
            user.CreatedTime.Should().Be(default);
            user.UpdateTime.Should().Be(default);
            user.PinYinCode.Should().BeNull();
        }

        [Fact]
        public void Map_User_With_AdminRole_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                RealName = "管理员",
                Role = UserRole.Admin,
                Status = UserStatus.Active
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Role.Should().Be(UserRole.Admin);
            userDto.Username.Should().Be("admin");
            userDto.RealName.Should().Be("管理员");
        }

        [Fact]
        public void Map_User_With_DoctorRole_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "doctor",
                RealName = "医生",
                Role = UserRole.Doctor,
                Status = UserStatus.Active
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Role.Should().Be(UserRole.Doctor);
            userDto.Username.Should().Be("doctor");
            userDto.RealName.Should().Be("医生");
        }

        [Fact]
        public void Map_User_With_LockedStatus_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "lockeduser",
                Status = UserStatus.Locked,
                FailedLoginCount = 5,
                LockoutEnd = DateTime.Now.AddHours(1)
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Status.Should().Be(UserStatus.Locked);
            userDto.Username.Should().Be("lockeduser");
        }

        [Fact]
        public void Map_UserCreateDto_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "minimumuser",
                Password = "password123",
                Role = UserRole.Doctor,
                Status = UserStatus.Active,
                RealName = null,
                PhoneNumber = null
            };

            // Act
            var user = _mapper.Map<User>(createDto);

            // Assert
            user.Should().NotBeNull();
            user.Username.Should().Be(createDto.Username);
            user.Role.Should().Be(createDto.Role);
            user.Status.Should().Be(createDto.Status);
            user.RealName.Should().BeNull();
            user.PhoneNumber.Should().BeNull();
        }

        [Fact]
        public void Map_UserUpdateDto_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var updateDto = new UserUpdateDto
            {
                Id = Guid.NewGuid(),
                Role = UserRole.Doctor,
                Status = UserStatus.Active,
                RealName = null,
                PhoneNumber = null
            };

            // Act
            var user = _mapper.Map<User>(updateDto);

            // Assert
            user.Should().NotBeNull();
            user.Role.Should().Be(updateDto.Role);
            user.Status.Should().Be(updateDto.Status);
            user.RealName.Should().BeNull();
            user.PhoneNumber.Should().BeNull();
        }

        [Fact]
        public void Map_User_With_SpecialCharacters_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "user_with-special.chars",
                RealName = "用户（特殊字符）",
                PhoneNumber = "+86-138-1234-5678",
                PinYinCode = "YHTSZKF"
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Username.Should().Be("user_with-special.chars");
            userDto.RealName.Should().Be("用户（特殊字符）");
            userDto.PhoneNumber.Should().Be("+86-138-1234-5678");
        }
    }
}
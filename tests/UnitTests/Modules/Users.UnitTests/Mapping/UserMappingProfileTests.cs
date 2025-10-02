using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Module.Users.Mapping;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
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
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new UserMappingProfile());
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
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                PinYinCode = "CSYH"
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Id.Should().Be(user.Id);
            userDto.UserName.Should().Be(user.UsernName);
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
                Status = CommonStatus.Enabled
            };

            // Act
            var user = _mapper.Map<User>(createDto);

            // Assert
            user.Should().NotBeNull();
            user.UsernName.Should().Be(createDto.Username);
            user.RealName.Should().Be(createDto.RealName);
            user.PhoneNumber.Should().Be(createDto.PhoneNumber);
            user.Role.Should().Be(createDto.Role);
            user.Status.Should().Be(createDto.Status);

            // 验证BaseEntity默认值
            user.Id.Should().NotBe(Guid.Empty, "BaseEntity构造函数会设置默认ID");
            user.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1), "BaseEntity设置创建时间");
            user.RowVersion.Should().BeNull("BaseEntity不会自动初始化版本字段");
            user.IsDeleted.Should().BeFalse("BaseEntity默认未删除");

            // 验证映射忽略的字段
            user.PasswordHash.Should().BeEmpty("密码哈希由业务逻辑处理");
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
            user.UpdatedAt.Should().BeNull("新建时更新时间为空");
            user.PinYinCode.Should().BeNull("拼音码由业务逻辑生成");
            user.LastLoginTime.Should().BeNull();
            user.Remark.Should().BeNull();
            user.CreatedBy.Should().BeNull();
            user.UpdatedBy.Should().BeNull();
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
                Status = CommonStatus.Disabled
            };

            // Act
            var user = _mapper.Map<User>(updateDto);

            // Assert
            user.Should().NotBeNull();
            user.RealName.Should().Be(updateDto.RealName);
            user.PhoneNumber.Should().Be(updateDto.PhoneNumber);
            user.Role.Should().Be(updateDto.Role);
            user.Status.Should().Be(updateDto.Status);

            // 验证BaseEntity默认值（更新操作不应改变这些字段）
            user.Id.Should().NotBe(Guid.Empty, "BaseEntity构造函数会设置默认ID，更新时由业务逻辑处理");
            user.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1), "BaseEntity设置创建时间");
            user.RowVersion.Should().BeNull("BaseEntity不会自动初始化版本字段");
            user.IsDeleted.Should().BeFalse("BaseEntity默认未删除");

            // 验证映射忽略的字段
            user.UsernName.Should().BeEmpty("用户名不允许修改");
            user.PasswordHash.Should().BeEmpty("密码哈希由业务逻辑处理");
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
            user.UpdatedAt.Should().BeNull("更新时间由业务逻辑设置");
            user.PinYinCode.Should().BeNull("拼音码由业务逻辑生成");
            user.LastLoginTime.Should().BeNull();
            user.Remark.Should().BeNull();
            user.CreatedBy.Should().BeNull();
            user.UpdatedBy.Should().BeNull();
        }

        [Fact]
        public void Map_User_With_AdminRole_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UsernName = "admin",
                RealName = "管理员",
                Role = UserRole.Admin,
                Status = CommonStatus.Enabled
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Role.Should().Be(UserRole.Admin);
            userDto.UserName.Should().Be("admin");
            userDto.RealName.Should().Be("管理员");
        }

        [Fact]
        public void Map_User_With_DoctorRole_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UsernName = "doctor",
                RealName = "医生",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Role.Should().Be(UserRole.Doctor);
            userDto.UserName.Should().Be("doctor");
            userDto.RealName.Should().Be("医生");
        }

        [Fact]
        public void Map_User_With_LockedStatus_Should_Success()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UsernName = "lockeduser",
                Status = CommonStatus.Disabled,
                FailedLoginCount = 5,
                LockoutEnd = DateTime.Now.AddHours(1)
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.Status.Should().Be(CommonStatus.Disabled);
            userDto.UserName.Should().Be("lockeduser");
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
                Status = CommonStatus.Enabled,
                RealName = null,
                PhoneNumber = null
            };

            // Act
            var user = _mapper.Map<User>(createDto);

            // Assert
            user.Should().NotBeNull();
            user.UsernName.Should().Be(createDto.Username);
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
                Status = CommonStatus.Enabled,
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
                UsernName = "user_with-special.chars",
                RealName = "用户（特殊字符）",
                PhoneNumber = "+86-138-1234-5678",
                PinYinCode = "YHTSZKF"
            };

            // Act
            var userDto = _mapper.Map<UserDto>(user);

            // Assert
            userDto.Should().NotBeNull();
            userDto.UserName.Should().Be("user_with-special.chars");
            userDto.RealName.Should().Be("用户（特殊字符）");
            userDto.PhoneNumber.Should().Be("+86-138-1234-5678");
        }
    }
}

using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using System;

namespace LYBT.Module.Users.Tests.Builders
{
    /// <summary>
    /// User实体构建器，用于快速创建测试用户数据
    /// </summary>
    public class UserBuilder
    {
        private User _user;

        public UserBuilder()
        {
            _user = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"testuser_{Guid.NewGuid():N}".Substring(0, 20),
                PasswordHash = PasswordHelper.Hash("TestPass@word1!"),
                RealName = "测试用户",
                Role = UserRole.Doctor,
                PhoneNumber = "13800138000",
                Email = "test@example.com",
                Status = CommonStatus.Enabled,
                PinYinCode = "CSYH",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 } // 默认RowVersion以支持SQLite测试
            };
        }

        public UserBuilder WithId(Guid id)
        {
            _user.Id = id;
            return this;
        }

        public UserBuilder WithUsername(string username)
        {
            _user.UserName = username;
            return this;
        }

        public UserBuilder WithPassword(string password)
        {
            _user.PasswordHash = PasswordHelper.Hash(password);
            return this;
        }

        public UserBuilder WithPasswordHash(string passwordHash)
        {
            _user.PasswordHash = passwordHash;
            return this;
        }

        public UserBuilder WithRealName(string realName)
        {
            _user.RealName = realName;
            return this;
        }

        public UserBuilder WithRole(UserRole role)
        {
            _user.Role = role;
            return this;
        }

        public UserBuilder WithPhoneNumber(string phoneNumber)
        {
            _user.PhoneNumber = phoneNumber;
            return this;
        }

        public UserBuilder WithEmail(string email)
        {
            _user.Email = email;
            return this;
        }

        public UserBuilder WithStatus(CommonStatus status)
        {
            _user.Status = status;
            return this;
        }

        public UserBuilder AsDeleted()
        {
            _user.IsDeleted = true;
            _user.Status = CommonStatus.Disabled;
            return this;
        }

        public UserBuilder AsDisabled()
        {
            _user.Status = CommonStatus.Disabled;
            return this;
        }

        public UserBuilder AsEnabled()
        {
            _user.Status = CommonStatus.Enabled;
            return this;
        }

        public UserBuilder WithCreatedAt(DateTime createdAt)
        {
            _user.CreatedAt = createdAt;
            return this;
        }

        public UserBuilder WithLastLoginTime(DateTime? lastLoginTime)
        {
            _user.LastLoginTime = lastLoginTime;
            return this;
        }

        public UserBuilder WithRowVersion(byte[] rowVersion)
        {
            _user.RowVersion = rowVersion;
            return this;
        }

        public UserBuilder WithDefaultRowVersion()
        {
            _user.RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
            return this;
        }

        public User Build()
        {
            return _user;
        }

        /// <summary>
        /// 创建一个管理员用户
        /// </summary>
        public static User CreateAdmin(string username = "admin")
        {
            return new UserBuilder()
                .WithUsername(username)
                .WithRole(UserRole.Admin)
                .WithRealName("系统管理员")
                .Build();
        }

        /// <summary>
        /// 创建一个医生用户
        /// </summary>
        public static User CreateDoctor(string username = "doctor")
        {
            return new UserBuilder()
                .WithUsername(username)
                .WithRole(UserRole.Doctor)
                .WithRealName("医生")
                .Build();
        }

        /// <summary>
        /// 创建一个禁用的用户
        /// </summary>
        public static User CreateDisabledUser(string username = "disabled")
        {
            return new UserBuilder()
                .WithUsername(username)
                .AsDisabled()
                .Build();
        }
    }

    /// <summary>
    /// UserCreateDto构建器
    /// </summary>
    public class UserCreateDtoBuilder
    {
        private UserCreateDto _dto;

        public UserCreateDtoBuilder()
        {
            _dto = new UserCreateDto
            {
                Username = $"newuser_{Guid.NewGuid():N}".Substring(0, 20),
                Password = "TestPass@word1!",
                RealName = "新用户",
                Role = UserRole.Doctor,
                PhoneNumber = "13900139000",
                Email = "new@example.com",
                Status = CommonStatus.Enabled
            };
        }

        public UserCreateDtoBuilder WithUsername(string username)
        {
            _dto.Username = username;
            return this;
        }

        public UserCreateDtoBuilder WithPassword(string password)
        {
            _dto.Password = password;
            return this;
        }

        public UserCreateDtoBuilder WithRealName(string realName)
        {
            _dto.RealName = realName;
            return this;
        }

        public UserCreateDtoBuilder WithRole(UserRole role)
        {
            _dto.Role = role;
            return this;
        }

        public UserCreateDtoBuilder WithPhoneNumber(string phoneNumber)
        {
            _dto.PhoneNumber = phoneNumber;
            return this;
        }

        public UserCreateDtoBuilder WithEmail(string email)
        {
            _dto.Email = email;
            return this;
        }

        public UserCreateDtoBuilder WithStatus(CommonStatus status)
        {
            _dto.Status = status;
            return this;
        }

        public UserCreateDto Build()
        {
            return _dto;
        }

        /// <summary>
        /// 创建一个有效的管理员DTO
        /// </summary>
        public static UserCreateDto CreateValidAdmin()
        {
            return new UserCreateDtoBuilder()
                .WithRole(UserRole.Admin)
                .WithRealName("管理员")
                .Build();
        }

        /// <summary>
        /// 创建一个有效的医生DTO
        /// </summary>
        public static UserCreateDto CreateValidDoctor()
        {
            return new UserCreateDtoBuilder()
                .WithRole(UserRole.Doctor)
                .WithRealName("医生")
                .Build();
        }
    }

    /// <summary>
    /// UserUpdateDto构建器
    /// </summary>
    public class UserUpdateDtoBuilder
    {
        private UserUpdateDto _dto;

        public UserUpdateDtoBuilder()
        {
            _dto = new UserUpdateDto
            {
                RealName = "更新用户",
                Role = UserRole.Doctor,
                PhoneNumber = "13700137000",
                Email = "update@example.com",
                Status = CommonStatus.Enabled
            };
        }

        public UserUpdateDtoBuilder WithRealName(string realName)
        {
            _dto.RealName = realName;
            return this;
        }

        public UserUpdateDtoBuilder WithRole(UserRole? role)
        {
            _dto.Role = role;
            return this;
        }

        public UserUpdateDtoBuilder WithPhoneNumber(string phoneNumber)
        {
            _dto.PhoneNumber = phoneNumber;
            return this;
        }

        public UserUpdateDtoBuilder WithEmail(string email)
        {
            _dto.Email = email;
            return this;
        }

        public UserUpdateDtoBuilder WithStatus(CommonStatus status)
        {
            _dto.Status = status;
            return this;
        }

        public UserUpdateDto Build()
        {
            return _dto;
        }
    }
}
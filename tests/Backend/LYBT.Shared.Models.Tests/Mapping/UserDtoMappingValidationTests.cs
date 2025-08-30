using System;
using System.Collections.Generic;
using AutoMapper;
using Xunit;
using LYBT.Module.Users.Mapping;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Models.Tests.Mapping
{
    /// <summary>
    /// User模块DTO映射验证测试
    /// UltraThink质量保证：确保User相关的所有DTO映射正确无误
    /// </summary>
    public class UserDtoMappingValidationTests : BaseDtoMappingValidationTests
    {
        protected override IEnumerable<Profile> GetMappingProfiles()
        {
            yield return new UserMappingProfile();
        }

        protected override IEnumerable<(Type Source, Type Destination)> GetMappingPairs()
        {
            // UserModel ↔ UserDto 双向映射
            yield return (typeof(UserModel), typeof(UserDto));
            yield return (typeof(UserDto), typeof(UserModel));

            // UserCreateDto → UserModel 单向映射
            yield return (typeof(UserCreateDto), typeof(UserModel));

            // UserUpdateDto → UserModel 单向映射  
            yield return (typeof(UserUpdateDto), typeof(UserModel));

            // UserModel → UserDetailDto 单向映射
            yield return (typeof(UserModel), typeof(UserDetailDto));
        }

        /// <summary>
        /// 测试User实体到DTO的映射
        /// </summary>
        [Fact]
        public void MapUserModelToUserDto_ShouldMapAllFields()
        {
            // Arrange
            var userModel = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                PinYinCode = "CSYH",
                PhoneNumber = "13800138000",
                Role = Entities.Users.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                PasswordHash = "hashedpassword123",
                CreateTime = DateTime.Now.AddDays(-30),
                UpdateTime = DateTime.Now
            };

            // Act
            var userDto = _mapper.Map<UserDto>(userModel);

            // Assert
            Assert.Equal(userModel.Id, userDto.Id);
            Assert.Equal(userModel.Username, userDto.Username);
            Assert.Equal(userModel.RealName, userDto.RealName);
            Assert.Equal(userModel.PinYinCode, userDto.PinYinCode);
            Assert.Equal(userModel.PhoneNumber, userDto.PhoneNumber);
            Assert.Equal(userModel.Role.ToString(), userDto.Role);
            Assert.Equal(userModel.Status.ToString(), userDto.Status);
            Assert.Equal(userModel.CreateTime, userDto.CreateTime);
            Assert.Equal(userModel.UpdateTime, userDto.UpdateTime);
        }

        /// <summary>
        /// 测试UserCreateDto到User实体的映射
        /// </summary>
        [Fact]
        public void MapUserCreateDtoToUserModel_ShouldMapAllFields()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13900139000",
                Password = "Password123!",
                Role = "Doctor"
            };

            // Act
            var userModel = _mapper.Map<UserModel>(createDto);

            // Assert
            Assert.Equal(createDto.Username, userModel.Username);
            Assert.Equal(createDto.RealName, userModel.RealName);
            Assert.Equal(createDto.PhoneNumber, userModel.PhoneNumber);
            Assert.Equal(Entities.Users.UserRole.Doctor, userModel.Role);
            // 注意：密码和其他字段需要在业务层处理
        }

        /// <summary>
        /// 测试UserUpdateDto到User实体的映射
        /// </summary>
        [Fact]
        public void MapUserUpdateDtoToUserModel_ShouldMapAllFields()
        {
            // Arrange
            var existingUser = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "existinguser",
                RealName = "现有用户",
                PinYinCode = "XYYH",
                PhoneNumber = "13700137000",
                Role = Entities.Users.UserRole.Doctor
            };

            var updateDto = new UserUpdateDto
            {
                Id = existingUser.Id,
                RealName = "更新用户",
                PhoneNumber = "13600136000",
                Role = "Admin",
                Status = "Disabled"
            };

            // Act
            _mapper.Map(updateDto, existingUser);

            // Assert - 验证更新的字段
            Assert.Equal(updateDto.Id, existingUser.Id);
            Assert.Equal(updateDto.RealName, existingUser.RealName);
            Assert.Equal(updateDto.PhoneNumber, existingUser.PhoneNumber);
            Assert.Equal(Entities.Users.UserRole.Admin, existingUser.Role);
            Assert.Equal(LYBT.Shared.Models.Enums.CommonStatus.Disabled, existingUser.Status);
        }

        /// <summary>
        /// 测试User实体到UserDetailDto的映射
        /// </summary>
        [Fact]
        public void MapUserModelToUserDetailDto_ShouldMapAllFields()
        {
            // Arrange
            var userModel = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "detailuser",
                RealName = "详情用户",
                PinYinCode = "XQYH",
                PhoneNumber = "13500135000",
                Role = Entities.Users.UserRole.Admin,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreateTime = DateTime.Now.AddDays(-15),
                UpdateTime = DateTime.Now.AddHours(-2)
            };

            // Act
            var detailDto = _mapper.Map<UserDetailDto>(userModel);

            // Assert
            Assert.Equal(userModel.Id, detailDto.Id);
            Assert.Equal(userModel.Username, detailDto.Username);
            Assert.Equal(userModel.RealName, detailDto.RealName);
            Assert.Equal(userModel.PinYinCode, detailDto.PinYinCode);
            Assert.Equal(userModel.PhoneNumber, detailDto.PhoneNumber);
            Assert.Equal(userModel.Role.ToString(), detailDto.Role);
        }

        /// <summary>
        /// 测试映射性能
        /// </summary>
        [Fact]
        public void UserMappings_ShouldHaveAcceptablePerformance()
        {
            // Arrange
            var userModel = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "perfuser",
                RealName = "性能测试用户"
            };

            var iterations = 1000;
            var startTime = DateTime.Now;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var userDto = _mapper.Map<UserDto>(userModel);
                Assert.NotNull(userDto);
            }

            var elapsed = DateTime.Now - startTime;

            // Assert - 1000次映射应该在1秒内完成
            Assert.True(elapsed.TotalMilliseconds < 1000, 
                $"映射性能不佳: {iterations}次映射耗时 {elapsed.TotalMilliseconds}ms");
        }
    }
}
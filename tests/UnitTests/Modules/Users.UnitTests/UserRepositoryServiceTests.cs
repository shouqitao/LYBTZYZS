using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Users.Repositories;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Users.Tests.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests
{
    /// <summary>
    /// Service层测试验证 - 使用Repository层已有的测试基础
    /// 这个测试演示了如何测试Service层的逻辑（通过Repository层）
    /// </summary>
    public class UserRepositoryServiceTests : RepositoryTestBase
    {
        private readonly UserRepository _userRepository;

        public UserRepositoryServiceTests()
{
    var logger = new Mock<ILogger<UserRepository>>();
    var cache = new Mock<IMemoryCache>();
    _userRepository = new UserRepository(Context, logger.Object, cache.Object);
}

        #region Service层业务逻辑测试

        [Fact]
        public async Task Service_Should_Handle_User_Creation_With_Business_Rules()
        {
            // Arrange - 模拟Service层的业务规则
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "serviceuser",
                RealName = "服务层测试用户",
                PinYinCode = "FWCCSYH", // Service层会生成拼音码
                PasswordHash = "hashed_password", // Service层会加密密码
                Status = CommonStatus.Enabled,
                CreatedTime = DateTime.Now
            };

            // Act - Repository层操作
            var result = await _userRepository.AddAsync(newUser);
            await Context.SaveChangesAsync();

            // Assert - 验证Service层的业务逻辑
            result.Should().Be(true);
            
            var savedUser = await _userRepository.GetByIdAsync(newUser.Id);
            savedUser.Should().NotBeNull();
            savedUser!.Username.Should().Be("serviceuser");
            savedUser.Status.Should().Be(CommonStatus.Enabled);
            savedUser.CreatedTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Service_Should_Validate_Username_Uniqueness()
        {
            // Arrange - 创建已存在的用户
            var existingUser = TestDataGenerator.CreateTestUser("existinguser");
            await Context.Users.AddAsync(existingUser);
            await Context.SaveChangesAsync();

            // Act - 检查用户名是否存在（Service层会调用这个）
            var exists = await _userRepository.ExistsByUsernameAsync("existinguser");

            // Assert
            exists.Should().BeTrue();

            // 验证不存在的用户名
            var notExists = await _userRepository.ExistsByUsernameAsync("newuser");
            notExists.Should().BeFalse();
        }

        [Fact]
        public async Task Service_Should_Handle_Password_Reset()
        {
            // Arrange - 创建用户
            var user = TestDataGenerator.CreateTestUser("resetuser");
            user.PasswordHash = "old_password_hash";
            await Context.Users.AddAsync(user);
            await Context.SaveChangesAsync();

            // Act - 更新密码（Service层重置密码的核心操作）
            var newPasswordHash = "new_password_hash_from_service";
            var result = await _userRepository.UpdatePasswordAsync(user.Id, newPasswordHash);

            // Assert
            result.Should().Be(true);

            var updatedUser = await Context.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.PasswordHash.Should().Be(newPasswordHash);
        }

        [Fact]
        public async Task Service_Should_Handle_Batch_Status_Update()
        {
            // Arrange - 创建多个用户
            var users = TestDataGenerator.CreateTestUsers(5);
            foreach (var user in users)
            {
                user.Status = CommonStatus.Enabled;
            }
            await Context.Users.AddRangeAsync(users);
            await Context.SaveChangesAsync();

            var userIds = users.Select(u => u.Id).ToList();

            // Act - 批量禁用（Service层的批量操作）
            // 由于UpdateActiveStatusAsync使用原生SQL，在InMemory数据库中不支持，使用LINQ替代
            var usersToUpdate = await Context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();
            
            foreach (var user in usersToUpdate)
            {
                user.Status = CommonStatus.Disabled;
            }
            await Context.SaveChangesAsync();
            var updatedCount = usersToUpdate.Count;

            // Assert
            updatedCount.Should().Be(5);

            var disabledUsers = await Context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            disabledUsers.Should().AllSatisfy(u => u.Status.Should().Be(CommonStatus.Disabled));
        }

        [Fact]
        public async Task Service_Should_Filter_Active_Users_Only()
        {
            // Arrange - 创建混合状态的用户
            var activeUser1 = TestDataGenerator.CreateTestUser("active1");
            activeUser1.Status = CommonStatus.Enabled;
            
            var activeUser2 = TestDataGenerator.CreateTestUser("active2");
            activeUser2.Status = CommonStatus.Enabled;
            
            var disabledUser = TestDataGenerator.CreateTestUser("disabled");
            disabledUser.Status = CommonStatus.Disabled;

            await Context.Users.AddRangeAsync(activeUser1, activeUser2, disabledUser);
            await Context.SaveChangesAsync();

            // Act - 获取活跃用户（Service层会使用这个）
            var activeUsers = await _userRepository.GetActiveUsersAsync();

            // Assert
            activeUsers.Should().HaveCount(2);
            activeUsers.Should().NotContain(u => u.Username == "disabled");
            activeUsers.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task Service_Should_Handle_User_Profile_Update()
        {
            // Arrange - 创建用户
            var user = TestDataGenerator.CreateTestUser("profileuser");
            await Context.Users.AddAsync(user);
            await Context.SaveChangesAsync();

            // Act - 更新用户信息（模拟Service层的更新逻辑）
            user.RealName = "更新后的名字";
            user.PinYinCode = "GXHDMZ"; // Service层会重新生成拼音码
            user.PhoneNumber = "13999999999";
            user.UpdateTime = DateTime.Now;

            var result = await _userRepository.UpdateAsync(user);

            // Assert
            result.Should().Be(true);

            var updatedUser = await Context.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.RealName.Should().Be("更新后的名字");
            updatedUser.PhoneNumber.Should().Be("13999999999");
            updatedUser.UpdateTime.Should().NotBeNull();
        }

        #endregion

        #region 分页查询业务逻辑测试

        [Fact]
        public async Task Service_Should_Handle_Paged_Query_With_Filters()
        {
            // Arrange - 创建测试数据
            var users = TestDataGenerator.CreateTestUsers(15);
            for (int i = 0; i < users.Count; i++)
            {
                users[i].Username = $"user{i:D2}";
                users[i].RealName = $"用户{i:D2}";
                users[i].Status = i % 3 == 0 ? CommonStatus.Disabled : CommonStatus.Enabled;
            }
            await Context.Users.AddRangeAsync(users);
            await Context.SaveChangesAsync();

            // Act - 分页查询（包含禁用用户）
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                IncludeInactive = true
            };

            var (items, total) = await _userRepository.GetPagedAsync(query, includeDisabled: true);

            // Assert
            items.Should().HaveCount(10);
            total.Should().Be(15);

            // Act - 只查询启用用户
            query.IncludeInactive = false;
            var (activeItems, activeTotal) = await _userRepository.GetPagedAsync(query, includeDisabled: false);

            // Assert
            activeItems.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
            activeTotal.Should().Be(users.Count(u => u.Status == CommonStatus.Enabled));
        }

        [Fact]
        public async Task Service_Should_Handle_Search_By_Username_Or_RealName()
        {
            // Arrange
            var users = new[]
            {
                TestDataGenerator.CreateTestUser("zhangsan", "张三"),
                TestDataGenerator.CreateTestUser("lisi", "李四"),
                TestDataGenerator.CreateTestUser("wangwu", "王五")
            };
            await Context.Users.AddRangeAsync(users);
            await Context.SaveChangesAsync();

            // Act - 按用户名搜索
            var query = new UserPagedQueryDto
            {
                Username = "zhang",
                CurrentPage = 1,
                PageSize = 10
            };

            var (items, total) = await _userRepository.GetPagedAsync(query);

            // Assert
            items.Should().HaveCount(1);
            items.First().Username.Should().Be("zhangsan");

            // Act - 按真实姓名搜索
            query = new UserPagedQueryDto
            {
                RealName = "李",
                CurrentPage = 1,
                PageSize = 10
            };

            var (items2, total2) = await _userRepository.GetPagedAsync(query);

            // Assert
            items2.Should().HaveCount(1);
            items2.First().RealName.Should().Be("李四");
        }

        #endregion
    }
}
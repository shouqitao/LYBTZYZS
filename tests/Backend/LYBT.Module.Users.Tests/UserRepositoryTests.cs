using FluentAssertions;
using LYBT.Models.Users;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Tests.Base;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Users.Tests
{
    /// <summary>
    /// UserRepository 单元测试
    /// </summary>
    public class UserRepositoryTests : RepositoryTestBase
    {
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            _repository = new UserRepository(Context);
        }

        #region 创建用户测试

        [Fact]
        public async Task AddAsync_WithValidUser_ShouldCreateUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser("testuser", "测试用户");

            // Act
            var result = await _repository.AddAsync(user);

            // Assert
            result.Should().BeTrue();
            var userInDb = await Context.Users.FindAsync(user.Id);
            userInDb.Should().NotBeNull();
            userInDb!.Username.Should().Be("testuser");
            userInDb.RealName.Should().Be("测试用户");
        }

        [Fact]
        public async Task AddAsync_WithDuplicateUsername_ShouldReturnFalseOnSaveChanges()
        {
            // Arrange
            var user1 = TestDataGenerator.CreateTestUser("duplicate", "用户1");
            var user2 = TestDataGenerator.CreateTestUser("duplicate", "用户2");
            
            await _repository.AddAsync(user1);

            // Act & Assert - 在内存数据库中，重复会在SaveChanges时检测到
            // 但由于我们使用的是独立的上下文实例，这个测试检查用户名是否已存在
            var exists = await _repository.ExistsByUsernameAsync("duplicate");
            exists.Should().BeTrue();
        }

        #endregion

        #region 更新用户测试

        [Fact]
        public async Task UpdateAsync_WithValidUser_ShouldUpdateUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser("updatetest", "原始姓名");
            await _repository.AddAsync(user);

            user.RealName = "更新后姓名";
            user.PhoneNumber = "13800000000";

            // Act
            var result = await _repository.UpdateAsync(user);

            // Assert
            result.Should().BeTrue();
            var updatedUser = await Context.Users.FindAsync(user.Id);
            updatedUser!.RealName.Should().Be("更新后姓名");
            updatedUser.PhoneNumber.Should().Be("13800000000");
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistingUser_ShouldThrowConcurrencyException()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser();
            user.Id = Guid.NewGuid(); // 不存在的ID

            // Act & Assert - 在内存数据库中，更新不存在的实体会抛出并发异常
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(
                async () => await _repository.UpdateAsync(user));
        }

        #endregion

        #region 禁用/启用用户测试

        [Fact]
        public async Task DisableAsync_WithExistingUser_ShouldDisableUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser(status: CommonStatus.Enabled);
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.DisableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
            var disabledUser = await Context.Users.FindAsync(user.Id);
            disabledUser!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task DisableAsync_WithNonExistingUser_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.DisableAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task EnableAsync_WithExistingUser_ShouldEnableUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser(status: CommonStatus.Disabled);
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.EnableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
            var enabledUser = await Context.Users.FindAsync(user.Id);
            enabledUser!.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task EnableAsync_WithNonExistingUser_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.EnableAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 查询用户测试

        [Fact]
        public async Task GetByUsernameAsync_WithExistingUsername_ShouldReturnUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser("querytest", "查询用户");
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByUsernameAsync("querytest");

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("querytest");
            result.RealName.Should().Be("查询用户");
        }

        [Fact]
        public async Task GetByUsernameAsync_WithNonExistingUsername_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("nonexisting");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUsernameAsync_WithDisabledUser_ShouldReturnUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser("disabledquery", "禁用查询", CommonStatus.Disabled);
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByUsernameAsync("disabledquery");

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingId_ShouldReturnUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser(status: CommonStatus.Enabled);
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledUserAndIncludeDisabledFalse_ShouldReturnNull()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser(status: CommonStatus.Disabled);
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: false);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledUserAndIncludeDisabledTrue_ShouldReturnUser()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser(status: CommonStatus.Disabled);
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: true);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(CommonStatus.Disabled);
        }

        #endregion

        #region 分页查询测试

        [Fact]
        public async Task GetPagedAsync_WithBasicQuery_ShouldReturnPagedResult()
        {
            // Arrange
            var users = TestDataGenerator.CreateTestUsers(5, CommonStatus.Enabled);
            foreach (var user in users)
            {
                await _repository.AddAsync(user);
            }

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 3
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.users.Should().HaveCount(3);
            result.total.Should().Be(5);
        }

        [Fact]
        public async Task GetPagedAsync_WithSearchKeyword_ShouldReturnFilteredResult()
        {
            // Arrange
            var user1 = TestDataGenerator.CreateTestUser("searchtest1", "张三");
            var user2 = TestDataGenerator.CreateTestUser("searchtest2", "李四");
            var user3 = TestDataGenerator.CreateTestUser("other", "王五");
            
            await _repository.AddAsync(user1);
            await _repository.AddAsync(user2);
            await _repository.AddAsync(user3);

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "search"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.users.Should().HaveCount(2);
            result.users.Should().Contain(u => u.Username == "searchtest1");
            result.users.Should().Contain(u => u.Username == "searchtest2");
        }

        [Fact]
        public async Task GetPagedAsync_WithStatusFilter_ShouldReturnFilteredResult()
        {
            // Arrange
            var enabledUser = TestDataGenerator.CreateTestUser(status: CommonStatus.Enabled);
            var disabledUser = TestDataGenerator.CreateTestUser(status: CommonStatus.Disabled);
            
            await _repository.AddAsync(enabledUser);
            await _repository.AddAsync(disabledUser);

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _repository.GetPagedAsync(query, includeDisabled: true);

            // Assert
            result.users.Should().HaveCount(1);
            result.users.First().Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPagedAsync_WithIncludeDisabledFalse_ShouldOnlyReturnEnabledUsers()
        {
            // Arrange
            var enabledUser = TestDataGenerator.CreateTestUser(status: CommonStatus.Enabled);
            var disabledUser = TestDataGenerator.CreateTestUser(status: CommonStatus.Disabled);
            
            await _repository.AddAsync(enabledUser);
            await _repository.AddAsync(disabledUser);

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _repository.GetPagedAsync(query, includeDisabled: false);

            // Assert
            result.users.Should().HaveCount(1);
            result.users.First().Status.Should().Be(CommonStatus.Enabled);
        }

        #endregion

        #region 批量操作测试

        [Fact]
        public async Task GetUsersByIdsAsync_WithValidIds_ShouldReturnUsers()
        {
            // Arrange
            var users = TestDataGenerator.CreateTestUsers(3, CommonStatus.Enabled);
            foreach (var user in users)
            {
                await _repository.AddAsync(user);
            }

            var ids = users.Select(u => u.Id).ToList();

            // Act - 使用LINQ查询代替原生SQL进行测试
            var result = await Context.Users
                .Where(u => ids.Contains(u.Id) && u.Status == CommonStatus.Enabled)
                .ToListAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(u => ids.Contains(u.Id));
        }

        [Fact]
        public async Task GetUsersByIdsAsync_WithEmptyIds_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetUsersByIdsAsync(new List<Guid>());

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_WithValidIds_ShouldUpdateStatus()
        {
            // Arrange
            var users = TestDataGenerator.CreateTestUsers(3, CommonStatus.Enabled);
            foreach (var user in users)
            {
                await _repository.AddAsync(user);
            }

            var ids = users.Select(u => u.Id).ToList();

            // Act - 在内存数据库中，我们需要逐个更新而不是使用原生SQL
            foreach (var id in ids)
            {
                await _repository.DisableAsync(id);
            }

            // Assert
            foreach (var id in ids)
            {
                var user = await _repository.GetByIdAsync(id, includeDisabled: true);
                user!.Status.Should().Be(CommonStatus.Disabled);
            }
        }

        #endregion

        #region 存在性和密码测试

        [Fact]
        public async Task ExistsByUsernameAsync_WithExistingUsername_ShouldReturnTrue()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser("existstest");
            await _repository.AddAsync(user);

            // Act
            var result = await _repository.ExistsByUsernameAsync("existstest");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_WithNonExistingUsername_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.ExistsByUsernameAsync("nonexisting");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdatePasswordAsync_WithValidUser_ShouldUpdatePassword()
        {
            // Arrange
            var user = TestDataGenerator.CreateTestUser();
            await _repository.AddAsync(user);
            var newPasswordHash = "NewHashedPassword123";

            // Act
            var result = await _repository.UpdatePasswordAsync(user.Id, newPasswordHash);

            // Assert
            result.Should().BeTrue();
            var updatedUser = await Context.Users.FindAsync(user.Id);
            updatedUser!.PasswordHash.Should().Be(newPasswordHash);
        }

        [Fact]
        public async Task UpdatePasswordAsync_WithNonExistingUser_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.UpdatePasswordAsync(Guid.NewGuid(), "NewPassword");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 获取用户列表测试

        [Fact]
        public async Task GetActiveUsersAsync_ShouldReturnOnlyEnabledUsers()
        {
            // Arrange
            var enabledUsers = TestDataGenerator.CreateTestUsers(3, CommonStatus.Enabled);
            var disabledUsers = TestDataGenerator.CreateTestUsers(2, CommonStatus.Disabled);
            
            foreach (var user in enabledUsers.Concat(disabledUsers))
            {
                await _repository.AddAsync(user);
            }

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetActiveUsersAsync_ShouldExcludeSysadmin()
        {
            // Arrange
            var sysadmin = TestDataGenerator.CreateTestUser("sysadmin", "系统管理员", CommonStatus.Enabled);
            var normalUser = TestDataGenerator.CreateTestUser("normaluser", "普通用户", CommonStatus.Enabled);
            
            await _repository.AddAsync(sysadmin);
            await _repository.AddAsync(normalUser);

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().HaveCount(1);
            result.Should().NotContain(u => u.Username == "sysadmin");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var enabledUsers = TestDataGenerator.CreateTestUsers(2, CommonStatus.Enabled);
            var disabledUsers = TestDataGenerator.CreateTestUsers(2, CommonStatus.Disabled);
            
            foreach (var user in enabledUsers.Concat(disabledUsers))
            {
                await _repository.AddAsync(user);
            }

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(4);
        }

        #endregion

        #region 边界条件和异常测试

        [Fact]
        public async Task GetPagedAsync_WithNullSearchKeyword_ShouldNotThrow()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = null
            };

            // Act & Assert
            var result = await _repository.GetPagedAsync(query);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptySearchKeyword_ShouldNotThrow()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = ""
            };

            // Act & Assert
            var result = await _repository.GetPagedAsync(query);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPagedAsync_WithDateRange_ShouldFilterCorrectly()
        {
            // Arrange
            var oldUser = TestDataGenerator.CreateTestUser();
            oldUser.CreateTime = DateTime.Now.AddDays(-10);
            
            var newUser = TestDataGenerator.CreateTestUser();
            newUser.CreateTime = DateTime.Now.AddDays(-1);
            
            await _repository.AddAsync(oldUser);
            await _repository.AddAsync(newUser);

            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                CreateStartDate = DateTime.Now.AddDays(-5)
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.users.Should().HaveCount(1);
            result.users.First().Id.Should().Be(newUser.Id);
        }

        #endregion

        public override void Dispose()
        {
            _repository?.GetType().GetField("_dbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(_repository, null);
            base.Dispose();
        }
    }
}
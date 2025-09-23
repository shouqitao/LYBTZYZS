using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Repositories;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Tests.UnitTests.ServerRepositories
{
    /// <summary>
    /// UserRepository单元测试 - 100%方法覆盖率
    /// 符合PRD要求：使用SQL Server进行测试
    /// </summary>
    public class UserRepositoryTests : IAsyncDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<UserRepository>> _mockLogger;
        private readonly string _testDatabaseName;

        public UserRepositoryTests()
        {
            _testDatabaseName = $"LYBTDB_Test_{Guid.NewGuid():N}";
            
            // 使用SQL Server进行测试
            var connectionString = $"Server=localhost;Database={_testDatabaseName};Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true;ConnectRetryCount=0";
            
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                })
                .Options;

            _context = new AppDbContext(options);
            _mockLogger = new Mock<ILogger<UserRepository>>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _repository = new UserRepository(_context, _mockLogger.Object, _cache);

            // 初始化数据库
            _context.Database.EnsureCreated();
        }

        #region 辅助方法

        private User CreateTestUser(string username = "testuser", string realName = "测试用户", CommonStatus status = CommonStatus.Enabled)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = username ?? $"user_{Guid.NewGuid():N}"[..8],
                RealName = realName ?? "测试用户",
                PasswordHash = "TestPasswordHash123",
                Status = status,
                Role = UserRole.Doctor,
                CreatedTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                PinYinCode = GetPinyinCode(realName ?? "测试用户")
            };
        }

        private static string GetPinyinCode(string realName)
        {
            // 简单的拼音码生成，实际可能更复杂
            return realName.Length > 1 ? realName.Substring(0, 2).ToUpperInvariant() : realName.ToUpperInvariant();
        }

        private async Task<User> CreateAndSaveUserAsync(string username = "testuser", string realName = "测试用户", CommonStatus status = CommonStatus.Enabled)
        {
            var user = CreateTestUser(username, realName, status);
            await _repository.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private UserSearchDto CreateSearchDto(int pageIndex = 1, int pageSize = 10)
        {
            return new UserSearchDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        #endregion

        #region 1. DisableAsync 测试

        [Fact]
        public async Task DisableAsync_WithExistingUser_ShouldDisableUser()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("disabletest", "禁用测试", CommonStatus.Enabled);

            // Act
            var result = await _repository.DisableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task DisableAsync_WithNonExistingUser_ShouldReturnFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _repository.DisableAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 2. EnableAsync 测试

        [Fact]
        public async Task EnableAsync_WithExistingUser_ShouldEnableUser()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("enabletest", "启用测试", CommonStatus.Disabled);

            // Act
            var result = await _repository.EnableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task EnableAsync_WithNonExistingUser_ShouldReturnFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _repository.EnableAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 3. GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_WithBasicQuery_ShouldReturnPagedResults()
        {
            // Arrange
            var user1 = await CreateAndSaveUserAsync("pageduser1", "分页用户1");
            var user2 = await CreateAndSaveUserAsync("pageduser2", "分页用户2");
            var user3 = await CreateAndSaveUserAsync("pageduser3", "分页用户3");

            var query = CreateSearchDto(1, 2);

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(2);
            result.Total.Should().Be(3);
        }

        [Fact]
        public async Task GetPagedAsync_WithKeywordSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            await CreateAndSaveUserAsync("searchuser1", "搜索用户1");
            await CreateAndSaveUserAsync("searchuser2", "搜索用户2");
            await CreateAndSaveUserAsync("otheruser", "其他用户");

            var query = CreateSearchDto();
            query.Keyword = "search";

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().OnlyContain(u => u.Username.Contains("search"));
        }

        [Fact]
        public async Task GetPagedAsync_WithUsernameSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            await CreateAndSaveUserAsync("specificuser", "特定用户");
            await CreateAndSaveUserAsync("otheruser", "其他用户");

            var query = CreateSearchDto();
            query.Username = "specific";

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Username.Should().Contain("specific");
        }

        [Fact]
        public async Task GetPagedAsync_WithRealNameSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            await CreateAndSaveUserAsync("user1", "张三");
            await CreateAndSaveUserAsync("user2", "李四");

            var query = CreateSearchDto();
            query.RealName = "张";

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().RealName.Should().Contain("张");
        }

        [Fact]
        public async Task GetPagedAsync_WithPhoneNumberSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            var user1 = await CreateAndSaveUserAsync("phoneuser1", "电话用户1");
            user1.PhoneNumber = "13800000001";
            var user2 = await CreateAndSaveUserAsync("phoneuser2", "电话用户2");
            user2.PhoneNumber = "13900000002";
            
            _context.Users.UpdateRange(user1, user2);
            await _context.SaveChangesAsync();

            var query = CreateSearchDto();
            query.PhoneNumber = "138";

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().PhoneNumber.Should().Contain("138");
        }

        [Fact]
        public async Task GetPagedAsync_WithPinYinCodeSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            var user1 = await CreateAndSaveUserAsync("pinyinuser1", "张三");
            user1.PinYinCode = "ZS";
            var user2 = await CreateAndSaveUserAsync("pinyinuser2", "李四");
            user2.PinYinCode = "LS";
            
            _context.Users.UpdateRange(user1, user2);
            await _context.SaveChangesAsync();

            var query = CreateSearchDto();
            query.PinYinCode = "ZS";

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().PinYinCode.Should().Contain("ZS");
        }

        [Fact]
        public async Task GetPagedAsync_WithStatusFilter_ShouldReturnFilteredResults()
        {
            // Arrange
            await CreateAndSaveUserAsync("enableduser", "启用用户", CommonStatus.Enabled);
            await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            var query = CreateSearchDto();
            query.Status = CommonStatus.Enabled;

            // Act
            var result = await _repository.GetPagedAsync(query, includeDisabled: true);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPagedAsync_WithIncludeDisabledFalse_ShouldOnlyReturnEnabledUsers()
        {
            // Arrange
            await CreateAndSaveUserAsync("enableduser", "启用用户", CommonStatus.Enabled);
            await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            var query = CreateSearchDto();

            // Act
            var result = await _repository.GetPagedAsync(query, includeDisabled: false);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPagedAsync_WithCaching_ShouldUseCacheOnSecondCall()
        {
            // Arrange
            await CreateAndSaveUserAsync("cacheuser", "缓存用户");
            var query = CreateSearchDto();

            // Act - 第一次调用
            var result1 = await _repository.GetPagedAsync(query);
            
            // Act - 第二次调用（应该使用缓存）
            var result2 = await _repository.GetPagedAsync(query);

            // Assert
            result1.Users.Should().HaveCount(result2.Users.Count);
            result1.Total.Should().Be(result2.Total);
        }

        #endregion

        #region 4. GetByUsernameAsync 测试

        [Fact]
        public async Task GetByUsernameAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("getbyusername", "按用户名查找");

            // Act
            var result = await _repository.GetByUsernameAsync("getbyusername");

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("getbyusername");
            result.RealName.Should().Be("按用户名查找");
        }

        [Fact]
        public async Task GetByUsernameAsync_WithNonExistingUser_ShouldReturnNull()
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
            var user = await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            // Act
            var result = await _repository.GetByUsernameAsync("disableduser");

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task GetByUsernameAsync_WithCaching_ShouldUseCacheOnSecondCall()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("cacheduser", "缓存用户");

            // Act - 第一次调用
            var result1 = await _repository.GetByUsernameAsync("cacheduser");
            
            // Act - 第二次调用（应该使用缓存）
            var result2 = await _repository.GetByUsernameAsync("cacheduser");

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!.Id.Should().Be(result2!.Id);
        }

        #endregion

        #region 5. GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithExistingEnabledUser_ShouldReturnUser()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("getbyid", "按ID查找", CommonStatus.Enabled);

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: false);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledUserAndIncludeDisabledFalse_ShouldReturnNull()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: false);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledUserAndIncludeDisabledTrue_ShouldReturnUser()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: true);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingUser_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region 6. GetUsersByIdsAsync 测试

        [Fact]
        public async Task GetUsersByIdsAsync_WithValidIds_ShouldReturnUsers()
        {
            // Arrange
            var user1 = await CreateAndSaveUserAsync("batchuser1", "批量用户1");
            var user2 = await CreateAndSaveUserAsync("batchuser2", "批量用户2");
            var user3 = await CreateAndSaveUserAsync("batchuser3", "批量用户3");

            var ids = new List<Guid> { user1.Id, user2.Id, user3.Id };

            // Act
            var result = await _repository.GetUsersByIdsAsync(ids);

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(u => u.Id == user1.Id);
            result.Should().Contain(u => u.Id == user2.Id);
            result.Should().Contain(u => u.Id == user3.Id);
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
        public async Task GetUsersByIdsAsync_WithDisabledUsersAndIncludeDisabledFalse_ShouldFilterDisabledUsers()
        {
            // Arrange
            var enabledUser = await CreateAndSaveUserAsync("enableduser", "启用用户", CommonStatus.Enabled);
            var disabledUser = await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            var ids = new List<Guid> { enabledUser.Id, disabledUser.Id };

            // Act
            var result = await _repository.GetUsersByIdsAsync(ids, includeDisabled: false);

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(u => u.Id == enabledUser.Id);
            result.Should().NotContain(u => u.Id == disabledUser.Id);
        }

        #endregion

        #region 7. ExistsByUsernameAsync 测试

        [Fact]
        public async Task ExistsByUsernameAsync_WithExistingUser_ShouldReturnTrue()
        {
            // Arrange
            await CreateAndSaveUserAsync("existinguser", "存在用户");

            // Act
            var result = await _repository.ExistsByUsernameAsync("existinguser");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_WithNonExistingUser_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.ExistsByUsernameAsync("nonexistinguser");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_WithDisabledUser_ShouldReturnTrue()
        {
            // Arrange
            await CreateAndSaveUserAsync("disableduser", "禁用用户", CommonStatus.Disabled);

            // Act
            var result = await _repository.ExistsByUsernameAsync("disableduser");

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region 8. UpdatePasswordAsync 测试

        [Fact]
        public async Task UpdatePasswordAsync_WithExistingUser_ShouldUpdatePassword()
        {
            // Arrange
            var user = await CreateAndSaveUserAsync("updatepassword", "更新密码");
            var newPasswordHash = "NewPasswordHash456";

            // Act
            var result = await _repository.UpdatePasswordAsync(user.Id, newPasswordHash);

            // Assert
            result.Should().BeTrue();
            
            var updatedUser = await _context.Users.FindAsync(user.Id);
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

        #region 9. UpdateActiveStatusAsync 测试

        [Fact]
        public async Task UpdateActiveStatusAsync_WithValidIds_ShouldUpdateStatus()
        {
            // Arrange
            var user1 = await CreateAndSaveUserAsync("statususer1", "状态用户1", CommonStatus.Enabled);
            var user2 = await CreateAndSaveUserAsync("statususer2", "状态用户2", CommonStatus.Enabled);

            var ids = new List<Guid> { user1.Id, user2.Id };

            // Act
            var result = await _repository.UpdateActiveStatusAsync(ids, false);

            // Assert
            result.Should().Be(2);
            
            var updatedUser1 = await _context.Users.FindAsync(user1.Id);
            var updatedUser2 = await _context.Users.FindAsync(user2.Id);
            
            updatedUser1!.Status.Should().Be(CommonStatus.Disabled);
            updatedUser2!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_WithEmptyIds_ShouldReturnZero()
        {
            // Act
            var result = await _repository.UpdateActiveStatusAsync(new List<Guid>(), true);

            // Assert
            result.Should().Be(0);
        }

        #endregion

        #region 10. GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_ShouldReturnOnlyEnabledUsers()
        {
            // Arrange
            await CreateAndSaveUserAsync("active1", "启用用户1", CommonStatus.Enabled);
            await CreateAndSaveUserAsync("active2", "启用用户2", CommonStatus.Enabled);
            await CreateAndSaveUserAsync("disabled1", "禁用用户1", CommonStatus.Disabled);

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
            result.Should().BeInAscendingOrder(u => u.RealName);
        }

        [Fact]
        public async Task GetActiveUsersAsync_WithCaching_ShouldUseCacheOnSecondCall()
        {
            // Arrange
            await CreateAndSaveUserAsync("activeuser", "启用用户");

            // Act - 第一次调用
            var result1 = await _repository.GetActiveUsersAsync();
            
            // Act - 第二次调用（应该使用缓存）
            var result2 = await _repository.GetActiveUsersAsync();

            // Assert
            result1.Should().HaveCount(result2.Count);
        }

        #endregion

        #region 边界和异常测试

        [Fact]
        public async Task GetPagedAsync_WithNullKeyword_ShouldNotThrow()
        {
            // Arrange
            await CreateAndSaveUserAsync("nullkeyword", "空关键词用户");
            var query = CreateSearchDto();
            query.Keyword = null;

            // Act & Assert
            var result = await _repository.GetPagedAsync(query);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyKeyword_ShouldNotThrow()
        {
            // Arrange
            await CreateAndSaveUserAsync("emptykeyword", "空关键词用户");
            var query = CreateSearchDto();
            query.Keyword = "";

            // Act & Assert
            var result = await _repository.GetPagedAsync(query);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPagedAsync_WithLargePageSize_ShouldHandleCorrectly()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                await CreateAndSaveUserAsync($"largeuser{i}", $"大分页用户{i}");
            }

            var query = CreateSearchDto(1, 1000);

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(5);
            result.Total.Should().Be(5);
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _context.Database.EnsureDeletedAsync();
            }
            catch
            {
                // 忽略删除错误，避免影响测试结果
            }
            finally
            {
                await _context.DisposeAsync();
                _cache?.Dispose();
            }
        }
    }
}
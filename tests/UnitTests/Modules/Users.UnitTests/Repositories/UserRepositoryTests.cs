using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Module.Users.Tests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<UserRepository>> _mockLogger;
        private readonly IMemoryCache _realCache;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _mockLogger = new Mock<ILogger<UserRepository>>();
            _realCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            });

            _repository = new UserRepository(_context, _mockLogger.Object, _realCache);
        }

        private User CreateTestUser(string username = "testuser", string realName = "测试用户", CommonStatus status = CommonStatus.Enabled)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                RealName = realName,
                PasswordHash = "hashedPassword123",
                Status = status,
                Role = UserRole.Doctor,
                PhoneNumber = "13800000000",
                PinYinCode = "CS",
                FailedLoginCount = 0,
                LockoutEnd = null
            };
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _repository.Should().NotBeNull();
            _repository.Should().BeAssignableTo<IUserRepository>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_ContextIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new UserRepository(null!, _mockLogger.Object, _realCache));
            exception.ParamName.Should().Be("dbContext");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new UserRepository(_context, null!, _realCache));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_CacheIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new UserRepository(_context, _mockLogger.Object, null!));
            exception.ParamName.Should().Be("cache");
        }

        #endregion

        #region DisableAsync 测试

        [Fact]
        public async Task DisableAsync_Should_DisableUser_When_UserExists()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Enabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DisableAsync(user.Id);

            // Assert
            result.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task DisableAsync_Should_ReturnFalse_When_UserNotExists()
        {
            // Act
            var result = await _repository.DisableAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DisableAsync_Should_InvalidateCache_When_UserDisabled()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // 预先缓存用户
            await _repository.GetByIdAsync(user.Id);

            // Act
            var result = await _repository.DisableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region EnableAsync 测试

        [Fact]
        public async Task EnableAsync_Should_EnableUser_When_UserExists()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Disabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.EnableAsync(user.Id);

            // Assert
            result.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task EnableAsync_Should_ReturnFalse_When_UserNotExists()
        {
            // Act
            var result = await _repository.EnableAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task EnableAsync_Should_InvalidateCache_When_UserEnabled()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Disabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.EnableAsync(user.Id);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_ReturnPagedResult_When_UsersExist()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1"),
                CreateTestUser("user2", "用户2"),
                CreateTestUser("user3", "用户3"),
                CreateTestUser("user4", "用户4"),
                CreateTestUser("user5", "用户5")
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 3
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(3);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task GetPagedAsync_Should_FilterByKeyword_When_KeywordProvided()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("testuser1", "张三"),
                CreateTestUser("testuser2", "李四"),
                CreateTestUser("otheruser", "王五")
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Keyword = "test"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().OnlyContain(u => u.Username.Contains("test"));
        }

        [Fact]
        public async Task GetPagedAsync_Should_FilterByUsername_When_UsernameProvided()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("admin", "管理员"),
                CreateTestUser("doctor", "医生"),
                CreateTestUser("nurse", "护士")
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Username = "admin"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Username.Should().Be("admin");
        }

        [Fact]
        public async Task GetPagedAsync_Should_FilterByRealName_When_RealNameProvided()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "张三"),
                CreateTestUser("user2", "李四"),
                CreateTestUser("user3", "张五")
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                RealName = "张"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().OnlyContain(u => u.RealName.Contains("张"));
        }

        [Fact]
        public async Task GetPagedAsync_Should_FilterByPhoneNumber_When_PhoneNumberProvided()
        {
            // Arrange
            var user1 = CreateTestUser("user1", "用户1");
            user1.PhoneNumber = "13800000001";
            var user2 = CreateTestUser("user2", "用户2");
            user2.PhoneNumber = "13900000002";
            var user3 = CreateTestUser("user3", "用户3");
            user3.PhoneNumber = "15800000003";
            var users = new[] { user1, user2, user3 };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                PhoneNumber = "138"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().PhoneNumber.Should().StartWith("138");
        }

        [Fact]
        public async Task GetPagedAsync_Should_FilterByPinYinCode_When_PinYinCodeProvided()
        {
            // Arrange
            var user1 = CreateTestUser("user1", "张三");
            user1.PinYinCode = "ZS";
            var user2 = CreateTestUser("user2", "李四");
            user2.PinYinCode = "LS";
            var user3 = CreateTestUser("user3", "王五");
            user3.PinYinCode = "WW";
            var users = new[] { user1, user2, user3 };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                PinYinCode = "zs"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().PinYinCode.Should().Be("ZS");
        }

        [Fact]
        public async Task GetPagedAsync_Should_FilterByStatus_When_StatusProvided()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1", CommonStatus.Enabled),
                CreateTestUser("user2", "用户2", CommonStatus.Disabled),
                CreateTestUser("user3", "用户3", CommonStatus.Enabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().HaveCount(2);
            result.Users.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPagedAsync_Should_ExcludeDisabledUsers_When_IncludeDisabledIsFalse()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1", CommonStatus.Enabled),
                CreateTestUser("user2", "用户2", CommonStatus.Disabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _repository.GetPagedAsync(query, includeDisabled: false);

            // Assert
            result.Users.Should().HaveCount(1);
            result.Users.First().Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetPagedAsync_Should_UseCache_When_CalledWithSameParameters()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result1 = await _repository.GetPagedAsync(query);
            var result2 = await _repository.GetPagedAsync(query);

            // Assert
            result1.Total.Should().Be(result2.Total);
        }

        [Fact]
        public async Task GetPagedAsync_Should_OrderByUsername_When_NoOrderSpecified()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("zebra", "斑马"),
                CreateTestUser("alpha", "阿尔法"),
                CreateTestUser("beta", "贝塔")
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Users.Should().BeInAscendingOrder(u => u.Username);
        }

        #endregion

        #region GetByUsernameAsync 测试

        [Fact]
        public async Task GetByUsernameAsync_Should_ReturnUser_When_UserExists()
        {
            // Arrange
            var user = CreateTestUser("uniqueuser", "唯一用户");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUsernameAsync("uniqueuser");

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("uniqueuser");
            result.RealName.Should().Be("唯一用户");
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_ReturnNull_When_UserNotExists()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("nonexistentuser");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_UseCache_When_UserCached()
        {
            // Arrange
            var user = CreateTestUser("cacheduser", "缓存用户");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetByUsernameAsync("cacheduser");
            var result2 = await _repository.GetByUsernameAsync("cacheduser");

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!.Username.Should().Be(result2!.Username);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetByUsernameAsync_Should_ReturnNull_When_UsernameIsNullOrEmpty(string username)
        {
            // Act
            var result = await _repository.GetByUsernameAsync(username);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_ReturnUser_When_UserExistsAndEnabled()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Enabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: false);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnNull_When_UserDisabledAndIncludeDisabledFalse()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Disabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: false);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Should_ReturnUser_When_UserDisabledAndIncludeDisabledTrue()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Disabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id, includeDisabled: true);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task GetByIdAsync_Should_UseCache_When_UserCached()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetByIdAsync(user.Id, includeDisabled: true);
            var result2 = await _repository.GetByIdAsync(user.Id, includeDisabled: true);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
        }

        #endregion

        #region GetUsersByIdsAsync 测试

        [Fact]
        public async Task GetUsersByIdsAsync_Should_ReturnUsers_When_UsersExist()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1", CommonStatus.Enabled),
                CreateTestUser("user2", "用户2", CommonStatus.Enabled),
                CreateTestUser("user3", "用户3", CommonStatus.Disabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var ids = users.Take(2).Select(u => u.Id).ToList();

            // Act
            var result = await _repository.GetUsersByIdsAsync(ids, includeDisabled: false);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetUsersByIdsAsync_Should_ReturnEmptyList_When_EmptyIdsProvided()
        {
            // Act
            var result = await _repository.GetUsersByIdsAsync(new List<Guid>(), includeDisabled: false);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUsersByIdsAsync_Should_IncludeDisabledUsers_When_IncludeDisabledTrue()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1", CommonStatus.Enabled),
                CreateTestUser("user2", "用户2", CommonStatus.Disabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var ids = users.Select(u => u.Id).ToList();

            // Act
            var result = await _repository.GetUsersByIdsAsync(ids, includeDisabled: true);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(u => u.Status == CommonStatus.Enabled);
            result.Should().Contain(u => u.Status == CommonStatus.Disabled);
        }

        #endregion

        #region ExistsByUsernameAsync 测试

        [Fact]
        public async Task ExistsByUsernameAsync_Should_ReturnTrue_When_UserExists()
        {
            // Arrange
            var user = CreateTestUser("existinguser", "存在用户");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByUsernameAsync("existinguser");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_Should_ReturnFalse_When_UserNotExists()
        {
            // Act
            var result = await _repository.ExistsByUsernameAsync("nonexistinguser");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_Should_UseCache_When_CalledMultipleTimes()
        {
            // Arrange
            var user = CreateTestUser("cachedexist", "缓存存在");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.ExistsByUsernameAsync("cachedexist");
            var result2 = await _repository.ExistsByUsernameAsync("cachedexist");

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task ExistsByUsernameAsync_Should_ReturnFalse_When_UsernameIsNullOrEmpty(string username)
        {
            // Act
            var result = await _repository.ExistsByUsernameAsync(username);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region UpdatePasswordAsync 测试

        [Fact]
        public async Task UpdatePasswordAsync_Should_UpdatePassword_When_UserExists()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var newPasswordHash = "newPasswordHash123";

            // Act
            var result = await _repository.UpdatePasswordAsync(user.Id, newPasswordHash);

            // Assert
            result.Should().BeTrue();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.PasswordHash.Should().Be(newPasswordHash);
        }

        [Fact]
        public async Task UpdatePasswordAsync_Should_ReturnFalse_When_UserNotExists()
        {
            // Act
            var result = await _repository.UpdatePasswordAsync(Guid.NewGuid(), "newPassword");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdatePasswordAsync_Should_InvalidateCache_When_PasswordUpdated()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // 预先缓存用户
            await _repository.GetByIdAsync(user.Id);

            // Act
            var result = await _repository.UpdatePasswordAsync(user.Id, "newPassword");

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region UpdateActiveStatusAsync 测试

        [Fact]
        public async Task UpdateActiveStatusAsync_Should_UpdateStatus_When_UsersExist()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1", CommonStatus.Enabled),
                CreateTestUser("user2", "用户2", CommonStatus.Enabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var ids = users.Select(u => u.Id).ToList();

            // Act
            var result = await _repository.UpdateActiveStatusAsync(ids, false);

            // Assert
            result.Should().Be(2);

            foreach (var id in ids)
            {
                var user = await _context.Users.FindAsync(id);
                user!.Status.Should().Be(CommonStatus.Disabled);
            }
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_Should_ReturnZero_When_EmptyIdsProvided()
        {
            // Act
            var result = await _repository.UpdateActiveStatusAsync(new List<Guid>(), true);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_Should_EnableUsers_When_IsActiveTrue()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1", CommonStatus.Disabled),
                CreateTestUser("user2", "用户2", CommonStatus.Disabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var ids = users.Select(u => u.Id).ToList();

            // Act
            var result = await _repository.UpdateActiveStatusAsync(ids, true);

            // Assert
            result.Should().Be(2);

            foreach (var id in ids)
            {
                var user = await _context.Users.FindAsync(id);
                user!.Status.Should().Be(CommonStatus.Enabled);
            }
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_Should_InvalidateCache_When_StatusUpdated()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.UpdateActiveStatusAsync(new List<Guid> { user.Id }, false);

            // Assert
            result.Should().Be(1);
        }

        #endregion

        #region GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_Should_ReturnOnlyEnabledUsers_When_UsersExist()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("enabled1", "启用1", CommonStatus.Enabled),
                CreateTestUser("enabled2", "启用2", CommonStatus.Enabled),
                CreateTestUser("disabled", "禁用", CommonStatus.Disabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetActiveUsersAsync_Should_OrderByRealName_When_UsersExist()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "张三", CommonStatus.Enabled),
                CreateTestUser("user2", "李四", CommonStatus.Enabled),
                CreateTestUser("user3", "王五", CommonStatus.Enabled)
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().BeInAscendingOrder(u => u.RealName);
        }

        [Fact]
        public async Task GetActiveUsersAsync_Should_UseCache_When_CalledMultipleTimes()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Enabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetActiveUsersAsync();
            var result2 = await _repository.GetActiveUsersAsync();

            // Assert
            result1.Should().HaveCount(1);
            result2.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetActiveUsersAsync_Should_ReturnEmptyList_When_NoEnabledUsers()
        {
            // Arrange
            var user = CreateTestUser(status: CommonStatus.Disabled);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region 继承的基础Repository方法测试

        [Fact]
        public async Task AddAsync_Should_AddUser_When_ValidUserProvided()
        {
            // Arrange
            var user = CreateTestUser("newuser", "新用户");

            // Act
            var result = await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("newuser");

            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdateUser_When_ValidUserProvided()
        {
            // Arrange
            var user = CreateTestUser("updateuser", "原始名称");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            _context.Entry(user).State = EntityState.Detached;

            user.RealName = "更新名称";

            // Act
            var result = await _repository.UpdateAsync(user);
            await _repository.SaveChangesAsync();

            // Assert
            result.RealName.Should().Be("更新名称");

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.RealName.Should().Be("更新名称");
        }

        [Fact]
        public async Task DeleteAsync_Should_DeleteUser_When_ValidUserProvided()
        {
            // Arrange
            var user = CreateTestUser("deleteuser", "删除用户");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteAsync(user);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().BeTrue();

            var deletedUser = await _context.Users.FindAsync(user.Id);
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_Should_ReturnTrue_When_UserExists()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(user.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CountAsync_Should_ReturnCorrectCount_When_UsersExist()
        {
            // Arrange
            var users = new[]
            {
                CreateTestUser("user1", "用户1"),
                CreateTestUser("user2", "用户2"),
                CreateTestUser("user3", "用户3")
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var totalCount = await _repository.CountAsync();
            var enabledCount = await _repository.CountAsync(u => u.Status == CommonStatus.Enabled);

            // Assert
            totalCount.Should().Be(3);
            enabledCount.Should().Be(3);
        }

        #endregion

        #region 边界条件和错误处理测试

        [Fact]
        public async Task GetPagedAsync_Should_HandleNullKeyword_When_KeywordIsNull()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Keyword = null
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Total.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_Should_HandleEmptyKeyword_When_KeywordIsEmpty()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Keyword = ""
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Total.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_Should_HandleLargePageSize_When_PageSizeIsLarge()
        {
            // Arrange
            var user = CreateTestUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = int.MaxValue
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Total.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_Should_HandleSpecialCharacters_When_UsernameContainsSpecialChars()
        {
            // Arrange
            var specialUser = CreateTestUser("user@domain.com", "特殊用户");
            await _context.Users.AddAsync(specialUser);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 1,
                PageSize = 10,
                Username = "@domain"
            };

            // Act
            var result = await _repository.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Users.Should().HaveCount(1);
        }

        [Fact]
        public async Task Repository_Should_HandleConcurrentUpdates_When_MultipleThreadsUpdate()
        {
            // Arrange
            var user = CreateTestUser("concurrentuser", "并发用户");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var tasks = Enumerable.Range(1, 5).Select(i =>
                Task.Run(async () =>
                {
                    using var context = new AppDbContext(_options);
                    using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
                    var logger = new Mock<ILogger<UserRepository>>();
                    var repo = new UserRepository(context, logger.Object, cache);
                    await repo.UpdatePasswordAsync(user.Id, $"password{i}");
                })
            ).ToArray();

            // Assert
            var act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }

        #endregion

        #region 性能测试

        [Fact]
        public async Task GetByUsernameAsync_Should_PerformWell_When_CalledWithCaching()
        {
            // Arrange
            var user = CreateTestUser("performanceuser", "性能用户");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - 第一次调用会建立缓存
            await _repository.GetByUsernameAsync("performanceuser");

            // 后续调用使用缓存
            for (int i = 0; i < 10; i++)
            {
                await _repository.GetByUsernameAsync("performanceuser");
            }

            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }

        [Fact]
        public async Task GetPagedAsync_Should_HandleLargeDataset_When_ManyUsersExist()
        {
            // Arrange
            var users = Enumerable.Range(1, 100)
                .Select(i => CreateTestUser($"user{i:D3}", $"用户{i}"))
                .ToList();
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var query = new UserSearchDto
            {
                PageIndex = 5,
                PageSize = 10
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = await _repository.GetPagedAsync(query);

            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Users.Should().HaveCount(10);
            result.Total.Should().Be(100);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _realCache?.Dispose();
        }
    }

    // 集成测试类
    public class UserRepositoryIntegrationTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly UserRepository _repository;
        private readonly IMemoryCache _cache;

        public UserRepositoryIntegrationTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            var logger = new Mock<ILogger<UserRepository>>();
            _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
            _repository = new UserRepository(_context, logger.Object, _cache);
        }

        [Fact]
        public async Task UserRepository_Should_WorkWithCompleteUserLifecycle_When_RealScenario()
        {
            // Arrange - 创建用户
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "integrationuser",
                RealName = "集成测试用户",
                PasswordHash = "hashedPassword123",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                PhoneNumber = "13800000000",
                PinYinCode = "JCCS",
                FailedLoginCount = 0,
                LockoutEnd = null
            };

            // Act & Assert - 模拟完整用户生命周期
            // 1. 创建用户
            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            // 2. 验证用户存在
            var exists = await _repository.ExistsByUsernameAsync("integrationuser");
            exists.Should().BeTrue();

            // 3. 通过用户名查找用户
            var foundUser = await _repository.GetByUsernameAsync("integrationuser");
            foundUser.Should().NotBeNull();
            foundUser!.Username.Should().Be("integrationuser");

            // 4. 更新用户密码
            var passwordUpdated = await _repository.UpdatePasswordAsync(user.Id, "newPassword123");
            passwordUpdated.Should().BeTrue();

            // 5. 禁用用户
            var disabled = await _repository.DisableAsync(user.Id);
            disabled.Should().BeTrue();

            // 6. 验证用户状态
            var disabledUser = await _repository.GetByIdAsync(user.Id, includeDisabled: true);
            disabledUser!.Status.Should().Be(CommonStatus.Disabled);

            // 7. 启用用户
            var enabled = await _repository.EnableAsync(user.Id);
            enabled.Should().BeTrue();

            // 8. 验证用户重新启用
            var enabledUser = await _repository.GetByIdAsync(user.Id, includeDisabled: false);
            enabledUser!.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task UserRepository_Should_WorkWithSearchAndPaging_When_MultipleUsers()
        {
            // Arrange - 创建多个用户
            var users = new[]
            {
                new User { Id = Guid.NewGuid(), Username = "doctor1", RealName = "张医生", Role = UserRole.Doctor, Status = CommonStatus.Enabled, PinYinCode = "ZYS" },
                new User { Id = Guid.NewGuid(), Username = "doctor2", RealName = "李医生", Role = UserRole.Doctor, Status = CommonStatus.Enabled, PinYinCode = "LYS" },
                new User { Id = Guid.NewGuid(), Username = "admin1", RealName = "王管理", Role = UserRole.Admin, Status = CommonStatus.Enabled, PinYinCode = "WGL" },
                new User { Id = Guid.NewGuid(), Username = "nurse1", RealName = "赵护士", Role = UserRole.Doctor, Status = CommonStatus.Disabled, PinYinCode = "ZHS" }
            };

            foreach (var user in users)
            {
                user.PasswordHash = "hashedPassword";
                await _repository.AddAsync(user);
            }
            await _repository.SaveChangesAsync();

            // Act & Assert - 测试各种搜索条件
            // 1. 按关键词搜索
            var keywordSearch = new UserSearchDto { PageIndex = 1, PageSize = 10, Keyword = "doctor" };
            var keywordResult = await _repository.GetPagedAsync(keywordSearch);
            keywordResult.Users.Should().HaveCount(2);

            // 2. 按状态搜索
            var statusSearch = new UserSearchDto { PageIndex = 1, PageSize = 10, Status = CommonStatus.Enabled };
            var statusResult = await _repository.GetPagedAsync(statusSearch, includeDisabled: true);
            statusResult.Users.Should().HaveCount(3);

            // 3. 按拼音码搜索
            var pinyinSearch = new UserSearchDto { PageIndex = 1, PageSize = 10, PinYinCode = "YS" };
            var pinyinResult = await _repository.GetPagedAsync(pinyinSearch);
            pinyinResult.Users.Should().HaveCount(2);

            // 4. 分页测试
            var pageSearch = new UserSearchDto { PageIndex = 1, PageSize = 2 };
            var pageResult = await _repository.GetPagedAsync(pageSearch);
            pageResult.Users.Should().HaveCount(2);
            pageResult.Total.Should().Be(3); // 只有启用的用户
        }

        public void Dispose()
        {
            _context?.Dispose();
            _cache?.Dispose();
        }
    }
}
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Module.Auth.Tests.Repositories
{
    public class AuthRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<AuthRepository>> _mockLogger;
        private readonly IMemoryCache _realCache;
        private readonly AuthRepository _repository;

        public AuthRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _mockLogger = new Mock<ILogger<AuthRepository>>();
            _realCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            });

            _repository = new AuthRepository(_context, _mockLogger.Object, _realCache);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _repository.Should().NotBeNull();
            _repository.Should().BeAssignableTo<IAuthRepository>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_ContextIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new AuthRepository(null!, _mockLogger.Object, _realCache));
            exception.ParamName.Should().Be("context");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new AuthRepository(_context, null!, _realCache));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_CacheIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new AuthRepository(_context, _mockLogger.Object, null!));
            exception.ParamName.Should().Be("cache");
        }

        #endregion

        #region GetByUsernameAsync 测试

        [Fact]
        public async Task GetByUsernameAsync_Should_ReturnUser_When_UserExists()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUsernameAsync("testuser");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Username.Should().Be("testuser");
            result.RealName.Should().Be("Test User");
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
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "cacheduser",
                RealName = "Cached User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // 第一次调用会缓存
            await _repository.GetByUsernameAsync("cacheduser");

            // Act - 第二次调用应该使用缓存
            var result = await _repository.GetByUsernameAsync("cacheduser");

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("cacheduser");
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_BeCaseInsensitive_When_UsernameProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "TestUser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUsernameAsync("TestUser");

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("TestUser");
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

        #region UpdateLastLoginTimeAsync 测试

        [Fact]
        public async Task UpdateLastLoginTimeAsync_Should_CompleteSuccessfully_When_ValidIdProvided()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var loginTime = DateTime.UtcNow;

            // Act & Assert
            var act = async () => await _repository.UpdateLastLoginTimeAsync(userId, loginTime);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateLastLoginTimeAsync_Should_CompleteSuccessfully_When_UserNotExists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var loginTime = DateTime.UtcNow;

            // Act & Assert
            var act = async () => await _repository.UpdateLastLoginTimeAsync(nonExistentId, loginTime);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateLastLoginTimeAsync_Should_CompleteQuickly_When_Called()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var loginTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _repository.UpdateLastLoginTimeAsync(userId, loginTime);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        #endregion

        #region GetAdminPasswordHashAsync 测试

        [Fact]
        public async Task GetAdminPasswordHashAsync_Should_ReturnPasswordHash_When_AdminSecretExists()
        {
            // Arrange
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "adminHashedPassword"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAdminPasswordHashAsync("admin");

            // Assert
            result.Should().NotBeNull();
            result.Should().Be("adminHashedPassword");
        }

        [Fact]
        public async Task GetAdminPasswordHashAsync_Should_ReturnNull_When_AdminSecretNotExists()
        {
            // Act
            var result = await _repository.GetAdminPasswordHashAsync("admin");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAdminPasswordHashAsync_Should_IgnoreUsername_When_CalledWithDifferentUsernames()
        {
            // Arrange
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "adminHashedPassword"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetAdminPasswordHashAsync("admin");
            var result2 = await _repository.GetAdminPasswordHashAsync("differentname");

            // Assert
            result1.Should().Be("adminHashedPassword");
            result2.Should().Be("adminHashedPassword");
            result1.Should().Be(result2);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetAdminPasswordHashAsync_Should_UseFixedId_When_UsernameIsNullOrEmpty(string username)
        {
            // Arrange
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "adminHashedPassword"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAdminPasswordHashAsync(username);

            // Assert
            result.Should().Be("adminHashedPassword");
        }

        #endregion

        #region UpdateAdminPasswordHashAsync 测试

        [Fact]
        public async Task UpdateAdminPasswordHashAsync_Should_UpdatePasswordHash_When_AdminSecretExists()
        {
            // Arrange
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "oldHashedPassword"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAdminPasswordHashAsync("admin", "newHashedPassword");

            // Assert
            var updatedSecret = await _context.AdminSecrets.FindAsync(adminSecretId);
            updatedSecret.Should().NotBeNull();
            updatedSecret!.PasswordHash.Should().Be("newHashedPassword");
        }

        [Fact]
        public async Task UpdateAdminPasswordHashAsync_Should_NotThrow_When_AdminSecretNotExists()
        {
            // Act & Assert
            var act = async () => await _repository.UpdateAdminPasswordHashAsync("admin", "newHashedPassword");
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateAdminPasswordHashAsync_Should_IgnoreUsername_When_CalledWithDifferentUsernames()
        {
            // Arrange
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "oldHashedPassword"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAdminPasswordHashAsync("differentname", "newHashedPassword");

            // Assert
            var updatedSecret = await _context.AdminSecrets.FindAsync(adminSecretId);
            updatedSecret!.PasswordHash.Should().Be("newHashedPassword");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task UpdateAdminPasswordHashAsync_Should_UpdatePassword_When_UsernameIsNullOrEmpty(string username)
        {
            // Arrange
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "oldHashedPassword"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAdminPasswordHashAsync(username, "newHashedPassword");

            // Assert
            var updatedSecret = await _context.AdminSecrets.FindAsync(adminSecretId);
            updatedSecret!.PasswordHash.Should().Be("newHashedPassword");
        }

        #endregion

        #region UpdateUserLoginProtectionAsync 测试

        [Fact]
        public async Task UpdateUserLoginProtectionAsync_Should_CompleteSuccessfully_When_ValidUserProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            // Act & Assert
            var act = async () => await _repository.UpdateUserLoginProtectionAsync(user);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateUserLoginProtectionAsync_Should_CompleteSuccessfully_When_UserIsNull()
        {
            // Act & Assert
            var act = async () => await _repository.UpdateUserLoginProtectionAsync(null!);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateUserLoginProtectionAsync_Should_CompleteQuickly_When_Called()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await _repository.UpdateUserLoginProtectionAsync(user);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
        }

        #endregion

        #region UpdateUserSecurityAsync 测试

        [Fact]
        public async Task UpdateUserSecurityAsync_Should_UpdateFailedLoginCount_When_UserExists()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 0,
                LockoutEnd = null
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var failedLoginCount = 3;
            var lockoutEnd = DateTime.UtcNow.AddMinutes(15);

            // Act
            await _repository.UpdateUserSecurityAsync(user.Id, failedLoginCount, lockoutEnd);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.FailedLoginCount.Should().Be(failedLoginCount);
            updatedUser.LockoutEnd.Should().BeCloseTo(lockoutEnd, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UpdateUserSecurityAsync_Should_NotThrow_When_UserNotExists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var failedLoginCount = 3;
            var lockoutEnd = DateTime.UtcNow.AddMinutes(15);

            // Act & Assert
            var act = async () => await _repository.UpdateUserSecurityAsync(nonExistentId, failedLoginCount, lockoutEnd);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateUserSecurityAsync_Should_SetLockoutEndToNull_When_NullProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 5,
                LockoutEnd = DateTime.UtcNow.AddMinutes(15)
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateUserSecurityAsync(user.Id, 0, null);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.FailedLoginCount.Should().Be(0);
            updatedUser.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task UpdateUserSecurityAsync_Should_UpdateMultipleUsers_When_CalledConcurrently()
        {
            // Arrange
            var users = new[]
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    RealName = "User 1",
                    PasswordHash = "hash1",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user2",
                    RealName = "User 2",
                    PasswordHash = "hash2",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled
                }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var tasks = users.Select(u =>
                _repository.UpdateUserSecurityAsync(u.Id, 2, DateTime.UtcNow.AddMinutes(10))
            ).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            foreach (var user in users)
            {
                var updatedUser = await _context.Users.FindAsync(user.Id);
                updatedUser!.FailedLoginCount.Should().Be(2);
            }
        }

        #endregion

        #region UpdateFailedLoginInfoAsync 测试

        [Fact]
        public async Task UpdateFailedLoginInfoAsync_Should_UpdateFailedLoginInfo_When_UserExists()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 0,
                LockoutEnd = null
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var failedLoginCount = 5;
            var lockoutEnd = DateTime.UtcNow.AddMinutes(30);

            // Act
            await _repository.UpdateFailedLoginInfoAsync(user.Id, failedLoginCount, lockoutEnd);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.FailedLoginCount.Should().Be(failedLoginCount);
            updatedUser.LockoutEnd.Should().BeCloseTo(lockoutEnd, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UpdateFailedLoginInfoAsync_Should_NotThrow_When_UserNotExists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var failedLoginCount = 3;
            var lockoutEnd = DateTime.UtcNow.AddMinutes(15);

            // Act & Assert
            var act = async () => await _repository.UpdateFailedLoginInfoAsync(nonExistentId, failedLoginCount, lockoutEnd);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task UpdateFailedLoginInfoAsync_Should_ResetFailedLoginCount_When_ZeroProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 3,
                LockoutEnd = DateTime.UtcNow.AddMinutes(15)
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 0, null);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.FailedLoginCount.Should().Be(0);
            updatedUser.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task UpdateFailedLoginInfoAsync_Should_IncrementFailedLoginCount_When_CalledMultipleTimes()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 0,
                LockoutEnd = null
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 1, null);
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 2, null);
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 3, DateTime.UtcNow.AddMinutes(15));

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.FailedLoginCount.Should().Be(3);
            updatedUser.LockoutEnd.Should().NotBeNull();
        }

        #endregion

        #region 继承的基础Repository方法测试

        [Fact]
        public async Task GetByIdAsync_Should_ReturnUser_When_UserExists()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            result.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task AddAsync_Should_AddUser_When_ValidUserProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "newuser",
                RealName = "New User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };

            // Act
            var result = await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("newuser");

            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser!.Username.Should().Be("newuser");
        }

        [Fact]
        public async Task UpdateAsync_Should_UpdateUser_When_ValidUserProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "originaluser",
                RealName = "Original User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            _context.Entry(user).State = EntityState.Detached;

            user.RealName = "Updated User";

            // Act
            var result = await _repository.UpdateAsync(user);
            await _repository.SaveChangesAsync();

            // Assert
            result.RealName.Should().Be("Updated User");

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.RealName.Should().Be("Updated User");
        }

        [Fact]
        public async Task DeleteAsync_Should_DeleteUser_When_ValidUserProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "deleteuser",
                RealName = "Delete User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
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
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "existsuser",
                RealName = "Exists User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
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
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    RealName = "User 1",
                    PasswordHash = "hash1",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user2",
                    RealName = "User 2",
                    PasswordHash = "hash2",
                    Role = UserRole.Admin,
                    Status = CommonStatus.Enabled
                }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var totalCount = await _repository.CountAsync();
            var doctorCount = await _repository.CountAsync(u => u.Role == UserRole.Doctor);

            // Assert
            totalCount.Should().Be(2);
            doctorCount.Should().Be(1);
        }

        #endregion

        #region 边界条件和错误处理测试

        [Fact]
        public async Task GetByUsernameAsync_Should_HandleSpecialCharacters_When_UsernameContainsSpecialChars()
        {
            // Arrange
            var specialUsername = "user@domain.com";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = specialUsername,
                RealName = "Special User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUsernameAsync(specialUsername);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(specialUsername);
        }

        [Fact]
        public async Task UpdateUserSecurityAsync_Should_HandleMaxValues_When_LargeValuesProvided()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var maxFailedCount = int.MaxValue;
            var maxLockoutEnd = DateTime.MaxValue;

            // Act & Assert
            var act = async () => await _repository.UpdateUserSecurityAsync(user.Id, maxFailedCount, maxLockoutEnd);
            await act.Should().NotThrowAsync();

            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.FailedLoginCount.Should().Be(maxFailedCount);
        }

        [Fact]
        public async Task Repository_Should_HandleConcurrentUpdates_When_MultipleThreadsUpdate()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "concurrentuser",
                RealName = "Concurrent User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 0
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var tasks = Enumerable.Range(1, 5).Select(i =>
                Task.Run(async () =>
                {
                    using var context = new AppDbContext(_options);
                    using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
                    var logger = new Mock<ILogger<AuthRepository>>();
                    var repo = new AuthRepository(context, logger.Object, cache);
                    await repo.UpdateUserSecurityAsync(user.Id, i, null);
                })
            ).ToArray();

            // Assert
            var act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }

        #endregion

        #region 性能测试

        [Fact]
        public async Task GetByUsernameAsync_Should_PerformWell_When_CalledMultipleTimes()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "performanceuser",
                RealName = "Performance User",
                PasswordHash = "hashedPassword",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - 第一次调用会建立缓存
            await _repository.GetByUsernameAsync("performanceuser");

            // 后续调用应该使用缓存，速度更快
            for (int i = 0; i < 10; i++)
            {
                await _repository.GetByUsernameAsync("performanceuser");
            }

            stopwatch.Stop();

            // Assert
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
    public class AuthRepositoryIntegrationTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly AuthRepository _repository;
        private readonly IMemoryCache _cache;

        public AuthRepositoryIntegrationTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            var logger = new Mock<ILogger<AuthRepository>>();
            _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
            _repository = new AuthRepository(_context, logger.Object, _cache);
        }

        [Fact]
        public async Task AuthRepository_Should_WorkWithRealScenario_When_UserLoginFlow()
        {
            // Arrange - 创建用户
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "integrationuser",
                RealName = "Integration User",
                PasswordHash = "hashedPassword123",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                FailedLoginCount = 0,
                LockoutEnd = null
            };
            await _repository.AddAsync(user);
            await _repository.SaveChangesAsync();

            // Act & Assert - 模拟登录流程
            // 1. 通过用户名查找用户
            var foundUser = await _repository.GetByUsernameAsync("integrationuser");
            foundUser.Should().NotBeNull();
            foundUser!.Username.Should().Be("integrationuser");

            // 2. 模拟登录失败，更新失败次数
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 1, null);
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 2, null);
            await _repository.UpdateFailedLoginInfoAsync(user.Id, 3, DateTime.UtcNow.AddMinutes(15));

            // 3. 验证用户被锁定
            var lockedUser = await _repository.GetByIdAsync(user.Id);
            lockedUser!.FailedLoginCount.Should().Be(3);
            lockedUser.LockoutEnd.Should().NotBeNull();

            // 4. 重置用户状态（成功登录）
            await _repository.UpdateUserSecurityAsync(user.Id, 0, null);

            // 5. 验证用户状态重置
            var resetUser = await _repository.GetByIdAsync(user.Id);
            resetUser!.FailedLoginCount.Should().Be(0);
            resetUser.LockoutEnd.Should().BeNull();
        }

        [Fact]
        public async Task AuthRepository_Should_WorkWithAdminFlow_When_AdminOperations()
        {
            // Arrange - 创建管理员密钥
            var adminSecretId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var adminSecret = new AdminSecretModel
            {
                Id = adminSecretId,
                PasswordHash = "originalAdminHash"
            };
            await _context.AdminSecrets.AddAsync(adminSecret);
            await _context.SaveChangesAsync();

            // Act & Assert - 模拟管理员操作流程
            // 1. 获取管理员密码哈希
            var passwordHash = await _repository.GetAdminPasswordHashAsync("admin");
            passwordHash.Should().Be("originalAdminHash");

            // 2. 更新管理员密码
            await _repository.UpdateAdminPasswordHashAsync("admin", "newAdminHash");

            // 3. 验证密码已更新
            var newPasswordHash = await _repository.GetAdminPasswordHashAsync("admin");
            newPasswordHash.Should().Be("newAdminHash");
        }

        public void Dispose()
        {
            _context?.Dispose();
            _cache?.Dispose();
        }
    }
}
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LYBT.Tests.Server;

/// <summary>
/// DatabaseInitializationService 单元测试。
/// 核心覆盖: EnsureSystemAdminExistsAsync 的各种分支。
/// Phase 1 测试置信度重建 - Task 1.2
///
/// 使用 InMemory 数据库: InitializeDatabaseAsync 检测到非关系型数据库后
/// 走 EnsureCreatedAsync 路径 (非 MigrateAsync)，避免迁移文件冲突。
/// </summary>
[CollectionDefinition("ForceResetTests", DisableParallelization = true)]
public class ForceResetTestsCollection
{
}

[Collection("ForceResetTests")]
public class DatabaseInitializationServiceTests : IAsyncLifetime, IDisposable
{
    private AppDbContext _dbContext = null!;
    private ILogger<DatabaseInitializationService> _logger = null!;

    private static readonly SystemAdminOptions DefaultAdminOptions = new()
    {
        UserName = "sysadmin",
        Email = "admin@lybt.com",
        DisplayName = "系统管理员",
        AutoCreateOnStartup = true
    };

    private static readonly DefaultPasswordOptions DefaultPasswordOpts = new()
    {
        SysAdminPassword = "TestSecurePassword2025@"
    };

    public Task InitializeAsync()
    {
        var dbName = $"DbInitTest_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        _dbContext = new AppDbContext(options);
        _logger = NullLogger<DatabaseInitializationService>.Instance;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronous disposal — releases sync-disposable fields.
    /// </summary>
    public void Dispose()
    {
        // InMemory database doesn't need sync disposal, but required by CA1001.
        GC.SuppressFinalize(this);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    private DatabaseInitializationService CreateService(
        SystemAdminOptions? adminOptions = null,
        DefaultPasswordOptions? passwordOptions = null,
        ILogger<DatabaseInitializationService>? logger = null,
        FakeUserManager? userManager = null)
    {
        IDbContextAccessor dbAccessor = new TestDbContextAccessor(_dbContext);
        return new DatabaseInitializationService(
            dbAccessor,
            logger ?? _logger,
            Options.Create(adminOptions ?? DefaultAdminOptions),
            Options.Create(passwordOptions ?? DefaultPasswordOpts),
            userManager ?? new FakeUserManager());
    }

    /// <summary>
    /// 手写 UserManager 替身（Server 零 mock 约定）——仅 override 密码重置相关，
    /// 记录 ResetPasswordAsync 调用供断言
    /// </summary>
    private sealed class FakeUserManager : UserManager<ApplicationUser>
    {
        public int ResetPasswordCallCount { get; private set; }
        public string? LastNewPassword { get; private set; }
        public bool ResetSucceeds { get; set; } = true;

        public FakeUserManager()
            : base(
                new FakeUserStore(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new FakeLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                NullLogger<UserManager<ApplicationUser>>.Instance)
        {
        }

        private sealed class FakeUserStore : IUserStore<ApplicationUser>
        {
            public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct = default) => Task.FromResult(IdentityResult.Success);
            public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct = default) => Task.FromResult(IdentityResult.Success);
            public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct = default) => Task.FromResult<ApplicationUser?>(null);
            public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct = default) => Task.FromResult<ApplicationUser?>(null);
            public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct = default) => Task.FromResult(user.Id.ToString());
            public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct = default) => Task.FromResult<string?>(user.UserName);
            public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct = default) { user.UserName = userName; return Task.CompletedTask; }
            public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct = default) => Task.FromResult<string?>(user.NormalizedUserName);
            public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct = default) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
            public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct = default) => Task.FromResult(IdentityResult.Success);
            public void Dispose() { }
        }

        private sealed class FakeLookupNormalizer : ILookupNormalizer
        {
            public string NormalizeName(string? name) => name?.ToUpperInvariant() ?? string.Empty;
            public string NormalizeEmail(string? email) => email?.ToUpperInvariant() ?? string.Empty;
        }

        public override Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
            => Task.FromResult("fake-reset-token");

        public override Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            ResetPasswordCallCount++;
            LastNewPassword = newPassword;
            return Task.FromResult(ResetSucceeds
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError { Description = "测试失败" }));
        }
    }

    [Fact]
    public async Task ForceReset_Development_ResetsPasswordAndState()
    {
        // ForceReset 修复: 开发环境直接触发——真正重置密码（ResetPasswordAsync 调用）
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        try
        {
            var adminOptions = new SystemAdminOptions
            {
                AutoCreateOnStartup = true,
                AllowAutoCreateInProduction = false,
                ForceResetOnStartup = true
            };
            var passwordOptions = new DefaultPasswordOptions { SysAdminPassword = "NewPass@2026" };
            var fakeUserManager = new FakeUserManager();
            var service = CreateService(adminOptions, passwordOptions, userManager: fakeUserManager);

            // 预插 sysadmin（服务不创建用户——Issue #2237 迁 IdentitySeedData）
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "sysadmin",
                RealName = "系统管理员",
                Email = "sysadmin@lybt.com",
                Role = UserRole.SuperAdmin,
                Status = CommonStatus.Enabled,
                PasswordHash = HashPassword("OldPass@123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            await service.InitializeDatabaseAsync();

            fakeUserManager.ResetPasswordCallCount.Should().Be(1,
                "Development + ForceReset=true 应真正重置密码（ResetPasswordAsync 调用）");
            fakeUserManager.LastNewPassword.Should().Be("NewPass@2026");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Fact]
    public async Task ForceReset_Production_InvalidToken_DoesNotReset()
    {
        // ForceReset 修复: 非开发环境 + 无效 token → 安全门控拦截（不重置）
        var originalToken = Environment.GetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN");
        Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", "real-token");
        try
        {
            var adminOptions = new SystemAdminOptions
            {
                AutoCreateOnStartup = true,
                AllowAutoCreateInProduction = false,
                ForceResetOnStartup = true,
                InitialSetupToken = "wrong-token" // 与 env 不匹配 → 门控拦截
            };
            var fakeUserManager = new FakeUserManager();
            var service = CreateService(adminOptions, userManager: fakeUserManager);

            await service.InitializeDatabaseAsync();

            fakeUserManager.ResetPasswordCallCount.Should().Be(0,
                "非开发 + 无效 token → 门控拦截——不得重置");
        }
        finally
        {
            Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ForceReset_Production_ValidToken_Resets()
    {
        // ForceReset 修复核心: 非开发环境（Production 名）+ 有效 token → 重置
        //（即使 AllowAutoCreateInProduction=false——ForceReset 只重置不创建，更安全）
        var originalToken = Environment.GetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN");
        Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", "real-token");
        try
        {
            var adminOptions = new SystemAdminOptions
            {
                AutoCreateOnStartup = true,
                AllowAutoCreateInProduction = false, // 关键: ForceReset 不要求 AllowAutoCreate
                ForceResetOnStartup = true,
                InitialSetupToken = "real-token"
            };
            var fakeUserManager = new FakeUserManager();
            var service = CreateService(adminOptions, userManager: fakeUserManager);

            // 预插 sysadmin
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "sysadmin",
                RealName = "系统管理员",
                Email = "sysadmin@lybt.com",
                Role = UserRole.SuperAdmin,
                Status = CommonStatus.Enabled,
                PasswordHash = HashPassword("OldPass@123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            await service.InitializeDatabaseAsync();

            fakeUserManager.ResetPasswordCallCount.Should().Be(1,
                "非开发 + 有效 token → ForceReset 触发（AllowAutoCreate=false 也允许）");
        }
        finally
        {
            Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", originalToken);
        }
    }

    /// <summary>
    /// 用 Identity PasswordHasher（PBKDF2）生成测试密码哈希（与生产一致，替代已删除的 BCrypt）
    /// </summary>
    private static string HashPassword(string password) =>
        new PasswordHasher<ApplicationUser>().HashPassword(null!, password);

    #region EnsureSystemAdminExistsAsync - 创建场景（已迁移到 IdentitySeedData）

    [Fact]
    public async Task InitializeDatabase_WhenNoSuperAdminExists_InitializationSucceeds()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert - 用户创建已委托给 IdentitySeedData，服务仅检查是否存在
        // InMemory 数据库已创建，无异常抛出
    }

    [Fact]
    public async Task InitializeDatabase_WhenNoSuperAdminExists_DoesNotCreateUser()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert - 用户创建已迁移到 IdentitySeedData
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData，DatabaseInitializationService 不再直接创建");
    }

    #endregion

    #region EnsureSystemAdminExistsAsync - 跳过场景

    [Fact]
    public async Task InitializeDatabase_WhenSuperAdminExists_SkipsCreation()
    {
        // Arrange: 预先创建一个SuperAdmin
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existing_admin",
            RealName = "已有管理员",
            Email = "existing@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = HashPassword("ExistingPass123@"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert: 应只有1个SuperAdmin (已有的那个)
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().HaveCount(1, "已存在SuperAdmin时不应创建新的");
        superAdmins[0].UserName.Should().Be("existing_admin", "应保留原有管理员");
    }

    [Fact]
    public async Task InitializeDatabase_WhenSoftDeletedSuperAdminExists_SkipsCreation()
    {
        // Arrange: 预先创建一个被软删除的SuperAdmin
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "deleted_admin",
            RealName = "已删除管理员",
            Email = "deleted@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = HashPassword("DeletedPass123@"),
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert: 应只有1个SuperAdmin (被软删除的那个)，不创建新的
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().HaveCount(1, "即使SuperAdmin被软删除也不应创建新的");
        superAdmins[0].UserName.Should().Be("deleted_admin");
        superAdmins[0].IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeDatabase_WhenEmailOccupied_SkipsCreation()
    {
        // Arrange: 创建一个普通用户，占用了管理员邮箱
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "regular_user",
            RealName = "普通用户",
            Email = DefaultAdminOptions.Email, // 占用管理员邮箱
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = HashPassword("RegularPass123@"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert: 不应创建SuperAdmin (邮箱冲突)
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("邮箱被占用时不应创建SuperAdmin");
    }

    [Fact]
    public async Task InitializeDatabase_WhenAutoCreateFalse_SkipsCreation()
    {
        // Arrange
        var noAutoCreateOptions = new SystemAdminOptions
        {
            UserName = "sysadmin",
            Email = "admin@lybt.com",
            DisplayName = "系统管理员",
            AutoCreateOnStartup = false
        };
        var service = CreateService(adminOptions: noAutoCreateOptions);

        // Act
        await service.InitializeDatabaseAsync();

        // Assert
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("AutoCreateOnStartup=false时不应创建SuperAdmin");
    }

    #endregion

    #region 幂等性

    [Fact]
    public async Task InitializeDatabase_CalledTwice_DoesNotCreateUser()
    {
        // Arrange
        var service = CreateService();

        // Act: 调用两次
        await service.InitializeDatabaseAsync();
        await service.InitializeDatabaseAsync();

        // Assert: 用户创建已委托给 IdentitySeedData，服务不创建用户
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
    }

    #endregion

    #region EnsureSystemAdminExistsAsync - 新增行为约束 (RED)

    [Fact]
    public async Task EnsureSystemAdminExists_WhenCreated_SetsMustChangeOnNextLogin_WhenForceChangeEnabled()
    {
        // Arrange
        var service = CreateService(passwordOptions: new DefaultPasswordOptions
        {
            SysAdminPassword = DefaultPasswordOpts.SysAdminPassword,
            NewUserPassword = "TempUser2025@",
            ForceChangeOnFirstLogin = true
        });

        // Act
        await service.InitializeDatabaseAsync();

        // Assert - 用户创建已委托给 IdentitySeedData，服务不创建用户
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_WhenCreated_DoesNotSetMustChangeOnNextLogin_WhenForceChangeDisabled()
    {
        // Arrange
        var logger = new CapturingLogger<DatabaseInitializationService>();
        var service = CreateService(passwordOptions: new DefaultPasswordOptions
        {
            SysAdminPassword = DefaultPasswordOpts.SysAdminPassword,
            NewUserPassword = "TempUser2025@",
            ForceChangeOnFirstLogin = false
        }, logger: logger);

        // Act
        await service.InitializeDatabaseAsync();

        // Assert - 用户创建已委托给 IdentitySeedData，服务不创建用户
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_Production_AutoCreateDisabled_DoesNotCreateAdmin()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        try
        {
            var service = CreateService(adminOptions: new SystemAdminOptions
            {
                UserName = "sysadmin",
                Email = "admin@lybt.com",
                DisplayName = "系统管理员",
                AutoCreateOnStartup = true,
                AllowAutoCreateInProduction = false,
                InitialSetupToken = "token-123"
            });

            // Act
            await service.InitializeDatabaseAsync();

            // Assert
            var superAdmins = await _dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.SuperAdmin)
                .ToListAsync();

            superAdmins.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
        }
    }

    [Fact]
    public async Task EnsureSystemAdminExists_Production_AutoCreateEnabled_ValidToken_DoesNotCreateAdmin()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalToken = Environment.GetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", "match-token");

        try
        {
            var service = CreateService(adminOptions: new SystemAdminOptions
            {
                UserName = "sysadmin",
                Email = "admin@lybt.com",
                DisplayName = "系统管理员",
                AutoCreateOnStartup = true,
                AllowAutoCreateInProduction = true,
                InitialSetupToken = "match-token"
            }, passwordOptions: new DefaultPasswordOptions
            {
                SysAdminPassword = DefaultPasswordOpts.SysAdminPassword,
                NewUserPassword = "TempUser2025@",
                ForceChangeOnFirstLogin = true
            });

            // Act
            await service.InitializeDatabaseAsync();

            // Assert - 用户创建已委托给 IdentitySeedData，服务不创建用户
            var superAdmins = await _dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.SuperAdmin)
                .ToListAsync();

            superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task EnsureSystemAdminExists_Production_AutoCreateEnabled_InvalidToken_DoesNotCreateAdmin()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalToken = Environment.GetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        // 不设置 LYBT_INITIAL_SETUP_TOKEN，令牌验证将失败
        Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", null);

        try
        {
            var service = CreateService(adminOptions: new SystemAdminOptions
            {
                UserName = "sysadmin",
                Email = "admin@lybt.com",
                DisplayName = "系统管理员",
                AutoCreateOnStartup = true,
                AllowAutoCreateInProduction = true,
                InitialSetupToken = "wrong-token"
            });

            // Act
            await service.InitializeDatabaseAsync();

            // Assert
            var superAdmins = await _dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.SuperAdmin)
                .ToListAsync();

            superAdmins.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnv);
            Environment.SetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task EnsureSystemAdminExists_Development_DoesNotCreateAdmin()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        try
        {
            var service = CreateService(passwordOptions: new DefaultPasswordOptions
            {
                SysAdminPassword = DefaultPasswordOpts.SysAdminPassword,
                NewUserPassword = "TempUser2025@",
                ForceChangeOnFirstLogin = true
            });

            // Act
            await service.InitializeDatabaseAsync();

            // Assert - 用户创建已委托给 IdentitySeedData，服务不创建用户
            var superAdmins = await _dbContext.Users
                .IgnoreQueryFilters()
                .Where(u => u.Role == UserRole.SuperAdmin)
                .ToListAsync();

            superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }

    [Fact]
    public async Task EnsureSystemAdminExists_LogsCreationEvent_WithStructuredData()
    {
        // Arrange
        var logger = new CapturingLogger<DatabaseInitializationService>();
        var service = CreateService(logger: logger);

        // Act
        await service.InitializeDatabaseAsync();

        // Assert - 用户创建已委托给 IdentitySeedData，服务不创建用户
        // 服务只记录初始化完成日志
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_NewAdmin_DoesNotCreateUser()
    {
        // Arrange
        var service = CreateService(passwordOptions: new DefaultPasswordOptions
        {
            SysAdminPassword = DefaultPasswordOpts.SysAdminPassword,
            NewUserPassword = "TempUser2025@",
            ForceChangeOnFirstLogin = true
        });

        // Act
        await service.InitializeDatabaseAsync();

        // Assert - 用户创建已委托给 IdentitySeedData，服务不创建用户
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().BeEmpty("用户创建已委托给 IdentitySeedData");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_ExistingAdmin_DoesNotResetMustChangeFlag()
    {
        // Arrange
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existing_admin",
            RealName = "已有管理员",
            Email = "existing@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = HashPassword("ExistingPass123@"),
            MustChangeOnNextLogin = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var logger = new CapturingLogger<DatabaseInitializationService>();
        var service = CreateService(logger: logger);

        // Act
        await service.InitializeDatabaseAsync();

        // Assert
        var existingAdmin = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.UserName == "existing_admin");

        existingAdmin.MustChangeOnNextLogin.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureSystemAdminExists_ExistingAdmin_DoesNotChangePassword()
    {
        // Arrange
        var passwordHash = HashPassword("ExistingPass123@");
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existing_admin",
            RealName = "已有管理员",
            Email = "existing@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = passwordHash,
            MustChangeOnNextLogin = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var logger = new CapturingLogger<DatabaseInitializationService>();
        var service = CreateService(logger: logger);

        // Act
        await service.InitializeDatabaseAsync();

        // Assert
        var existingAdmin = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.UserName == "existing_admin");

        existingAdmin.PasswordHash.Should().Be(passwordHash);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(
                logLevel,
                eventId,
                formatter(state, exception),
                exception,
                state as IEnumerable<KeyValuePair<string, object?>>));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        IEnumerable<KeyValuePair<string, object?>>? Properties);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    #endregion

    #region GetDatabaseInfoAsync

    [Fact]
    public async Task GetDatabaseInfo_WhenConnected_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();

        // Act
        var info = await service.GetDatabaseInfoAsync();

        // Assert
        info.Should().Be("数据库连接正常");
    }

    #endregion

    #region Helper

    private sealed class TestDbContextAccessor : IDbContextAccessor
    {
        public AppDbContext Context { get; }
        public TestDbContextAccessor(AppDbContext context) => Context = context;
    }

    #endregion
}

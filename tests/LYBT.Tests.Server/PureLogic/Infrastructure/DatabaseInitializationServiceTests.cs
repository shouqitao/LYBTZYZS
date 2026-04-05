using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LYBT.Tests.Server.PureLogic.Infrastructure;

/// <summary>
/// DatabaseInitializationService 单元测试。
/// 核心覆盖: EnsureSystemAdminExistsAsync 的各种分支。
/// Phase 1 测试置信度重建 - Task 1.2
///
/// 使用 InMemory 数据库: InitializeDatabaseAsync 检测到非关系型数据库后
/// 走 EnsureCreatedAsync 路径 (非 MigrateAsync)，避免迁移文件冲突。
/// </summary>
public class DatabaseInitializationServiceTests : IAsyncLifetime
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

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    private DatabaseInitializationService CreateService(
        SystemAdminOptions? adminOptions = null,
        DefaultPasswordOptions? passwordOptions = null,
        ILogger<DatabaseInitializationService>? logger = null)
    {
        return new DatabaseInitializationService(
            _dbContext,
            logger ?? _logger,
            Options.Create(adminOptions ?? DefaultAdminOptions),
            Options.Create(passwordOptions ?? DefaultPasswordOpts));
    }

    #region EnsureSystemAdminExistsAsync - 创建场景

    [Fact]
    public async Task InitializeDatabase_WhenNoSuperAdminExists_CreatesUser()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert
        var superAdmin = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin);

        superAdmin.Should().NotBeNull("应创建SuperAdmin用户");
        superAdmin!.UserName.Should().Be("sysadmin");
        superAdmin.RealName.Should().Be("系统管理员");
        superAdmin.Email.Should().Be("admin@lybt.com");
        superAdmin.Role.Should().Be(UserRole.SuperAdmin);
        superAdmin.Status.Should().Be(CommonStatus.Enabled);
        superAdmin.PasswordHash.Should().NotBeNullOrEmpty("密码应被Hash");
        superAdmin.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeDatabase_CreatedUser_PasswordCanBeVerified()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.InitializeDatabaseAsync();

        // Assert
        var superAdmin = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin);
        superAdmin.Should().NotBeNull();

        var passwordVerifies = BCrypt.Net.BCrypt.Verify(
            DefaultPasswordOpts.SysAdminPassword,
            superAdmin!.PasswordHash);
        passwordVerifies.Should().BeTrue("创建的密码Hash应可被验证");
    }

    #endregion

    #region EnsureSystemAdminExistsAsync - 跳过场景

    [Fact]
    public async Task InitializeDatabase_WhenSuperAdminExists_SkipsCreation()
    {
        // Arrange: 预先创建一个SuperAdmin
        _dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "existing_admin",
            RealName = "已有管理员",
            Email = "existing@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPass123@"),
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
        _dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleted_admin",
            RealName = "已删除管理员",
            Email = "deleted@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("DeletedPass123@"),
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
        _dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "regular_user",
            RealName = "普通用户",
            Email = DefaultAdminOptions.Email, // 占用管理员邮箱
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("RegularPass123@"),
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
    public async Task InitializeDatabase_CalledTwice_CreatesOnlyOneSuperAdmin()
    {
        // Arrange
        var service = CreateService();

        // Act: 调用两次
        await service.InitializeDatabaseAsync();
        await service.InitializeDatabaseAsync();

        // Assert: 应只有1个SuperAdmin
        var superAdmins = await _dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role == UserRole.SuperAdmin)
            .ToListAsync();

        superAdmins.Should().HaveCount(1, "多次调用不应创建重复的SuperAdmin");
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

        // Assert
        var superAdmin = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Role == UserRole.SuperAdmin);

        superAdmin.MustChangeOnNextLogin.Should().BeTrue();
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

        // Assert
        var superAdmin = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Role == UserRole.SuperAdmin);

        superAdmin.MustChangeOnNextLogin.Should().BeFalse();
        logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning,
            "关闭首次登录强制改密时应记录安全警告");
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
    public async Task EnsureSystemAdminExists_Production_AutoCreateEnabled_ValidToken_CreatesAdmin()
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

            // Assert
            var superAdmin = await _dbContext.Users
                .IgnoreQueryFilters()
                .SingleAsync(u => u.Role == UserRole.SuperAdmin);

            superAdmin.Should().NotBeNull();
            superAdmin.MustChangeOnNextLogin.Should().BeTrue();
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
    public async Task EnsureSystemAdminExists_Development_AlwaysCreatesAdmin()
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

            // Assert
            var superAdmin = await _dbContext.Users
                .IgnoreQueryFilters()
                .SingleAsync(u => u.Role == UserRole.SuperAdmin);

            superAdmin.Should().NotBeNull();
            superAdmin.MustChangeOnNextLogin.Should().BeTrue();
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

        // Assert
        logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning);
        var entry = logger.Entries.Single(e => e.Level == LogLevel.Warning);
        entry.Properties.Should().NotBeNull();
        entry.Properties!.Should().Contain(kvp => kvp.Key == "UserName");
        entry.Properties!.Should().Contain(kvp => kvp.Key == "Email");
        entry.Properties!.Should().Contain(kvp => kvp.Key == "Role");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_ExistingAdmin_DoesNotResetMustChangeFlag()
    {
        // Arrange
        _dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "existing_admin",
            RealName = "已有管理员",
            Email = "existing@lybt.com",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPass123@"),
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
        logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning,
            "检测到已存在的系统管理员时应记录告警级事件");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_ExistingAdmin_DoesNotChangePassword()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("ExistingPass123@");
        _dbContext.Users.Add(new User
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
        logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning,
            "跳过已有系统管理员时应记录告警级事件");
    }

    [Fact]
    public async Task EnsureSystemAdminExists_NewAdmin_PasswordIsHashedWithBCrypt()
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

        // Assert
        var superAdmin = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Role == UserRole.SuperAdmin);

        superAdmin.PasswordHash.Should().MatchRegex(@"^\$(2a|2b)\$");
        superAdmin.MustChangeOnNextLogin.Should().BeTrue();
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
}

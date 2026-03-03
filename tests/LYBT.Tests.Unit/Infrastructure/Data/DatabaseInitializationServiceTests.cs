using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LYBT.Tests.Unit.Infrastructure.Data;

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
        _logger = Substitute.For<ILogger<DatabaseInitializationService>>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    private DatabaseInitializationService CreateService(
        SystemAdminOptions? adminOptions = null,
        DefaultPasswordOptions? passwordOptions = null)
    {
        return new DatabaseInitializationService(
            _dbContext,
            _logger,
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

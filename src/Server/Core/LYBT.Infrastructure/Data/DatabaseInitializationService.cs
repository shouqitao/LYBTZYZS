using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Data;

/// <summary>
/// 简化版数据库初始化服务
/// 采用最小化设计原则，仅保留必要功能
/// Issue #2237: 支持系统管理员自动创建
/// </summary>
public class DatabaseInitializationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private readonly LybtOptions _lybtOptions;

    public DatabaseInitializationService(
        AppDbContext context,
        ILogger<DatabaseInitializationService> logger,
        IOptions<LybtOptions> lybtOptions)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lybtOptions = lybtOptions?.Value ?? throw new ArgumentNullException(nameof(lybtOptions));
    }

    /// <summary>
    /// 初始化数据库 - 使用 EF Migrations
    /// </summary>
    /// <summary>
    /// 初始化数据库 - 使用 EF Migrations
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("开始初始化数据库并应用迁移");

            // 检查是否为关系型数据库（排除 InMemory 数据库）
            if (_context.Database.IsRelational())
            {
                // 幂等性检查：先检查是否有待应用的 Migration
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("发现 {Count} 个待应用的迁移: {Migrations}",
                        pendingMigrations.Count(),
                        string.Join(", ", pendingMigrations));

                    // 重试逻辑：最多重试 3 次，每次等待 1-3 秒（随机退避）
                    var retryCount = 0;
                    var maxRetries = 3;
                    var random = new Random();

                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            await _context.Database.MigrateAsync();
                            _logger.LogInformation("数据库迁移应用成功");
                            break;
                        }
                        catch (Exception ex) when (retryCount < maxRetries - 1)
                        {
                            retryCount++;
                            var delaySeconds = random.Next(1, 4); // 1-3秒随机延迟
                            _logger.LogWarning(ex, "迁移失败，第 {Retry}/{MaxRetries} 次重试，等待 {Delay}秒后重试...",
                                retryCount, maxRetries, delaySeconds);
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("无待应用的迁移，数据库已是最新状态");
                }

                _logger.LogInformation("数据库初始化完成");
            }
            else
            {
                // InMemory 或其他非关系型数据库，确保数据库已创建
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("数据库初始化完成（InMemory 数据库）");
            }

            // Issue #2237: 自动创建系统管理员
            if (_lybtOptions.SystemAdmin.AutoCreateOnStartup)
            {
                await EnsureSystemAdminExistsAsync();
            }
            else
            {
                _logger.LogInformation("AutoCreateOnStartup = false，跳过系统管理员自动创建");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 获取数据库信息 - 简化版本
    /// </summary>
    public async Task<string> GetDatabaseInfoAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            return canConnect ? "数据库连接正常" : "数据库连接失败";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据库信息失败");
            return "数据库状态未知";
        }
    }

    /// <summary>
    /// 确保系统管理员存在（仅创建，不更新）
    /// Issue #2237: 如果不存在则创建，如果存在则不变（管理员可自行修改密码和邮箱）
    /// </summary>
    private async Task EnsureSystemAdminExistsAsync()
    {
        try
        {
            _logger.LogInformation("开始检查系统管理员是否存在");

            // 检查是否已存在SuperAdmin用户
            var existingSuperAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin && !u.IsDeleted);

            if (existingSuperAdmin != null)
            {
                _logger.LogInformation(
                    "系统管理员已存在，跳过创建。UserName: {UserName}, Email: {Email}",
                    existingSuperAdmin.UserName,
                    existingSuperAdmin.Email);
                return;
            }

            // 不存在，创建新的SuperAdmin用户
            var config = _lybtOptions.SystemAdmin;
            var defaultPassword = _lybtOptions.DefaultPasswords.SysAdminPassword;

            var superAdmin = new User
            {
                Id = Guid.NewGuid(),
                UserName = config.Username,
                RealName = config.DisplayName,
                Email = config.Email,
                Role = UserRole.SuperAdmin,
                Status = CommonStatus.Enabled,
                PasswordHash = PasswordHelper.HashPassword(defaultPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,  // 系统创建
                UpdatedBy = Guid.Empty,
                IsDeleted = false
            };

            _context.Users.Add(superAdmin);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "系统管理员创建成功。UserName: {UserName}, Email: {Email}, Role: {Role}",
                superAdmin.UserName,
                superAdmin.Email,
                superAdmin.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建系统管理员失败");
            throw;
        }
    }
}

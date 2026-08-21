using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

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
    private readonly SystemAdminOptions _systemAdminOptions;
    private readonly DefaultPasswordOptions _defaultPasswordOptions;

    public DatabaseInitializationService(
        IDbContextAccessor dbAccessor,
        ILogger<DatabaseInitializationService> logger,
        IOptions<SystemAdminOptions> systemAdminOptions,
        IOptions<DefaultPasswordOptions> defaultPasswordOptions)
    {
        _context = dbAccessor.Context;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _systemAdminOptions = systemAdminOptions?.Value ?? throw new ArgumentNullException(nameof(systemAdminOptions));
        _defaultPasswordOptions = defaultPasswordOptions?.Value ?? throw new ArgumentNullException(nameof(defaultPasswordOptions));
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
            if (_systemAdminOptions.AutoCreateOnStartup)
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
            throw; // P2-7-5 已确认：异常直接 throw 不吞没 ProductionConfigurationException，Program 层将 Fatal 退出，健康探针不误导为 200。
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

            var config = _systemAdminOptions;

            // 生产环境安全门控（生命周期回归 2026-08-13: 无 ForceReset——仅 AllowAutoCreate+token）
            if (!IsDevelopment())
            {
                if (!config.AllowAutoCreateInProduction)
                {
                    _logger.LogWarning("非开发环境且 AllowAutoCreateInProduction=false，跳过系统管理员自动创建");
                    return;
                }

                if (!ValidateSetupToken(config.InitialSetupToken))
                {
                    _logger.LogWarning("非开发环境初始设置令牌验证失败，跳过系统管理员自动创建");
                    return;
                }
            }

            // 检查是否已存在SuperAdmin用户（包括已删除的）
            var existingSuperAdmin = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin);

            if (existingSuperAdmin != null)
            {
                // 生命周期回归（2026-08-13）: 存在即跳过——无任何密码重置机制
                //（密码遗忘唯一途径 = PasswordHashGenerator 工具人工恢复）
                if (existingSuperAdmin.IsDeleted || existingSuperAdmin.Status != CommonStatus.Enabled)
                {
                    // 软异常: 存在但被删除/禁用——不阻断启动，登录时明确提示（运维可观测性）
                    _logger.LogWarning(
                        "[SYSADMIN] 系统管理员账号异常（IsDeleted={Deleted}, Status={Status}）——登录将提示联系运维使用密码初始化工具恢复",
                        existingSuperAdmin.IsDeleted, existingSuperAdmin.Status);
                }
                else
                {
                    _logger.LogInformation(
                        "系统管理员已存在，跳过创建。UserName: {UserName}",
                        existingSuperAdmin.UserName);
                }
                return;
            }

            // 硬删边界检测（生命周期回归 2026-08-13）:
            // 数据存在（非 sysadmin 用户/业务已用）但 SuperAdmin 缺失 → 启动 Fatal（需工具恢复）
            var hasData = await _context.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.Role != UserRole.SuperAdmin);
            if (hasData)
            {
                _logger.LogCritical(
                    "[SYSADMIN] 系统数据存在但 SuperAdmin 用户缺失（可能被硬删）——禁止启动，需用密码初始化工具（PasswordHashGenerator）恢复 Users 表");
                throw new InvalidOperationException(
                    "系统数据存在但系统管理员账号缺失（可能被物理删除）——请使用密码初始化工具（PasswordHashGenerator）恢复后重新启动");
            }

            // 空库首次启动: 正常（IdentitySeedData 将创建——Issue #2237 用户创建迁移）
            _logger.LogInformation("空库首次启动——系统管理员将由 IdentitySeedData 创建");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建系统管理员失败");
            throw;
        }
    }

    /// <summary>
    /// 判断当前是否为开发环境。
    /// 仅当 ASPNETCORE_ENVIRONMENT 为 "Development"（不区分大小写）时返回 true。
    /// 环境变量为 null/空字符串时视为生产环境（保守默认，避免绕过生产门控）。
    /// </summary>
    private static bool IsDevelopment()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证初始设置令牌。
    /// 将配置中的 InitialSetupToken 与环境变量 LYBT_INITIAL_SETUP_TOKEN 进行常量时间比较。
    /// </summary>
    private static bool ValidateSetupToken(string? configuredToken)
    {
        if (string.IsNullOrEmpty(configuredToken))
            return false;

        var expectedToken = Environment.GetEnvironmentVariable("LYBT_INITIAL_SETUP_TOKEN");
        if (string.IsNullOrEmpty(expectedToken))
            return false;

        var configBytes = Encoding.UTF8.GetBytes(configuredToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return CryptographicOperations.FixedTimeEquals(configBytes, expectedBytes);
    }
}



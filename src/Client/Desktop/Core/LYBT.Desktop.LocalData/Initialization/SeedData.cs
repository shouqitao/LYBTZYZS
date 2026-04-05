using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.LocalData.Initialization;

/// <summary>
/// 种子数据 - 初始化默认数据
/// OpenSpec: implement-local-mode
/// </summary>
public static class SeedData
{
    /// <summary>
    /// 默认管理员用户名
    /// </summary>
    public const string DefaultAdminUsername = "admin";

    /// <summary>
    /// 默认管理员密码
    /// </summary>
    public const string DefaultAdminPassword = "Admin@123";

    /// <summary>
    /// 初始化种子数据
    /// </summary>
    public static async Task SeedAsync(LocalDbContext context, ILogger logger, CancellationToken ct = default)
    {
        await SeedAdminUserAsync(context, logger, ct);
    }

    /// <summary>
    /// 创建默认管理员账户
    /// </summary>
    private static async Task SeedAdminUserAsync(LocalDbContext context, ILogger logger, CancellationToken ct)
    {
        // 检查是否已存在管理员
        var adminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.UserName == DefaultAdminUsername, ct);

        if (adminExists)
        {
            logger.LogDebug("[SeedData] 管理员账户已存在，跳过创建");
            return;
        }

        // 创建管理员账户
        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = DefaultAdminUsername,
            RealName = "系统管理员",
            PasswordHash = PasswordHelper.HashPassword(DefaultAdminPassword, UserRole.SuperAdmin, logger),
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("[SeedData] 默认管理员账户已创建 - 用户名: {Username}", DefaultAdminUsername);
    }
}

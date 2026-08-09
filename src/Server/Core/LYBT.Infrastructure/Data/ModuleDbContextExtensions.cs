using LYBT.Shared.Configuration;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Data;

/// <summary>
/// 模块级 DbContext 注册扩展（A-31-C5-2 收敛）
/// 各模块 DbContext 引导代码逐行相同（DatabaseOptions+ConnectionStringResolver+UseSqlServer），统一提取。
/// OnModelCreating 各自注册配置保留在各模块 DbContext 中，不合并。
/// </summary>
public static class ModuleDbContextExtensions
{
    /// <summary>
    /// 注册模块自己的 DbContext（同库，连接串与 AppDbContext 一致）
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = ConnectionStringResolver.GetEffectiveConnectionString(dbOptions, configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("未配置数据库连接字符串");
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}

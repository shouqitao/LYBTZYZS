using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;

namespace LYBT.WebAPI.IntegrationTests.Infrastructure;

/// <summary>
/// 自定义 Web 应用程序工厂，用于集成测试
/// </summary>
/// <remarks>
/// 设计原则：
/// - 使用真实 SQL Server 数据库（遵循 PRD 要求）
/// - 每个测试类使用独立数据库（避免测试间相互影响）
/// - 自动初始化和清理数据库
/// - 配置测试专用环境变量
/// </remarks>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// 测试数据库名称（每次创建工厂时生成唯一名称）
    /// </summary>
    public string TestDatabaseName { get; }

    /// <summary>
    /// 构造函数 - 生成唯一的测试数据库名称
    /// </summary>
    public CustomWebApplicationFactory()
    {
        // 生成唯一的测试数据库名称（格式：LYBT_IntegrationTest_{GUID}）
        TestDatabaseName = $"LYBT_IntegrationTest_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 配置测试 Web 主机
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 设置测试环境
        builder.UseEnvironment("Test");

        // 配置测试专用设置
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 添加测试专用配置文件
            config.AddJsonFile("appsettings.IntegrationTests.json", optional: true);
        });

        // 配置服务
        builder.ConfigureServices((context, services) =>
        {
            // 移除现有的 AppDbContext 注册
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 获取测试数据库连接字符串
            var connectionString = GetTestDatabaseConnectionString();

            // 注册测试数据库上下文
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);

                // 启用敏感数据日志（仅测试环境）
                options.EnableSensitiveDataLogging();

                // 启用详细错误（仅测试环境）
                options.EnableDetailedErrors();
            });

            // 构建服务提供程序并初始化数据库
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

            try
            {
                // 确保数据库已创建
                db.Database.EnsureCreated();
                logger.LogInformation("测试数据库 {DatabaseName} 已初始化", TestDatabaseName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "初始化测试数据库 {DatabaseName} 失败", TestDatabaseName);
                throw;
            }
        });

    }

    /// <summary>
    /// 获取测试数据库连接字符串
    /// </summary>
    /// <remarks>
    /// 优先级：
    /// 1. 环境变量 LYBT_TEST_CONNECTION_STRING
    /// 2. 默认本地 SQL Server 连接字符串
    /// </remarks>
    private string GetTestDatabaseConnectionString()
    {
        // 从环境变量获取连接字符串（用于 CI 环境）
        var connectionString = Environment.GetEnvironmentVariable("LYBT_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // 默认本地 SQL Server 连接字符串（用于开发环境）
            connectionString = $"Server=(localdb)\\mssqllocaldb;Database={TestDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true";
        }
        else
        {
            // 替换数据库名称占位符
            connectionString = connectionString.Replace("{DatabaseName}", TestDatabaseName);
        }

        return connectionString;
    }

    /// <summary>
    /// 清理资源 - 删除测试数据库
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // 获取数据库上下文并删除测试数据库
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
            }
            catch
            {
                // 忽略清理错误（数据库可能已被删除）
            }
        }

        base.Dispose(disposing);
    }
}

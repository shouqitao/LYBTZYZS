using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Infrastructure.Data;
using Serilog;
using Serilog.Events;

namespace LYBT.WebAPI.Tests;

/// <summary>
/// 自定义 WebApplicationFactory - 用于集成测试
/// 使用内存数据库替代真实数据库
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 配置测试用 Serilog - 直接配置，替换全局 Logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 添加测试配置 (覆盖默认配置，包括 Serilog 配置)
            var testConfig = new Dictionary<string, string?>
            {
                // 数据库连接字符串 (用于 InMemory 提供程序名称)
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["Lybt:Infrastructure:Database:ConnectionString"] = "DataSource=:memory:",

                // JWT 配置
                ["Lybt:Authentication:Jwt:SecretKey"] = "TestSecretKey_MinLength32Characters_ForJWTTokenGeneration_123456789",
                ["Lybt:Authentication:Jwt:Issuer"] = "LYBT.WebAPI.Tests",
                ["Lybt:Authentication:Jwt:Audience"] = "LYBT.Client.Tests",
                ["Lybt:Authentication:Jwt:AccessTokenExpirationMinutes"] = "30",
                ["Lybt:Authentication:Jwt:RefreshTokenExpirationDays"] = "7",

                // 默认密码配置
                ["Lybt:Authentication:DefaultPasswords:SysAdminPassword"] = "Admin@123456",
                ["Lybt:Authentication:DefaultPasswords:NewUserPassword"] = "User@123456",

                // 系统管理员配置
                ["Lybt:Business:SystemAdmin:Username"] = "sysadmin",
                ["Lybt:Business:SystemAdmin:Email"] = "admin@test.com",
                ["Lybt:Business:SystemAdmin:AutoCreateOnStartup"] = "true",

                // 禁用自动迁移
                ["Lybt:Infrastructure:Database:Migration:AutoMigrate"] = "false",
                ["Lybt:Infrastructure:Database:Migration:EnsureCreatedInDevelopment"] = "false",

                // 移除 Serilog 的 MSSqlServer Sink 配置
                // 将所有 Serilog WriteTo 数组清空
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["Serilog:WriteTo:1:Name"] = null, // 移除 File sink
                ["Serilog:WriteTo:2:Name"] = null  // 移除 MSSqlServer sink
            };

            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // 移除现有的 DbContext 配置
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 添加内存数据库
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
                options.EnableSensitiveDataLogging();
            });

            // 确保数据库已创建
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();
        });

        // 使用测试环境
        builder.UseEnvironment("Test");
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WebAPI.Tests;

/// <summary>
/// 自定义 WebApplicationFactory - 用于集成测试
/// 使用内存数据库替代真实数据库
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        // 在 Program.cs 读取环境变量之前设置测试环境
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 使用测试环境（会自动加载 appsettings.Test.json）
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // 移除所有与两个 AppDbContext 相关的服务描述符
            // 注意：项目中存在两个 AppDbContext：
            // 1. LYBT.Infrastructure.Data.AppDbContext
            // 2. LYBT.Core.Infrastructure.Data.AppDbContext
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.Name.Contains("AppDbContext") ||
                           d.ServiceType.Name.Contains("DbContextOptions"))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 添加内存数据库 - 为两个 AppDbContext 都注册
            var databaseName = $"TestDatabase_{Guid.NewGuid()}";

            // 注册 LYBT.Infrastructure.Data.AppDbContext
            services.AddDbContext<LYBT.Infrastructure.Data.AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // 注册 LYBT.Core.Infrastructure.Data.AppDbContext
            services.AddDbContext<LYBT.Core.Infrastructure.Data.AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });

            // 确保数据库已创建
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;

            // 只需要创建一次数据库（两个 DbContext 使用相同的内存数据库）
            var db = scopedServices.GetRequiredService<LYBT.Core.Infrastructure.Data.AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}

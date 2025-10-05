using LYBT.Infrastructure.Data;
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
    }
}

using LYBT.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LYBT.Tests.IntegrationTests
{
    /// <summary>
    /// 自定义Web应用程序工厂，用于集成测试
    /// 符合PRD要求：使用SQL Server（非LocalDB）进行测试
    /// </summary>
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private readonly string _testDatabaseName;

        public CustomWebApplicationFactory()
        {
            _testDatabaseName = $"LYBTDB_Test_{Guid.NewGuid():N}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // 添加测试配置文件
                var testConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Test.json");
                if (File.Exists(testConfigPath))
                {
                    config.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true);
                }
            });

            builder.ConfigureServices(services =>
            {
                // 移除生产环境的DbContext
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // 添加测试专用的SQL Server数据库
                var configuration = BuildConfiguration();
                var connectionString = configuration.GetConnectionString("SqlServerConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("未找到SQL Server连接字符串配置");
                }

                // 使用唯一的测试数据库名称
                var testConnectionString = connectionString.Replace("LYBTDB_Test", _testDatabaseName);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(testConnectionString, sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(30);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    });

                    // 启用详细错误信息用于测试
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                });

                // 配置测试日志级别
                services.Configure<LoggerFilterOptions>(options =>
                {
                    options.MinLevel = LogLevel.Warning;
                });
            });

            builder.UseEnvironment("Testing");
        }

        private IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Test.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        /// <summary>
        /// 获取测试数据库名称
        /// </summary>
        public string GetTestDatabaseName() => _testDatabaseName;

        /// <summary>
        /// 初始化测试数据库
        /// </summary>
        public async Task InitializeTestDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                await context.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                logger.LogError(ex, "初始化测试数据库失败: {DatabaseName}", _testDatabaseName);
                throw;
            }
        }

        /// <summary>
        /// 清理测试数据库
        /// </summary>
        public async Task CleanupTestDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                await context.Database.EnsureDeletedAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                logger.LogWarning(ex, "清理测试数据库失败: {DatabaseName}", _testDatabaseName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 异步清理，但不等待完成以避免阻塞
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CleanupTestDatabaseAsync();
                    }
                    catch
                    {
                        // 忽略清理错误，避免影响测试结果
                    }
                });
            }

            base.Dispose(disposing);
        }
    }
}

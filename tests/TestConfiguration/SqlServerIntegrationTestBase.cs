using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using LYBT.WebAPI;
using System;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.Tests.Configuration
{
    /// <summary>
    /// SQL Server集成测试基类
    /// 符合PRD要求：所有集成测试使用SQL Server（非LocalDB）
    /// </summary>
    public abstract class SqlServerIntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncDisposable
    {
        protected readonly WebApplicationFactory<Program> _factory;
        protected readonly IServiceScope _scope;
        protected readonly AppDbContext _context;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger<SqlServerIntegrationTestBase> _logger;
        private readonly SqlServerTestDbContextFactory _dbFactory;

        protected SqlServerIntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 移除默认的DbContext注册
                    var descriptor = services.BuildServiceProvider()
                        .GetService<DbContextOptions<AppDbContext>>();
                    if (descriptor != null)
                    {
                        var optionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (optionsDescriptor != null) services.Remove(optionsDescriptor);

                        var dbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(AppDbContext));
                        if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);
                    }

                    // 注册SQL Server测试数据库
                    var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                    var connectionString = configuration.GetConnectionString("SqlServerConnection");
                    
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("未找到SQL Server连接字符串配置");
                    }

                    // 使用唯一的测试数据库名称
                    var testDatabaseName = $"LYBTDB_Test_{Guid.NewGuid():N}";
                    var testConnectionString = connectionString.Replace("LYBTDB_Test", testDatabaseName);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(testConnectionString, sqlOptions =>
                        {
                            sqlOptions.CommandTimeout(30);
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 3,
                                maxRetryDelay: TimeSpan.FromSeconds(5),
                                errorNumbersToAdd: null);
                        }));
                });
            });

            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _configuration = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
            _logger = _scope.ServiceProvider.GetRequiredService<ILogger<SqlServerIntegrationTestBase>>();
            var dbFactoryLogger = _scope.ServiceProvider.GetRequiredService<ILogger<SqlServerTestDbContextFactory>>();
            _dbFactory = new SqlServerTestDbContextFactory(_configuration, dbFactoryLogger);
        }

        /// <summary>
        /// 初始化测试数据库
        /// </summary>
        protected virtual async Task InitializeDatabaseAsync()
        {
            try
            {
                // 确保数据库已创建并迁移到最新版本
                await _context.Database.EnsureCreatedAsync();
                _logger.LogInformation("测试数据库初始化完成");

                // 如果需要，可以在这里添加种子数据
                await SeedTestDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试数据库初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 种子测试数据 - 子类可以重写此方法
        /// </summary>
        protected virtual async Task SeedTestDataAsync()
        {
            // 默认为空实现，子类可以重写添加特定的测试数据
            await Task.CompletedTask;
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        protected virtual async Task CleanupTestDataAsync()
        {
            try
            {
                // 清理所有表的数据，但保留表结构
                var tables = new[]
                {
                    "Prescriptions", "ConsultationRecords", "MedicalCases", 
                    "Patients", "Herbs", "Formulas", "Users"
                };

                foreach (var table in tables)
                {
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
                }

                _logger.LogInformation("测试数据已清理");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理测试数据时发生错误");
            }
        }

        /// <summary>
        /// 验证SQL Server连接
        /// </summary>
        protected async Task<bool> VerifyDatabaseConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接验证失败");
                return false;
            }
        }

        /// <summary>
        /// 开始数据库事务（用于测试隔离）
        /// </summary>
        protected async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// 获取HTTP客户端
        /// </summary>
        protected HttpClient CreateClient()
        {
            return _factory.CreateClient();
        }

        /// <summary>
        /// 获取配置的服务
        /// </summary>
        protected T GetRequiredService<T>() where T : notnull
        {
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        public virtual async ValueTask DisposeAsync()
        {
            try
            {
                await CleanupTestDataAsync();
                await _dbFactory.CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理测试资源时发生错误");
            }
            finally
            {
                _dbFactory?.Dispose();
                _scope?.Dispose();
            }
        }
    }
}
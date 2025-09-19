using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AutoMapper;
using LYBT.Infrastructure.Data;
using Xunit;

namespace LYBT.Tests.Core
{
    /// <summary>
    /// SQL Server测试基类 - Phase D1 统一SQL Server测试基座
    /// 为小诊所环境提供真实数据库测试支持，移除LocalDB/SQLite依赖
    /// </summary>
    public abstract class SqlServerTestBase : IDisposable, IAsyncDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IServiceCollection Services;
        protected readonly Mock<ILogger> LoggerMock;
        protected readonly IMapper Mapper;
        protected readonly AppDbContext DbContext;
        protected readonly string DatabaseName;

        // Phase D1: SQL Server测试配置
        private const string TEST_CONNECTION_TEMPLATE = 
            "Server=localhost;Database=LYBTDB_Test_{0};Trusted_Connection=True;TrustServerCertificate=true;" +
            "MultipleActiveResultSets=true;Connection Timeout=10;Command Timeout=10;" +
            "Max Pool Size=5;Min Pool Size=1;Pooling=true";

        protected SqlServerTestBase()
        {
            Services = new ServiceCollection();
            
            // Phase D1: 生成唯一测试数据库名称
            DatabaseName = $"Test_{Guid.NewGuid():N}";
            var connectionString = string.Format(TEST_CONNECTION_TEMPLATE, DatabaseName);
            
            // 配置SQL Server数据库
            Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
                
                // Phase D1: 测试环境优化配置
                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(false);
                options.EnableServiceProviderCaching(true);
                options.EnableModelCaching(true);
            });

            // 配置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
            }, NullLoggerFactory.Instance);
            Mapper = mapperConfig.CreateMapper();
            Services.AddSingleton(Mapper);

            // 配置日志
            LoggerMock = new Mock<ILogger>();
            Services.AddSingleton(LoggerMock.Object);
            Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

            // 构建服务提供者
            ServiceProvider = Services.BuildServiceProvider();
            DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
            
            // 初始化测试数据库
            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步初始化测试数据库 - Phase D1 优化
        /// </summary>
        protected virtual async Task InitializeDatabaseAsync()
        {
            try
            {
                // 确保数据库被创建
                await DbContext.Database.EnsureCreatedAsync();
                
                // 种子测试数据
                await SeedTestDataAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法初始化测试数据库 {DatabaseName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 种子测试数据 - 子类可重写
        /// </summary>
        protected virtual async Task SeedTestDataAsync()
        {
            // 默认实现，子类可重写添加测试数据
            await Task.CompletedTask;
        }

        /// <summary>
        /// 添加测试数据 - 异步版本
        /// </summary>
        protected async Task SeedDataAsync<T>(params T[] entities) where T : class
        {
            DbContext.Set<T>().AddRange(entities);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// 添加测试数据 - 同步版本（兼容性）
        /// </summary>
        protected void SeedData<T>(params T[] entities) where T : class
        {
            SeedDataAsync(entities).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 清理数据库中的所有数据 - 测试隔离
        /// </summary>
        protected async Task CleanupDatabaseAsync()
        {
            // Phase D1: 使用事务确保清理的原子性
            using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                // 禁用外键约束
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE PrescriptionItems NOCHECK CONSTRAINT ALL");
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Prescriptions NOCHECK CONSTRAINT ALL");
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Consultations NOCHECK CONSTRAINT ALL");
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE MedicalCases NOCHECK CONSTRAINT ALL");

                // 清理所有表数据
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM PrescriptionItems");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Prescriptions");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Consultations");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM MedicalCases");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Patients");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Herbs");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Formulas");
                await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM Users");

                // 重新启用外键约束
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE PrescriptionItems CHECK CONSTRAINT ALL");
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Prescriptions CHECK CONSTRAINT ALL");
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE Consultations CHECK CONSTRAINT ALL");
                await DbContext.Database.ExecuteSqlRawAsync("ALTER TABLE MedicalCases CHECK CONSTRAINT ALL");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// 创建Mock对象
        /// </summary>
        protected Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }

        /// <summary>
        /// 断言异常 - 异步版本
        /// </summary>
        protected async Task AssertThrowsAsync<TException>(Func<Task> action, string expectedMessage = null)
            where TException : Exception
        {
            var exception = await Assert.ThrowsAsync<TException>(action);
            if (!string.IsNullOrEmpty(expectedMessage))
            {
                Assert.Contains(expectedMessage, exception.Message);
            }
        }

        /// <summary>
        /// 断言集合
        /// </summary>
        protected void AssertCollection<T>(IEnumerable<T> collection, params Action<T>[] assertions)
        {
            Assert.Collection(collection, assertions);
        }

        /// <summary>
        /// 验证数据库连接 - Phase D1 诊断方法
        /// </summary>
        protected async Task<bool> VerifyDatabaseConnectionAsync()
        {
            try
            {
                return await DbContext.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取数据库统计信息 - Phase D1 监控方法
        /// </summary>
        protected async Task<DatabaseStats> GetDatabaseStatsAsync()
        {
            try
            {
                var userCount = await DbContext.Users.CountAsync();
                var patientCount = await DbContext.Patients.CountAsync();
                var medicalCaseCount = await DbContext.MedicalCases.CountAsync();
                var prescriptionCount = await DbContext.Prescriptions.CountAsync();
                var herbCount = await DbContext.Herbs.CountAsync();

                return new DatabaseStats
                {
                    UserCount = userCount,
                    PatientCount = patientCount,
                    MedicalCaseCount = medicalCaseCount,
                    PrescriptionCount = prescriptionCount,
                    HerbCount = herbCount,
                    DatabaseName = DatabaseName
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"获取数据库统计信息失败: {ex.Message}", ex);
            }
        }

        public virtual void Dispose()
        {
            try
            {
                // 同步清理
                CleanupDatabaseAsync().GetAwaiter().GetResult();
                
                // 删除测试数据库
                DbContext?.Database.EnsureDeleted();
            }
            catch
            {
                // 忽略清理错误
            }
            finally
            {
                DbContext?.Dispose();
                (ServiceProvider as IDisposable)?.Dispose();
            }
        }

        public virtual async ValueTask DisposeAsync()
        {
            try
            {
                // 异步清理
                await CleanupDatabaseAsync();
                
                // 删除测试数据库
                await DbContext.Database.EnsureDeletedAsync();
            }
            catch
            {
                // 忽略清理错误
            }
            finally
            {
                if (DbContext != null)
                {
                    await DbContext.DisposeAsync();
                }
                
                if (ServiceProvider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    (ServiceProvider as IDisposable)?.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// SQL Server集成测试基类 - Phase D1
    /// </summary>
    public abstract class SqlServerIntegrationTestBase : SqlServerTestBase
    {
        protected override async Task SeedTestDataAsync()
        {
            await base.SeedTestDataAsync();
            
            // 添加集成测试需要的种子数据
            await SeedIntegrationTestDataAsync();
        }

        protected virtual async Task SeedIntegrationTestDataAsync()
        {
            // 子类可重写此方法添加集成测试数据
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// SQL Server性能测试基类 - Phase D1
    /// </summary>
    public abstract class SqlServerPerformanceTestBase : SqlServerTestBase
    {
        protected TimeSpan MeasureExecutionTime(Action action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        protected async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        protected void AssertPerformance(TimeSpan actual, TimeSpan expected, string metric)
        {
            Assert.True(actual <= expected, 
                $"Performance test failed for {metric}. Expected: {expected.TotalMilliseconds}ms, Actual: {actual.TotalMilliseconds}ms");
        }

        /// <summary>
        /// 断言事务性能 - Phase D1 专用
        /// </summary>
        protected void AssertTransactionPerformance(TimeSpan actual, TimeSpan maxAllowed, string operation)
        {
            Assert.True(actual <= maxAllowed,
                $"Transaction performance test failed for {operation}. Max allowed: {maxAllowed.TotalMilliseconds}ms, Actual: {actual.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// 数据库统计信息 - Phase D1 监控
    /// </summary>
    public class DatabaseStats
    {
        public int UserCount { get; set; }
        public int PatientCount { get; set; }
        public int MedicalCaseCount { get; set; }
        public int PrescriptionCount { get; set; }
        public int HerbCount { get; set; }
        public string DatabaseName { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Database: {DatabaseName}, Users: {UserCount}, Patients: {PatientCount}, " +
                   $"MedicalCases: {MedicalCaseCount}, Prescriptions: {PrescriptionCount}, Herbs: {HerbCount}";
        }
    }
}
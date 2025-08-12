using System;
using System.Collections.Generic;
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
    /// 测试基类 - UltraThink重构测试框架
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IServiceCollection Services;
        protected readonly Mock<ILogger> LoggerMock;
        protected readonly IMapper Mapper;
        protected readonly AppDbContext DbContext;

        protected TestBase()
        {
            Services = new ServiceCollection();
            
            // 配置内存数据库
            var dbName = Guid.NewGuid().ToString();
            Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // 配置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
            });
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
            
            // 初始化数据库
            InitializeDatabase();
        }

        /// <summary>
        /// 初始化测试数据库
        /// </summary>
        protected virtual void InitializeDatabase()
        {
            DbContext.Database.EnsureCreated();
        }

        /// <summary>
        /// 添加测试数据
        /// </summary>
        protected void SeedData<T>(params T[] entities) where T : class
        {
            DbContext.Set<T>().AddRange(entities);
            DbContext.SaveChanges();
        }

        /// <summary>
        /// 创建Mock对象
        /// </summary>
        protected Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }

        /// <summary>
        /// 断言异常
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

        public virtual void Dispose()
        {
            DbContext?.Dispose();
            (ServiceProvider as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// 集成测试基类
    /// </summary>
    public abstract class IntegrationTestBase : TestBase
    {
        protected override void InitializeDatabase()
        {
            base.InitializeDatabase();
            // 添加种子数据
            SeedTestData();
        }

        protected virtual void SeedTestData()
        {
            // 子类可重写此方法添加测试数据
        }
    }

    /// <summary>
    /// 性能测试基类
    /// </summary>
    public abstract class PerformanceTestBase : TestBase
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
    }
}
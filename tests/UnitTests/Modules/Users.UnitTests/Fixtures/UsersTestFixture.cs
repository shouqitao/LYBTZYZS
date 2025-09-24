using AutoMapper;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Mapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;

namespace LYBT.Module.Users.Tests.Fixtures
{
    /// <summary>
    /// Users模块测试基础设施
    /// 提供统一的测试环境配置，包括DbContext、MemoryCache、AutoMapper等
    /// </summary>
    public class UsersTestFixture : IDisposable
    {
        public ServiceProvider ServiceProvider { get; }
        public AppDbContext DbContext { get; }
        public IMemoryCache MemoryCache { get; }
        public IMapper Mapper { get; }
        public UserOptions UserOptions { get; }
        public DefaultPasswordService DefaultPasswordService { get; }

        public UsersTestFixture()
        {
            var services = new ServiceCollection();

            // 配置日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // 配置InMemory数据库
            var dbName = $"UsersTestDb_{Guid.NewGuid()}";
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase(databaseName: dbName);
                options.EnableSensitiveDataLogging(); // 测试环境启用敏感数据日志
            });

            // 配置真实的MemoryCache
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100; // 设置缓存大小限制
            });

            // 配置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UserMappingProfile>();
            });
            
            mapperConfig.AssertConfigurationIsValid(); // 验证映射配置
            services.AddSingleton(mapperConfig.CreateMapper());

            // 配置UserOptions
            services.Configure<UserOptions>(options =>
            {
                options.EnableUserCache = true;
                options.UserCacheExpirationMinutes = 30;
                options.MaxBatchOperationSize = 100;
                options.EnableDetailedAuditLogging = true;
                options.SendPasswordResetNotification = false;
                options.SessionTimeoutMinutes = 480;
                options.EnableOnlineStatusTracking = true;
            });

            // 配置DefaultPasswordOptions
            services.Configure<DefaultPasswordOptions>(options =>
            {
                options.SystemAdmin = "AdminPass@word1!";
                options.NewUser = "UserPass@word1!";
                options.EnableInDevelopment = true;
                options.OnlyWhenDatabaseEmpty = false;
                options.ExpiryDays = 30;
            });

            // Mock IWebHostEnvironment
            var mockEnvironment = new Mock<IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.EnvironmentName).Returns("Test");
            mockEnvironment.Setup(x => x.ApplicationName).Returns("LYBT.Module.Users.Tests");
            services.AddSingleton(mockEnvironment.Object);

            // 注册DefaultPasswordService
            services.AddScoped<DefaultPasswordService>();

            // 构建ServiceProvider
            ServiceProvider = services.BuildServiceProvider();

            // 获取常用服务实例
            DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
            MemoryCache = ServiceProvider.GetRequiredService<IMemoryCache>();
            Mapper = ServiceProvider.GetRequiredService<IMapper>();
            UserOptions = ServiceProvider.GetRequiredService<IOptions<UserOptions>>().Value;
            DefaultPasswordService = ServiceProvider.GetRequiredService<DefaultPasswordService>();

            // 确保数据库已创建
            DbContext.Database.EnsureCreated();
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        public void ClearData()
        {
            // 清除所有用户数据
            DbContext.Users.RemoveRange(DbContext.Users);
            DbContext.SaveChanges();

            // 清除缓存
            if (MemoryCache is MemoryCache cache)
            {
                cache.Compact(1.0);
            }
        }

        /// <summary>
        /// 创建新的Scope，用于测试中的依赖注入
        /// </summary>
        public IServiceScope CreateScope()
        {
            return ServiceProvider.CreateScope();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
            ServiceProvider?.Dispose();
        }
    }

    /// <summary>
    /// 空日志工厂，用于AutoMapper配置
    /// </summary>
    public class NullLoggerFactory : ILoggerFactory
    {
        public static readonly NullLoggerFactory Instance = new();

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// 空日志实现
    /// </summary>
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
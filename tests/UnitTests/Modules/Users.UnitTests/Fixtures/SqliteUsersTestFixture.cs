using AutoMapper;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Mapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;

namespace LYBT.Module.Users.Tests.Fixtures
{
    /// <summary>
    /// Users模块SQLite测试基础设施
    /// 使用SQLite In-Memory数据库提供更真实的数据库行为测试
    /// 支持事务、批量操作等InMemory Provider不支持的功能
    /// </summary>
    public class SqliteUsersTestFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public ServiceProvider ServiceProvider { get; }
        public AppDbContext DbContext { get; }
        public IMemoryCache MemoryCache { get; }
        public IMapper Mapper { get; }
        public UserOptions UserOptions { get; }
        public DefaultPasswordService DefaultPasswordService { get; }

        public SqliteUsersTestFixture()
        {
            // 创建SQLite内存连接
            // 使用命名内存数据库，允许多个连接共享同一数据库
            var connectionString = $"DataSource=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _connection = new SqliteConnection(connectionString);
            _connection.Open(); // 必须保持连接打开，否则内存数据库会丢失

            var services = new ServiceCollection();

            // 配置日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // 配置SQLite数据库
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.EnableSensitiveDataLogging(); // 测试环境启用敏感数据日志

                // SQLite特定配置
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
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

            // 确保数据库架构已创建
            DbContext.Database.EnsureCreated();
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        public void ClearData()
        {
            // SQLite支持事务，可以更高效地清理数据
            using var transaction = DbContext.Database.BeginTransaction();
            try
            {
                // 清除所有用户数据
                DbContext.Database.ExecuteSqlRaw("DELETE FROM Users");

                // 重置自增ID（如果有）
                DbContext.Database.ExecuteSqlRaw("DELETE FROM sqlite_sequence WHERE name='Users'");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                // 回退到简单清理
                DbContext.Users.RemoveRange(DbContext.Users);
                DbContext.SaveChanges();
            }

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

        /// <summary>
        /// 开始新的数据库事务
        /// </summary>
        public IDbContextTransaction BeginTransaction()
        {
            return DbContext.Database.BeginTransaction();
        }

        /// <summary>
        /// 执行原始SQL（用于测试特定场景）
        /// </summary>
        public int ExecuteSql(string sql, params object[] parameters)
        {
            return DbContext.Database.ExecuteSqlRaw(sql, parameters);
        }

        public void Dispose()
        {
            // 清理顺序很重要
            DbContext?.Dispose();
            ServiceProvider?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
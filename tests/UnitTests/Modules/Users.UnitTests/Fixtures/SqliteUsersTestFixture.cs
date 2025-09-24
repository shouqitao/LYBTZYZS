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
using System.Collections.Generic;

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

            // 将 DbContext 注册为 Scoped 以支持测试隔离
            // 使用工厂方法创建SqliteAppDbContext来处理RowVersion
            services.AddScoped<AppDbContext>(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlite(_connection);
                optionsBuilder.EnableSensitiveDataLogging();
                return new SqliteAppDbContext(optionsBuilder.Options);
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

            // 获取常用服务实例（非Scoped服务）
            MemoryCache = ServiceProvider.GetRequiredService<IMemoryCache>();
            Mapper = ServiceProvider.GetRequiredService<IMapper>();
            UserOptions = ServiceProvider.GetRequiredService<IOptions<UserOptions>>().Value;
            DefaultPasswordService = ServiceProvider.GetRequiredService<DefaultPasswordService>();

            // 使用Scope确保数据库架构已创建
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }
        }

        /// <summary>
        /// 清理测试数据
        /// </summary>
        public void ClearData()
        {
            // 使用独立的Scope进行数据清理，避免影响测试中的DbContext
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // SQLite支持事务，可以更高效地清理数据
                using var transaction = dbContext.Database.BeginTransaction();
                try
                {
                    // 清除所有用户数据
                    dbContext.Database.ExecuteSqlRaw("DELETE FROM Users");

                    // 重置自增ID（如果有）
                    dbContext.Database.ExecuteSqlRaw("DELETE FROM sqlite_sequence WHERE name='Users'");

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    // 回退到简单清理
                    dbContext.Users.RemoveRange(dbContext.Users);
                    dbContext.SaveChanges();
                }
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
        /// <param name="dbContext">使用的DbContext实例</param>
        public IDbContextTransaction BeginTransaction(AppDbContext dbContext)
        {
            return dbContext.Database.BeginTransaction();
        }

        /// <summary>
        /// 执行原始SQL（用于测试特定场景）
        /// </summary>
        /// <param name="dbContext">使用的DbContext实例</param>
        public int ExecuteSql(AppDbContext dbContext, string sql, params object[] parameters)
        {
            // SQLite需要使用SqliteParameter
            var sqliteParams = new List<Microsoft.Data.Sqlite.SqliteParameter>();

            // 替换SQL中的?占位符为@p0, @p1等，并创建参数
            var modifiedSql = sql;
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramName = $"@p{i}";
                // 替换第一个?为参数名
                var index = modifiedSql.IndexOf('?');
                if (index >= 0)
                {
                    modifiedSql = modifiedSql.Remove(index, 1).Insert(index, paramName);
                }
                sqliteParams.Add(new Microsoft.Data.Sqlite.SqliteParameter(paramName, parameters[i]));
            }

            return dbContext.Database.ExecuteSqlRaw(modifiedSql, sqliteParams.ToArray());
        }

        public void Dispose()
        {
            // 清理顺序很重要
            ServiceProvider?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
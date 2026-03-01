using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LYBT.Tests.Common
{
    /// <summary>
    /// 测试基类 - 提供通用的测试基础设施
    /// 注：项目已迁移到Mapperly，不再需要AutoMapper配置
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IServiceCollection Services;
        protected readonly ILogger MockLogger;

        protected TestBase()
        {
            Services = new ServiceCollection();

            // 配置日志
            MockLogger = Substitute.For<ILogger>();
            Services.AddSingleton(MockLogger);
            Services.AddLogging(builder => builder.AddDebug());

            // 允许子类添加额外的服务
            ConfigureServices(Services);

            ServiceProvider = Services.BuildServiceProvider();
        }

        /// <summary>
        /// 子类可以重写此方法来添加额外的服务配置
        /// </summary>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // 默认为空，子类可重写
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        protected T? GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// 获取必需的服务实例
        /// </summary>
        protected T GetRequiredService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 创建NSubstitute替代对象
        /// </summary>
        protected T CreateMock<T>() where T : class
        {
            var substitute = Substitute.For<T>();
            Services.AddSingleton(substitute);
            return substitute;
        }

        /// <summary>
        /// 创建指定类型的Logger替代对象
        /// </summary>
        protected ILogger<T> CreateLoggerMock<T>()
        {
            var substitute = Substitute.For<ILogger<T>>();
            Services.AddSingleton(substitute);
            return substitute;
        }

        /// <summary>
        /// 创建指定类型的Logger实例（用于internal类型，避免代理创建失败）
        /// Issue #2244: Repository类是internal的，NSubstitute无法为强命名程序集创建代理
        /// </summary>
        protected ILogger<T> CreateLogger<T>()
        {
            var logger = NullLogger<T>.Instance;
            Services.AddSingleton<ILogger<T>>(logger);
            return logger;
        }

        /// <summary>
        /// 创建InMemory数据库上下文（用于Repository测试）
        /// Issue #2244: AppDbContext无法使用替代，需要真实的InMemory数据库
        /// </summary>
        protected AppDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        public virtual void Dispose()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// 数据库测试基类 - 提供In-Memory数据库支持
    /// </summary>
    public abstract class DatabaseTestBase : TestBase
    {
        protected DatabaseTestBase() : base()
        {
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 配置In-Memory数据库
            ConfigureDatabase(services);
        }

        /// <summary>
        /// 配置数据库 - 子类必须实现
        /// </summary>
        protected abstract void ConfigureDatabase(IServiceCollection services);
    }
}

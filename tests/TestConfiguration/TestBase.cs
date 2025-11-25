using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using LYBT.Infrastructure.Data;

namespace LYBT.Tests.Common
{
    /// <summary>
    /// 测试基类 - 提供通用的测试基础设施
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IServiceCollection Services;
        protected readonly Mock<ILogger> MockLogger;
        protected IMapper Mapper { get; private set; } = null!;

        protected TestBase()
        {
            Services = new ServiceCollection();

            // 配置日志
            MockLogger = new Mock<ILogger>();
            Services.AddSingleton(MockLogger.Object);
            Services.AddLogging(builder => builder.AddDebug());

            // 配置AutoMapper
            ConfigureAutoMapper();

            // 允许子类添加额外的服务
            ConfigureServices(Services);

            ServiceProvider = Services.BuildServiceProvider();

            // 初始化Mapper
            Mapper = ServiceProvider.GetService<IMapper>()!;
        }

        /// <summary>
        /// 配置AutoMapper - 使用统一的测试配置
        /// </summary>
        private void ConfigureAutoMapper()
        {
            // 使用统一的AutoMapper测试配置
            var mapper = AutoMapperTestConfiguration.GetMapper();
            var mapperConfig = AutoMapperTestConfiguration.GetConfiguration();

            // 如果子类需要自定义配置，创建隔离的Mapper
            if (HasCustomMapperConfiguration())
            {
                mapper = AutoMapperTestConfiguration.CreateIsolatedMapper(cfg =>
                {
                    ConfigureMapperProfiles(cfg);
                });

                Services.AddSingleton<IMapper>(mapper);
            }
            else
            {
                Services.AddSingleton<IMapper>(mapper);
                Services.AddSingleton(mapperConfig);
            }
        }

        /// <summary>
        /// 检查是否有自定义的Mapper配置
        /// </summary>
        private bool HasCustomMapperConfiguration()
        {
            // 通过反射检查子类是否重写了ConfigureMapperProfiles方法
            var method = GetType().GetMethod("ConfigureMapperProfiles",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return method != null && method.DeclaringType != typeof(TestBase);
        }

        /// <summary>
        /// 子类可以重写此方法来添加额外的服务配置
        /// </summary>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // 默认为空，子类可重写
        }

        /// <summary>
        /// 子类可以重写此方法来添加额外的AutoMapper配置
        /// </summary>
        protected virtual void ConfigureMapperProfiles(IMapperConfigurationExpression cfg)
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
        /// 创建Mock对象
        /// </summary>
        protected Mock<T> CreateMock<T>() where T : class
        {
            var mock = new Mock<T>();
            Services.AddSingleton(mock.Object);
            return mock;
        }

        /// <summary>
        /// 创建指定类型的Logger Mock
        /// </summary>
        protected Mock<ILogger<T>> CreateLoggerMock<T>()
        {
            var mock = new Mock<ILogger<T>>();
            Services.AddSingleton(mock.Object);
            return mock;
        }

        /// <summary>
        /// 创建指定类型的Logger实例（用于internal类型，避免Moq代理创建失败）
        /// Issue #2244: Repository类是internal的，Moq无法为强命名程序集创建代理
        /// </summary>
        protected ILogger<T> CreateLogger<T>()
        {
            var logger = NullLogger<T>.Instance;
            Services.AddSingleton<ILogger<T>>(logger);
            return logger;
        }

        /// <summary>
        /// 创建InMemory数据库上下文（用于Repository测试）
        /// Issue #2244: AppDbContext无法使用Mock，需要真实的InMemory数据库
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

using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Infrastructure.DataSources.Remote;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.LocalData.Initialization;
using LYBT.Desktop.LocalData.Services;
using SyncService = LYBT.Desktop.LocalData.Services.SyncService;
using LYBT.Desktop.Shell.Services.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// DataSource DI 注册扩展方法
/// OpenSpec: implement-local-mode
/// 根据 ConnectionMode 注册不同的 DataSource 实现
/// </summary>
public static class DataSourceRegistrationExtensions
{
    /// <summary>
    /// 本地模式默认连接字符串 (SQL Server LocalDB)
    /// </summary>
    private const string DefaultLocalConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=LYBTZYZS_Local;Trusted_Connection=True;TrustServerCertificate=True;";

    /// <summary>
    /// 注册 DataSource 服务
    /// </summary>
    /// <param name="containerRegistry">容器注册器</param>
    /// <param name="mode">连接模式</param>
    /// <param name="configuration">配置 (可选，用于读取本地连接字符串)</param>
    public static void RegisterDataSources(
        this IContainerRegistry containerRegistry,
        ConnectionMode mode,
        IConfiguration? configuration = null)
    {
        // 注册 CurrentUserProvider（两种模式都需要）
        RegisterCurrentUserProvider(containerRegistry);

        if (mode == ConnectionMode.Remote)
        {
            RegisterRemoteDataSources(containerRegistry);
        }
        else
        {
            var connectionString = configuration?["LocalConnectionString"] ?? DefaultLocalConnectionString;
            RegisterLocalDataSources(containerRegistry, connectionString);
        }

    }

    /// <summary>
    /// 注册 CurrentUserProvider（为 LocalDbContext 提供当前用户信息）
    /// </summary>
    private static void RegisterCurrentUserProvider(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<ICurrentUserProvider, SessionBasedCurrentUserProvider>();
    }

    /// <summary>
    /// 注册远程模式 DataSource（通过 WebAPI）
    /// </summary>
    private static void RegisterRemoteDataSources(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IPatientDataSource, RemotePatientDataSource>();
        containerRegistry.Register<IHerbDataSource, RemoteHerbDataSource>();
        containerRegistry.Register<IFormulaDataSource, RemoteFormulaDataSource>();
        containerRegistry.Register<IMedicalCaseDataSource, RemoteMedicalCaseDataSource>();
        containerRegistry.Register<IUserDataSource, RemoteUserDataSource>();
        containerRegistry.Register<IRegistrationDataSource, RemoteRegistrationDataSource>();
    }

    /// <summary>
    /// 注册本地模式 DataSource（SQL Server LocalDB）
    /// </summary>
    private static void RegisterLocalDataSources(IContainerRegistry containerRegistry, string connectionString)
    {
        // 注册本地数据库上下文
        RegisterLocalDbContext(containerRegistry, connectionString);

        // 注册本地认证服务
        containerRegistry.RegisterSingleton<ILocalAuthService, LocalAuthService>();

        // OpenSpec: implement-data-sync - 注册同步服务
        containerRegistry.RegisterSingleton<ISyncService, SyncService>();

        // US-SYNC-008: 注册模式切换验证器 (本地模式可用时注册)
        RegisterModeSwitchValidator(containerRegistry, connectionString);

        // 注册 DataSource
        containerRegistry.Register<IPatientDataSource, LocalPatientDataSource>();
        containerRegistry.Register<IHerbDataSource, LocalHerbDataSource>();
        containerRegistry.Register<IFormulaDataSource, LocalFormulaDataSource>();
        containerRegistry.Register<IMedicalCaseDataSource, LocalMedicalCaseDataSource>();
        containerRegistry.Register<IUserDataSource, LocalUserDataSource>();
        containerRegistry.Register<IRegistrationDataSource, LocalRegistrationDataSource>();
    }

    /// <summary>
    /// US-SYNC-008: 注册模式切换验证器
    /// 本地模式下注册完整验证器 (可检查 Active/Suspended 医案)
    /// </summary>
    private static void RegisterModeSwitchValidator(IContainerRegistry containerRegistry, string connectionString)
    {
        containerRegistry.RegisterSingleton<IModeSwitchValidator>(resolver =>
        {
            var medicalCaseDataSource = resolver.Resolve<IMedicalCaseDataSource>();
            var loggerFactory = resolver.Resolve<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ModeSwitchValidator>();
            return new ModeSwitchValidator(medicalCaseDataSource, connectionString, logger);
        });
    }

    /// <summary>
    /// 注册本地数据库上下文和相关服务 (SQL Server LocalDB)
    /// </summary>
    private static void RegisterLocalDbContext(IContainerRegistry containerRegistry, string connectionString)
    {
        // 注册 DbContextOptions (SQL Server LocalDB)
        containerRegistry.RegisterSingleton<DbContextOptions<LocalDbContext>>(_ =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return optionsBuilder.Options;
        });

        // 注册 LocalDbContext（每次请求新实例，避免并发问题）
        containerRegistry.Register<LocalDbContext>(resolver =>
        {
            var options = resolver.Resolve<DbContextOptions<LocalDbContext>>();
            var currentUserProvider = resolver.Resolve<ICurrentUserProvider>();
            return new LocalDbContext(options, currentUserProvider);
        });

        // 注册数据库初始化器
        containerRegistry.RegisterSingleton<DatabaseInitializer>(resolver =>
        {
            var context = resolver.Resolve<LocalDbContext>();
            var loggerFactory = resolver.Resolve<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<DatabaseInitializer>();
            return new DatabaseInitializer(context, logger);
        });
    }
}

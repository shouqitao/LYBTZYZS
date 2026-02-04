using System.IO;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Infrastructure.DataSources.Mappers;
using LYBT.Desktop.Infrastructure.DataSources.Remote;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Desktop.LocalData.Initialization;
using LYBT.Desktop.LocalData.Services;
using SyncService = LYBT.Desktop.LocalData.Services.SyncService;
using LYBT.Desktop.Shell.Services.Session;
using Microsoft.EntityFrameworkCore;
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
    /// 注册 DataSource 服务
    /// </summary>
    /// <param name="containerRegistry">容器注册器</param>
    /// <param name="mode">连接模式</param>
    public static void RegisterDataSources(
        this IContainerRegistry containerRegistry,
        ConnectionMode mode)
    {
        // 注册 CurrentUserProvider（两种模式都需要）
        RegisterCurrentUserProvider(containerRegistry);

        if (mode == ConnectionMode.Remote)
        {
            RegisterRemoteDataSources(containerRegistry);
        }
        else
        {
            RegisterLocalDataSources(containerRegistry);
        }

        // DataSource Mapper 统一注册（Remote 模式需要）
        RegisterDataSourceMappers(containerRegistry);
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
    }

    /// <summary>
    /// 注册本地模式 DataSource（SQLite）
    /// </summary>
    private static void RegisterLocalDataSources(IContainerRegistry containerRegistry)
    {
        // 注册本地数据库上下文
        RegisterLocalDbContext(containerRegistry);

        // 注册本地认证服务
        containerRegistry.RegisterSingleton<ILocalAuthService, LocalAuthService>();

        // OpenSpec: implement-data-sync - 注册同步服务
        containerRegistry.RegisterSingleton<ISyncService, SyncService>();

        // 注册 DataSource
        containerRegistry.Register<IPatientDataSource, LocalPatientDataSource>();
        containerRegistry.Register<IHerbDataSource, LocalHerbDataSource>();
        containerRegistry.Register<IFormulaDataSource, LocalFormulaDataSource>();
        containerRegistry.Register<IMedicalCaseDataSource, LocalMedicalCaseDataSource>();
        containerRegistry.Register<IUserDataSource, LocalUserDataSource>();
    }

    /// <summary>
    /// 注册 DataSource Mapper（DTO ↔ Entity 映射）
    /// </summary>
    private static void RegisterDataSourceMappers(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<PatientDataSourceMapper>();
        containerRegistry.RegisterSingleton<HerbDataSourceMapper>();
        containerRegistry.RegisterSingleton<FormulaDataSourceMapper>();
        containerRegistry.RegisterSingleton<MedicalCaseDataSourceMapper>();
        containerRegistry.RegisterSingleton<UserDataSourceMapper>();
    }

    /// <summary>
    /// 注册本地数据库上下文和相关服务
    /// </summary>
    private static void RegisterLocalDbContext(IContainerRegistry containerRegistry)
    {
        // 数据库路径: %APPDATA%\LYBTZYZS\lybtzyzs.db
        var dbPath = DatabaseInitializer.DatabasePath;
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // 注册 DbContextOptions
        containerRegistry.RegisterSingleton<DbContextOptions<LocalDbContext>>(_ =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
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

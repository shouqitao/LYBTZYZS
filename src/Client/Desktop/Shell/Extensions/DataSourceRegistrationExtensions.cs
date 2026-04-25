using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Initialization;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.Initialization;
using LYBT.Desktop.LocalData.Repositories;
using LYBT.Desktop.LocalData.Services;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Registration.Repositories;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.Users.Repositories;
using LYBT.LocalWebAPI;
using LYBT.LocalWebAPI.Repositories;
using LocalDbBackupService = LYBT.Desktop.LocalData.Services.LocalDbBackupService;
using SyncService = LYBT.Desktop.LocalData.Services.SyncService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// Repository DI 工厂注册 (SYNC-D02/D03)
/// 两套基础设施始终注册，Repository 通过工厂模式在 resolve 时根据当前模式选择实现。
/// 远程模式: Repository 通过 Refit API 客户端访问 WebAPI
/// 本地模式: Repository 通过 EF Core + LocalDbContext 访问 SQL Server LocalDB
/// </summary>
public static class DataSourceRegistrationExtensions
{
    /// <summary>
    /// 本地模式默认连接字符串 (SQL Server LocalDB)
    /// </summary>
    private const string DefaultLocalConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=LYBTZYZS_Local;Trusted_Connection=True;TrustServerCertificate=True;";

    /// <summary>
    /// 注册 IConnectionModeProvider + Repository 工厂 + 双模式基础设施 (SYNC-D03)
    /// 两套基础设施始终注册，支持运行时切换。
    /// </summary>
    public static void RegisterRepositories(
        this IContainerRegistry containerRegistry,
        ConnectionMode initialMode,
        IConfiguration? configuration = null)
    {
        var connectionString = configuration?["LocalConnectionString"] ?? DefaultLocalConnectionString;

        // 1. 注册 CurrentUserProvider (两种模式都需要)
        containerRegistry.RegisterSingleton<ICurrentUserProvider, SessionBasedCurrentUserProvider>();

        // 2. 始终注册本地基础设施 (SYNC-D03: 运行时切换需要两套都可用)
        RegisterLocalInfrastructure(containerRegistry, connectionString);

        // 3. 注册 6 个 Repository (工厂模式，resolve 时根据当前模式选择)
        RegisterRepositoryFactories(containerRegistry);

        // 4. 注册 IConnectionModeProvider (Singleton, 依赖验证器和导航协调器)
        RegisterConnectionModeProvider(containerRegistry, initialMode);
    }

    /// <summary>
    /// 注册 ConnectionModeProvider (SYNC-D03)
    /// 延迟注入依赖: IModeSwitchValidator, IActiveConsultationService, INavigationCoordinator
    /// </summary>
    private static void RegisterConnectionModeProvider(
        IContainerRegistry containerRegistry, ConnectionMode initialMode)
    {
        containerRegistry.RegisterSingleton<IConnectionModeProvider>(resolver =>
        {
            var logger = resolver.Resolve<ILogger<ConnectionModeProvider>>();
            var validator = resolver.Resolve<IModeSwitchValidator>();
            var activeConsultation = resolver.Resolve<IActiveConsultationService>();
            var navigation = resolver.Resolve<INavigationCoordinator>();
            var databaseInitializer = resolver.Resolve<IDatabaseInitializer>();
            LocalWebApiHost? localWebApiHost = null;
            try { localWebApiHost = resolver.Resolve<LocalWebApiHost>(); }
            catch { /* not registered in all configurations */ }
            return new ConnectionModeProvider(initialMode, logger, validator, activeConsultation, navigation, databaseInitializer, localWebApiHost);
        });
    }

    /// <summary>
    /// 工厂模式注册 6 个 Repository (SYNC-D03)
    /// 每次 resolve 时根据 IConnectionModeProvider.CurrentMode 选择实现。
    /// Transient 生命周期确保模式切换后获取正确实现。
    /// 三模式: Remote (Refit API) | Local (LocalDbContext) | LocalWebAPI (HTTP Proxy)
    /// </summary>
    private static void RegisterRepositoryFactories(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IPatientRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new PatientRepository(resolver.Resolve<Desktop.Contracts.Api.IPatientApi>(), resolver.Resolve<ILogger<PatientRepository>>());
            if (mode == ConnectionMode.Local)
                return new LocalPatientRepository(resolver.Resolve<LocalDbContext>(), resolver.Resolve<ILogger<LocalPatientRepository>>());
            return new HttpPatientRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpPatientRepository>>());
        });

        containerRegistry.Register<IHerbRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new HerbRepository(resolver.Resolve<Desktop.Contracts.Api.IHerbApi>(), resolver.Resolve<ILogger<HerbRepository>>());
            if (mode == ConnectionMode.Local)
                return new LocalHerbRepository(resolver.Resolve<LocalDbContext>(), resolver.Resolve<ILogger<LocalHerbRepository>>());
            return new HttpHerbRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpHerbRepository>>());
        });

        containerRegistry.Register<IFormulaRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new FormulaRepository(resolver.Resolve<Desktop.Contracts.Api.IFormulaApi>(), resolver.Resolve<ILogger<FormulaRepository>>());
            if (mode == ConnectionMode.Local)
                return new LocalFormulaRepository(resolver.Resolve<LocalDbContext>(), resolver.Resolve<ILogger<LocalFormulaRepository>>());
            return new HttpFormulaRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpFormulaRepository>>());
        });

        containerRegistry.Register<IUserRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new UserRepository(resolver.Resolve<Desktop.Contracts.Api.IUserApi>(), resolver.Resolve<ILogger<UserRepository>>());
            if (mode == ConnectionMode.Local)
                return new LocalUserRepository(resolver.Resolve<LocalDbContext>(), resolver.Resolve<ILogger<LocalUserRepository>>());
            return new HttpUserRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpUserRepository>>());
        });

        containerRegistry.Register<IMedicalCaseRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new MedicalCaseRepository(resolver.Resolve<Desktop.Contracts.Api.IMedicalCaseApi>(), resolver.Resolve<ILogger<MedicalCaseRepository>>());
            if (mode == ConnectionMode.Local)
                return new LocalMedicalCaseRepository(resolver.Resolve<LocalDbContext>(), resolver.Resolve<ILogger<LocalMedicalCaseRepository>>());
            return new HttpMedicalCaseRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpMedicalCaseRepository>>());
        });

        containerRegistry.Register<IRegistrationRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new RegistrationRepository(resolver.Resolve<Desktop.Contracts.Api.IRegistrationApi>(), resolver.Resolve<ILogger<RegistrationRepository>>());
            if (mode == ConnectionMode.Local)
                return new LocalRegistrationRepository(resolver.Resolve<LocalDbContext>(), resolver.Resolve<ILogger<LocalRegistrationRepository>>());
            return new HttpRegistrationRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpRegistrationRepository>>());
        });
    }

    /// <summary>
    /// 创建 LocalWebAPI HTTP 客户端 (动态端口从 LocalWebApiHost 获取)
    /// </summary>
    private static HttpClient CreateLocalWebApiHttpClient(IContainerResolver resolver)
    {
        var host = resolver.Resolve<LocalWebApiHost>();
        var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{host.Port}"), Timeout = TimeSpan.FromSeconds(30) };
        return client;
    }

    /// <summary>
    /// 注册本地模式基础设施 (SYNC-D03: 始终注册，支持运行时切换)
    /// </summary>
    private static void RegisterLocalInfrastructure(
        IContainerRegistry containerRegistry, string connectionString)
    {
        RegisterLocalDbContext(containerRegistry, connectionString);
        containerRegistry.RegisterSingleton<ILocalAuthService, LocalAuthService>();
        containerRegistry.RegisterSingleton<ILocalDbBackupService, LocalDbBackupService>();
        containerRegistry.RegisterSingleton<ISyncService, SyncService>();
        RegisterModeSwitchValidator(containerRegistry, connectionString);

        var dbPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LYBT", "localwebapi.db");
        containerRegistry.RegisterSingleton<LocalWebApiHost>(resolver =>
        {
            var loggerFactory = resolver.Resolve<ILoggerFactory>();
            return new LocalWebApiHost(dbPath, loggerFactory.CreateLogger<LocalWebApiHost>());
        });
    }

    /// <summary>
    /// US-SYNC-008: 注册模式切换验证器
    /// </summary>
    private static void RegisterModeSwitchValidator(
        IContainerRegistry containerRegistry, string connectionString)
    {
        containerRegistry.RegisterSingleton<IModeSwitchValidator>(resolver =>
        {
            var loggerFactory = resolver.Resolve<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ModeSwitchValidator>();
            return new ModeSwitchValidator(connectionString, logger);
        });
    }

    /// <summary>
    /// 注册本地数据库上下文 (SQL Server LocalDB)
    /// </summary>
    private static void RegisterLocalDbContext(
        IContainerRegistry containerRegistry, string connectionString)
    {
        // DbContextOptions (Singleton, 配置不变)
        containerRegistry.RegisterSingleton<DbContextOptions<LocalDbContext>>(_ =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<LocalDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return optionsBuilder.Options;
        });

        // LocalDbContext (Transient, 每次请求新实例避免并发问题)
        containerRegistry.Register<LocalDbContext>(resolver =>
        {
            var options = resolver.Resolve<DbContextOptions<LocalDbContext>>();
            var currentUserProvider = resolver.Resolve<ICurrentUserProvider>();
            return new LocalDbContext(options, currentUserProvider);
        });

        // 数据库初始化器 (Singleton) - 延迟初始化，使用工厂模式
        containerRegistry.RegisterSingleton<DatabaseInitializer>(resolver =>
        {
            var loggerFactory = resolver.Resolve<ILoggerFactory>();
            return new DatabaseInitializer(
                () => resolver.Resolve<LocalDbContext>(),
                loggerFactory.CreateLogger<DatabaseInitializer>());
        });

        // 数据库初始化器接口注册
        containerRegistry.RegisterSingleton<IDatabaseInitializer>(resolver =>
            resolver.Resolve<DatabaseInitializer>());
    }
}

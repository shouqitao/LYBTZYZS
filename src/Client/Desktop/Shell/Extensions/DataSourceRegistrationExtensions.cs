using System.Net.Http;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Initialization;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Registration.Repositories;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.LocalData.Services;
using LYBT.Desktop.Users.Repositories;
using LYBT.LocalWebAPI.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// Repository DI 工厂注册 (SYNC-D02/D03)
/// 两套基础设施始终注册，Repository 通过工厂模式在 resolve 时根据当前模式选择实现。
/// 远程模式: Repository 通过 Refit API 客户端访问远程 WebAPI
/// 本地模式: Repository 通过 HttpClient 访问本地独立 WebAPI (localhost)
/// </summary>
public static class DataSourceRegistrationExtensions
{
    /// <summary>
    /// 注册 IConnectionModeProvider + Repository 工厂 + 双模式基础设施 (SYNC-D03)
    /// 两套基础设施始终注册，支持运行时切换。
    /// </summary>
    public static void RegisterRepositories(
        this IContainerRegistry containerRegistry,
        ConnectionMode initialMode,
        IConfiguration? configuration = null)
    {
        // 1. 注册 CurrentUserProvider (两种模式都需要)
        containerRegistry.RegisterSingleton<ICurrentUserProvider, SessionBasedCurrentUserProvider>();

        // 2. 注册模式切换验证器
        RegisterModeSwitchValidator(containerRegistry);

        // 3. 注册 6 个 Repository (工厂模式，resolve 时根据当前模式选择)
        RegisterRepositoryFactories(containerRegistry);

        // 4. 注册 IConnectionModeProvider (Singleton)
        RegisterConnectionModeProvider(containerRegistry, initialMode);
    }

    /// <summary>
    /// 注册 ConnectionModeProvider (SYNC-D03)
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
            return new ConnectionModeProvider(initialMode, logger, validator, activeConsultation, navigation);
        });
    }

    /// <summary>
    /// 工厂模式注册 6 个 Repository (SYNC-D03)
    /// 每次 resolve 时根据 IConnectionModeProvider.CurrentMode 选择实现。
    /// Transient 生命周期确保模式切换后获取正确实现。
    /// 双模式: Remote (Refit API) | Local (HttpXxxRepository → localhost WebAPI)
    /// </summary>
    private static void RegisterRepositoryFactories(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IPatientRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new PatientRepository(resolver.Resolve<Desktop.Contracts.Api.IPatientApi>(), resolver.Resolve<ILogger<PatientRepository>>());
            return new HttpPatientRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpPatientRepository>>());
        });

        containerRegistry.Register<IHerbRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new HerbRepository(resolver.Resolve<Desktop.Contracts.Api.IHerbApi>(), resolver.Resolve<ILogger<HerbRepository>>());
            return new HttpHerbRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpHerbRepository>>());
        });

        containerRegistry.Register<IFormulaRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new FormulaRepository(resolver.Resolve<Desktop.Contracts.Api.IFormulaApi>(), resolver.Resolve<ILogger<FormulaRepository>>());
            return new HttpFormulaRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpFormulaRepository>>());
        });

        containerRegistry.Register<IUserRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new UserRepository(resolver.Resolve<Desktop.Contracts.Api.IUserApi>(), resolver.Resolve<ILogger<UserRepository>>());
            return new HttpUserRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpUserRepository>>());
        });

        containerRegistry.Register<IMedicalCaseRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new MedicalCaseRepository(resolver.Resolve<Desktop.Contracts.Api.IMedicalCaseApi>(), resolver.Resolve<ILogger<MedicalCaseRepository>>());
            return new HttpMedicalCaseRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpMedicalCaseRepository>>());
        });

        containerRegistry.Register<IRegistrationRepository>(resolver =>
        {
            var mode = resolver.Resolve<IConnectionModeProvider>().CurrentMode;
            if (mode == ConnectionMode.Remote)
                return new RegistrationRepository(resolver.Resolve<Desktop.Contracts.Api.IRegistrationApi>(), resolver.Resolve<ILogger<RegistrationRepository>>());
            return new HttpRegistrationRepository(CreateLocalWebApiHttpClient(resolver), resolver.Resolve<ILogger<HttpRegistrationRepository>>());
        });
    }

    /// <summary>
    /// 创建本地 WebAPI HTTP 客户端 (连接到独立进程的 localhost WebAPI)
    /// </summary>
    private static HttpClient CreateLocalWebApiHttpClient(IContainerProvider resolver)
    {
        var configuration = resolver.Resolve<IConfiguration>();
        var port = configuration["LocalWebApi:Port"] ?? "5290";
        var client = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return client;
    }

    /// <summary>
    /// US-SYNC-008: 注册模式切换验证器
    /// </summary>
    private static void RegisterModeSwitchValidator(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IModeSwitchValidator>(resolver =>
        {
            var loggerFactory = resolver.Resolve<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ModeSwitchValidator>();
            return new ModeSwitchValidator(logger);
        });
    }
}

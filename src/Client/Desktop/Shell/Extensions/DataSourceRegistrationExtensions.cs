using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Initialization;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Registration.Repositories;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.Users.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Ioc;

namespace LYBT.Desktop.Shell.Extensions;

/// <summary>
/// Repository DI 注册 — 通过 IApiClient 统一访问
/// </summary>
public static class DataSourceRegistrationExtensions
{
    /// <summary>
    /// 注册 Repository + CurrentUserProvider
    /// </summary>
    public static void RegisterRepositories(
        this IContainerRegistry containerRegistry,
        IConfiguration? configuration = null)
    {
        containerRegistry.RegisterSingleton<ICurrentUserProvider, SessionBasedCurrentUserProvider>();
        RegisterRemoteRepositories(containerRegistry);
    }

    private static void RegisterRemoteRepositories(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IPatientRepository>(resolver =>
            new PatientRepository(
                resolver.Resolve<IApiClient>(),
                resolver.Resolve<ILogger<PatientRepository>>()));

        containerRegistry.Register<IHerbRepository>(resolver =>
            new HerbRepository(
                resolver.Resolve<IApiClient>(),
                resolver.Resolve<ILogger<HerbRepository>>()));

        containerRegistry.Register<IFormulaRepository>(resolver =>
            new FormulaRepository(
                resolver.Resolve<IApiClient>(),
                resolver.Resolve<ILogger<FormulaRepository>>()));

        containerRegistry.Register<IUserRepository>(resolver =>
            new UserRepository(
                resolver.Resolve<IApiClient>(),
                resolver.Resolve<ILogger<UserRepository>>()));

        containerRegistry.Register<IMedicalCaseRepository>(resolver =>
            new MedicalCaseRepository(
                resolver.Resolve<IApiClient>(),
                resolver.Resolve<ILogger<MedicalCaseRepository>>()));

        containerRegistry.Register<IRegistrationRepository>(resolver =>
            new RegistrationRepository(
                resolver.Resolve<IApiClient>(),
                resolver.Resolve<ILogger<RegistrationRepository>>()));
    }
}

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
/// Repository DI 注册 — 远程模式 (Refit API)
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
                resolver.Resolve<Desktop.Contracts.Api.IPatientApi>(),
                resolver.Resolve<Desktop.Contracts.Api.ILocalPatientApi>(),
                resolver.Resolve<IApiRouter>(),
                resolver.Resolve<ILogger<PatientRepository>>()));

        containerRegistry.Register<IHerbRepository>(resolver =>
            new HerbRepository(
                resolver.Resolve<Desktop.Contracts.Api.IHerbApi>(),
                resolver.Resolve<Desktop.Contracts.Api.ILocalHerbApi>(),
                resolver.Resolve<IApiRouter>(),
                resolver.Resolve<ILogger<HerbRepository>>()));

        containerRegistry.Register<IFormulaRepository>(resolver =>
            new FormulaRepository(
                resolver.Resolve<Desktop.Contracts.Api.IFormulaApi>(),
                resolver.Resolve<Desktop.Contracts.Api.ILocalFormulaApi>(),
                resolver.Resolve<IApiRouter>(),
                resolver.Resolve<ILogger<FormulaRepository>>()));

        containerRegistry.Register<IUserRepository>(resolver =>
            new UserRepository(
                resolver.Resolve<Desktop.Contracts.Api.IUserApi>(),
                resolver.Resolve<Desktop.Contracts.Api.ILocalUserApi>(),
                resolver.Resolve<IApiRouter>(),
                resolver.Resolve<ILogger<UserRepository>>()));

        containerRegistry.Register<IMedicalCaseRepository>(resolver =>
            new MedicalCaseRepository(
                resolver.Resolve<Desktop.Contracts.Api.IMedicalCaseApi>(),
                resolver.Resolve<Desktop.Contracts.Api.ILocalMedicalCaseApi>(),
                resolver.Resolve<IApiRouter>(),
                resolver.Resolve<ILogger<MedicalCaseRepository>>()));

        containerRegistry.Register<IRegistrationRepository>(resolver =>
            new RegistrationRepository(
                resolver.Resolve<Desktop.Contracts.Api.IRegistrationApi>(),
                resolver.Resolve<Desktop.Contracts.Api.ILocalRegistrationApi>(),
                resolver.Resolve<IApiRouter>(),
                resolver.Resolve<ILogger<RegistrationRepository>>()));
    }
}

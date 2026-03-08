using LYBT.Desktop.Admin;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Auth;
using LYBT.Desktop.Clinical;
using LYBT.Desktop.Formula;
using LYBT.Desktop.Formula.Repositories;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Modules;
using LYBT.Desktop.Foundation.Performance;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Herbs;
using LYBT.Desktop.Herbs.Repositories;
using LYBT.Desktop.Infrastructure.Http;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.Services.Notifications;
using LYBT.Desktop.MedicalCase;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Patients;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services;
using LYBT.Desktop.Registration;
using LYBT.Desktop.Registration.Repositories;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Desktop.Printing.Services;
using LYBT.Desktop.Shell.Services;
using LYBT.Desktop.Shell.Services.Bootstrap;
using LYBT.Desktop.Shell.Services.HealthCheck;
using LYBT.Desktop.Shell.Services.Login;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Desktop.Users;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.ExceptionHandling.Handlers;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Serilog;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>日志服务注册扩展方法</summary>
    public static class LoggingRegistrationExtensions
    {
        /// <summary>注册所有日志服务</summary>
        public static void RegisterLogging(this IContainerRegistry containerRegistry)
        {
            RegisterLoggerFactory(containerRegistry);
            RegisterInfrastructureLoggers(containerRegistry);
            RegisterFoundationLoggers(containerRegistry);
            RegisterPresentationAndShellLoggers(containerRegistry);
            RegisterModuleLoggers(containerRegistry);
            RegisterRepositoryLoggers(containerRegistry);
            RegisterServiceLoggers(containerRegistry);
            RegisterComponentLoggers(containerRegistry);
        }

        /// <summary>
        /// 注册 DataSource 相关 Logger
        /// OpenSpec: implement-local-mode
        /// </summary>
        public static void RegisterDataSourceLoggers(this IContainerRegistry containerRegistry, ConnectionMode mode)
        {
            if (mode == ConnectionMode.Local)
            {
                RegisterLogger<LYBT.Desktop.LocalData.Context.LocalDbContext>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.Initialization.DatabaseInitializer>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.Services.LocalAuthService>(containerRegistry);
                // OpenSpec: implement-data-sync
                RegisterLogger<LYBT.Desktop.LocalData.Services.SyncService>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalPatientDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalHerbDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalFormulaDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalMedicalCaseDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalUserDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.LocalData.DataSources.LocalRegistrationDataSource>(containerRegistry);
            }
            else
            {
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemotePatientDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteHerbDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteFormulaDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteMedicalCaseDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteUserDataSource>(containerRegistry);
                RegisterLogger<LYBT.Desktop.Infrastructure.DataSources.Remote.RemoteRegistrationDataSource>(containerRegistry);
            }
        }

        /// <summary>注册LoggerFactory和泛型ILogger&lt;&gt;</summary>
        /// <remarks>refactor-logging-system: 使用Serilog作为日志提供程序</remarks>
        private static void RegisterLoggerFactory(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
                LoggerFactory.Create(builder => builder.AddSerilog(dispose: false)));
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>通用Logger注册辅助方法</summary>
        private static void RegisterLogger<T>(IContainerRegistry containerRegistry) =>
            containerRegistry.RegisterSingleton<ILogger<T>>(resolver => resolver.Resolve<ILoggerFactory>().CreateLogger<T>());

        /// <summary>注册Infrastructure层Logger</summary>
        private static void RegisterInfrastructureLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<MainWindowServicesFacade>(containerRegistry);
            // [已删除] RegisterLogger<RoleNavigationService> - OpenSpec: unify-navigation-architecture (ADR-7)
            RegisterLogger<ActiveConsultationService>(containerRegistry);
            RegisterLogger<ApplicationTickService>(containerRegistry);
            RegisterLogger<UserActivityTracker>(containerRegistry);
        }

        /// <summary>注册Foundation层Logger</summary>
        private static void RegisterFoundationLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<LYBT.Desktop.Foundation.Http.ApiService>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Http.AuthorizationMessageHandler>(containerRegistry);
            RegisterLogger<TokenRefreshHandler>(containerRegistry);
            RegisterLogger<LYBT.Desktop.Foundation.Security.AuthenticationService>(containerRegistry);
            RegisterLogger<TokenStorageService>(containerRegistry);
            RegisterLogger<TokenManager>(containerRegistry); // OpenSpec: refactor-login-authentication
            RegisterLogger<CredentialVault>(containerRegistry); // OpenSpec: refactor-login-authentication
            RegisterLogger<LYBT.Desktop.Foundation.Security.AuthenticationStateMachine>(containerRegistry); // OpenSpec: refactor-auth-role-system (Phase 1.1)
            RegisterLogger<LogoutService>(containerRegistry); // OpenSpec: refactor-login-authentication (Phase 2.3)
            RegisterLogger<UsernameStorageService>(containerRegistry);
            // OpenSpec: remove-secure-credential-storage - SecureCredentialStorage已移除
            RegisterLogger<LocalTokenValidator>(containerRegistry);
            RegisterLogger<ModuleLoadingService>(containerRegistry);
            RegisterLogger<StartupOptimizationService>(containerRegistry);
            RegisterLogger<TokenLifecycleService>(containerRegistry);
        }

        /// <summary>注册Presentation和Shell层Logger</summary>
        private static void RegisterPresentationAndShellLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<NotificationService>(containerRegistry);
            RegisterLogger<DesktopExceptionHandler>(containerRegistry);
            RegisterLogger<App>(containerRegistry);
            RegisterLogger<ApplicationInitializationService>(containerRegistry);
            RegisterLogger<ApplicationBootstrapper>(containerRegistry);
            RegisterLogger<ApplicationStateService>(containerRegistry);
            // [已删除] RegisterLogger<NavigationManager> - OpenSpec: unify-navigation-architecture (ADR-7)
            RegisterLogger<MenuManager>(containerRegistry);
            RegisterLogger<NavigationCoordinator>(containerRegistry);

            // Shell启动流程重构 - Phase 1 新增Logger
            RegisterLogger<SessionLifecycleManager>(containerRegistry);

            // Shell启动流程重构 - Phase 2 新增Logger
            RegisterLogger<LoginCoordinator>(containerRegistry);

            // Shell架构整合 - HealthCheckCoordinator Logger
            RegisterLogger<HealthCheckCoordinator>(containerRegistry);
        }

        /// <summary>注册业务模块Logger</summary>
        private static void RegisterModuleLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<AuthenticationModule>(containerRegistry);
            RegisterLogger<UsersModule>(containerRegistry);
            RegisterLogger<PatientsModule>(containerRegistry);
            RegisterLogger<MedicalCaseModule>(containerRegistry);
            // [已删除] RegisterLogger<PrescriptionsModule> - 模块已移除
            RegisterLogger<HerbsModule>(containerRegistry);
            RegisterLogger<FormulaModule>(containerRegistry);
            RegisterLogger<ClinicalModule>(containerRegistry);
            RegisterLogger<AdminModule>(containerRegistry);
            // PRD: registration.md - 挂号管理模块
            RegisterLogger<RegistrationModule>(containerRegistry);
        }

        /// <summary>注册Repository层Logger</summary>
        private static void RegisterRepositoryLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<UserRepository>(containerRegistry);
            RegisterLogger<PatientRepository>(containerRegistry);
            RegisterLogger<HerbRepository>(containerRegistry);
            RegisterLogger<FormulaRepository>(containerRegistry);
            RegisterLogger<MedicalCaseRepository>(containerRegistry);
            // PRD: registration.md - 挂号仓储
            RegisterLogger<RegistrationRepository>(containerRegistry);
        }

        /// <summary>注册业务服务Logger</summary>
        private static void RegisterServiceLoggers(IContainerRegistry containerRegistry)
        {
            // [已删除] RegisterLogger<PrescriptionEditorService> - 服务已删除
            RegisterLogger<SystemSettingsService>(containerRegistry);
            // OpenSpec: create-printing-module - 打印服务Logger
            RegisterLogger<PrescriptionPrintService>(containerRegistry);
        }

        /// <summary>注册Component层Logger（CommandHandler/DataManager/Validator等）</summary>
        private static void RegisterComponentLoggers(IContainerRegistry containerRegistry)
        {
            RegisterLogger<UserService>(containerRegistry);
            // OpenSpec: standardize-service-layer - 统一使用Service命名
            RegisterLogger<FormulaService>(containerRegistry);
            RegisterLogger<PatientService>(containerRegistry);
            // OpenSpec: simplify-desktop-data-layer - HerbService已删除，功能合并到HerbRepository
            RegisterLogger<MedicalCaseService>(containerRegistry);
            // OpenSpec: cleanup-patient-dead-code - PatientStateManager已删除（死代码）
            RegisterLogger<PatientValidator>(containerRegistry);
            // LOG-012: LoggingHttpHandler日志
            RegisterLogger<LoggingHttpHandler>(containerRegistry);
        }
    }
}

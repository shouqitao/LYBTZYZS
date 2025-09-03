using System;
using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AutoMapper;
using Prism.Ioc;
using Refit;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Mapping;
using LYBT.Desktop.Infrastructure;
using LYBT.Desktop.Services;
using LYBT.Desktop.Services.Handlers;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>
    /// 服务注册扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册所有服务
        /// </summary>
        public static void RegisterAllServices(this IContainerRegistry containerRegistry)
        {
            RegisterLogging(containerRegistry);
            RegisterAutoMapper(containerRegistry);
            RegisterCacheServices(containerRegistry);
            RegisterHttpServices(containerRegistry);
            RegisterApiServices(containerRegistry);
            RegisterBusinessServices(containerRegistry);
            RegisterErrorHandlingServices(containerRegistry);  // 添加统一错误处理服务
            RegisterDialogs(containerRegistry);
            RegisterPerformanceServices(containerRegistry);
            RegisterUltraThinkServices(containerRegistry);
            // ViewModels和Views通过Prism的ViewModelLocator自动解析，无需手动注册
        }

        /// <summary>
        /// 注册UltraThink高级服务
        /// </summary>
        private static void RegisterUltraThinkServices(IContainerRegistry containerRegistry)
        {
            // Phase I: 简化主题服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Theming.IThemeService,
                LYBT.Desktop.Core.Services.Theming.ThemeService>();

            // UltraThink Phase H: 高级功能优化服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IStartupOptimizationService,
                LYBT.Desktop.Core.Services.Performance.StartupOptimizationService>();
            
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Settings.IUserPreferencesService,
                LYBT.Desktop.Core.Services.Settings.UserPreferencesService>();
        }

        /// <summary>
        /// 注册统一错误处理服务 - UltraThink简化版
        /// </summary>
        private static void RegisterErrorHandlingServices(IContainerRegistry containerRegistry)
        {
            // 注册统一错误处理器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Services.IStandardErrorHandler,
                LYBT.Desktop.Infrastructure.Services.StandardErrorHandler>();
        }

        /// <summary>
        /// 注册日志服务
        /// </summary>
        private static void RegisterLogging(IContainerRegistry containerRegistry)
        {
            // 注册简单的控制台日志提供程序
            containerRegistry.RegisterSingleton<ILoggerFactory>(() =>
            {
                return LoggerFactory.Create(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Information));
            });

            // 注册泛型日志接口
            containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));
        }

        /// <summary>
        /// 注册AutoMapper
        /// </summary>
        private static void RegisterAutoMapper(IContainerRegistry containerRegistry)
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile());
            }, NullLoggerFactory.Instance);

            var mapper = mapperConfig.CreateMapper();
            containerRegistry.RegisterInstance<IMapper>(mapper);
        }

        /// <summary>
        /// 注册缓存服务
        /// </summary>
        private static void RegisterCacheServices(IContainerRegistry containerRegistry)
        {
            // 注册内存缓存服务
            containerRegistry.RegisterSingleton<IMemoryCache, MemoryCache>();
        }

        /// <summary>
        /// 注册HTTP相关服务
        /// </summary>
        private static void RegisterHttpServices(IContainerRegistry containerRegistry)
        {
            // 注册基础HttpClient
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                return HttpClientFactory.CreateBasicClient(ApiConfiguration.BaseUrl);
            });
        }

        /// <summary>
        /// 注册API服务 - UltraThink统一API客户端管理器
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册认证处理器
            containerRegistry.Register<AuthHeaderHandler>();
            
            // 注册统一API客户端管理器 - 替代原有8个独立API客户端
            containerRegistry.RegisterSingleton<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager, 
                LYBT.Desktop.Infrastructure.Api.UnifiedApiClientManager>();
            
            // 注册各个API接口的便捷访问器（委托给统一管理器）
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IAuthApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().AuthApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IUserApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().UserApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IPatientApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().PatientApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IHerbApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().HerbApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IFormulaApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().FormulaApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IConsultationApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().ConsultationApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IPrescriptionApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().PrescriptionApi);
            containerRegistry.Register<LYBT.Shared.Interfaces.Api.IMedicalCaseApi>(container => 
                container.Resolve<LYBT.Desktop.Infrastructure.Api.IUnifiedApiClientManager>().MedicalCaseApi);
            
            // 注册通用API服务 - UltraThink统一架构：使用完整版Http.ApiService
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Http.IApiService, LYBT.Desktop.Core.Http.ApiService>();
        }

        /// <summary>
        /// 注册业务服务
        /// </summary>
        private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
        {
            RegisterCoreServices(containerRegistry);
            RegisterDomainServices(containerRegistry);
        }

        /// <summary>
        /// 注册核心基础服务
        /// </summary>
        private static void RegisterCoreServices(IContainerRegistry containerRegistry)
        {
            // 权限服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPermissionService, PermissionService>();
            
            // 会话管理服务 - UserSessionManager实现多个接口
            containerRegistry.RegisterSingleton<UserSessionManager>();
            containerRegistry.Register<LYBT.Desktop.Core.Interfaces.Services.IUserSessionManager>(container => container.Resolve<UserSessionManager>());
            containerRegistry.Register<LYBT.Desktop.Core.Interfaces.Services.ITokenManager>(container => container.Resolve<UserSessionManager>());
            
            // 其他核心服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ISessionManager, 
                LYBT.Desktop.Core.Services.SessionManager>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.INotificationService, 
                LYBT.Desktop.Core.Services.NotificationService>();
            // UltraThink统一架构: AuthModule直接实现IAuthenticationService，消除双重服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Auth.Services.AuthModule>();
            containerRegistry.Register<LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService>(container => 
                container.Resolve<LYBT.Desktop.Auth.Services.AuthModule>());
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService>(container =>
                new ErrorHandlingService(container.Resolve<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>()));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Workbench.Core.IWorkbenchRouter, LYBT.Desktop.Workbench.Core.WorkbenchRouter>();
            
            // 主窗口服务门面 - 简化MainWindowViewModel的依赖注入
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IMainWindowServicesFacade, 
                LYBT.Desktop.Core.Services.MainWindowServicesFacade>();
            
            // P7-03: 处方打印服务 - UltraThink标准打印系统
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IPrescriptionPrintService, 
                LYBT.Desktop.Core.Services.PrescriptionPrintService>();
                
            // P7-04: 用户体验优化服务 - UltraThink用户体验增强
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IUserExperienceService, 
                LYBT.Desktop.Core.Services.UserExperienceService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IKeyboardShortcutService, 
                LYBT.Desktop.Core.Services.KeyboardShortcutService>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            // API测试服务
            containerRegistry.RegisterSingleton<ApiTestService>();
            
            // UltraThink统一架构：所有业务模块服务由各自模块注册，实现模块自治
            // 8个业务模块(Auth/Users/Patients/Herbs/Formula/Consultation/Prescriptions/MedicalCase)
            // 均在各自的XxxModule.RegisterTypes中注册服务接口实现
        }

        /// <summary>
        /// 注册对话框服务
        /// </summary>
        private static void RegisterDialogs(IContainerRegistry containerRegistry)
        {
            // 统一对话框服务 - WpfDialogService提供完整功能
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService, 
                LYBT.Desktop.Core.Services.WpfDialogService>();
                
            // 注册业务对话框（在服务启动后动态注册）
            containerRegistry.RegisterInstance<Action<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>>(RegisterBusinessDialogs);
        }
        
        /// <summary>
        /// 注册业务对话框Views
        /// </summary>
        private static void RegisterBusinessDialogs(LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService dialogService)
        {
            // 使用WpfDialogService的RegisterDialog方法注册业务对话框
            if (dialogService is LYBT.Desktop.Core.Services.WpfDialogService wpfDialogService)
            {
                // 患者管理对话框
                wpfDialogService.RegisterDialog("PatientAddEditDialog", typeof(LYBT.Desktop.Patients.Views.PatientAddEditDialog));
                
                // 用户管理对话框
                wpfDialogService.RegisterDialog("UserAddEditDialog", typeof(LYBT.Desktop.Users.Views.UserAddEditDialog));
                
                // 药材管理对话框
                wpfDialogService.RegisterDialog("HerbAddEditDialog", typeof(LYBT.Desktop.Herbs.Views.HerbAddEditDialog));
                
                // 医案管理对话框
                wpfDialogService.RegisterDialog("CreateMedicalCaseDialog", typeof(LYBT.Desktop.MedicalCase.Views.CreateMedicalCaseDialog));
                
                // 验方管理对话框
                wpfDialogService.RegisterDialog("AddFormulaDialog", typeof(LYBT.Desktop.Formula.Views.AddFormulaDialog));
                wpfDialogService.RegisterDialog("EditFormulaDialog", typeof(LYBT.Desktop.Formula.Views.EditFormulaDialog));
                wpfDialogService.RegisterDialog("ViewFormulaDialog", typeof(LYBT.Desktop.Formula.Views.ViewFormulaDialog));
                
                // 处方管理对话框
                wpfDialogService.RegisterDialog("PrescriptionEditorDialog", typeof(LYBT.Desktop.Prescriptions.Views.PrescriptionEditorDialog));
                wpfDialogService.RegisterDialog("HerbSelectionDialog", typeof(LYBT.Desktop.Prescriptions.Views.HerbSelectionDialog));
                wpfDialogService.RegisterDialog("FormulaTemplateDialog", typeof(LYBT.Desktop.Prescriptions.Views.FormulaTemplateDialog));
            }
        }

        /// <summary>
        /// 注册性能优化服务
        /// </summary>
        /// <summary>
        /// 注册性能优化服务
        /// </summary>
        private static void RegisterPerformanceServices(IContainerRegistry containerRegistry)
        {
            // UltraThink深度清理: 只保留实际使用的模块加载协调器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator,
                LYBT.Desktop.Core.Services.Performance.ModuleLoadingCoordinator>();
        }


        #region 辅助方法
        // UltraThink统一API客户端管理器已替代原有的独立API服务注册方式
        // 所有API客户端现由UnifiedApiClientManager统一管理，提供更好的一致性和可维护性
        #endregion
    }
}

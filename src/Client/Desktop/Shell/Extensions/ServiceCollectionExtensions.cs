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
using LYBT.Desktop.Modules.Users.Api;
using LYBT.Desktop.Modules.Patients.Api;

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
            RegisterDialogs(containerRegistry);
            RegisterPerformanceServices(containerRegistry);
            // ViewModels和Views通过Prism的ViewModelLocator自动解析，无需手动注册
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
        /// 注册API服务
        /// </summary>
        private static void RegisterApiServices(IContainerRegistry containerRegistry)
        {
            // 注册认证处理器
            containerRegistry.Register<AuthHeaderHandler>();
            
            // 注册Refit API客户端 - 完整的8个API接口
            RegisterApiService<LYBT.Shared.Interfaces.Api.IAuthApi>(containerRegistry);
            RegisterApiService<IUserApi>(containerRegistry);
            RegisterApiService<IPatientApi>(containerRegistry);
            RegisterApiService<LYBT.Desktop.Modules.Herbs.Api.IHerbApi>(containerRegistry);
            RegisterApiService<LYBT.Desktop.Modules.Formula.Api.IFormulaApi>(containerRegistry);
            RegisterApiService<LYBT.Desktop.Modules.Consultation.Api.IConsultationApi>(containerRegistry);
            RegisterApiService<LYBT.Desktop.Modules.Prescriptions.Api.IPrescriptionApi>(containerRegistry);
            RegisterApiService<LYBT.Desktop.Modules.MedicalCase.Api.IMedicalCaseApi>(containerRegistry);
            
            // 注册通用API服务
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.IApiService, LYBT.Desktop.Services.ApiService>();
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
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService, SimplifiedAuthenticationService>();
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IErrorHandlingService>(container =>
                new ErrorHandlingService(container.Resolve<LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService>()));
            containerRegistry.RegisterSingleton<LYBT.Desktop.Workbench.Core.IWorkbenchRouter, LYBT.Desktop.Workbench.Core.WorkbenchRouter>();
            
            // 主窗口服务门面 - 简化MainWindowViewModel的依赖注入
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Interfaces.Services.IMainWindowServicesFacade, 
                LYBT.Desktop.Core.Services.MainWindowServicesFacade>();
        }

        /// <summary>
        /// 注册领域业务服务
        /// </summary>
        private static void RegisterDomainServices(IContainerRegistry containerRegistry)
        {
            // API测试服务
            containerRegistry.RegisterSingleton<ApiTestService>();
            
            // 模块业务服务注册 - 简化版
            // UltraThink修复：IUserService和IPatientService由各自模块注册，避免时序冲突
            // containerRegistry.RegisterSingleton<IUserService, LYBT.Desktop.Users.Services.UserModule>();
            // containerRegistry.RegisterSingleton<IPatientService, LYBT.Desktop.Patients.Services.PatientModule>();
            containerRegistry.RegisterSingleton<IPrescriptionService, LYBT.Desktop.Prescriptions.Services.PrescriptionsModule>();
            containerRegistry.RegisterSingleton<IHerbService, LYBT.Desktop.Herbs.Services.HerbModule>();
            containerRegistry.RegisterSingleton<IFormulaService, LYBT.Desktop.Formula.Services.FormulaModule>();
            containerRegistry.RegisterSingleton<IConsultationService, LYBT.Desktop.Consultation.Services.ConsultationModule>();
            containerRegistry.RegisterSingleton<IMedicalCaseService, LYBT.Desktop.MedicalCase.Services.MedicalCaseModule>();
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
            // 注册UI性能优化器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IUIPerformanceOptimizer, 
                LYBT.Desktop.Core.Services.Performance.UIPerformanceOptimizer>();
                
            // UltraThink Phase 9: 注册模块加载协调器
            containerRegistry.RegisterSingleton<LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator,
                LYBT.Desktop.Core.Services.Performance.ModuleLoadingCoordinator>();
        }


        #region 辅助方法

        /// <summary>
        /// 注册API服务
        /// </summary>
        private static void RegisterApiService<T>(IContainerRegistry containerRegistry) where T : class
        {
            containerRegistry.RegisterSingleton<T>((container) =>
            {
                var authHandler = container.Resolve<AuthHeaderHandler>();
                var httpClient = HttpClientFactory.CreateAuthenticatedClient(authHandler, ApiConfiguration.BaseUrl);
                return RestService.For<T>(httpClient);
            });
        }

        #endregion
    }
}

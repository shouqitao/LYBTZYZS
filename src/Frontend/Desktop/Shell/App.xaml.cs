using Prism.Ioc;
using Prism.DryIoc;
using System;
using System.Windows;
using System.Net.Http;
using LYBT.WPF.Client.Shell.Views;
using LYBT.WPF.Client.Modules.Authentication;
using LYBT.WPF.Client.Modules.SystemManagement;
using LYBT.WPF.Client.Modules.FrontDesk;
using LYBT.WPF.Client.Modules.Doctor;
using LYBT.WPF.Client.Modules.Cashier;
using LYBT.WPF.Client.Services.Interfaces;
using Prism.Modularity;
using Refit;

namespace LYBT.WPF.Client.Shell
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册HttpClient，配置超时时间
            containerRegistry.RegisterSingleton<HttpClient>(() =>
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60); // 增加到60秒超时
                return client;
            });
            
            // 注册Refit API服务
            containerRegistry.RegisterSingleton<IAuthApiService>(() =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new Uri("http://localhost:5297/"),
                    Timeout = TimeSpan.FromSeconds(60)
                };
                return RestService.For<IAuthApiService>(httpClient);
            });
            
            // 注册Token管理器
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.ITokenManager, LYBT.WPF.Client.Services.TokenManager>();
            
            // 注册API服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Services.IApiService, LYBT.WPF.Client.Services.ApiService>();
            
            // 注册认证服务 - 使用Refit实现
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IAuthenticationService, LYBT.WPF.Client.Services.AuthenticationService>();
            
            // 注册用户服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IUserService, LYBT.WPF.Client.Services.UserService>();
            
            // 注册药材服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IHerbService, LYBT.WPF.Client.Services.HerbService>();
            
            // 注册病历服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IRecordService, LYBT.WPF.Client.Services.RecordService>();
            
            // 注册处方打印服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IPrescriptionPrintService, LYBT.WPF.Client.Services.PrescriptionPrintService>();
            
            // 注册患者服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IPatientService, LYBT.WPF.Client.Services.PatientService>();
            
            // 暂时注释处方服务以避免编译错误
            // containerRegistry.RegisterSingleton<LYBT.Frontend.Desktop.Core.Interfaces.Services.IPrescriptionService, LYBT.Frontend.Desktop.Services.PrescriptionService>();
            
            // 注册权限服务
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IPermissionService, LYBT.WPF.Client.Services.PermissionService>();
            
            // 注册会话管理器
            containerRegistry.RegisterSingleton<LYBT.WPF.Client.Core.Interfaces.Services.IUserSessionManager, LYBT.WPF.Client.Services.UserSessionManager>();
        }

        protected override void ConfigureModuleCatalog(Prism.Modularity.IModuleCatalog moduleCatalog)
        {
            // 配置模块目录
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(AuthenticationModule),
                ModuleType = typeof(AuthenticationModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(SystemManagementModule),
                ModuleType = typeof(SystemManagementModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(FrontDeskModule),
                ModuleType = typeof(FrontDeskModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(DoctorModule),
                ModuleType = typeof(DoctorModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            moduleCatalog.AddModule(new ModuleInfo
            {
                ModuleName = nameof(CashierModule),
                ModuleType = typeof(CashierModule).AssemblyQualifiedName,
                InitializationMode = InitializationMode.WhenAvailable
            });
            
            base.ConfigureModuleCatalog(moduleCatalog);
        }
    }
}
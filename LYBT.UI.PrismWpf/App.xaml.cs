using LYBT.UI.PrismWpf.Views;
using LYBT.UI.PrismWpf.ViewModels;
using LYBT.UI.PrismWpf.Services;
using LYBT.UI.PrismWpf.Services.Api;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Refit;
using System.Net.Http;

namespace LYBT.UI.PrismWpf 
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App 
    {
        protected override Window CreateShell() 
        {
            // 首先显示登录窗口
            return ShowLoginWindow();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) 
        {
            // 注册配置服务
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            containerRegistry.RegisterInstance<IConfiguration>(configuration);

            // 注册HTTP客户端
            containerRegistry.Register<HttpClient>();

            // 注册Refit API接口
            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            containerRegistry.RegisterInstance(RestService.For<IAuthApi>(baseUrl));
            containerRegistry.RegisterInstance(RestService.For<IUsersApi>(baseUrl));

            // 注册服务
            containerRegistry.RegisterSingleton<IAuthService, AuthService>();

            // 注册ViewModel
            containerRegistry.Register<LoginWindowViewModel>();
            containerRegistry.Register<MainWindowViewModel>();

            // 注册对话框服务
            containerRegistry.Register<IDialogService, DialogService>();
        }

        /// <summary>
        /// 显示登录窗口
        /// </summary>
        private Window ShowLoginWindow()
        {
            var loginWindow = new LoginWindow();
            var loginViewModel = Container.Resolve<LoginWindowViewModel>();
            
            loginWindow.SetViewModel(loginViewModel);

            // 显示登录对话框
            var result = loginWindow.ShowDialog();

            if (result == true)
            {
                // 登录成功，返回主窗口
                return Container.Resolve<MainWindow>();
            }
            else
            {
                // 登录失败或取消，退出应用程序
                Shutdown();
                return null!;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 应用程序退出时的清理工作
            var authService = Container.Resolve<IAuthService>();
            authService?.LogoutAsync();
            
            base.OnExit(e);
        }
    }
}

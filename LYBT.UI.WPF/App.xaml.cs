using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.Services.Api;
using LYBT.UI.WPF.Views;
using LYBT.UI.WPF.Views.Main;
using Prism.Ioc;
using Refit;
using System;
using System.Net.Http;
using System.Windows;

namespace LYBT.UI.WPF {
    public partial class App : PrismApplication {
        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            // 1. 构造无Token的HttpClient，首次登录还没有token，正常用即可
            var httpClient = new HttpClient() {
                BaseAddress = new Uri("http://localhost:5297")
            };

            // 2. 用Refit创建IAuthApi实例
            var authApi = RestService.For<IAuthApi>(httpClient);

            // 3. 手动new AuthService（注入authApi实例），不让Unity自动构造！
            var authService = new AuthService(authApi);

            // 4. 用RegisterInstance注册，后续所有用IAuthService和IAuthApi的地方都能用
            containerRegistry.RegisterInstance(authApi);
            containerRegistry.RegisterInstance<IAuthService>(authService);

            // 5. 如果你还有其他API接口，也用同样方式new出来后RegisterInstance
            containerRegistry.RegisterForNavigation<LoginView>("LoginView");
            containerRegistry.RegisterForNavigation<MainWindow>("MainWindow");
            containerRegistry.RegisterForNavigation<HomeView>("HomeView");

        }

        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void OnInitialized() {
            base.OnInitialized();
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("LoginRegion", "LoginView");
        }
    }
}

using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.ViewModels;
using LYBT.UI.WPF.Views;
using System.Windows;

namespace LYBT.UI.WPF {

    public partial class App : PrismApplication {

        /// <summary>
        /// 重写Prism框架方法，注册类型和服务
        /// </summary>
        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            // 注册服务接口与实现，用于依赖注入
            containerRegistry.Register<IAuthService, AuthService>();
            // 注册导航视图（将视图类型映射到导航名称）
            containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
            containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
            containerRegistry.RegisterForNavigation<AdminView, AdminViewModel>();
            containerRegistry.RegisterForNavigation<DiagnosingDoctorView, DiagnosingDoctorViewModel>();
            containerRegistry.RegisterForNavigation<TreatmentDoctorView, TreatmentDoctorViewModel>();
            containerRegistry.RegisterForNavigation<PharmacyStaffView, PharmacyStaffViewModel>();
            containerRegistry.RegisterForNavigation<RegistrationStaffView, RegistrationStaffViewModel>();
            containerRegistry.RegisterForNavigation<BillingStaffView, BillingStaffViewModel>();
        }

        /// <summary>
        /// 重写Prism框架方法，创建应用程序主窗口(Shell)
        /// </summary>
        protected override Window CreateShell() {
            // 利用依赖容器创建主窗口实例（会自动注入依赖项）
            return Container.Resolve<MainWindow>();
        }

        /// <summary>
        /// 可选重写：应用程序初始化完成时的钩子
        /// </summary>
        protected override void OnInitialized() {
            base.OnInitialized();
            // 初始化后导航到登录页面，使LoginView加载到MainWindow的ContentRegion区域
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("ContentRegion", "LoginView");
        }
    }
}
using LYBT.Common.Enums.Users;
using LYBT.UI.WPF.Events;
using LYBT.UI.WPF.ViewModels.Components;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 整合后的主布局视图模型 - 统一管理各个组件
    /// </summary>
    public class IntegratedMainLayoutViewModel : BindableBase {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        #region Properties

        private bool _isNavDrawerOpen;
        public bool IsNavDrawerOpen {
            get => _isNavDrawerOpen;
            set => SetProperty(ref _isNavDrawerOpen, value);
        }

        private bool _showWelcomePanel = true;
        public bool ShowWelcomePanel {
            get => _showWelcomePanel;
            set => SetProperty(ref _showWelcomePanel, value);
        }

        // 子组件视图模型（移除TopToolBar和UserMenu）
        public NavigationDrawerViewModel NavigationDrawerViewModel { get; private set; }
        public WelcomePanelViewModel WelcomePanelViewModel { get; private set; }
        public StatusBarViewModel StatusBarViewModel { get; private set; }

        #endregion

        #region Commands

        public DelegateCommand ToggleNavDrawerCommand { get; private set; }
        public DelegateCommand HideWelcomePanelCommand { get; private set; }

        #endregion

        public IntegratedMainLayoutViewModel(
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            NavigationDrawerViewModel navigationDrawerViewModel,
            WelcomePanelViewModel welcomePanelViewModel,
            StatusBarViewModel statusBarViewModel) {

            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 注入子组件视图模型（移除TopToolBar和UserMenu）
            NavigationDrawerViewModel = navigationDrawerViewModel ?? throw new ArgumentNullException(nameof(navigationDrawerViewModel));
            WelcomePanelViewModel = welcomePanelViewModel ?? throw new ArgumentNullException(nameof(welcomePanelViewModel));
            StatusBarViewModel = statusBarViewModel ?? throw new ArgumentNullException(nameof(statusBarViewModel));

            InitializeCommands();
            SubscribeToEvents();
        }

        #region Initialization

        private void InitializeCommands() {
            ToggleNavDrawerCommand = new DelegateCommand(() => IsNavDrawerOpen = !IsNavDrawerOpen);
            HideWelcomePanelCommand = new DelegateCommand(() => ShowWelcomePanel = false);
        }

        private void SubscribeToEvents() {
            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 订阅导航事件
            _eventAggregator.GetEvent<NavigationCompletedEvent>().Subscribe(OnNavigationCompleted);

            // 订阅功能界面导航事件
            _eventAggregator.GetEvent<NavigateToFunctionEvent>().Subscribe(OnNavigateToFunction);

            // 订阅集成内容区域导航事件
            _eventAggregator.GetEvent<NavigateToIntegratedContentEvent>().Subscribe(OnNavigateToIntegratedContent);

            // 订阅医生档案导航事件
            _eventAggregator.GetEvent<NavigateToDoctorProfileEvent>().Subscribe(OnNavigateToDoctorProfile);

            // 订阅退出登录事件
            _eventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout);

            // 订阅切换导航抽屉事件
            _eventAggregator.GetEvent<ToggleNavDrawerEvent>().Subscribe(() => IsNavDrawerOpen = !IsNavDrawerOpen);
        }

        #endregion

        #region Event Handlers

        private async void OnLoginSuccess(IList<UserRole> roles) {
            try {
                // 更新所有子组件的用户信息
                await NavigationDrawerViewModel.LoadNavigationAsync(roles);
                WelcomePanelViewModel.UpdateUserInfo(roles);
                StatusBarViewModel.UpdateSystemStatus("用户登录成功");

                // 显示欢迎面板
                ShowWelcomePanel = true;

                System.Diagnostics.Debug.WriteLine("Integrated main layout initialized after login");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnLoginSuccess error: {ex.Message}");
                StatusBarViewModel.UpdateSystemStatus($"初始化失败：{ex.Message}");
            }
        }

        private void OnNavigationCompleted(string targetView) {
            // 当导航到功能页面时，隐藏欢迎面板
            if (!string.IsNullOrEmpty(targetView) && targetView != "HomeView") {
                ShowWelcomePanel = false;
            }

            // 关闭导航抽屉
            IsNavDrawerOpen = false;
        }

        private void OnNavigateToFunction(string functionView) {
            // 隐藏欢迎面板
            ShowWelcomePanel = false;

            // 导航到功能界面
            _regionManager.RequestNavigate("FunctionRegion", functionView);

            // 发布界面切换事件
            _eventAggregator.GetEvent<NavigationCompletedEvent>().Publish(functionView);
        }

        private void OnNavigateToIntegratedContent(NavigationArgs args) {
            // 隐藏欢迎面板
            ShowWelcomePanel = false;

            // 导航到集成内容区域
            _regionManager.RequestNavigate("IntegratedContentRegion", args.TargetView);

            // 发布界面切换事件
            _eventAggregator.GetEvent<NavigationCompletedEvent>().Publish(args.TargetView);
        }

        private void OnNavigateToDoctorProfile(DoctorProfileNavigationArgs args) {
            // 隐藏欢迎面板
            ShowWelcomePanel = false;

            // 准备导航参数
            var parameters = new NavigationParameters { { "Mode", args.Mode } };
            if (args.UserId != null)
                parameters.Add("UserId", args.UserId.Value);
            if (args.UserName != null)
                parameters.Add("UserName", args.UserName);
            if (args.RealName != null)
                parameters.Add("RealName", args.RealName);

            // 导航到医生档案
            _regionManager.RequestNavigate("FunctionRegion", "DoctorProfileView", parameters);

            // 发布界面切换事件
            _eventAggregator.GetEvent<NavigationCompletedEvent>().Publish("DoctorProfileView");
        }

        private void OnLogout() {
            // 重置布局状态
            ResetLayout();

            // 导航回登录界面
            _regionManager.RequestNavigate("FunctionRegion", "LoginView");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 重置布局状态
        /// </summary>
        public void ResetLayout() {
            ShowWelcomePanel = true;
            IsNavDrawerOpen = false;

            // 重置子组件状态（移除TopToolBar和UserMenu的重置）
            NavigationDrawerViewModel.Reset();
            WelcomePanelViewModel.Reset();
            StatusBarViewModel.Reset();
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup() {
            WelcomePanelViewModel.Cleanup();
            StatusBarViewModel.Cleanup();
        }

        #endregion
    }
}
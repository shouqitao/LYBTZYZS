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

        // 子组件视图模型
        public NavigationDrawerViewModel NavigationDrawerViewModel { get; private set; }
        public WelcomePanelViewModel WelcomePanelViewModel { get; private set; }
        public StatusBarViewModel StatusBarViewModel { get; private set; }

        #endregion

        #region Commands

        public DelegateCommand ToggleNavDrawerCommand { get; private set; }
        public DelegateCommand HideWelcomePanelCommand { get; private set; }
        public DelegateCommand ShowWelcomePanelCommand { get; private set; }

        #endregion

        public IntegratedMainLayoutViewModel(
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            NavigationDrawerViewModel navigationDrawerViewModel,
            WelcomePanelViewModel welcomePanelViewModel,
            StatusBarViewModel statusBarViewModel) {

            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 注入子组件视图模型
            NavigationDrawerViewModel = navigationDrawerViewModel ?? throw new ArgumentNullException(nameof(navigationDrawerViewModel));
            WelcomePanelViewModel = welcomePanelViewModel ?? throw new ArgumentNullException(nameof(welcomePanelViewModel));
            StatusBarViewModel = statusBarViewModel ?? throw new ArgumentNullException(nameof(statusBarViewModel));

            InitializeCommands();
            SubscribeToEvents();
        }

        #region Initialization

        private void InitializeCommands() {
            ToggleNavDrawerCommand = new DelegateCommand(() => IsNavDrawerOpen = !IsNavDrawerOpen);

            HideWelcomePanelCommand = new DelegateCommand(() => {
                ShowWelcomePanel = false;
                // 确保导航菜单在隐藏欢迎面板后仍然可以访问
                IsNavDrawerOpen = false;
            });

            ShowWelcomePanelCommand = new DelegateCommand(() => {
                ShowWelcomePanel = true;
                // 清除当前功能区域的内容
                try {
                    var region = _regionManager.Regions["IntegratedContentRegion"];
                    region.RemoveAll();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Clear content region error: {ex.Message}");
                }
            });
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
            try {
                System.Diagnostics.Debug.WriteLine($"Navigation completed to: {targetView}");

                // 当导航到功能页面时，隐藏欢迎面板，但保持导航菜单可用
                if (!string.IsNullOrEmpty(targetView) &&
                    targetView != "HomeView" &&
                    targetView != "LoginView") {
                    ShowWelcomePanel = false;
                }

                // 自动关闭导航抽屉（移动端体验）
                IsNavDrawerOpen = false;

                // 更新状态
                StatusBarViewModel.UpdateSystemStatus($"已切换到 {targetView}");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnNavigationCompleted error: {ex.Message}");
            }
        }

        private void OnNavigateToFunction(string functionView) {
            try {
                System.Diagnostics.Debug.WriteLine($"Navigate to function: {functionView}");

                // 隐藏欢迎面板
                ShowWelcomePanel = false;

                // 导航到功能界面（登录、修改密码等）
                _regionManager.RequestNavigate("FunctionRegion", functionView);

                // 发布界面切换事件
                _eventAggregator.GetEvent<NavigationCompletedEvent>().Publish(functionView);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnNavigateToFunction error: {ex.Message}");
            }
        }

        private void OnNavigateToIntegratedContent(NavigationArgs args) {
            try {
                System.Diagnostics.Debug.WriteLine($"Navigate to integrated content: {args.TargetView}");

                // 隐藏欢迎面板
                ShowWelcomePanel = false;

                // 导航到集成内容区域
                var parameters = new NavigationParameters();
                foreach (var param in args.Parameters) {
                    parameters.Add(param.Key, param.Value);
                }

                _regionManager.RequestNavigate("IntegratedContentRegion", args.TargetView, result => {
                    if (result.Success) {
                        // 发布界面切换事件
                        _eventAggregator.GetEvent<NavigationCompletedEvent>().Publish(args.TargetView);
                    } else {
                        System.Diagnostics.Debug.WriteLine($"Navigation to {args.TargetView} failed: {result.Exception?.Message}");
                        StatusBarViewModel.UpdateSystemStatus($"导航到 {args.TargetView} 失败");
                    }
                }, parameters);
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnNavigateToIntegratedContent error: {ex.Message}");
                StatusBarViewModel.UpdateSystemStatus($"导航失败：{ex.Message}");
            }
        }

        private void OnNavigateToDoctorProfile(DoctorProfileNavigationArgs args) {
            try {
                System.Diagnostics.Debug.WriteLine($"Navigate to doctor profile: {args.Mode}");

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
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnNavigateToDoctorProfile error: {ex.Message}");
            }
        }

        private void OnLogout() {
            try {
                // 重置布局状态
                ResetLayout();

                // 导航回登录界面
                _regionManager.RequestNavigate("FunctionRegion", "LoginView");

                System.Diagnostics.Debug.WriteLine("User logout completed in integrated layout");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"OnLogout error: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 重置布局状态
        /// </summary>
        public void ResetLayout() {
            try {
                ShowWelcomePanel = true;
                IsNavDrawerOpen = false;

                // 清除内容区域
                try {
                    var region = _regionManager.Regions["IntegratedContentRegion"];
                    region.RemoveAll();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"Clear region error: {ex.Message}");
                }

                // 重置子组件状态
                NavigationDrawerViewModel.Reset();
                WelcomePanelViewModel.Reset();
                StatusBarViewModel.Reset();

                System.Diagnostics.Debug.WriteLine("Layout reset completed");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"ResetLayout error: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup() {
            try {
                WelcomePanelViewModel.Cleanup();
                StatusBarViewModel.Cleanup();
                System.Diagnostics.Debug.WriteLine("Integrated layout cleanup completed");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        #endregion
    }
}
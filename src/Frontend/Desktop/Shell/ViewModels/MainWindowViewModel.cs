using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.Configuration;
using LYBT.WPF.Client.Modules.Authentication.Views;
using LYBT.WPF.Client.Modules.Authentication.ViewModels;
using LYBT.WPF.Client.Services;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Dialogs;
using Prism.Events;
using Prism.Commands;

namespace LYBT.WPF.Client.Shell.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;
        
        private string _title = "凌隐宝堂中医诊所管理系统";
        private UserInfo? _currentUser;
        private bool _isLoggedIn = false;

        public DelegateCommand LogoutCommand { get; }
        public DelegateCommand TestApiCommand { get; }

        public MainWindowViewModel(
            IRegionManager regionManager, 
            IEventAggregator eventAggregator, 
            IAuthenticationService authService,
            IPermissionService permissionService,
            IUserService userService)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _authService = authService;
            _permissionService = permissionService;
            _userService = userService;

            LogoutCommand = new DelegateCommand(ExecuteLogout);
            TestApiCommand = new DelegateCommand(ExecuteTestApi, () => _isLoggedIn);

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 延迟检查登录状态，等待主窗口完全加载
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CheckLoginStatus();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>窗口标题</summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>当前用户</summary>
        public UserInfo? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        /// <summary>是否已登录</summary>
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set 
            { 
                SetProperty(ref _isLoggedIn, value);
                RaisePropertyChanged(nameof(IsNotLoggedIn));
            }
        }

        /// <summary>是否未登录（用于界面显示）</summary>
        public bool IsNotLoggedIn => !_isLoggedIn;

        /// <summary>
        /// 退出登录命令执行
        /// </summary>
        private async void ExecuteLogout()
        {
            var result = MessageBox.Show("确定要退出登录吗？", "退出确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _authService.LogoutAsync();
                    
                    // 发布登出事件以清除登录状态消息
                    _eventAggregator.GetEvent<LogoutEvent>().Publish();
                    
                    // 清除用户信息
                    CurrentUser = null;
                    IsLoggedIn = false;
                    Title = "凌隐宝堂中医诊所管理系统";
                    
                    // 清除内容区域
                    if (_regionManager.Regions.ContainsRegionWithName("ContentRegion"))
                    {
                        _regionManager.Regions["ContentRegion"].RemoveAll();
                    }
                    
                    // 显示登录界面
                    ShowLoginDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"退出登录失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        private async void CheckLoginStatus()
        {
            if (_authService.IsLoggedIn)
            {
                CurrentUser = await _authService.GetCurrentUserAsync();
                IsLoggedIn = true;
                TestApiCommand.RaiseCanExecuteChanged();
                LoadMainContent();
            }
            else
            {
                ShowLoginDialog();
            }
        }

        /// <summary>
        /// 登录成功事件处理
        /// </summary>
        private void OnLoginSuccess()
        {
            // 重新检查登录状态
            CheckLoginStatus();
        }

        /// <summary>
        /// 显示登录界面
        /// </summary>
        private void ShowLoginDialog()
        {
            // 在单窗口模式下，导航到登录视图
            if (_regionManager != null)
            {
                _regionManager.RequestNavigate("LoginRegion", "LoginView");
            }
        }

        /// <summary>
        /// 加载主界面内容
        /// </summary>
        private void LoadMainContent()
        {
            if (CurrentUser == null) return;

            // 使用配置类统一处理角色导航
            var roleDisplay = RoleNavigationConfig.GetRoleDisplayName(CurrentUser.Role);
            Title = $"凌隐宝堂中医诊所管理系统 - {CurrentUser.RealName} ({roleDisplay})";

            // 获取对应的主界面视图名称
            var mainViewName = RoleNavigationConfig.GetMainViewName(CurrentUser.Role);
            
            if (_regionManager != null)
            {
                try
                {
                    // 导航到主界面
                    _regionManager.RequestNavigate("ContentRegion", mainViewName);
                }
                catch (Exception ex)
                {
                    // 如果视图不存在，显示欢迎消息
                    var welcomeMessage = RoleNavigationConfig.GetWelcomeMessage(CurrentUser.Role, CurrentUser.RealName);
                    MessageBox.Show($"{welcomeMessage}\n\n注意：{mainViewName} 模块尚未实现。\n错误详情：{ex.Message}", 
                        "登录成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // 显示欢迎消息
                var welcomeMessage = RoleNavigationConfig.GetWelcomeMessage(CurrentUser.Role, CurrentUser.RealName);
                MessageBox.Show(welcomeMessage, "登录成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 执行API测试
        /// </summary>
        private async void ExecuteTestApi()
        {
            try
            {
                var testService = new ApiTestService(_authService, _userService);
                var result = await testService.RunFullApiTestAsync();
                ApiTestService.ShowTestResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API测试失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
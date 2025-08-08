using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Core;
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

using LYBT.WPF.Client.Core.Models.Users;

namespace LYBT.WPF.Client.Shell.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IAuthenticationService _authService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;

        private string _title = "凌隐宝堂中医诊所诊疗系统";
        private UserInfo? _currentUser;
        private bool _isLoggedIn = false;

        public DelegateCommand LogoutCommand { get; }
        public DelegateCommand TestApiCommand { get; }
        public DelegateCommand ShowControlExamplesCommand { get; }

        public MainWindowViewModel(IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IAuthenticationService authService,
            IPermissionService permissionService,
            IUserService userService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _authService = authService;
            _permissionService = permissionService;
            _userService = userService;

            LogoutCommand = new DelegateCommand(async () => await ExecuteLogoutAsync());
            TestApiCommand = new DelegateCommand(async () => await ExecuteTestApiAsync(), () => _isLoggedIn);
            ShowControlExamplesCommand = new DelegateCommand(ExecuteShowControlExamples, () => _isLoggedIn);

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 延迟检查登录状态，等待主窗口完全加载
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _ = CheckLoginStatusAsync();
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
        private async Task ExecuteLogoutAsync()
        {
            var result = await _commonDialogService.ShowConfirmationAsync("确定要退出登录吗？", "退出确认");
            if (result)
            {
                try
                {
                    await _authService.LogoutAsync();

                    // 发布登出事件以清除登录状态消息
                    _eventAggregator.GetEvent<LogoutEvent>().Publish();

                    // 清除用户信息
                    CurrentUser = null;
                    IsLoggedIn = false;
                    Title = "凌隐宝堂中医诊所诊疗系统";

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
                    _commonDialogService.ShowErrorAsync($"退出登录失败：{ex.Message}", "错误").GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        private async Task CheckLoginStatusAsync()
        {
            if (_authService.IsLoggedIn)
            {
                CurrentUser = await _authService.GetCurrentUserAsync();
                IsLoggedIn = true;
                TestApiCommand.RaiseCanExecuteChanged();
                ShowControlExamplesCommand.RaiseCanExecuteChanged();

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
            _ = CheckLoginStatusAsync();
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
            if (CurrentUser == null)
            {
                _commonDialogService.ShowErrorAsync("当前用户信息为空，无法加载主界面", "错误").GetAwaiter().GetResult();
                return;
            }

            // 使用配置类统一处理角色导航
            var roleDisplay = RoleNavigationConfig.GetRoleDisplayName(CurrentUser.Username == "sysadmin" ? "管理员" : "用户");
            Title = $"凌隐宝堂中医诊所诊疗系统 - {CurrentUser.RealName} ({roleDisplay})";

            // 获取对应的主界面视图名称
            var mainViewName = RoleNavigationConfig.GetMainViewName(CurrentUser.Username == "sysadmin" ? "管理员" : "用户");

            if (_regionManager != null)
            {
                try
                {
                    // 清除内容区域的旧内容
                    if (_regionManager.Regions.ContainsRegionWithName("ContentRegion"))
                    {
                        _regionManager.Regions["ContentRegion"].RemoveAll();
                    }

                    // 清除登录区域
                    if (_regionManager.Regions.ContainsRegionWithName("LoginRegion"))
                    {
                        _regionManager.Regions["LoginRegion"].RemoveAll();
                    }

                    // 导航到主界面
                    _regionManager.RequestNavigate("ContentRegion", mainViewName);
                }
                catch (Exception ex)
                {
                    // 如果视图不存在，显示欢迎消息
                    var welcomeMessage = RoleNavigationConfig.GetWelcomeMessage(CurrentUser.Username == "sysadmin" ? "管理员" : "用户", CurrentUser.RealName);
                    _commonDialogService.ShowInformationAsync($"{welcomeMessage}\n\n注意：{mainViewName} 模块尚未实现。\n错误详情：{ex.Message}", "登录成功").GetAwaiter().GetResult();
                }
            }
            else
            {
                // 显示欢迎消息
                var welcomeMessage = RoleNavigationConfig.GetWelcomeMessage(CurrentUser.Username == "sysadmin" ? "管理员" : "用户", CurrentUser.RealName);
                _commonDialogService.ShowWarningAsync($"RegionManager为空\n{welcomeMessage}", "登录成功").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行API测试
        /// </summary>
        private async Task ExecuteTestApiAsync()
        {
            try
            {
                var testService = new ApiTestService(_authService, _userService);
                var result = await testService.RunFullApiTestAsync();
                ApiTestService.ShowTestResult(result);
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"API测试失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteShowControlExamples()
        {
            try
            {
                // 导航到控件示例页面
                _regionManager.RequestNavigate("ContentRegion", "ControlExamplesView");
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开控件示例页面失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

    }
}
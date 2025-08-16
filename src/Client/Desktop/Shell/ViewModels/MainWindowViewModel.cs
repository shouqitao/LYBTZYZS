using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Auth.Views;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Services;
using LYBT.Desktop.Workbench.Core;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Dialogs;
using Prism.Events;
using Prism.Commands;

using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Shell.ViewModels
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
        private readonly IWorkbenchRouter _workbenchRouter;
        private readonly ApiTestService _apiTestService;
        private readonly LYBT.Desktop.Core.Services.Performance.IUIPerformanceOptimizer _uiOptimizer;

        private string _title = "凌隐宝堂中医诊所诊疗系统";
        private UserDto? _currentUser;
        private bool _isLoggedIn = false;

        public DelegateCommand LogoutCommand { get; }
        public DelegateCommand TestApiCommand { get; }
        public DelegateCommand ShowControlExamplesCommand { get; }

        public MainWindowViewModel(IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IAuthenticationService authService,
            IPermissionService permissionService,
            IUserService userService,
            ICommonDialogService commonDialogService,
            IWorkbenchRouter workbenchRouter,
            ApiTestService apiTestService,
            LYBT.Desktop.Core.Services.Performance.IUIPerformanceOptimizer uiOptimizer)
        {
            _commonDialogService = commonDialogService;
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _authService = authService;
            _permissionService = permissionService;
            _userService = userService;
            _workbenchRouter = workbenchRouter;
            _apiTestService = apiTestService;
            _uiOptimizer = uiOptimizer;

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
        public UserDto? CurrentUser
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
        /// 检查登录状态 - 简化版本
        /// </summary>
        private async Task CheckLoginStatusAsync()
        {
            try
            {
                if (_authService.IsLoggedIn)
                {
                    var user = await _authService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        CurrentUser = user;
                        IsLoggedIn = true;
                        TestApiCommand.RaiseCanExecuteChanged();
                        ShowControlExamplesCommand.RaiseCanExecuteChanged();
                        LoadMainContent();
                        return;
                    }
                }
                
                ShowLoginDialog();
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"检查登录状态失败：{ex.Message}", "错误");
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
        /// 加载主界面内容 - 使用新的WorkbenchRouter系统
        /// </summary>
        private void LoadMainContent()
        {
            if (CurrentUser == null)
            {
                _commonDialogService.ShowErrorAsync("当前用户信息为空，无法加载主界面", "错误").GetAwaiter().GetResult();
                return;
            }

            // UltraThink重构: 使用UserDto的Role属性判断用户角色
            string userRole;
            if (CurrentUser.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true)
            {
                userRole = "管理员";
            }
            else if (CurrentUser.Role?.Equals("Doctor", StringComparison.OrdinalIgnoreCase) == true ||
                     CurrentUser.Role?.Equals("医生", StringComparison.OrdinalIgnoreCase) == true)
            {
                // 基于Role字段判断是否为医生
                userRole = "医生";
            }
            else if (CurrentUser.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
                     CurrentUser.Role?.Equals("管理员", StringComparison.OrdinalIgnoreCase) == true)
            {
                userRole = "管理员";
            }
            else
            {
                // 默认为医生用户（看诊界面）
                userRole = "医生";
            }

            // 使用WorkbenchRouter获取工作台信息
            var workbenchView = _workbenchRouter.GetWorkbenchForRole(userRole);
            var roleDisplay = _workbenchRouter.GetRoleDisplayName(userRole);
            var welcomeMessage = _workbenchRouter.GetWelcomeMessage(userRole, CurrentUser.RealName);
            
            Title = $"凌隐宝堂中医诊所诊疗系统 - {CurrentUser.RealName} ({roleDisplay})";

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

                    // 根据角色导航到对应的工作台主视图
                    _regionManager.RequestNavigate("ContentRegion", workbenchView, navigationResult =>
                    {
                        if (!navigationResult.Success)
                        {
                            // 如果导航失败，显示错误信息
                            var errorMessage = navigationResult.Exception?.Message ?? "未知导航错误";
                            _commonDialogService.ShowWarningAsync(
                                $"{welcomeMessage}\n\n注意：工作台模块加载失败。\n错误详情：{errorMessage}", 
                                "登录成功").GetAwaiter().GetResult();
                        }
                        else
                        {
                            // 导航成功，显示欢迎消息（可选）
                            System.Diagnostics.Debug.WriteLine($"成功导航到工作台：{workbenchView}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    // 显示欢迎消息和错误
                    _commonDialogService.ShowInformationAsync($"{welcomeMessage}\n\n注意：工作台模块加载失败。\n错误详情：{ex.Message}", "登录成功").GetAwaiter().GetResult();
                }
            }
            else
            {
                // RegionManager为空的错误处理
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
                var result = await _apiTestService.RunFullApiTestAsync();
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
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Ioc;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Auth.Views;
using LYBT.Desktop.Core.Configuration;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Services;
using LYBT.Desktop.Workbench.Core;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainWindowViewModel : ServiceViewModel
    {
        private readonly IMainWindowServicesFacade _servicesFacade;
        private readonly IRegionManager _regionManager;

        private string _title = SystemConstants.SystemTitle;
        private UserDto? _currentUser;
        private bool _isLoggedIn = false;
        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        private readonly System.Windows.Threading.DispatcherTimer _clockTimer;

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
                System.Diagnostics.Debug.WriteLine($"🔒 MainWindow.IsLoggedIn设置为: {value} (之前: {_isLoggedIn})");
                SetProperty(ref _isLoggedIn, value);
                RaisePropertyChanged(nameof(IsNotLoggedIn)); // 确保通知界面更新
            }
        }

        /// <summary>当前时间</summary>
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public DelegateCommand LogoutCommand { get; }
        public DelegateCommand TestApiCommand { get; }
        public DelegateCommand ShowControlExamplesCommand { get; }

        public MainWindowViewModel(
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            IMainWindowServicesFacade servicesFacade,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _servicesFacade = servicesFacade ?? throw new ArgumentNullException(nameof(servicesFacade));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化时钟计时器
            _clockTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();

            LogoutCommand = new DelegateCommand(async () => await ExecuteLogoutAsync());
            TestApiCommand = new DelegateCommand(async () => await ExecuteTestApiAsync(), () => _isLoggedIn);
            ShowControlExamplesCommand = new DelegateCommand(ExecuteShowControlExamples, () => _isLoggedIn);

            // 订阅登录成功事件
            EventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 延迟检查登录状态，等待主窗口完全加载
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _ = CheckLoginStatusAsync();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>时钟计时器事件</summary>
        private void OnClockTick(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>是否未登录（用于界面显示）</summary>
        public bool IsNotLoggedIn => !_isLoggedIn;

        /// <summary>
        /// 退出登录命令执行
        /// </summary>
        private async Task ExecuteLogoutAsync()
        {
            var result = await _servicesFacade.CustomDialogService.ShowConfirmationAsync("确定要退出登录吗？", "退出确认");
            if (result)
            {
                try
                {
                    // 立即更新UI状态，给用户即时反馈
                    CurrentUser = null;
                    IsLoggedIn = false;
                    Title = "凌隐宝堂中医诊所诊疗系统";

                    // 立即清理界面
                    // 清除内容区域
                    if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
                    {
                        _regionManager.Regions[RegionNames.ContentRegion].RemoveAll();
                    }
                    
                    // 立即显示登录界面
                    ShowLoginDialog();

                    // 后台异步处理网络请求和事件，不阻塞UI
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // 网络登出请求
                            await _servicesFacade.AuthenticationService.LogoutAsync();
                            
                            // 发布登出事件以清除登录状态消息
                            EventAggregator.GetEvent<LogoutEvent>().Publish();
                        }
                        catch (Exception ex)
                        {
                            // 后台错误不影响用户界面，记录到调试输出
                            System.Diagnostics.Debug.WriteLine($"后台登出处理异常: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    await _servicesFacade.CustomDialogService.ShowErrorAsync($"退出登录失败：{ex.Message}", "错误");
                }
            }
        }

        /// <summary>
        /// 检查登录状态 - UltraThink性能优化版
        /// </summary>
        private async Task CheckLoginStatusAsync()
        {
            try
            {
                if (_servicesFacade.AuthenticationService.IsLoggedIn)
                {
                    var user = await _servicesFacade.AuthenticationService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        CurrentUser = ConvertToUserDto(user);
                        IsLoggedIn = true;
                        
                        // 更新命令状态
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
                await _servicesFacade.CustomDialogService.ShowErrorAsync($"检查登录状态失败：{ex.Message}", "错误");
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
                _regionManager.RequestNavigate(RegionNames.LoginRegion, "LoginView");
            }
        }

        /// <summary>
        /// 加载主界面内容 - UltraThink Phase 9 性能优化版
        /// </summary>
        private void LoadMainContent()
        {
            if (CurrentUser == null)
            {
                throw new InvalidOperationException("当前用户信息为空，无法加载主界面");
            }

            // 简化角色判断逻辑：只区分管理员和医生
            string workbenchView;
            string roleDisplay;
            
            // 管理员判断（包括sysadmin用户名和Admin角色）
            bool isAdmin = CurrentUser.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true ||
                          CurrentUser.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
                          
            if (isAdmin)
            {
                workbenchView = "SystemWorkbenchMainView";
                roleDisplay = "管理员";
            }
            else
            {
                // 其他角色默认为医生工作台
                workbenchView = "ConsultationWorkbenchMainView";
                roleDisplay = "医生";
            }

            // 更新标题和清理登录区域
                Title = $"凌隐宝堂中医诊所诊疗系统 - {CurrentUser.RealName} ({roleDisplay})";
                
                // 清除登录区域
                if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
                {
                    _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
                }

                // 导航到对应的工作台
                System.Diagnostics.Debug.WriteLine($"🚀 导航到: {workbenchView}");
                _regionManager.RequestNavigate(RegionNames.ContentRegion, workbenchView, navigationResult =>
                {
                    if (navigationResult.Result != true)
                    {
                        // 导航失败时显示错误信息
                        var errorMessage = navigationResult.Error?.Message ?? "未知导航错误";
                        System.Diagnostics.Debug.WriteLine($"❌ 工作台导航失败: {errorMessage}");
                        
                        // 异步显示错误对话框
                        _ = Task.Run(async () =>
                        {
                            await _servicesFacade.CustomDialogService.ShowErrorAsync(
                                $"无法加载工作台: {errorMessage}", "系统错误");
                        });
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ 成功导航到: {workbenchView}");
                    }
                });
            }

        /// <summary>
        /// 执行API测试
        /// </summary>
        private async Task ExecuteTestApiAsync()
        {
            try
            {
                await _servicesFacade.CustomDialogService.ShowInformationAsync("API测试功能将在未来版本中实现", "提示");
            }
            catch (Exception ex)
            {
                await _servicesFacade.CustomDialogService.ShowErrorAsync($"API测试失败: {ex.Message}", "错误");
            }
        }

        private void ExecuteShowControlExamples()
        {
            try
            {
                // 导航到控件示例页面
                _regionManager.RequestNavigate(RegionNames.ContentRegion, "ControlExamplesView");
            }
            catch (Exception ex)
            {
                // 简化错误处理，记录到调试输出
                System.Diagnostics.Debug.WriteLine($"打开控件示例页面失败: {ex.Message}");
                throw new InvalidOperationException($"打开控件示例页面失败: {ex.Message}", ex);
            }
        }

        #region 私有转换方法

        /// <summary>
        /// 转换用户数据
        /// </summary>
        private static UserDto ConvertToUserDto(UserDto userDto)
        {
            return userDto;
        }

        #endregion

    }
}
using System;
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
        private readonly LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService _commonDialogService;

        private readonly LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService _authService;
        private readonly IRegionManager _regionManager;
        private readonly LYBT.Desktop.Core.Interfaces.Services.IPermissionService _permissionService;
        private readonly IUserService _userService;
        private readonly IPatientService _patientService;
        private readonly IWorkbenchRouter _workbenchRouter;
        private readonly ApiTestService _apiTestService;
        private readonly LYBT.Desktop.Core.Services.Performance.IUIPerformanceOptimizer _uiOptimizer;
        private readonly LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator _moduleLoadingCoordinator;

        private string _title = SystemConstants.SystemTitle;
        private UserDto? _currentUser;
        private bool _isLoggedIn = false;

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
            set => SetProperty(ref _isLoggedIn, value);
        }

        public DelegateCommand LogoutCommand { get; }
        public DelegateCommand TestApiCommand { get; }
        public DelegateCommand ShowControlExamplesCommand { get; }

        public MainWindowViewModel(IRegionManager regionManager,
            IEventAggregator eventAggregator,
            LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService authService,
            LYBT.Desktop.Core.Interfaces.Services.IPermissionService permissionService,
            IUserService userService,
            IPatientService patientService,
            LYBT.Desktop.Core.Interfaces.Services.ICustomDialogService commonDialogService,
            IWorkbenchRouter workbenchRouter,
            ApiTestService apiTestService,
            LYBT.Desktop.Core.Services.Performance.IUIPerformanceOptimizer uiOptimizer,
            LYBT.Desktop.Core.Services.Performance.IModuleLoadingCoordinator moduleLoadingCoordinator,
            IErrorHandlingService errorHandlingService)
            : base(eventAggregator, errorHandlingService)
        {
            _commonDialogService = commonDialogService;
            _regionManager = regionManager;
            _authService = authService;
            _permissionService = permissionService;
            _userService = userService;
            _patientService = patientService;
            _workbenchRouter = workbenchRouter;
            _apiTestService = apiTestService;
            _uiOptimizer = uiOptimizer;
            _moduleLoadingCoordinator = moduleLoadingCoordinator;

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
                    EventAggregator.GetEvent<LogoutEvent>().Publish();

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
                    await _commonDialogService.ShowErrorAsync($"退出登录失败：{ex.Message}", "错误");
                }
            }
        }

        /// <summary>
        /// 检查登录状态 - UltraThink性能优化版
        /// </summary>
        private async Task CheckLoginStatusAsync()
        {
            using var performanceSession = _uiOptimizer.StartUIPerformanceSession("启动登录检查");
            
            try
            {
                if (_authService.IsLoggedIn)
                {
                    var user = await _authService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        CurrentUser = ConvertToUserDto(user);
                        IsLoggedIn = true;
                        
                        // 批量UI更新以提升性能
                        _uiOptimizer.BatchUIUpdates(() =>
                        {
                            TestApiCommand.RaiseCanExecuteChanged();
                            ShowControlExamplesCommand.RaiseCanExecuteChanged();
                        });
                        
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
        /// 加载主界面内容 - UltraThink Phase 9 性能优化版
        /// </summary>
        private void LoadMainContent()
        {
            using var performanceSession = _uiOptimizer.StartUIPerformanceSession("主界面加载");
            
            if (CurrentUser == null)
            {
                throw new InvalidOperationException("当前用户信息为空，无法加载主界面");
            }

            // 判断用户角色
            string userRole;
            if (CurrentUser.Username?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true)
            {
                userRole = SystemConstants.RoleDisplayNames.SuperAdmin;
            }
            else if (CurrentUser.Role?.Equals(SystemConstants.DoctorRole, StringComparison.OrdinalIgnoreCase) == true ||
                     CurrentUser.Role?.Equals(SystemConstants.RoleDisplayNames.Doctor, StringComparison.OrdinalIgnoreCase) == true)
            {
                userRole = SystemConstants.RoleDisplayNames.Doctor;
            }
            else if (CurrentUser.Role?.Equals(SystemConstants.AdminRole, StringComparison.OrdinalIgnoreCase) == true ||
                     CurrentUser.Role?.Equals(SystemConstants.RoleDisplayNames.Admin, StringComparison.OrdinalIgnoreCase) == true)
            {
                userRole = SystemConstants.RoleDisplayNames.Admin;
            }
            else
            {
                // 默认角色
                userRole = SystemConstants.RoleDisplayNames.Doctor;
            }

            // UltraThink Phase 9: 启动智能模块预加载
            _ = StartIntelligentModulePreloadingAsync(userRole);

            // 使用WorkbenchRouter获取工作台信息
            var workbenchView = _workbenchRouter.GetWorkbenchForRole(userRole);
            var roleDisplay = _workbenchRouter.GetRoleDisplayName(userRole);
            var welcomeMessage = _workbenchRouter.GetWelcomeMessage(userRole, CurrentUser.RealName);
            
            // 预加载工作台需要的数据
            _uiOptimizer.PreloadData(async () => await _userService.GetPagedAsync(new UserPagedQueryDto { PageSize = 10 }), $"user_cache_{userRole}", priority: 1);

            if (_regionManager == null)
            {
                throw new InvalidOperationException("RegionManager为空");
            }

            try
            {
                // 批量UI更新 - 更新标题和清理区域
                _uiOptimizer.BatchUIUpdates(() =>
                {
                    Title = $"凌隐宝堂中医诊所诊疗系统 - {CurrentUser.RealName} ({roleDisplay})";
                    
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
                });

                // 根据角色导航到对应的工作台主视图
                _regionManager.RequestNavigate("ContentRegion", workbenchView, navigationResult =>
                {
                    if (navigationResult.Result != true)
                    {
                        // 导航失败，记录到调试输出
                        var errorMessage = navigationResult.Error?.Message ?? "未知导航错误";
                        System.Diagnostics.Debug.WriteLine($"工作台模块加载失败: {errorMessage}");
                    }
                    else
                    {
                        // 导航成功 - 开始预加载其他可能需要的数据
                        System.Diagnostics.Debug.WriteLine($"成功导航到工作台：{workbenchView}");
                        StartDataPreloading(userRole);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载主界面内容时发生错误: {ex.Message}");
                throw new InvalidOperationException($"工作台模块加载失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// UltraThink Phase 9: 启动智能模块预加载
        /// </summary>
        private async Task StartIntelligentModulePreloadingAsync(string userRole)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UltraThink Phase 9: 开始为角色 {userRole} 预加载模块");
                
                // 使用模块加载协调器进行智能预加载
                await _moduleLoadingCoordinator.PreloadModulesAsync(userRole);
                
                // 输出模块加载性能指标
                var metrics = _moduleLoadingCoordinator.GetLoadingMetrics();
                var loadedModules = metrics.Where(kvp => kvp.Value.LoadCount > 0).ToList();
                
                if (loadedModules.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"UltraThink性能报告: 已加载 {loadedModules.Count} 个模块");
                    foreach (var (moduleName, metric) in loadedModules)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {moduleName}: {metric.InitializationTime.TotalMilliseconds:F0}ms");
                    }
                }

                // 执行模块加载顺序优化
                _moduleLoadingCoordinator.OptimizeModuleLoadingOrder();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"智能模块预加载失败: {ex.Message}");
                // 不抛出异常，避免影响主界面加载
            }
        }

        /// <summary>
        /// 根据用户角色预加载相关数据
        /// </summary>
        private void StartDataPreloading(string userRole)
        {
            // 根据角色预加载不同的数据
            switch (userRole)
            {
                case var role when role == SystemConstants.RoleDisplayNames.Doctor:
                    // 医生角色预加载患者和验方数据
                    _uiOptimizer.PreloadData(async () => 
                    {
                        return await _patientService.GetPagedAsync(new LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto { PageSize = 20 });
                    }, "recent_patients", priority: 2);
                    break;
                    
                case var role when role == SystemConstants.RoleDisplayNames.Admin:
                    // 管理员角色预加载用户和系统数据
                    _uiOptimizer.PreloadData(async () => 
                    {
                        return await _userService.GetPagedAsync(new UserPagedQueryDto { PageSize = 50 });
                    }, "all_users", priority: 2);
                    break;
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
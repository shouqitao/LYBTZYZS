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
            var result = await _commonDialogService.ShowConfirmationAsync("确定要退出登录吗？", "退出确认");
            if (result)
            {
                try
                {
                    // 立即更新UI状态，给用户即时反馈
                    CurrentUser = null;
                    IsLoggedIn = false;
                    Title = "凌隐宝堂中医诊所诊疗系统";

                    // 批量UI更新 - 立即清理界面
                    _uiOptimizer.BatchUIUpdates(() =>
                    {
                        // 清除内容区域
                        if (_regionManager.Regions.ContainsRegionWithName("ContentRegion"))
                        {
                            _regionManager.Regions["ContentRegion"].RemoveAll();
                        }
                        
                        // 立即显示登录界面
                        ShowLoginDialog();
                    });

                    // 后台异步处理网络请求和事件，不阻塞UI
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            // 网络登出请求
                            await _authService.LogoutAsync();
                            
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

            // 简化角色判断逻辑：只区分管理员和医生
            string userRole;
            string workbenchView;
            string roleDisplay;
            
            // 管理员判断（包括sysadmin用户名和Admin角色）
            if (CurrentUser.Username?.Equals("sysadmin", StringComparison.OrdinalIgnoreCase) == true ||
                CurrentUser.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true ||
                CurrentUser.Role?.Equals("管理员", StringComparison.OrdinalIgnoreCase) == true)
            {
                userRole = "管理员";
                workbenchView = "SystemWorkbenchMainView";
                roleDisplay = "管理员";
            }
            else if (CurrentUser.Role?.Equals("Doctor", StringComparison.OrdinalIgnoreCase) == true ||
                     CurrentUser.Role?.Equals("医生", StringComparison.OrdinalIgnoreCase) == true)
            {
                // 医生角色
                userRole = "医生";
                workbenchView = "ConsultationWorkbenchMainView";
                roleDisplay = "医生";
            }
            else
            {
                // 其他角色默认为医生工作台，但显示欢迎页
                userRole = "医生";
                workbenchView = "ConsultationWorkbenchMainView";
                roleDisplay = CurrentUser.Role ?? "访客";
            }

            // UltraThink 诊断日志 - 增强调试信息（同时输出到调试和文件）
            var diagnosticInfo = $"""
                === 导航诊断开始 ===
                时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                用户: {CurrentUser.Username}
                角色: {CurrentUser.Role}
                分配工作台: {workbenchView}
                ContentRegion 是否存在: {_regionManager?.Regions?.ContainsRegionWithName("ContentRegion")}
                所有区域: {string.Join(", ", _regionManager?.Regions?.Select(r => r.Name) ?? new string[0])}
                """;
            
            System.Diagnostics.Debug.WriteLine(diagnosticInfo);
            
            // 同时输出到文件以便调试
            try 
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_Navigation_Debug.txt");
                File.AppendAllText(logPath, diagnosticInfo + Environment.NewLine);
            }
            catch { /* 忽略文件写入错误 */ }

            if (_regionManager == null)
            {
                throw new InvalidOperationException("RegionManager为空");
            }

            try
            {
                // 批量UI更新 - 更新标题和清理登录区域
                _uiOptimizer.BatchUIUpdates(() =>
                {
                    Title = $"凌隐宝堂中医诊所诊疗系统 - {CurrentUser.RealName} ({roleDisplay})";
                    
                    // 修复: 不要清除ContentRegion，导航会自动替换内容
                    // 只清除登录区域，因为登录已完成
                    if (_regionManager.Regions.ContainsRegionWithName("LoginRegion"))
                    {
                        _regionManager.Regions["LoginRegion"].RemoveAll();
                        System.Diagnostics.Debug.WriteLine("✅ LoginRegion 已清理");
                    }
                });

                // 根据角色导航到对应的工作台主视图
                System.Diagnostics.Debug.WriteLine($"🚀 开始导航到: {workbenchView}");
                _regionManager.RequestNavigate("ContentRegion", workbenchView, navigationResult =>
                {
                    string resultInfo;
                    if (navigationResult.Result != true)
                    {
                        // 导航失败，记录详细错误信息
                        var errorMessage = navigationResult.Error?.Message ?? "未知导航错误";
                        var innerException = navigationResult.Error?.InnerException?.Message ?? "无内部异常";
                        
                        resultInfo = $"""
                            ❌ 工作台模块加载失败:
                            错误消息: {errorMessage}
                            内部异常: {innerException}
                            目标视图: {workbenchView}
                            目标区域: ContentRegion
                            完整错误: {navigationResult.Error}
                            === 导航诊断结束 ===
                            """;
                    }
                    else
                    {
                        // 导航成功 - 开始预加载其他可能需要的数据
                        resultInfo = $"""
                            ✅ 成功导航到工作台：{workbenchView}
                            导航URI: {navigationResult.Context?.Uri}
                            === 导航诊断结束 ===
                            """;
                        StartDataPreloading(userRole);
                    }
                    
                    System.Diagnostics.Debug.WriteLine(resultInfo);
                    
                    // 同时输出到文件
                    try 
                    {
                        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "LYBT_Navigation_Debug.txt");
                        File.AppendAllText(logPath, resultInfo + Environment.NewLine + Environment.NewLine);
                    }
                    catch { /* 忽略文件写入错误 */ }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ 加载主界面内容时发生错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
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
            // 根据用户角色预加载不同的数据
            switch (userRole)
            {
                case "医生":
                    // 医生角色预加载患者和验方数据
                    _uiOptimizer.PreloadData(async () => 
                    {
                        return await _patientService.GetPagedAsync(new LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto { PageSize = 20 });
                    }, "recent_patients", priority: 2);
                    break;
                    
                case "管理员":
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
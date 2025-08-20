using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Events;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Events;
// UltraThink v2.0: 添加SessionAware相关依赖
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 主页视图模型 - 基于角色显示不同内容
    /// </summary>
    /// <summary>
    /// 主页视图模型 - 基于角色显示不同内容
    /// UltraThink v2.0: 重构为SessionAware架构，集成统一会话管理
    /// </summary>
    public class HomeViewModel : SessionAwareViewModel, INavigationAware
    {
        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IAuthenticationService _authService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IEventAggregator _eventAggregator;
        private readonly DispatcherTimer _timer;

        #endregion

        #region 属性

        private string _subTitle = "工作台";
        public string SubTitle
        {
            get => _subTitle;
            set => SetProperty(ref _subTitle, value);
        }

        private string _welcomeMessage = "";
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        private bool _isDoctorRole;
        public bool IsDoctorRole
        {
            get => _isDoctorRole;
            set => SetProperty(ref _isDoctorRole, value);
        }

        private bool _isAdminRole;
        public bool IsAdminRole
        {
            get => _isAdminRole;
            set => SetProperty(ref _isAdminRole, value);
        }

        private int _todayCompletedCount;
        public int TodayCompletedCount
        {
            get => _todayCompletedCount;
            set => SetProperty(ref _todayCompletedCount, value);
        }

        private int _todayInProgressCount;
        public int TodayInProgressCount
        {
            get => _todayInProgressCount;
            set => SetProperty(ref _todayInProgressCount, value);
        }

        private decimal _todayTotalAmount;
        public decimal TodayTotalAmount
        {
            get => _todayTotalAmount;
            set => SetProperty(ref _todayTotalAmount, value);
        }

        private string _statusMessage = "就绪";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private string _currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        #endregion

        #region 命令

        // 通用命令
        public DelegateCommand LogoutCommand { get; }

        // 医生命令
        public DelegateCommand StartConsultationCommand { get; }
        public DelegateCommand NavigateToPatientReceptionCommand { get; }
        public DelegateCommand NavigateToMedicalCaseCommand { get; }
        public DelegateCommand NavigateToPrescriptionQueryCommand { get; }
        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToHerbViewCommand { get; }
        public DelegateCommand NavigateToFormulaViewCommand { get; }

        // 管理员命令
        public DelegateCommand EnterSystemManagementCommand { get; }
        public DelegateCommand NavigateToUserManagementCommand { get; }
        public DelegateCommand NavigateToHerbManagementCommand { get; }
        public DelegateCommand NavigateToFormulaManagementCommand { get; }
        public DelegateCommand NavigateToSystemSettingsCommand { get; }
        public DelegateCommand NavigateToDataBackupCommand { get; }

        #endregion

        #region 构造函数

        public HomeViewModel(
            IRegionManager regionManager,
            IAuthenticationService authService,
            IUserSessionManager userSessionManager,
            IMedicalCaseService medicalCaseService,
            IEventAggregator eventAggregator,
            ISessionManager sessionManager,
            INotificationService notificationService,
            ILogger<HomeViewModel> logger)
            : base(sessionManager, notificationService, logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userSessionManager = userSessionManager ?? throw new ArgumentNullException(nameof(userSessionManager));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // 初始化命令
            LogoutCommand = new DelegateCommand(async () => await LogoutAsync());

            // 医生命令
            StartConsultationCommand = new DelegateCommand(StartConsultation);
            NavigateToPatientReceptionCommand = new DelegateCommand(() => NavigateTo("PatientReceptionView"));
            NavigateToMedicalCaseCommand = new DelegateCommand(() => NavigateTo("MedicalCaseListView"));
            NavigateToPrescriptionQueryCommand = new DelegateCommand(() => NavigateTo("PrescriptionManagementView"));
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToHerbViewCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToFormulaViewCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));

            // 管理员命令
            EnterSystemManagementCommand = new DelegateCommand(() => NavigateTo("AdminMainView"));
            NavigateToUserManagementCommand = new DelegateCommand(EnterSystemManagementWithUserModule);
            NavigateToHerbManagementCommand = new DelegateCommand(EnterSystemManagementWithHerbModule);
            NavigateToFormulaManagementCommand = new DelegateCommand(EnterSystemManagementWithFormulaModule);
            NavigateToSystemSettingsCommand = new DelegateCommand(() => NavigateTo("SystemSettingsView"));
            NavigateToDataBackupCommand = new DelegateCommand(() => NavigateTo("DataBackupView"));

            // 初始化定时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();

            // 初始化
            _ = InitializeAsync();
            
            LogInfo("HomeViewModel 已初始化，使用 UltraThink SessionManager 架构");
        }

        #endregion

        #region UltraThink SessionAware 重写方法

        /// <summary>
        /// 当SessionManager中的用户状态变化时调用
        /// </summary>
        protected override void OnUserChanged(UserChangedEventArgs args)
        {
            base.OnUserChanged(args);
            
            // 用户状态变化时重新初始化界面
            _ = Task.Run(async () => await InitializeAsync());
            LogInfo($"用户状态变化，重新初始化HomeViewModel: {args.NewUser?.UserName ?? "null"}");
        }

        #endregion

        #region 初始化

        private async Task InitializeAsync()
        {
            try
            {
                ShowLoading("正在加载主页...");
                
                // 先设置默认值，确保界面能显示
                WelcomeMessage = "欢迎使用系统";
                SubTitle = "加载中...";
                
                // UltraThink SessionManager: 优先使用SessionManager中的用户信息
                var currentUser = CurrentUser ?? await _authService.GetCurrentUserAsync();
                
                if (currentUser != null)
                {
                    WelcomeMessage = $"欢迎，{currentUser.RealName}";
                    
                    // 判断角色 - 使用Role属性字符串比较
                    if (currentUser.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        IsAdminRole = true;
                        IsDoctorRole = false;
                        SubTitle = "系统管理工作台";
                    }
                    else
                    {
                        IsDoctorRole = true;
                        IsAdminRole = false;
                        SubTitle = "医生工作台";
                        
                        // 尝试加载今日统计，但不让它阻塞界面显示
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await LoadTodayStatisticsAsync();
                            }
                            catch (Exception ex)
                            {
                                LogError(ex, "加载今日统计失败");
                                // 设置默认统计值
                                TodayCompletedCount = 0;
                                TodayInProgressCount = 0;
                                TodayTotalAmount = 0;
                            }
                        });
                    }
                }
                else
                {
                    // 如果无法获取用户信息，设置默认显示
                    WelcomeMessage = "用户信息获取失败";
                    IsAdminRole = true; // 默认显示管理员界面
                    IsDoctorRole = false;
                    SubTitle = "系统管理工作台（默认）";
                }

                UpdateDateTime();
                ShowSuccess("主页加载完成");
            }
            catch (Exception ex)
            {
                LogError(ex, "初始化主页失败");
                ShowError("初始化主页失败，请重试");
                
                // 即使出错也要设置基本的界面状态
                WelcomeMessage = "初始化失败，请重试";
                IsAdminRole = true; // 默认显示管理员界面
                IsDoctorRole = false;
                SubTitle = "系统管理工作台（错误恢复）";
                UpdateDateTime();
            }
            finally
            {
                HideLoading();
            }
        }

        private async Task LoadTodayStatisticsAsync()
        {
            try
            {
                // 获取今日医疗案例统计
                var query = new LYBT.Shared.Models.Contracts.Common.PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    IsDescending = true
                };
                var result = await _medicalCaseService.GetPagedAsync(query);

                if (result != null && result.IsSuccess && result.Data?.Items != null)
                {
                    // 使用CaseStatus专用状态字段进行正确的医疗案例状态统计
                    TodayCompletedCount = result.Data.Items
                        .Count(c => c.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed);
                    
                    TodayInProgressCount = result.Data.Items
                        .Count(c => c.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation);
                    
                    // TODO: 计算今日收入（需要从处方模块获取）
                    TodayTotalAmount = TodayCompletedCount * 150; // 临时模拟数据
                    
                    LogInfo($"今日统计加载完成 - 完成: {TodayCompletedCount}, 进行中: {TodayInProgressCount}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载今日统计失败");
                throw; // 重新抛出，让调用方处理
            }
        }

        private void UpdateDateTime()
        {
            CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        #endregion

        #region 导航方法

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("ContentRegion", viewName);
                StatusMessage = $"已导航到 {GetViewDisplayName(viewName)}";
                ShowInfo($"已导航到 {GetViewDisplayName(viewName)}");
            }
            catch (Exception ex)
            {
                LogError(ex, "导航失败: {ViewName}", viewName);
                ShowError($"导航到 {GetViewDisplayName(viewName)} 失败");
            }
        }

        private void StartConsultation()
        {
            // 导航到患者接待，开始看诊流程
            NavigateTo("PatientReceptionView");
            StatusMessage = "开始看诊流程";
        }

        private void EnterSystemManagementWithUserModule()
        {
            // 使用字符串参数方式导航 - Prism 9兼容
            _regionManager.RequestNavigate("ContentRegion", "AdminMainView?DefaultModule=UserManagement");
        }

        private void EnterSystemManagementWithHerbModule()
        {
            // 使用字符串参数方式导航 - Prism 9兼容
            _regionManager.RequestNavigate("ContentRegion", "AdminMainView?DefaultModule=HerbManagement");
        }

        private void EnterSystemManagementWithFormulaModule()
        {
            // 使用字符串参数方式导航 - Prism 9兼容
            _regionManager.RequestNavigate("ContentRegion", "AdminMainView?DefaultModule=FormulaManagement");
        }

        private async Task LogoutAsync()
        {
            try
            {
                var confirm = await NotificationService.ShowConfirmAsync("确定要退出登录吗？", "退出确认");
                if (confirm)
                {
                    await _authService.LogoutAsync();
                    
                    // UltraThink SessionManager: 清除会话状态
                    SessionManager.ClearUserSession();
                    
                    _eventAggregator.GetEvent<LogoutEvent>().Publish();
                    ShowSuccess("已成功退出登录");
                    LogInfo("用户已退出登录");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "退出登录失败");
                ShowError("退出登录失败，请重试");
            }
        }

        private string GetViewDisplayName(string viewName)
        {
            return viewName switch
            {
                "PatientReceptionView" => "患者接待",
                "MedicalCaseListView" => "医疗案例",
                "PrescriptionManagementView" => "处方管理",
                "PatientManagementView" => "患者管理",
                "HerbManagementView" => "药材管理",
                "FormulaManagementView" => "验方管理",
                "AdminMainView" => "系统管理中心",
                "UserManagementView" => "用户管理",
                _ => viewName
            };
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _ = InitializeAsync();
            
            // 如果是医生角色，定时刷新统计数据
            if (IsDoctorRole)
            {
                var refreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(5)
                };
                refreshTimer.Tick += async (s, e) => await LoadTodayStatisticsAsync();
                refreshTimer.Start();
            }
            
            LogInfo("HomeViewModel 导航进入");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 停止定时器
            _timer?.Stop();
            LogInfo("HomeViewModel 导航离开");
        }

        #endregion

        #region IDisposable 补充实现

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 停止定时器
                _timer?.Stop();
                LogInfo("HomeViewModel 定时器已停止");
            }
            
            base.Dispose(disposing);
        }

        #endregion
    }
}
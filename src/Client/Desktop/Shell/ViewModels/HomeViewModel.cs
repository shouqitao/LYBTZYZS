using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Shell.Models;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.ViewModels
{

    /// <summary>
    /// 主页视图模型 - 基于角色显示不同内容，集成统一会话管理
    /// </summary>
    public class HomeViewModel : SessionAwareViewModel, INavigationAware
    {

        #region 依赖服务

        private readonly IRegionManager _regionManager;
        private readonly IAuthenticationService _authService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IPatientService _patientService;
        private readonly IEventAggregator _eventAggregator;
        private readonly DispatcherTimer _timer;
        private DispatcherTimer? _refreshTimer; // DT-013: 追踪第二个定时器防止内存泄漏

        #endregion 依赖服务

        #region 属性

        private string _subTitle = "工作台";

        public string SubTitle
        {
            get => _subTitle;
            set => SetProperty(ref _subTitle, value);
        }

        private string _welcomeMessage = string.Empty;

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

        private ObservableCollection<TodayPatientDto> _todayPatients = new();

        public ObservableCollection<TodayPatientDto> TodayPatients
        {
            get => _todayPatients;
            set => SetProperty(ref _todayPatients, value);
        }

        private TodayPatientDto? _selectedPatient;

        public TodayPatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        #endregion 属性

        #region 命令

        // 通用命令
        public DelegateCommand LogoutCommand { get; }

        // 医生命令
        public DelegateCommand StartConsultationCommand { get; }

        public DelegateCommand NavigateToPatientReceptionCommand { get; }
        public DelegateCommand NavigateToMedicalCaseCommand { get; }
        public DelegateCommand NavigateToPrescriptionQueryCommand { get; }
        public DelegateCommand NavigateToPatientManagementCommand { get; }
        public DelegateCommand NavigateToHerbsCommand { get; }
        public DelegateCommand NavigateToFormulasCommand { get; }

        // 管理员命令
        public DelegateCommand EnterSystemManagementCommand { get; }

        public DelegateCommand NavigateToUserManagementCommand { get; }
        public DelegateCommand NavigateToHerbManagementCommand { get; }
        public DelegateCommand NavigateToFormulaManagementCommand { get; }

        // 今日患者操作命令
        public DelegateCommand<TodayPatientDto> StartConsultationForPatientCommand { get; }
        public DelegateCommand<TodayPatientDto> ViewPatientDetailsCommand { get; }
        public DelegateCommand RefreshTodayPatientsCommand { get; }

        // 非核心功能命令已清理
        #endregion 命令

        #region 构造函数

        public HomeViewModel(
            IRegionManager regionManager,
            IAuthenticationService authService,
            IUserSessionManager userSessionManager,
            IMedicalCaseService medicalCaseService,
            IPatientService patientService,
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
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // 初始化命令
            LogoutCommand = new DelegateCommand(async () => await LogoutAsync());

            // 医生命令
            StartConsultationCommand = new DelegateCommand(StartConsultation);
            NavigateToPatientReceptionCommand = new DelegateCommand(() => NavigateTo("PatientReceptionView"));
            NavigateToMedicalCaseCommand = new DelegateCommand(() => NavigateTo("MedicalCaseListView"));
            NavigateToPrescriptionQueryCommand = new DelegateCommand(() => NavigateTo("PrescriptionManagementView"));
            NavigateToPatientManagementCommand = new DelegateCommand(() => NavigateTo("PatientManagementView"));
            NavigateToHerbsCommand = new DelegateCommand(() => NavigateTo("HerbManagementView"));
            NavigateToFormulasCommand = new DelegateCommand(() => NavigateTo("FormulaManagementView"));

            // 管理员命令
            EnterSystemManagementCommand = new DelegateCommand(() => NavigateTo("SystemWorkbenchMainView"));
            NavigateToUserManagementCommand = new DelegateCommand(EnterSystemManagementWithUserModule);
            NavigateToHerbManagementCommand = new DelegateCommand(EnterSystemManagementWithHerbModule);
            NavigateToFormulaManagementCommand = new DelegateCommand(EnterSystemManagementWithFormulaModule);

            // 今日患者操作命令
            StartConsultationForPatientCommand = new DelegateCommand<TodayPatientDto>(async patient => await StartConsultationForPatientAsync(patient), CanExecutePatientCommand);
            ViewPatientDetailsCommand = new DelegateCommand<TodayPatientDto>(ViewPatientDetails, CanExecutePatientCommand);

            // Epic 04-P0-04: 界面响应性提升 - 优化刷新命令，使用统一的加载方法
            RefreshTodayPatientsCommand = new DelegateCommand(async () => await LoadTodayDataAsync());

            // 非核心功能命令初始化已清理

            // 初始化定时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();

            // 初始化
            _ = InitializeAsync();

            LogInfo("HomeViewModel 已初始化");
        }

        #endregion 构造函数

        /// <inheritdoc/>
        protected override void OnUserChanged(UserChangedEventArgs args)
        {
            base.OnUserChanged(args);
            _ = Task.Run(async () => await InitializeAsync());
            LogInfo($"用户状态变化，重新初始化HomeViewModel: {args.NewUser?.Username ?? "null"}");
        }

        #region 初始化

        private async Task InitializeAsync()
        {
            try
            {
                ShowLoading("正在加载主页...");

                // 先设置默认值，确保界面能显示
                WelcomeMessage = "欢迎使用系统";
                SubTitle = "加载中...";

                // 获取当前用户信息
                var currentUser = CurrentUser ?? await _authService.GetCurrentUserAsync();

                if (currentUser != null)
                {
                    WelcomeMessage = $"欢迎，{currentUser.RealName}";

                    // 判断用户角色
                    if (currentUser.Role == UserRole.Admin)
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

                        // Epic 04-P0-04: 界面响应性提升 - 异步加载今日数据并提供状态反馈
                        _ = LoadTodayDataAsync();
                    }
                }
                else
                {
                    WelcomeMessage = "用户信息获取失败";
                    IsAdminRole = true;
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

                // 错误恢复：设置基本界面状态
                WelcomeMessage = "初始化失败，请重试";
                IsAdminRole = true;
                IsDoctorRole = false;
                SubTitle = "系统管理工作台（错误恢复）";
                UpdateDateTime();
            }
            finally
            {
                HideLoading();
            }
        }

        /// <summary>
        /// Epic 04-P0-04: 界面响应性提升 - 异步加载今日数据并提供状态反馈
        /// 专为小诊所优化，提供流畅的用户体验和适当的错误恢复
        /// </summary>
        private async Task LoadTodayDataAsync()
        {
            try
            {
                // 显示加载状态
                StatusMessage = "正在加载今日工作台数据...";

                // 并行加载数据以提升响应性
                var statisticsTask = LoadTodayStatisticsAsync();
                var patientsTask = LoadTodayPatientsAsync();

                // 等待所有任务完成，但在UI线程上更新状态
                await Task.WhenAll(statisticsTask, patientsTask);

                StatusMessage = "今日数据加载完成";
                LogInfo("今日工作台数据加载完成");
            }
            catch (Exception ex)
            {
                LogError(ex, "加载今日数据失败");
                StatusMessage = "数据加载失败，已设置默认值";

                // Epic 04-P0-04: 优雅的错误恢复 - 设置合理的默认值
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TodayCompletedCount = 0;
                    TodayInProgressCount = 0;
                    TodayTotalAmount = 0;
                    TodayPatients.Clear();
                });

                ShowWarning("今日数据加载失败，请稍后重试或使用刷新按钮");
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
                    TodayCompletedCount = result.Data.Items
                        .Count(c => c.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed);

                    TodayInProgressCount = result.Data.Items
                        .Count(c => c.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation);

                    // 简化收入计算 (每个案例固定150元)
                    TodayTotalAmount = TodayCompletedCount * 150;

                    LogInfo($"今日统计加载完成 - 完成: {TodayCompletedCount}, 进行中: {TodayInProgressCount}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载今日统计失败");
                throw; // 重新抛出，让调用方处理
            }
        }

        private async Task LoadTodayPatientsAsync()
        {
            try
            {
                // 获取今日有就诊记录的患者
                var todayStart = DateTime.Today;
                var todayEnd = DateTime.Today.AddDays(1).AddTicks(-1);

                var query = new LYBT.Shared.Models.Contracts.Common.PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100, // 增加页面大小以获取更多今日数据
                    IsDescending = true
                };

                var medicalCasesResult = await _medicalCaseService.GetPagedAsync(query);

                if (medicalCasesResult?.IsSuccess == true && medicalCasesResult.Data?.Items != null)
                {
                    var todayCases = medicalCasesResult.Data.Items
                        .Where(c => c.CreateTime >= todayStart && c.CreateTime <= todayEnd)
                        .ToList();

                    var todayPatientsList = new List<TodayPatientDto>();

                    if (todayCases.Any())
                    {
                        // 批量获取患者信息，避免逐个查询
                        var patientIds = todayCases.Select(c => c.PatientId).Distinct().ToList();
                        var patientTasks = patientIds.Select(async patientId =>
                        {
                            try
                            {
                                var result = await _patientService.GetByIdAsync(patientId);
                                return result?.IsSuccess == true ? result.Data : null;
                            }
                            catch (Exception ex)
                            {
                                LogError(ex, "批量查询患者信息失败: {PatientId}", patientId);
                                return null;
                            }
                        });

                        var patients = await Task.WhenAll(patientTasks);
                        var patientDict = patients
                            .Where(p => p != null)
                            .ToDictionary(p => p!.Id, p => p);

                        // 构建今日患者列表
                        foreach (var medicalCase in todayCases)
                        {
                            if (patientDict.TryGetValue(medicalCase.PatientId, out var patient) && patient != null)
                            {
                                var todayPatient = new TodayPatientDto
                                {
                                    Id = patient.Id,
                                    Name = patient.Name,
                                    Age = patient.Age,
                                    Gender = patient.Gender,
                                    PhoneNumber = patient.PhoneNumber ?? string.Empty,
                                    MedicalCaseId = medicalCase.Id,
                                    Status = GetCaseStatusText(medicalCase.CaseStatus),
                                    StatusColor = GetCaseStatusColor(medicalCase.CaseStatus),
                                    CreateTime = medicalCase.CreateTime,

                                    // 添加更多有用信息
                                    DoctorName = medicalCase.DoctorName ?? "未指定",
                                    CaseStatus = medicalCase.CaseStatus
                                };
                                todayPatientsList.Add(todayPatient);
                            }
                        }
                    }

                    // 更新UI（需要在UI线程上执行）
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TodayPatients.Clear();

                        // 按状态优先级排序：诊疗中 > 已挂号 > 已完成 > 已取消
                        var sortedPatients = todayPatientsList
                            .OrderBy(p => GetStatusPriority(p.CaseStatus))
                            .ThenByDescending(p => p.CreateTime);

                        foreach (var patient in sortedPatients)
                        {
                            TodayPatients.Add(patient);
                        }

                        // 更新统计数据
                        UpdateTodayStatisticsFromPatients(todayPatientsList);
                    });

                    LogInfo($"今日患者列表加载完成，共 {todayPatientsList.Count} 人");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载今日患者列表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取状态优先级，用于排序
        /// </summary>
        private int GetStatusPriority(LYBT.Shared.Models.Enums.MedicalCaseStatus status)
        {
            return status switch
            {
                LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation => 1, // 最高优先级
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Registered => 2,
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed => 3,
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Cancelled => 4,
                _ => 5
            };
        }

        /// <summary>
        /// 根据患者列表更新今日统计数据
        /// </summary>
        private void UpdateTodayStatisticsFromPatients(List<TodayPatientDto> patients)
        {
            try
            {
                var registeredCount = patients.Count(p => p.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.Registered);
                var inConsultationCount = patients.Count(p => p.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation);
                var completedCount = patients.Count(p => p.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed);

                // 待诊人数 = 已挂号 + 诊疗中
                TodayInProgressCount = registeredCount + inConsultationCount;
                TodayCompletedCount = completedCount;

                LogInfo($"今日统计更新: 待诊 {TodayInProgressCount} 人, 已完成 {TodayCompletedCount} 人");
            }
            catch (Exception ex)
            {
                LogError(ex, "更新今日统计数据失败");
            }
        }

        private string GetCaseStatusText(LYBT.Shared.Models.Enums.MedicalCaseStatus status)
        {
            return status switch
            {
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Registered => "已挂号",
                LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation => "诊疗中",
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed => "已完成",
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Cancelled => "已取消",
                _ => "未知"
            };
        }

        private string GetCaseStatusColor(LYBT.Shared.Models.Enums.MedicalCaseStatus status)
        {
            return status switch
            {
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Registered => "#FFA500", // 橙色 - 等待
                LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation => "#1E90FF", // 蓝色 - 进行中
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed => "#32CD32", // 绿色 - 已完成
                LYBT.Shared.Models.Enums.MedicalCaseStatus.Cancelled => "#DC143C", // 红色 - 已取消
                _ => "#808080" // 灰色 - 未知
            };
        }

        private void UpdateDateTime()
        {
            CurrentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        #endregion 初始化

        #region 导航方法

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName);
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
            NavigateTo("PatientReceptionView");
            StatusMessage = "开始看诊流程";
        }

        private void EnterSystemManagementWithUserModule()
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "SystemWorkbenchMainView?DefaultModule=UserManagement");
        }

        private void EnterSystemManagementWithHerbModule()
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "SystemWorkbenchMainView?DefaultModule=HerbManagement");
        }

        private void EnterSystemManagementWithFormulaModule()
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "SystemWorkbenchMainView?DefaultModule=FormulaManagement");
        }

        private async Task LogoutAsync()
        {
            try
            {
                var confirm = await NotificationService.ShowConfirmAsync("确定要退出登录吗？", "退出确认");
                if (confirm)
                {
                    await _authService.LogoutAsync();

                    // 清除会话状态
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
                "SystemWorkbenchMainView" => "系统管理中心",
                "UserManagementView" => "用户管理",
                _ => viewName
            };
        }

        #endregion 导航方法

        #region 患者操作方法

        private bool CanExecutePatientCommand(TodayPatientDto? patient)
        {
            return patient != null;
        }

        private async Task StartConsultationForPatientAsync(TodayPatientDto? patient)
        {
            if (patient == null)
            {
                ShowWarning("请选择要诊疗的患者");
                return;
            }

            try
            {
                ShowLoading($"正在为患者 {patient.Name} 开始诊疗...");

                // 导航到诊疗页面，传递患者和医案信息
                var navigationParameters = new NavigationParameters
                {
                    { "PatientId", patient.Id },
                    { "MedicalCaseId", patient.MedicalCaseId },
                    { "PatientName", patient.Name }
                };

                _regionManager.RequestNavigate(RegionNames.ContentRegion, "ConsultationMainView", navigationParameters);

                StatusMessage = $"已开始为患者 {patient.Name} 诊疗";
                ShowSuccess($"已开始为患者 {patient.Name} 诊疗");

                LogInfo($"开始为患者 {patient.Name}(ID: {patient.Id}) 诊疗，医案ID: {patient.MedicalCaseId}");
            }
            catch (Exception ex)
            {
                LogError(ex, "开始诊疗失败: 患者 {PatientName}", patient.Name);
                ShowError($"开始为患者 {patient.Name} 诊疗失败，请重试");
            }
            finally
            {
                HideLoading();
            }
        }

        private void ViewPatientDetails(TodayPatientDto? patient)
        {
            if (patient == null)
            {
                ShowWarning("请选择要查看的患者");
                return;
            }

            try
            {
                // 导航到患者详情页面
                var navigationParameters = new NavigationParameters
                {
                    { "PatientId", patient.Id },
                    { "PatientName", patient.Name }
                };

                _regionManager.RequestNavigate(RegionNames.ContentRegion, "PatientManagementView", navigationParameters);

                StatusMessage = $"已打开患者 {patient.Name} 的详细信息";
                ShowInfo($"已打开患者 {patient.Name} 的详细信息");

                LogInfo($"查看患者详情: {patient.Name}(ID: {patient.Id})");
            }
            catch (Exception ex)
            {
                LogError(ex, "查看患者详情失败: 患者 {PatientName}", patient.Name);
                ShowError($"查看患者 {patient.Name} 详情失败，请重试");
            }
        }

        #endregion 患者操作方法

        #region INavigationAware

        /// <inheritdoc/>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _ = InitializeAsync();

            // 如果是医生角色，定时刷新统计数据和患者列表
            if (IsDoctorRole)
            {
                // DT-013: 使用实例变量追踪定时器，防止内存泄漏
                _refreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(5)
                };

                // Epic 04-P0-04: 界面响应性提升 - 优化定时器刷新，避免UI阻塞
                _refreshTimer.Tick += async (s, e) =>
                {
                    try
                    {
                        // 使用优化后的异步加载方法，提供状态反馈
                        await LoadTodayDataAsync();
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "定时器刷新数据失败");
                        StatusMessage = "自动刷新失败";
                    }
                };
                _refreshTimer.Start();
            }

            LogInfo("HomeViewModel 导航进入");
        }

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 停止定时器
            _timer?.Stop();

            // DT-013: 同时停止刷新定时器，防止内存泄漏
            _refreshTimer?.Stop();
            LogInfo("HomeViewModel 导航离开");
        }

        #endregion INavigationAware

        #region IDisposable 补充实现

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // DT-013: 停止所有定时器，防止内存泄漏
                _timer?.Stop();
                _refreshTimer?.Stop();

                System.Diagnostics.Debug.WriteLine("🧹 [HomeViewModel] 所有定时器已停止 - 内存泄漏风险已消除");
                LogInfo("HomeViewModel 定时器已停止");
            }

            base.Dispose(disposing);
        }

        #endregion IDisposable 补充实现
    }
}

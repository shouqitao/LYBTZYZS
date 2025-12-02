using System.Collections.ObjectModel;
using System.Net.Http;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Epic #1773: 使用DataManager替代Repository
using LYBT.Desktop.MedicalCase.Models; // OpenSpec: refine-medicalcase-edit-modes
// Epic #1773: 已移除LYBT.Desktop.MedicalCase.Interfaces（不再直接使用IMedicalCaseRepository）
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Components; // Issue #1788: 添加Component命名空间
using LYBT.Desktop.Patients.Services; // Issue #1790: 引入Manager服务
using LYBT.Desktop.Patients.Events; // Issue #2221: 引入PatientUpdatedEvent
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者选择ViewModel - Issue #1557 Step 1
    /// 看诊流程模块化迁移 - DDD聚合根对齐
    ///
    /// 功能：
    /// - 搜索患者（姓名/拼音码/手机号）
    /// - 分页加载（每页50条）
    /// - 选择患者后发布PatientSelectedEvent事件
    /// - 支持新建患者快速对话框
    /// - 通过EventAggregator与MedicalCaseFlowViewModel解耦通信
    /// </summary>
    public class PatientSelectionViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        // Issue #1788: 使用CommandHandler替代直接Repository访问
        private readonly PatientCommandHandler _commandHandler;
        // Epic #1773: 使用MedicalCaseDataManager替代IMedicalCaseRepository直接依赖
        private readonly MedicalCaseDataManager _medicalCaseDataManager;
        private readonly IDialogService _dialogService;
        private readonly IMedicalCaseApi _medicalCaseApi;
        private System.Threading.Timer? _searchDebounceTimer;

        // Issue #2221: IDisposable相关字段
        private bool _disposed = false;
        private Prism.Events.SubscriptionToken? _patientUpdatedToken;

        // Issue #1790: 组件化服务 - 搜索、未完成医案、待诊队列逻辑
        private readonly PatientSearchManager _searchManager;
        private readonly UnfinishedCaseHandler _unfinishedCaseHandler;
        private readonly PendingQueueManager _pendingQueueManager;

        // OpenSpec: cleanup-ui-layer Phase 1.2 - 医案启动协调器
        private readonly MedicalCaseStartCoordinator _medicalCaseStartCoordinator;

        #endregion

        #region 流程上下文属性

        private Guid _medicalCaseFlowId;
        /// <summary>
        /// 医案流程ID（从MedicalCaseFlowViewModel通过NavigationParameters传入）
        /// </summary>
        public Guid MedicalCaseFlowId
        {
            get => _medicalCaseFlowId;
            set => SetProperty(ref _medicalCaseFlowId, value);
        }

        #endregion

        #region 数据属性

        /// <summary>
        /// 患者列表（搜索结果或分页数据）
        /// Issue #1790: 委托给SearchManager
        /// </summary>
        public ObservableCollection<PatientDto> Patients => _searchManager.Patients;

        private PatientDto? _selectedPatient;
        /// <summary>
        /// 全部患者列表中选中的患者
        /// Epic #1583: 选中后自动更新CurrentPatient
        /// Epic #2210 Issue #2216: FR-001 双列表互斥选择 - 清除SelectedPendingPatient
        /// </summary>
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    SelectPatientCommand.RaiseCanExecuteChanged();

                    // Epic #2210 Issue #2216: FR-001 双列表互斥选择
                    if (value != null)
                    {
                        // 清除待诊队列选择（避免循环通知：使用_字段赋值）
                        if (_selectedPendingPatient != null)
                        {
                            _selectedPendingPatient = null;
                            RaisePropertyChanged(nameof(SelectedPendingPatient));
                            Logger.LogDebug("选择来源：全部患者列表，已清除待诊队列选择");
                        }

                        // Epic #1583: 全部患者列表选中 → 更新CurrentPatient
                        CurrentPatient = value;
                    }
                }
            }
        }

        private string _searchKeyword = string.Empty;
        /// <summary>
        /// 搜索关键字（支持姓名/拼音码/手机号）
        /// UX优化：实时搜索，300ms防抖
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set
            {
                if (SetProperty(ref _searchKeyword, value))
                {
                    SearchCommand.RaiseCanExecuteChanged();

                    // 实时搜索：300ms防抖
                    _searchDebounceTimer?.Dispose();
                    _searchDebounceTimer = new System.Threading.Timer(
                        _ => System.Windows.Application.Current.Dispatcher.Invoke(async () => await ExecuteSearchAsync()),
                        null,
                        300,
                        System.Threading.Timeout.Infinite
                    );
                }
            }
        }

        #endregion

        #region 分页属性

        private int _currentPage = 1;
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private int _totalPages = 1;
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        private int _totalCount = 0;
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        private const int PageSize = 50; // 每页50条记录（Epic #2210 Task 3.2.1）

        #endregion

        #region Epic #1583: 待看诊队列属性

        /// <summary>
        /// 待看诊队列（未完成医案的患者列表）
        /// Issue #1790: 委托给PendingQueueManager
        /// </summary>
        public ObservableCollection<PendingMedicalCaseDto> PendingQueue => _pendingQueueManager.PendingQueue;

        /// <summary>
        /// 是否无待诊患者（用于空状态UI显示）
        /// Epic #2210 Task 3.2.3: FR-007 空状态UI
        /// </summary>
        public bool HasNoPendingPatients => PendingQueue?.Count == 0;

        // Issue #1790: _pendingCaseCache已移至UnfinishedCaseHandler

        private PendingMedicalCaseDto? _selectedPendingPatient;
        /// <summary>
        /// 待看诊队列中选中的患者
        /// Issue #1790: 委托给PendingQueueManager加载患者详情
        /// </summary>
        public PendingMedicalCaseDto? SelectedPendingPatient
        {
            get => _selectedPendingPatient;
            set
            {
                if (SetProperty(ref _selectedPendingPatient, value))
                {
                    // 待看诊队列选中患者 → 更新CurrentPatient
                    if (value != null)
                    {
                        // Epic #2210 Issue #2216: FR-001 双列表互斥选择
                        // 清除全部患者列表选择（避免循环通知：使用_字段赋值）
                        if (_selectedPatient != null)
                        {
                            _selectedPatient = null;
                            RaisePropertyChanged(nameof(SelectedPatient));
                            SelectPatientCommand.RaiseCanExecuteChanged();
                            Logger.LogDebug("选择来源：待诊队列，已清除全部患者选择");
                        }

                        Logger.LogInformation("待看诊队列选中患者：{PatientName}，MedicalCaseId：{MedicalCaseId}",
                            value.PatientName, value.MedicalCaseId);

                        // Bug修复：将待诊队列的医案ID存入UnfinishedCaseHandler缓存
                        // 这样在点击"开始看诊"时就能直接从缓存获取，无需再次查询API
                        if (value.MedicalCaseId.HasValue && value.MedicalCaseId.Value != Guid.Empty)
                        {
                            Logger.LogInformation("预填充未完成医案缓存：PatientId={PatientId}, MedicalCaseId={MedicalCaseId}",
                                value.PatientId, value.MedicalCaseId.Value);
                            _unfinishedCaseHandler.SetCache(value.PatientId, value.MedicalCaseId.Value);
                        }

                        // Issue #1790: 异步加载患者详情并设置CurrentPatient（通过事件处理）
                        _ = _pendingQueueManager.LoadPatientForPendingCaseAsync(value.PatientId, Patients);
                    }
                }
            }
        }

        private PatientDto? _currentPatient;
        /// <summary>
        /// 当前选中的患者（核心概念 - 显示在患者信息详情区）
        /// Epic #1583: "当前选中患者"概念，统一来自两个列表的选择
        /// </summary>
        public PatientDto? CurrentPatient
        {
            get => _currentPatient;
            set
            {
                if (SetProperty(ref _currentPatient, value))
                {
                    StartConsultationCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #region Epic #2210 Issue #2217: StatusBar异常消息显示

        private string _statusBarMessage = string.Empty;
        /// <summary>
        /// StatusBar消息内容
        /// Epic #2210 Issue #2217: FR-002 异常处理优化
        /// </summary>
        public string StatusBarMessage
        {
            get => _statusBarMessage;
            set => SetProperty(ref _statusBarMessage, value);
        }

        private bool _statusBarIsError;
        /// <summary>
        /// StatusBar是否为错误消息（控制文本颜色）
        /// Epic #2210 Issue #2217: FR-002 异常处理优化
        /// </summary>
        public bool StatusBarIsError
        {
            get => _statusBarIsError;
            set => SetProperty(ref _statusBarIsError, value);
        }

        private bool _isRefreshing;
        /// <summary>
        /// 是否正在刷新待诊队列
        /// Epic #2210 Task 3.2.2: FR-006 手动刷新队列
        /// </summary>
        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        #endregion

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand NewPatientCommand { get; }
        public DelegateCommand SelectPatientCommand { get; }
        public DelegateCommand<PatientDto> DoubleClickPatientCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }

        // Epic #1583: 新增命令
        public DelegateCommand BackToHomeCommand { get; }
        public DelegateCommand StartConsultationCommand { get; }
        // Epic #2210 Task 3.2.2: FR-006 手动刷新队列
        public DelegateCommand RefreshPendingQueueCommand { get; }

        #endregion

        #region 构造函数

        public PatientSelectionViewModel(
            PatientCommandHandler commandHandler, // Issue #1788: 注入CommandHandler
            MedicalCaseDataManager medicalCaseDataManager, // Epic #1773: 注入MedicalCaseDataManager
            PatientSearchManager searchManager, // Issue #1790: 注入搜索管理器
            UnfinishedCaseHandler unfinishedCaseHandler, // Issue #1790: 注入未完成医案处理器
            PendingQueueManager pendingQueueManager, // Issue #1790: 注入待诊队列管理器
            MedicalCaseStartCoordinator medicalCaseStartCoordinator, // OpenSpec: cleanup-ui-layer Phase 1.2
            IDialogService dialogService,
            IMedicalCaseApi medicalCaseApi,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null,
            ICommonDialogService? commonDialogService = null) // Issue #2247: 统一对话框服务
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            // Issue #1788: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            // Epic #1773: 注入MedicalCaseDataManager
            _medicalCaseDataManager = medicalCaseDataManager ?? throw new ArgumentNullException(nameof(medicalCaseDataManager));
            // Issue #1790: 注入三个管理器
            _searchManager = searchManager ?? throw new ArgumentNullException(nameof(searchManager));
            _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));
            _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
            // OpenSpec: cleanup-ui-layer Phase 1.2
            _medicalCaseStartCoordinator = medicalCaseStartCoordinator ?? throw new ArgumentNullException(nameof(medicalCaseStartCoordinator));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanExecuteSearch);
            NewPatientCommand = new DelegateCommand(ExecuteNewPatient);
            SelectPatientCommand = new DelegateCommand(ExecuteSelectPatient, CanExecuteSelectPatient);
            DoubleClickPatientCommand = new DelegateCommand<PatientDto>(ExecuteDoubleClickPatient);
            PreviousPageCommand = new DelegateCommand(async () => await ExecutePreviousPageAsync(), CanExecutePreviousPage);
            NextPageCommand = new DelegateCommand(async () => await ExecuteNextPageAsync(), CanExecuteNextPage);

            // Epic #1583: 初始化新命令
            BackToHomeCommand = new DelegateCommand(ExecuteBackToHome);
            StartConsultationCommand = new DelegateCommand(ExecuteStartConsultation, CanExecuteStartConsultation);

            // Epic #2210 Task 3.2.2: FR-006 手动刷新队列
            RefreshPendingQueueCommand = new DelegateCommand(
                async () => await RefreshPendingQueueAsync(),
                () => !IsRefreshing)
                .ObservesProperty(() => IsRefreshing);

            // Issue #1790: 订阅管理器事件
            _searchManager.SearchCompleted += OnSearchCompleted;
            _unfinishedCaseHandler.CaseCheckCompleted += OnCaseCheckCompleted;
            _unfinishedCaseHandler.CaseClosed += OnCaseClosed;
            _pendingQueueManager.PendingQueueLoaded += OnPendingQueueLoaded;
            _pendingQueueManager.PatientLoaded += OnPatientLoaded;

            // Issue #2221: 订阅患者更新事件
            _patientUpdatedToken = EventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(OnPatientUpdated);

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 搜索患者
        /// Issue #1794: 优化方法长度（54→31行），提取错误处理和列表更新逻辑
        /// Issue #1790: 委托给SearchManager
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                SetIsBusy(true, "正在搜索患者...");

                // Issue #1790: 委托给SearchManager执行搜索
                var success = await _searchManager.ExecuteSearchAsync(SearchKeyword);

                if (!success)
                {
                    await ShowErrorMessageAsync("搜索失败");
                    return;
                }

                // 同步分页属性
                CurrentPage = _searchManager.CurrentPage;
                TotalPages = _searchManager.TotalPages;
                TotalCount = _searchManager.TotalCount;

                // 更新命令状态
                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索患者失败");
                await ShowErrorMessageAsync($"搜索失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 处理搜索失败
        /// Issue #1794: 从ExecuteSearchAsync提取
        /// </summary>
        private async Task HandleSearchErrorAsync(string? errorMessage)
        {
            Logger.LogError("搜索患者失败：{ErrorMessage}", errorMessage);
            await ShowErrorMessageAsync($"搜索失败：{errorMessage}");
        }

        /// <summary>
        /// 更新患者列表和分页信息
        /// Issue #1794: 从ExecuteSearchAsync提取
        /// </summary>
        private bool CanExecuteSearch()
        {
            // UX优化：实时搜索，包括空关键字（显示所有患者）
            return true;
        }

        /// <summary>
        /// 新建患者
        /// Issue #1543: 集成QuickCreatePatientDialog
        /// </summary>
        private void ExecuteNewPatient()
        {
            try
            {
                Logger.LogInformation("打开快速新建患者对话框");

                _dialogService.ShowDialog("QuickCreatePatientDialog", new DialogParameters(), result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        var newPatient = result.Parameters.GetValue<PatientDto>("NewPatient");
                        if (newPatient != null)
                        {
                            Logger.LogInformation("新建患者成功：{PatientName}（ID: {PatientId}）",
                                newPatient.Name, newPatient.Id);

                            // 1. 将新患者添加到列表顶部
                            Patients.Insert(0, newPatient);

                            // 2. 自动选中新患者
                            SelectedPatient = newPatient;

                            // 3. 发布患者选择事件（使用EventAggregator）
                            PublishPatientSelectedEvent(newPatient);
                        }
                        else
                        {
                            Logger.LogWarning("对话框返回的患者数据为空");
                        }
                    }
                    else
                    {
                        Logger.LogInformation("用户取消了快速新建患者");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开新建患者对话框失败");
            }
        }

        /// <summary>
        /// 选择患者（点击【选择】按钮）
        /// </summary>
        private void ExecuteSelectPatient()
        {
            if (SelectedPatient == null)
            {
                Logger.LogWarning("未选择患者");
                return;
            }

            try
            {
                Logger.LogInformation("选择患者：{PatientName}（ID: {PatientId}）", SelectedPatient.Name, SelectedPatient.Id);

                // 发布患者选择事件（使用EventAggregator）
                PublishPatientSelectedEvent(SelectedPatient);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "选择患者失败");
            }
        }

        private bool CanExecuteSelectPatient()
        {
            return SelectedPatient != null;
        }

        /// <summary>
        /// 双击患者行（快捷选择）
        /// </summary>
        private void ExecuteDoubleClickPatient(PatientDto? patient)
        {
            if (patient == null)
            {
                Logger.LogWarning("双击的患者为空");
                return;
            }

            try
            {
                Logger.LogInformation("双击选择患者：{PatientName}（ID: {PatientId}）", patient.Name, patient.Id);

                SelectedPatient = patient;

                // 发布患者选择事件（使用EventAggregator）
                PublishPatientSelectedEvent(patient);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "双击选择患者失败");
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        /// <summary>
        /// Issue #1790: 委托给SearchManager处理
        /// </summary>
        private async Task ExecutePreviousPageAsync()
        {
            if (!CanExecutePreviousPage())
                return;

            try
            {
                SetIsBusy(true, "正在加载上一页...");

                // Issue #1790: 委托给SearchManager处理分页
                await _searchManager.PreviousPageAsync(SearchKeyword);

                // 同步分页属性
                CurrentPage = _searchManager.CurrentPage;
                TotalPages = _searchManager.TotalPages;
                TotalCount = _searchManager.TotalCount;

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载上一页失败");
                await ShowErrorMessageAsync($"加载失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecutePreviousPage()
        {
            return _searchManager.CanPreviousPage();
        }

        /// <summary>
        /// 下一页
        /// Issue #1790: 委托给SearchManager处理
        /// </summary>
        private async Task ExecuteNextPageAsync()
        {
            if (!CanExecuteNextPage())
                return;

            try
            {
                SetIsBusy(true, "正在加载下一页...");

                // Issue #1790: 委托给SearchManager处理分页
                await _searchManager.NextPageAsync(SearchKeyword);

                // 同步分页属性
                CurrentPage = _searchManager.CurrentPage;
                TotalPages = _searchManager.TotalPages;
                TotalCount = _searchManager.TotalCount;

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载下一页失败");
                await ShowErrorMessageAsync($"加载失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteNextPage()
        {
            return _searchManager.CanNextPage();
        }

        /// <summary>
        /// 返回主页
        /// Epic #1583
        /// </summary>
        private void ExecuteBackToHome()
        {
            try
            {
                Logger.LogInformation("返回主页");

                // 根据用户角色导航到对应的主页
                if (SessionManager?.CurrentUser?.Role == UserRole.Admin)
                {
                    RegionManager.RequestNavigate("ContentRegion", "AdminHomeView");
                }
                else
                {
                    // Issue #1584 - 修复导航错误：HomeView不存在，应为ClinicalHomeView
                    RegionManager.RequestNavigate("ContentRegion", "ClinicalHomeView");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页失败");
            }
        }

        /// <summary>
        /// 开始看诊（替代原有的SelectPatientCommand）
        /// Epic #1583: 统一的开始看诊入口，基于CurrentPatient
        /// OpenSpec: cleanup-ui-layer Phase 1.2 - 委托给MedicalCaseStartCoordinator
        /// </summary>
        private async void ExecuteStartConsultation()
        {
            if (CurrentPatient == null)
            {
                Logger.LogWarning("当前未选择患者");
                return;
            }

            try
            {
                SetIsBusy(true, "正在检查患者医案...");

                Logger.LogInformation("开始看诊，患者：{PatientName}（ID: {PatientId}）",
                    CurrentPatient.Name, CurrentPatient.Id);

                // OpenSpec: cleanup-ui-layer Phase 1.2 - 委托给Coordinator检查未完成医案
                var unfinishedCase = await _medicalCaseStartCoordinator.CheckUnfinishedCaseAsync(CurrentPatient.Id);

                if (unfinishedCase != null)
                {
                    // 检查是否是其他医生的挂起医案
                    if (_medicalCaseStartCoordinator.IsOtherDoctorCase(unfinishedCase))
                    {
                        var doctorName = _medicalCaseStartCoordinator.GetOtherDoctorName(unfinishedCase);
                        Logger.LogInformation("检测到患者在其他医生处有挂起医案: DoctorId={DoctorId}, DoctorName={DoctorName}",
                            unfinishedCase.DoctorId, doctorName);

                        SetIsBusy(false);
                        await ShowOtherDoctorCaseMessageAsync(CurrentPatient.Name, doctorName);
                        return;
                    }

                    // 当前医生的挂起医案，显示操作对话框
                    await HandleUnfinishedCaseAsync(unfinishedCase.Id);
                }
                else
                {
                    HandleNoUnfinishedCase();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始看诊失败");
                await ShowErrorMessageAsync($"开始看诊失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 处理有未完成医案的情况
        /// Issue #1794: 从ExecuteStartConsultation提取
        /// OpenSpec: cleanup-ui-layer Phase 1.2 - 委托给MedicalCaseStartCoordinator
        /// </summary>
        private async Task HandleUnfinishedCaseAsync(Guid unfinishedCaseId)
        {
            SetIsBusy(false); // 对话框前关闭繁忙状态

            var choice = await ShowUnfinishedCaseDialogAsync(CurrentPatient!.Name, unfinishedCaseId);

            if (choice == 0)
            {
                Logger.LogInformation("用户取消操作");
                return;
            }

            // 设置忙状态
            if (choice == 2 || choice == 3)
            {
                SetIsBusy(true, choice == 2 ? "正在关闭旧医案..." : "正在关闭医案...");
            }

            // OpenSpec: cleanup-ui-layer Phase 1.2 - 委托给Coordinator处理用户选择
            var result = await _medicalCaseStartCoordinator.HandleUserChoiceAsync(
                choice,
                CurrentPatient,
                unfinishedCaseId,
                LoadPendingCasesAsync); // 传递刷新队列回调

            // 处理结果
            await HandleStartResultAsync(result);
        }

        /// <summary>
        /// 处理无未完成医案的情况
        /// Issue #1794: 从ExecuteStartConsultation提取
        /// </summary>
        private void HandleNoUnfinishedCase()
        {
            Logger.LogInformation("患者无未完成医案，直接开始看诊");
            PublishPatientSelectedEvent(CurrentPatient!);
        }

        /// <summary>
        /// 处理医案启动结果
        /// OpenSpec: cleanup-ui-layer Phase 1.2 - 统一处理Coordinator返回的结果
        /// </summary>
        private async Task HandleStartResultAsync(MedicalCaseStartCoordinator.StartResultData result)
        {
            switch (result.Result)
            {
                case MedicalCaseStartCoordinator.StartResult.ContinueExisting:
                    // 继续现有医案 - 发布事件并导航
                    PublishPatientSelectedEvent(CurrentPatient!, result.ExistingMedicalCaseId);
                    break;

                case MedicalCaseStartCoordinator.StartResult.CreateNew:
                    // 创建新医案 - 发布事件（不带医案ID，MedicalCaseFlowViewModel将创建新医案）
                    PublishPatientSelectedEvent(CurrentPatient!);
                    break;

                case MedicalCaseStartCoordinator.StartResult.CloseOnly:
                    // 仅关闭 - 队列已在Coordinator中刷新，无需额外操作
                    Logger.LogInformation("旧医案已关闭，待诊队列已刷新");
                    break;

                case MedicalCaseStartCoordinator.StartResult.Error:
                    // 错误 - 显示错误消息
                    await ShowErrorMessageAsync(result.ErrorMessage ?? "操作失败");
                    break;

                case MedicalCaseStartCoordinator.StartResult.Cancelled:
                case MedicalCaseStartCoordinator.StartResult.BlockedByOtherDoctor:
                default:
                    // 取消或阻塞 - 无需操作
                    break;
            }
        }

        private bool CanExecuteStartConsultation()
        {
            return CurrentPatient != null && !IsBusy;
        }

        #endregion

        #region Phase 2: 智能路由方法

        /// <summary>
        /// Phase 2: 显示未完成医案对话框（三选一）
        /// </summary>
        /// <param name="patientName">患者姓名</param>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>用户选择：1=继续看诊, 2=新建医案, 3=关闭医案, 0=取消</returns>
        private Task<int> ShowUnfinishedCaseDialogAsync(string patientName, Guid medicalCaseId)
        {
            var tcs = new TaskCompletionSource<int>();

            try
            {
                // 使用自定义对话框（支持4个选项）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new Views.UnfinishedCaseDialog();
                    dialog.SetPatientName(patientName);

                    // 设置Owner为主窗口（如果能找到）
                    var mainWindow = System.Windows.Application.Current.MainWindow;
                    if (mainWindow != null && mainWindow != dialog)
                    {
                        dialog.Owner = mainWindow;
                    }

                    dialog.ShowDialog();

                    // 获取用户选择结果
                    int choice = dialog.Result;
                    Logger.LogInformation("用户选择：{Choice} (1=继续, 2=新建, 3=仅关闭, 0=取消)", choice);

                    tcs.SetResult(choice);
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "显示对话框失败");
                tcs.SetResult(0); // 异常时返回取消
            }

            return tcs.Task;
        }

        /// <summary>
        /// 显示其他医生挂起医案提示
        /// OpenSpec: multi-doctor-unfinished-case
        /// </summary>
        /// <param name="patientName">患者姓名</param>
        /// <param name="doctorName">其他医生姓名</param>
        /// <remarks>Issue #2247: 使用ICommonDialogService替代直接MessageBox.Show调用</remarks>
        private async Task ShowOtherDoctorCaseMessageAsync(string patientName, string doctorName)
        {
            try
            {
                var message = $"患者「{patientName}」在{doctorName}处有挂起医案，暂时无法为其开始新的诊断。\n\n请联系{doctorName}完成或关闭该医案后再试。";
                await ShowSuccessMessageAsync(message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "显示其他医生挂起医案提示失败");
            }
        }

        // OpenSpec: cleanup-ui-layer Phase 1.2
        // 以下方法已委托给MedicalCaseStartCoordinator处理：
        // - ContinueConsultationAsync → _medicalCaseStartCoordinator.ContinueExistingCaseAsync
        // - CreateNewCaseAfterClosingOldAsync → _medicalCaseStartCoordinator.CloseAndCreateNewAsync
        // - CloseOldCaseOnlyAsync → _medicalCaseStartCoordinator.CloseOnlyAsync

        #endregion

        #region 事件发布辅助方法

        /// <summary>
        /// 发布患者选择事件
        /// Issue #1557 - 使用EventAggregator替代.NET Event，实现模块间解耦
        /// Issue #1597 - 支持可选的MedicalCaseId（继续看诊场景）
        /// </summary>
        /// <param name="patient">选中的患者</param>
        /// <param name="medicalCaseId">可选的医案ID（继续看诊时传递现有ID，新建时为null）</param>
        
        /// <summary>
        /// 创建患者选中事件载荷
        /// </summary>
        private PatientSelectedPayload CreatePatientPayload(PatientDto patient)
        {
            return new PatientSelectedPayload
            {
                PatientId = patient.Id,
                PatientName = patient.Name,
                Gender = patient.Gender.ToString(),
                Age = patient.Age ?? 0,
                PhoneNumber = patient.PhoneNumber ?? string.Empty,
                LastVisitDate = patient.LastVisitTime,
                VisitCount = patient.VisitCount,
                AllergyHistory = patient.AllergyHistory ?? string.Empty,
                MedicalCaseFlowId = this.MedicalCaseFlowId,
                SelectedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 执行医案流程导航
        /// </summary>
        private void NavigateToMedicalCaseFlow(PatientDto patient, Guid? medicalCaseId)
        {
            // OpenSpec: refine-medicalcase-edit-modes - 使用MedicalCaseNavigationParameters
            var parameters = MedicalCaseNavigationParameters.ForClinical(patient.Id, medicalCaseId);
            // 兼容性: 保留CurrentPatient参数供其他逻辑使用
            parameters.Add("CurrentPatient", patient);

            if (medicalCaseId.HasValue && medicalCaseId.Value != Guid.Empty)
            {
                Logger.LogInformation("导航到医案录入界面（继续看诊）：PatientId={PatientId}, MedicalCaseId={MedicalCaseId}",
                    patient.Id, medicalCaseId.Value);
            }
            else
            {
                Logger.LogInformation("导航到医案录入界面（新建医案）：PatientId={PatientId}, PatientName={PatientName}",
                    patient.Id, patient.Name);
            }

            // Epic #2210 Phase 4: 导航到新的4:6统一工作区视图
            RegionManager.RequestNavigate("ContentRegion", "MedicalCaseWorkspaceView", parameters);
        }

        private void PublishPatientSelectedEvent(PatientDto patient, Guid? medicalCaseId = null)
        {
            try
            {
                var payload = CreatePatientPayload(patient);
                EventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);
                Logger.LogInformation("已发布PatientSelectedEvent，患者：{PatientName}，流程ID：{FlowId}",
                    patient.Name, MedicalCaseFlowId);
                
                NavigateToMedicalCaseFlow(patient, medicalCaseId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "发布PatientSelectedEvent或导航失败");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 加载当前页数据
        /// </summary>
        /// <summary>
        /// Issue #1790: 委托给SearchManager处理
        /// </summary>
        private async Task LoadCurrentPageAsync()
        {
            await _searchManager.LoadCurrentPageAsync(SearchKeyword);

            // 同步分页属性
            CurrentPage = _searchManager.CurrentPage;
            TotalPages = _searchManager.TotalPages;
            TotalCount = _searchManager.TotalCount;
        }

        /// <summary>
        /// 加载初始患者列表（第1页）
        /// Issue #1790: 委托给SearchManager处理
        /// </summary>
        private async Task LoadInitialPatientsAsync()
        {
            // Epic #2210 Task 3.2.1: 添加性能监控
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                // Issue #1790: 委托给SearchManager加载初始数据
                await _searchManager.LoadInitialPatientsAsync();

                // 同步分页属性
                CurrentPage = _searchManager.CurrentPage;
                TotalPages = _searchManager.TotalPages;
                TotalCount = _searchManager.TotalCount;

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                
                // Epic #2210 Task 3.2.1: 性能监控
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                Logger.LogInformation("患者列表加载完成: 数量={Count}, 耗时={ElapsedMs}ms", 
                    TotalCount, elapsedMs);
                
                // Epic #2210 Task 3.2.1: 性能警告阈值 500ms
                if (elapsedMs > 500)
                {
                    Logger.LogWarning("患者列表加载耗时过长: {ElapsedMs}ms > 500ms阈值", elapsedMs);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "加载患者列表失败，耗时={ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                await ShowErrorMessageAsync($"加载患者列表失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 加载待看诊队列（Epic #1583 - Phase 5）
        /// Issue #1790: 委托给PendingQueueManager处理
        /// </summary>
        private async Task LoadPendingCasesAsync()
        {
            // Issue #1790: 完全委托给PendingQueueManager
            await _pendingQueueManager.LoadPendingCasesAsync();
        }

        /// <summary>
        /// 手动刷新待诊队列
        /// Epic #2210 Task 3.2.2: FR-006 手动刷新队列
        /// </summary>
        private async Task RefreshPendingQueueAsync()
        {
            try
            {
                IsRefreshing = true;
                Logger.LogInformation("用户手动刷新待诊队列");

                // 调用LoadPendingCasesAsync刷新队列
                await LoadPendingCasesAsync();

                await ShowSuccessMessageAsync("待诊队列已刷新");
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "刷新待诊队列失败: 网络错误");
                await ShowErrorMessageAsync($"刷新待诊队列失败：网络错误 - {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "刷新待诊队列失败");
                await ShowErrorMessageAsync($"刷新待诊队列失败：{ex.Message}");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 显示StatusBar错误消息，3秒后自动清除
        /// Epic #2210 Issue #2217: FR-002 异常处理优化
        /// Override基类方法，改用StatusBar而非MessageBox
        /// </summary>
        /// <param name="message">错误消息</param>
        protected override async Task ShowErrorMessageAsync(string message)
        {
            StatusBarMessage = message;
            StatusBarIsError = true;

            // 3秒后自动清除消息（避免覆盖新消息）
            await Task.Delay(3000);
            if (StatusBarMessage == message)
            {
                StatusBarMessage = string.Empty;
                StatusBarIsError = false;
            }
        }

        /// <summary>
        /// 显示StatusBar成功消息，3秒后自动清除
        /// Epic #2210 Issue #2222: FR-004 操作成功反馈
        /// Override基类方法，改用StatusBar而非MessageBox
        /// </summary>
        /// <param name="message">成功消息</param>
        protected override async Task ShowSuccessMessageAsync(string message)
        {
            StatusBarMessage = message;
            StatusBarIsError = false;

            // 3秒后自动清除消息（避免覆盖新消息）
            await Task.Delay(3000);
            if (StatusBarMessage == message)
            {
                StatusBarMessage = string.Empty;
            }
        }

        /// <summary>
        /// 创建新医案并导航到医案详情
        /// Epic #2210 Issue #2222: FR-004 操作成功反馈
        /// </summary>
        private async Task CreateNewMedicalCaseAndNavigateAsync()
        {
            if (CurrentPatient == null)
            {
                Logger.LogWarning("当前未选择患者，无法创建医案");
                return;
            }

            try
            {
                Logger.LogInformation("开始创建新医案: PatientId={PatientId}, PatientName={PatientName}",
                    CurrentPatient.Id, CurrentPatient.Name);

                // 创建医案输入DTO
                var dto = new MedicalCaseInputDto
                {
                    PatientId = CurrentPatient.Id,
                    DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                    VisitDate = DateTime.Now
                };

                // 调用DataManager创建医案
                var medicalCase = await _medicalCaseDataManager.CreateAsync(dto);

                if (medicalCase == null)
                {
                    await ShowErrorMessageAsync($"创建医案失败");
                    return;
                }

                // Epic #2210 Issue #2222: FR-004 显示成功反馈
                await ShowSuccessMessageAsync($"已为 {CurrentPatient.Name} 创建新医案");

                Logger.LogInformation("医案创建成功: PatientId={PatientId}, MedicalCaseId={MedicalCaseId}",
                    CurrentPatient.Id, medicalCase.Id);

                // OpenSpec: refine-medicalcase-edit-modes - 使用新的导航参数
                var parameters = MedicalCaseNavigationParameters.ForClinical(CurrentPatient.Id, medicalCase.Id);
                parameters.Add("MedicalCaseFlowId", MedicalCaseFlowId);
                RegionManager.RequestNavigate("ContentRegion", "MedicalCaseWorkspaceView", parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建医案失败: PatientId={PatientId}", CurrentPatient.Id);
                await ShowErrorMessageAsync($"创建医案失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 为待看诊队列选中的患者加载完整信息
        /// Issue #1790: 委托给PendingQueueManager处理（通过事件设置CurrentPatient）
        /// </summary>
        private async Task LoadPatientForPendingCaseAsync(Guid patientId)
        {
            // Issue #1790: 委托给PendingQueueManager，它会触发PatientLoaded事件
            // 事件处理程序OnPatientLoaded会设置CurrentPatient
            await _pendingQueueManager.LoadPatientForPendingCaseAsync(patientId, Patients);
        }

        #endregion

        #region INavigationAware

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            try
            {
                // Issue #1557 - 接收MedicalCaseFlowViewModel传来的流程ID
                var flowId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseFlowId");
                if (flowId != Guid.Empty)
                {
                    Logger.LogInformation("接收到医案流程ID：{FlowId}", flowId);
                    MedicalCaseFlowId = flowId;
                }
                else
                {
                    Logger.LogWarning("未接收到有效的医案流程ID，将生成新的流程ID");
                    MedicalCaseFlowId = Guid.NewGuid();
                }

                // 接收HomeView传来的搜索关键字（保留原有功能）
                var searchKeyword = navigationContext.Parameters.GetValue<string>("SearchKeyword");
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    Logger.LogInformation("接收到搜索关键字：{SearchKeyword}", searchKeyword);
                    SearchKeyword = searchKeyword;
                    // 自动触发搜索
                    _ = ExecuteSearchAsync();
                }
                else
                {
                    // 无搜索关键字，加载第1页数据
                    _ = LoadInitialPatientsAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到患者选择视图时发生异常");
            }

            // Epic #2210 Issue #2217: FR-002 异常处理优化
            // 加载待看诊队列（异步独立处理，异常不阻断全部患者列表）
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadPendingCasesAsync();
                }
                catch (HttpRequestException ex)
                {
                    Logger.LogError(ex, "加载待看诊队列失败：网络请求异常");
                    await ShowErrorMessageAsync("加载待看诊队列失败：网络连接异常，请检查网络连接");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "加载待看诊队列失败：未知异常");
                    await ShowErrorMessageAsync("加载待看诊队列失败，请稍后重试");
                }
            });
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 允许重复导航（每次进入Step 1都重新加载数据）
            return false;
        }

        public override void OnNavigatedFrom(NavigationContext navigationContext)
        {
            base.OnNavigatedFrom(navigationContext);
            Logger.LogInformation("离开患者选择视图，当前选择：{PatientName}，流程ID：{FlowId}",
                SelectedPatient?.Name ?? "未选择", MedicalCaseFlowId);
        }

        #endregion

        #region Issue #1790: 管理器事件处理器

        private void OnSearchCompleted(object? sender, SearchCompletedEventArgs e)
        {
            Logger.LogInformation("搜索完成事件：关键字={Keyword}，结果数={Count}，当前页={Page}",
                e.Keyword, e.ResultCount, e.CurrentPage);
        }

        private void OnCaseCheckCompleted(object? sender, CaseCheckCompletedEventArgs e)
        {
            Logger.LogInformation("医案检查完成事件：PatientId={PatientId}，UnfinishedCase={HasCase}",
                e.PatientId, e.UnfinishedCase != null);
        }

        private void OnCaseClosed(object? sender, CaseClosedEventArgs e)
        {
            Logger.LogInformation("医案关闭事件：PatientId={PatientId}，MedicalCaseId={CaseId}，CreateNew={CreateNew}",
                e.PatientId, e.MedicalCaseId, e.CreateNew);
        }

        private void OnPendingQueueLoaded(object? sender, PendingQueueLoadedEventArgs e)
        {
            Logger.LogInformation("待诊队列加载完成事件：共{Count}条记录", e.QueueCount);
            // Epic #2210 Task 3.2.3: FR-007 空状态UI - 通知HasNoPendingPatients属性变化
            RaisePropertyChanged(nameof(HasNoPendingPatients));
        }

        private void OnPatientLoaded(object? sender, PatientLoadedEventArgs e)
        {
            // 设置CurrentPatient
            CurrentPatient = e.Patient;
            Logger.LogInformation("患者加载完成事件：{PatientName}，来源={Source}",
                e.Patient.Name, e.Source);
        }

        /// <summary>
        /// Issue #2221: 患者更新事件处理 - 刷新当前患者数据
        /// </summary>
        private async void OnPatientUpdated(PatientDto patient)
        {
            Logger.LogInformation("收到患者更新事件：{PatientId} - {PatientName}", patient.Id, patient.Name);

            // 如果当前选中的患者被更新，刷新其数据
            if (CurrentPatient?.Id == patient.Id)
            {
                CurrentPatient = patient;
                Logger.LogDebug("已更新CurrentPatient数据");
            }

            // 如果更新的患者在全部患者列表中，刷新列表
            if (Patients.Any(p => p.Id == patient.Id))
            {
                var index = Patients.ToList().FindIndex(p => p.Id == patient.Id);
                if (index >= 0)
                {
                    Patients[index] = patient;
                    Logger.LogDebug("已更新Patients列表中的患者数据，索引={Index}", index);
                }
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Issue #2221: IDisposable实现

        /// <summary>
        /// 释放资源 - 隐藏基类Dispose方法（基类Dispose不是virtual）
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            base.Dispose(); // 调用基类Dispose
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放托管和非托管资源 - 重写基类方法
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 释放托管资源

                // 1. 取消管理器事件订阅
                _searchManager.SearchCompleted -= OnSearchCompleted;
                _unfinishedCaseHandler.CaseCheckCompleted -= OnCaseCheckCompleted;
                _unfinishedCaseHandler.CaseClosed -= OnCaseClosed;
                _pendingQueueManager.PendingQueueLoaded -= OnPendingQueueLoaded;
                _pendingQueueManager.PatientLoaded -= OnPatientLoaded;

                // 2. 取消PatientUpdatedEvent订阅
                if (_patientUpdatedToken != null)
                {
                    EventAggregator.GetEvent<PatientUpdatedEvent>().Unsubscribe(_patientUpdatedToken);
                    _patientUpdatedToken = null;
                }

                // 3. 释放Timer
                _searchDebounceTimer?.Dispose();
                _searchDebounceTimer = null;

                Logger.LogInformation("PatientSelectionViewModel disposed");
            }

            _disposed = true;
        }

        #endregion
    }
}

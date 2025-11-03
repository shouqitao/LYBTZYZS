using System.Collections.ObjectModel;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Epic #1773: 使用DataManager替代Repository
// Epic #1773: 已移除LYBT.Desktop.MedicalCase.Interfaces（不再直接使用IMedicalCaseRepository）
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Components; // Issue #1788: 添加Component命名空间
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
        /// </summary>
        public ObservableCollection<PatientDto> Patients { get; } = new();

        private PatientDto? _selectedPatient;
        /// <summary>
        /// 全部患者列表中选中的患者
        /// Epic #1583: 选中后自动更新CurrentPatient
        /// </summary>
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    SelectPatientCommand.RaiseCanExecuteChanged();

                    // Epic #1583: 全部患者列表选中 → 更新CurrentPatient
                    if (value != null)
                    {
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

        private const int PageSize = 20; // 每页20条记录（Epic #1583）

        #endregion

        #region Epic #1583: 待看诊队列属性

        /// <summary>
        /// 待看诊队列（未完成医案的患者列表）
        /// </summary>
        public ObservableCollection<PendingMedicalCaseDto> PendingQueue { get; } = new();

        /// <summary>
        /// Phase 2: 本地缓存（PatientId -> MedicalCaseId）
        /// 用于快速查询患者是否有未完成医案
        /// </summary>
        private readonly Dictionary<Guid, Guid> _pendingCaseCache = new();

        private PendingMedicalCaseDto? _selectedPendingPatient;
        /// <summary>
        /// 待看诊队列中选中的患者
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
                        Logger.LogInformation("待看诊队列选中患者：{PatientName}", value.PatientName);

                        // 异步加载患者详悠信息并设置CurrentPatient
                        _ = LoadPatientForPendingCaseAsync(value.PatientId);
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

        #endregion

        #region 构造函数

        public PatientSelectionViewModel(
            PatientCommandHandler commandHandler, // Issue #1788: 注入CommandHandler
            MedicalCaseDataManager medicalCaseDataManager, // Epic #1773: 注入MedicalCaseDataManager
            IDialogService dialogService,
            IMedicalCaseApi medicalCaseApi,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1788: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            // Epic #1773: 注入MedicalCaseDataManager
            _medicalCaseDataManager = medicalCaseDataManager ?? throw new ArgumentNullException(nameof(medicalCaseDataManager));
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

            Logger.LogInformation("PatientSelectionViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 搜索患者
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            try
            {
                SetIsBusy(true, "正在搜索患者...");

                Logger.LogInformation("搜索患者，关键字：{Keyword}", SearchKeyword);

                // 重置到第1页
                CurrentPage = 1;

                // Issue #1788: 使用CommandHandler分页查询
                var commandResult = await _commandHandler.GetPatientsPagedAsync(CurrentPage, PageSize, SearchKeyword);

                if (!commandResult.IsSuccess || commandResult.Data == null)
                {
                    Logger.LogError("搜索患者失败：{ErrorMessage}", commandResult.ErrorMessage);
                    await ShowErrorMessageAsync($"搜索失败：{commandResult.ErrorMessage}");
                    return;
                }

                var result = commandResult.Data;

                // 清空选中状态（Bug修复：搜索后应重置选中项）
                SelectedPatient = null;
                CurrentPatient = null;

                // 更新患者列表
                Patients.Clear();
                foreach (var patient in result.Items)
                {
                    Patients.Add(patient);
                }

                // 更新分页信息
                TotalPages = result.TotalPages;
                TotalCount = result.TotalCount;

                Logger.LogInformation("搜索成功，找到{TotalCount}条记录，实际加载{ItemCount}条，当前显示第{CurrentPage}页", TotalCount, result.Items.Count, CurrentPage);

                // 触发分页命令更新
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
        private async Task ExecutePreviousPageAsync()
        {
            if (!CanExecutePreviousPage())
                return;

            try
            {
                SetIsBusy(true, "正在加载上一页...");

                CurrentPage--;
                await LoadCurrentPageAsync();

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
            return CurrentPage > 1;
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private async Task ExecuteNextPageAsync()
        {
            if (!CanExecuteNextPage())
                return;

            try
            {
                SetIsBusy(true, "正在加载下一页...");

                CurrentPage++;
                await LoadCurrentPageAsync();

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
            return CurrentPage < TotalPages;
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
        /// 开始诊断（替代原有的SelectPatientCommand）
        /// Epic #1583: 统一的开始诊断入口，基于CurrentPatient
        /// </summary>
        /// <summary>
        /// Phase 2: 智能路由（检查未完成医案 + 三选一对话框）
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

                Logger.LogInformation("开始诊断，患者：{PatientName}（ID: {PatientId}）",
                    CurrentPatient.Name, CurrentPatient.Id);

                // Phase 2: 智能路由逻辑
                // 1. 检查是否有未完成医案
                var unfinishedCase = await CheckUnfinishedMedicalCaseAsync(CurrentPatient.Id);

                if (unfinishedCase != null)
                {
                    // 2. 有未完成医案，弹出三选一对话框
                    SetIsBusy(false); // 对话框前关闭繁忙状态

                    var choice = await ShowUnfinishedCaseDialogAsync(CurrentPatient.Name, unfinishedCase.Id);

                    switch (choice)
                    {
                        case 1: // 继续看诊
                            await ContinueConsultationAsync(CurrentPatient, unfinishedCase.Id);
                            break;

                        case 2: // 新建医案（先关闭旧的）
                            SetIsBusy(true, "正在关闭旧医案...");
                            await CreateNewCaseAfterClosingOldAsync(CurrentPatient, unfinishedCase.Id);
                            break;

                        case 3: // 仅关闭旧医案（不创建新医案）
                            SetIsBusy(true, "正在关闭医案...");
                            await CloseOldCaseOnlyAsync(CurrentPatient, unfinishedCase.Id);
                            break;

                        case 0: // 取消/关闭窗口
                        default:
                            Logger.LogInformation("用户取消操作");
                            break;
                    }
                }
                else
                {
                    // 3. 无未完成医案，直接发布患者选择事件
                    Logger.LogInformation("患者无未完成医案，直接开始诊断");
                    PublishPatientSelectedEvent(CurrentPatient);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "开始诊断失败");
                await ShowErrorMessageAsync($"开始诊断失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        private bool CanExecuteStartConsultation()
        {
            return CurrentPatient != null && !IsBusy;
        }

        #endregion

        #region Phase 2: 智能路由方法

        /// <summary>
        /// Phase 2: 检查患者是否有未完成医案（缓存优先策略）
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>未完成的医案，如果没有则返回null</returns>
        private async Task<MedicalCaseDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId)
        {
            try
            {
                // 1. 先查本地缓存
                if (_pendingCaseCache.TryGetValue(patientId, out var cachedMedicalCaseId))
                {
                    Logger.LogInformation("缓存命中：PatientId={PatientId}, MedicalCaseId={MedicalCaseId}",
                        patientId, cachedMedicalCaseId);

                    // 缓存命中，返回一个包含ID的MedicalCaseDto
                    return new MedicalCaseDto { Id = cachedMedicalCaseId };
                }

                // 2. 缓存未命中，调用API查询
                Logger.LogInformation("缓存未命中，调用API查询：PatientId={PatientId}", patientId);

                // Epic #1773: 使用MedicalCaseDataManager包装方法
                var unfinishedCase = await _medicalCaseDataManager.GetUnfinishedCaseByPatientIdAsync(patientId);

                if (unfinishedCase != null)
                {
                    // 3. 找到未完成医案，更新缓存
                    _pendingCaseCache[patientId] = unfinishedCase.Id;
                    Logger.LogInformation("找到未完成医案，已更新缓存：MedicalCaseId={MedicalCaseId}",
                        unfinishedCase.Id);
                }
                else
                {
                    Logger.LogInformation("患者无未完成医案");
                }

                return unfinishedCase;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查未完成医案失败：PatientId={PatientId}", patientId);
                return null;
            }
        }

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
        /// Phase 2: 继续看诊（直接发布患者选择事件）
        /// Issue #1597: 传递现有MedicalCaseId
        /// </summary>
        private async Task ContinueConsultationAsync(PatientDto patient, Guid medicalCaseId)
        {
            try
            {
                Logger.LogInformation("用户选择继续看诊，患者：{PatientName}，MedicalCaseId: {MedicalCaseId}",
                    patient.Name, medicalCaseId);

                // 直接发布患者选择事件，传递现有MedicalCaseId
                PublishPatientSelectedEvent(patient, medicalCaseId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "继续看诊失败");
            }
        }

        /// <summary>
        /// Phase 2: 新建医案（关闭旧医案后发布事件）
        /// </summary>
        private async Task CreateNewCaseAfterClosingOldAsync(PatientDto patient, Guid oldMedicalCaseId)
        {
            try
            {
                Logger.LogInformation("用户选择新建医案，先关闭旧医案：OldMedicalCaseId={OldMedicalCaseId}",
                    oldMedicalCaseId);

                // 1. 关闭旧医案
                // Epic #1773: 使用MedicalCaseDataManager包装方法
                var response = await _medicalCaseDataManager.CloseCaseAsync(oldMedicalCaseId);
                var closed = response.Success;

                if (closed)
                {
                    // 2. 从缓存中移除
                    _pendingCaseCache.Remove(patient.Id);
                    Logger.LogInformation("旧医案已关闭，缓存已清理");

                    // 3. 发布患者选择事件（MedicalCaseFlowViewModel将创建新医案）
                    PublishPatientSelectedEvent(patient);
                }
                else
                {
                    Logger.LogWarning("关闭旧医案失败，取消操作");
                    await ShowErrorMessageAsync("关闭旧医案失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "新建医案失败");
                await ShowErrorMessageAsync($"新建医案失败：{ex.Message}");
            }
        }

        /// <summary>
        /// Phase 2: 仅关闭旧医案（不创建新医案）
        /// 修复：添加"仅关闭"选项支持
        /// </summary>
        private async Task CloseOldCaseOnlyAsync(PatientDto patient, Guid oldMedicalCaseId)
        {
            try
            {
                Logger.LogInformation("用户选择仅关闭医案：OldMedicalCaseId={OldMedicalCaseId}",
                    oldMedicalCaseId);

                // 1. 关闭旧医案
                // Epic #1773: 使用MedicalCaseDataManager包装方法
                var response = await _medicalCaseDataManager.CloseCaseAsync(oldMedicalCaseId);
                var closed = response.Success;

                if (closed)
                {
                    // 2. 从缓存中移除
                    _pendingCaseCache.Remove(patient.Id);
                    Logger.LogInformation("旧医案已关闭，缓存已清理");

                    // 3. 刷新待看诊列表（移除已关闭的医案）
                    await LoadPendingCasesAsync();

                    Logger.LogInformation("待看诊列表已刷新");
                }
                else
                {
                    Logger.LogWarning("关闭医案失败");
                    await ShowErrorMessageAsync("关闭医案失败，请稍后重试");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "关闭医案失败");
                await ShowErrorMessageAsync($"关闭医案失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

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
            var parameters = new NavigationParameters { { "CurrentPatient", patient } };

            if (medicalCaseId.HasValue && medicalCaseId.Value != Guid.Empty)
            {
                parameters.Add("MedicalCaseId", medicalCaseId.Value);
                Logger.LogInformation("导航到医案录入界面（继续看诊）：PatientId={PatientId}, MedicalCaseId={MedicalCaseId}",
                    patient.Id, medicalCaseId.Value);
            }
            else
            {
                Logger.LogInformation("导航到医案录入界面（新建医案）：PatientId={PatientId}, PatientName={PatientName}",
                    patient.Id, patient.Name);
            }

            RegionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
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
        private async Task LoadCurrentPageAsync()
        {
            Logger.LogInformation("加载第{CurrentPage}页患者数据", CurrentPage);

            // Issue #1788: 使用CommandHandler分页查询
            var commandResult = await _commandHandler.GetPatientsPagedAsync(CurrentPage, PageSize, SearchKeyword);

            if (!commandResult.IsSuccess || commandResult.Data == null)
            {
                Logger.LogError("加载患者列表失败：{ErrorMessage}", commandResult.ErrorMessage);
                throw new InvalidOperationException($"加载患者列表失败：{commandResult.ErrorMessage}");
            }

            var result = commandResult.Data;

            Patients.Clear();
            foreach (var patient in result.Items)
            {
                Patients.Add(patient);
            }

            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;

            Logger.LogInformation("加载成功，当前第{CurrentPage}/{TotalPages}页，共{TotalCount}条记录", CurrentPage, TotalPages, TotalCount);
        }

        /// <summary>
        /// 加载初始患者列表（第1页）
        /// </summary>
        private async Task LoadInitialPatientsAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载患者列表...");

                Logger.LogInformation("加载初始患者列表（第1页）");

                CurrentPage = 1;
                await LoadCurrentPageAsync();

                PreviousPageCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者列表失败");
                await ShowErrorMessageAsync($"加载患者列表失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 加载待看诊队列（Epic #1583 - Phase 5）
        /// </summary>
        private async Task LoadPendingCasesAsync()
        {
            try
            {
                Logger.LogInformation("开始加载待看诊队列");

                var response = await _medicalCaseApi.GetPendingCasesAsync();

                if (response.Success && response.Data != null)
                {
                    PendingQueue.Clear();
                    foreach (var item in response.Data)
                    {
                        PendingQueue.Add(item);

                        // MedicalCaseId可能为null，只有有值时才加入缓存
                        if (item.MedicalCaseId.HasValue)
                        {
                            _pendingCaseCache[item.PatientId] = item.MedicalCaseId.Value;
                        }
                    }

                    Logger.LogInformation("待看诊队列加载完成，共{Count}条记录", PendingQueue.Count);
                }
                else
                {
                    Logger.LogWarning("加载待看诊队列失败：{Message}", response.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载待看诊队列异常");
            }
        }

        /// <summary>
        /// 为待看诊队列选中的患者加载完整信息
        /// 修复双击功能：确保CurrentPatient被正确设置
        /// </summary>
        private async Task LoadPatientForPendingCaseAsync(Guid patientId)
        {
            try
            {
                Logger.LogInformation("加载患者详情：PatientId={PatientId}", patientId);

                // 先从Patients列表中查找
                var patientInList = Patients.FirstOrDefault(p => p.Id == patientId);
                if (patientInList != null)
                {
                    Logger.LogInformation("从当前列表中找到患者，直接设置CurrentPatient");
                    CurrentPatient = patientInList;
                    return;
                }

                // Issue #1788: 列表中没有，通过CommandHandler加载
                var result = await _commandHandler.GetByIdAsync(patientId);
                if (result.IsSuccess && result.Data != null)
                {
                    Logger.LogInformation("从API加载患者成功：{PatientName}", result.Data.Name);
                    CurrentPatient = result.Data;
                }
                else
                {
                    Logger.LogWarning("加载患者失败：PatientId={PatientId}, ErrorMessage={ErrorMessage}",
                        patientId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者详情失败：PatientId={PatientId}", patientId);
            }
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

                // Epic #1583 - Phase 5: 加载待看诊队列
                _ = LoadPendingCasesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到患者选择视图时发生异常");
            }
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
    }
}

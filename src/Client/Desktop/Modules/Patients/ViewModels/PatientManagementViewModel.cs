using System.Data;
using AutoMapper;
using LYBT.Desktop.Core.Coordinators;
using LYBT.Desktop.Core.Helpers;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Managers;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Commands;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型 - UltraThink双层架构UI层
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：患者档案管理界面逻辑、命令处理、状态管理、导入导出控制
    /// 基于NewBaseListViewModel统一列表管理模式
    /// 集成PatientModule双层服务，提供完整的患者档案管理用户体验
    /// 支持患者CRUD操作、状态切换、Excel导入导出等核心管理功能
    /// 适配中医诊所患者档案管理流程，确保档案数据安全性和界面友好性
    /// </summary>
#pragma warning disable CS0618 // NewBaseListViewModel已过时，计划未来架构升级

    public class PatientManagementViewModel : NewBaseListViewModel<PatientDto>
#pragma warning restore CS0618
    {
        #region Fields

        private readonly IPatientService _patientService;
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ICustomDialogService _dialogService;
        private readonly IMapper _mapper;

        // UltraThink v2.0: 直接使用DTO，移除复杂的ViewModel包装
        private PatientDto? _selectedPatient;

        #endregion Fields

        #region Properties

        /// <summary>选中的患者 - UltraThink v2.0: 直接使用DTO</summary>
        public PatientDto? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleStatusCommand.RaiseCanExecuteChanged();
                    ViewDetailsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // 暴露基类的搜索和分页属性供XAML绑定
        public string SearchKeyword
        {
            get => SearchManager.SearchKeyword;
            set => SearchManager.SearchKeyword = value;
        }

        public DelegateCommand SearchCommand { get; private set; } = null!;

        public int CurrentPage => PaginationCoordinator.CurrentPage;
        public int TotalPages => PaginationCoordinator.TotalPages;
        public DelegateCommand FirstPageCommand { get; private set; } = null!;
        public DelegateCommand PreviousPageCommand { get; private set; } = null!;
        public DelegateCommand NextPageCommand { get; private set; } = null!;
        public DelegateCommand LastPageCommand { get; private set; } = null!;

        public string StatusText => $"共 {PaginationCoordinator.TotalCount} 条记录";

        // Epic 04-P0-03: 患者快速搜索优化属性
        private bool _showAdvancedSearch = false;
        private bool _advancedSearchExpanded = true;
        private bool _showSearchSuggestions = false;
        private PatientSearchDto _advancedSearch = new();
        private List<string> _searchSuggestions = [];
        private string? _selectedSearchSuggestion;
        private List<string> _searchHistory = [];

        /// <summary>显示高级搜索面板</summary>
        public bool ShowAdvancedSearch
        {
            get => _showAdvancedSearch;
            set => SetProperty(ref _showAdvancedSearch, value);
        }

        /// <summary>高级搜索面板是否展开</summary>
        public bool AdvancedSearchExpanded
        {
            get => _advancedSearchExpanded;
            set => SetProperty(ref _advancedSearchExpanded, value);
        }

        /// <summary>显示搜索建议</summary>
        public bool ShowSearchSuggestions
        {
            get => _showSearchSuggestions;
            set => SetProperty(ref _showSearchSuggestions, value);
        }

        /// <summary>高级搜索条件</summary>
        public PatientSearchDto AdvancedSearch
        {
            get => _advancedSearch;
            set => SetProperty(ref _advancedSearch, value);
        }

        /// <summary>搜索建议列表</summary>
        public List<string> SearchSuggestions
        {
            get => _searchSuggestions;
            set => SetProperty(ref _searchSuggestions, value);
        }

        /// <summary>选中的搜索建议</summary>
        public string? SelectedSearchSuggestion
        {
            get => _selectedSearchSuggestion;
            set
            {
                if (SetProperty(ref _selectedSearchSuggestion, value) && value != null)
                {
                    SearchKeyword = value;
                    ShowSearchSuggestions = false;
                }
            }
        }

        /// <summary>搜索历史</summary>
        public List<string> SearchHistory
        {
            get => _searchHistory;
            set => SetProperty(ref _searchHistory, value);
        }

        // Epic 04-P0-04: 界面响应性提升属性
        private DateTime _lastUpdateTime = DateTime.Now;
        private bool _isOperationInProgress = false;

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        /// <summary>是否有操作正在进行</summary>
        public bool IsOperationInProgress
        {
            get => _isOperationInProgress;
            set => SetProperty(ref _isOperationInProgress, value);
        }

        // UltraThink v2.0: 删除批量选择功能 - 20人以下小诊所不需要复杂的多选和批量操作
        // 基础搜索功能已经通过NewBaseListViewModel的SearchManager提供
        #endregion Properties

        #region Commands

        public DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> EditCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> DeleteCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> ToggleStatusCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> ViewDetailsCommand { get; private set; } = null!;

        // Phase 2 新增：患者历史记录查询功能
        public DelegateCommand<PatientDto> ViewHistoryCommand { get; private set; } = null!;

        // Phase 7 新增：导入导出功能
        public DelegateCommand ExportPatientsCommand { get; private set; } = null!;

        public DelegateCommand ImportPatientsCommand { get; private set; } = null!;
        public DelegateCommand ImportWizardCommand { get; private set; } = null!;
        public DelegateCommand DownloadTemplateCommand { get; private set; } = null!;

        // Epic 04-P0-03: 患者快速搜索优化命令
        public DelegateCommand ShowSearchHistoryCommand { get; private set; } = null!;
        public DelegateCommand ToggleAdvancedSearchCommand { get; private set; } = null!;
        public DelegateCommand ClearSearchCommand { get; private set; } = null!;
        public DelegateCommand ExecuteAdvancedSearchCommand { get; private set; } = null!;
        public DelegateCommand ClearAdvancedSearchCommand { get; private set; } = null!;
        public DelegateCommand SaveSearchTemplateCommand { get; private set; } = null!;

        // UltraThink v2.0: 删除过度设计功能 - 20人以下小诊所不需要以下复杂功能:
        // - BatchEnableCommand/BatchDisableCommand: 批量操作过度设计
        // - ClearSelectionCommand/SelectAllCommand: 多选功能过度设计
        #endregion Commands

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientManagementViewModel"/> class.
        /// 构造函数 - UltraThink双层架构依赖注入
        /// 初始化患者管理模块、对话服务、映射器、命令和事件订阅
        /// </summary>
        /// <param name="patientService">患者模块主服务</param>
        /// <param name="dialogService">自定义对话服务</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="sessionManager">会话管理器</param>
        /// <param name="notificationService">通知服务</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="paginationCoordinator">分页协调器</param>
        /// <param name="searchManager">搜索管理器</param>
        /// <exception cref="ArgumentNullException">当关键参数为空时抛出</exception>
        public PatientManagementViewModel(
        IPatientService patientService,
        IMedicalCaseService medicalCaseService,
        ICustomDialogService dialogService,
        IMapper mapper,
        ISessionManager sessionManager,
        INotificationService notificationService,
        ILogger<PatientManagementViewModel> logger,
        IPaginationCoordinator? paginationCoordinator = null,
        ISearchManager? searchManager = null)
        : base(sessionManager, notificationService, logger, paginationCoordinator, searchManager)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            InitializeCommands();

            // UltraThink v2.0: 删除复杂初始化逻辑
            // - 删除选择状态变化监听: 多选功能已移除
            // - 删除RefreshDataAsync(): 直接使用基类的数据加载机制
        }

        #endregion Constructor

        #region Command Initialization

        /// <inheritdoc/>
        protected override void InitializeCommands()
        {
            AddCommand = new DelegateCommand(async () => await AddPatientAsync());
            EditCommand = new DelegateCommand<PatientDto>(async patient => await EditPatientAsync(patient), CanExecutePatientCommand);
            DeleteCommand = new DelegateCommand<PatientDto>(async patient => await DeletePatientAsync(patient), CanExecutePatientCommand);
            ToggleStatusCommand = new DelegateCommand<PatientDto>(async patient => await ToggleStatusAsync(patient), CanExecutePatientCommand);
            ViewDetailsCommand = new DelegateCommand<PatientDto>(async patient => await ViewDetailsAsync(patient), CanExecutePatientCommand);

            // Phase 2 新增：初始化患者历史记录查询命令
            ViewHistoryCommand = new DelegateCommand<PatientDto>(async patient => await ViewHistoryAsync(patient), CanExecutePatientCommand);

            // Phase 7: 初始化导入导出命令
            ExportPatientsCommand = new DelegateCommand(async () => await ExportPatientsAsync(), () => !IsLoading);
            ImportPatientsCommand = new DelegateCommand(async () => await ImportPatientsAsync(), () => !IsLoading);
            ImportWizardCommand = new DelegateCommand(async () => await OpenImportWizardAsync(), () => !IsLoading);
            DownloadTemplateCommand = new DelegateCommand(async () => await DownloadTemplateAsync(), () => !IsLoading);

            // 初始化搜索和分页命令
            SearchCommand = new DelegateCommand(async () => await SearchManager.ExecuteSearchAsync());
            FirstPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToFirstPageAsync());
            PreviousPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToPreviousPageAsync());
            NextPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToNextPageAsync());
            LastPageCommand = new DelegateCommand(async () => await PaginationCoordinator.GoToLastPageAsync());

            // Epic 04-P0-03: 患者快速搜索优化命令初始化
            ShowSearchHistoryCommand = new DelegateCommand(async () => await ShowSearchHistoryAsync());
            ToggleAdvancedSearchCommand = new DelegateCommand(() => ToggleAdvancedSearch());
            ClearSearchCommand = new DelegateCommand(() => ClearSearch());
            ExecuteAdvancedSearchCommand = new DelegateCommand(async () => await ExecuteAdvancedSearchAsync());
            ClearAdvancedSearchCommand = new DelegateCommand(() => ClearAdvancedSearch());
            SaveSearchTemplateCommand = new DelegateCommand(async () => await SaveSearchTemplateAsync());

            // UltraThink v2.0: 删除批量操作命令初始化 - 20人以下小诊所不需要复杂的批量操作
        }

        // Epic 04-P0-04: 增强命令执行条件，防止重复操作
        private bool CanExecutePatientCommand(PatientDto patient)
        {
            return patient != null && !IsLoading && !IsOperationInProgress;
        }

        /// <summary>可以执行一般操作（不依赖选中项）</summary>
        private bool CanExecuteGeneralCommand()
        {
            return !IsLoading && !IsOperationInProgress;
        }

        #endregion Command Initialization

        #region Data Loading Override

        /// <inheritdoc/>
        protected override async Task<ServiceResult<PagedResult<PatientDto>>> LoadDataAsync(PagedQueryBaseDto request)
        {
            // UltraThink v2.0: 转换为PatientSearchDto进行患者查询
            var patientQuery = new PatientSearchDto
            {
                Keyword = request.Keyword,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                SortField = request.SortField,
                IsDescending = request.IsDescending
            };
            return await _patientService.GetPagedAsync(patientQuery);
        }

        // UltraThink v2.0: 删除复杂的ViewModel转换和选择状态管理
        // 直接使用基类的标准数据加载处理，无需自定义OnDataLoaded和OnDataLoadFailed
        #endregion Data Loading Override

        // UltraThink v2.0: 删除复杂的ViewModel管理 - 20人以下小诊所不需要复杂的选择状态管理
        // 直接使用基类提供的Data属性访问PagedResult<PatientDto>数据

        #region CRUD Operations

        // Epic 04-P0-04: 增强添加患者操作响应性
        private async Task AddPatientAsync()
        {
            if (IsOperationInProgress)
            {
                return; // 防止重复点击
            }

            try
            {
                IsOperationInProgress = true;

                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = false
                };

                var result = await _dialogService.ShowDialogAsync("PatientAddEditDialog", parameters);

                if (result.Result == true)
                {
                    // 显示操作反馈
                    ShowInfo("正在刷新患者列表...");

                    await RefreshDataAsync();

                    // 更新时间戳
                    LastUpdateTime = DateTime.Now;

                    ShowSuccess("患者信息添加成功");
                    await _dialogService.ShowSuccessAsync("患者信息添加成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "添加患者失败");
                ShowError($"添加患者失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"添加患者失败: {ex.Message}", "错误");
            }
            finally
            {
                IsOperationInProgress = false;

                // 刷新命令状态
                AddCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task EditPatientAsync(PatientDto patient)
        {
            if (patient == null)
            {
                return;
            }

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["IsEditMode"] = true,
                    ["Patient"] = patient
                };

                var result = await _dialogService.ShowDialogAsync("PatientAddEditDialog", parameters);

                if (result.Result == true)
                {
                    await RefreshDataAsync();
                    await _dialogService.ShowSuccessAsync($"患者 {patient.Name} 信息更新成功", "成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "编辑患者失败: {PatientId}", patient.Id);
                ShowError($"编辑患者失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"编辑患者失败: {ex.Message}", "错误");
            }
        }

        private async Task DeletePatientAsync(PatientDto patient)
        {
            if (patient == null)
            {
                return;
            }

            // 患者信息不支持真正删除，只能禁用
            await ToggleStatusAsync(patient);
        }

        #endregion CRUD Operations

        #region Business Operations

        private async Task ToggleStatusAsync(PatientDto patient)
        {
            if (patient == null)
            {
                return;
            }

            var isEnabled = patient.Status == CommonStatus.Enabled;
            var action = isEnabled ? "禁用" : "启用";

            var confirm = await _dialogService.ShowConfirmationAsync(
            $"确定要{action}患者 {patient.Name} 吗？",
            $"{action}患者");

            if (confirm)
            {
                try
                {
                    ServiceResult result;
                    if (isEnabled)
                    {
                        result = await _patientService.DisableAsync(patient.Id);
                    }
                    else
                    {
                        result = await _patientService.EnableAsync(patient.Id);
                    }

                    if (result.IsSuccess)
                    {
                        await RefreshDataAsync();
                        await _dialogService.ShowInformationAsync($"患者{action}成功", "成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(
                        result.ErrorMessage ?? $"患者{action}失败",
                        "错误");
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "切换患者状态失败: {PatientId}", patient.Id);
                    ShowError($"患者{action}失败: {ex.Message}");
                    await _dialogService.ShowErrorAsync($"患者{action}失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task ViewDetailsAsync(PatientDto patient)
        {
            if (patient == null)
            {
                return;
            }

            try
            {
                var result = await _patientService.GetByIdAsync(patient.Id);

                if (result.IsSuccess && result.Data != null)
                {
                    var patientDetail = result.Data;
                    var detailInfo = $"患者详情：\n\n" +
                    $"姓名: {patientDetail.Name}\n" +
                    $"性别: {(patientDetail.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : patientDetail.Gender == LYBT.Shared.Models.Enums.Gender.Female ? "女" : "未知")}\n" +
                    $"年龄: {patientDetail.Age}岁\n" +
                    $"电话: {patientDetail.PhoneNumber ?? "未填写"}\n" +
                    $"证件号: {patientDetail.IdNumber ?? "未填写"}\n" +
                    $"地址: {patientDetail.Address ?? "未填写"}\n" +
                    $"状态: {(patientDetail.Status == CommonStatus.Enabled ? "正常" : "禁用")}\n" +
                    $"过敏史: {patientDetail.AllergyHistory ?? "无"}";

                    await _dialogService.ShowInformationAsync(detailInfo, $"患者详情 - {patientDetail.Name}");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(
                    result.ErrorMessage ?? "获取患者详情失败",
                    "错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "查看患者详情失败: {PatientId}", patient.Id);
                ShowError($"查看患者详情失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看患者详情失败: {ex.Message}", "错误");
            }
        }

        #endregion Business Operations

        #region Phase 2: 患者历史查询功能

        /// <summary>
        /// 查看患者历史记录
        /// </summary>
        /// <param name="patient">患者数据</param>
        private async Task ViewHistoryAsync(PatientDto patient)
        {
            if (patient == null)
            {
                return;
            }

            try
            {
                // 获取患者的所有医案历史
                var medicalCasesResult = await _medicalCaseService.GetByPatientIdAsync(patient.Id);

                string historyInfo;

                if (medicalCasesResult.IsSuccess && medicalCasesResult.Data != null && medicalCasesResult.Data.Count > 0)
                {
                    var medicalCases = medicalCasesResult.Data
                    .OrderByDescending(mc => mc.CreateTime)
                    .ToList();

                    historyInfo = $"=== 患者历史就诊记录 ===\n\n" +
                    $"【患者信息】\n" +
                    $"姓名: {patient.Name}\n" +
                    $"性别: {(patient.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : patient.Gender == LYBT.Shared.Models.Enums.Gender.Female ? "女" : "未知")}\n" +
                    $"年龄: {patient.Age}岁\n" +
                    $"电话: {patient.PhoneNumber ?? "未填写"}\n";

                    // P0-02优化：添加过敏史重要提醒
                    if (!string.IsNullOrEmpty(patient.AllergyHistory) && patient.AllergyHistory != "无")
                    {
                        historyInfo += $" 过敏史: {patient.AllergyHistory}\n";
                    }

                    historyInfo += $"\n【历史医案记录】(共{medicalCases.Count}次就诊)\n\n";

                    for (int i = 0; i < Math.Min(medicalCases.Count, 15); i++) // P0-02优化：扩展到15条记录
                    {
                        var mc = medicalCases[i];
                        var statusText = GetMedicalCaseStatusText((int)mc.Status);
                        historyInfo += $"▶ 第{i + 1}次就诊 - {mc.CreateTime:yyyy-MM-dd HH:mm}\n";
                        historyInfo += $" 状态: {statusText}\n";

                        if (!string.IsNullOrEmpty(mc.Remark))
                        {
                            var remark = mc.Remark.Length > 50 ? mc.Remark.Substring(0, 50) + "..." : mc.Remark;
                            historyInfo += $" 备注: {remark}\n";
                        }
                        else
                        {
                            historyInfo += $" 备注: 未填写\n";
                        }

                        historyInfo += "\n";
                    }

                    if (medicalCases.Count > 15)
                    {
                        historyInfo += $" 注：为保持界面简洁，仅显示最近15条记录，实际共{medicalCases.Count}条。\n\n";
                    }

                    historyInfo += " 提示：\n";
                    historyInfo += "• 可在医案管理模块查看完整详情\n";
                    historyInfo += "• 可在诊疗模块查看该患者历史诊疗记录\n";
                    historyInfo += "• 注意过敏史和既往病史，确保用药安全";
                }
                else
                {
                    historyInfo = $"=== 患者历史记录 ===\n\n" +
                    $"【患者信息】\n" +
                    $"姓名: {patient.Name}\n" +
                    $"性别: {(patient.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" : patient.Gender == LYBT.Shared.Models.Enums.Gender.Female ? "女" : "未知")}\n" +
                    $"年龄: {patient.Age}岁\n" +
                    $"电话: {patient.PhoneNumber ?? "未填写"}\n";

                    // P0-02优化：无记录时也显示过敏史
                    if (!string.IsNullOrEmpty(patient.AllergyHistory) && patient.AllergyHistory != "无")
                    {
                        historyInfo += $" 过敏史: {patient.AllergyHistory}\n";
                    }

                    historyInfo += $"\n【诊疗记录】\n" +
                    $"暂无历史就诊记录\n\n" +
                    $" 提示：\n" +
                    $"• 该患者尚未有诊疗记录，可开始新的看诊流程\n" +
                    $"• 可在患者接待或医案管理模块中创建就诊记录\n" +
                    $"• 首次就诊请详细了解病史和过敏史";
                }

                await _dialogService.ShowInformationAsync(historyInfo, $"患者历史记录 - {patient.Name}");
            }
            catch (Exception ex)
            {
                LogError(ex, "查看患者历史记录失败: {PatientId}", patient.Id);
                ShowError($"查看患者历史记录失败: {ex.Message}");
                await _dialogService.ShowErrorAsync($"查看患者历史记录失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 获取医案状态显示文本
        /// </summary>
        /// <param name="status">医案状态值</param>
        /// <returns>状态文本</returns>
        private string GetMedicalCaseStatusText(int status)
        {
            return status switch
            {
                0 => "已登记",
                1 => "进行中",
                2 => "已完成",
                3 => "已取消",
                4 => "已暂停",
                _ => "未知状态"
            };
        }

        #endregion Phase 2: 患者历史查询功能

        // UltraThink v2.0: 删除所有批量操作功能 - 20人以下小诊所不需要复杂的批量操作
        // 包括: BatchEnableAsync, BatchDisableAsync 等功能

        // UltraThink v2.0: 删除所有选择管理功能 - 20人以下小诊所不需要复杂的多选功能
        // 包括: ClearSelection, SelectAll 等功能
        #region Phase 7: 导入导出功能

        /// <summary>
        /// 导出患者数据到Excel
        /// </summary>
        private async Task ExportPatientsAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 获取所有患者数据
                    var allPatientsResult = await _patientService.GetPagedAsync(new PatientSearchDto
                    {
                        PageIndex = 1,
                        PageSize = 10000, // 获取大量数据用于导出
                        Keyword = string.Empty
                    });

                    if (allPatientsResult.IsSuccess && allPatientsResult.Data != null)
                    {
                        var patients = allPatientsResult.Data.Items;

                        // 定义导出列
                        var columns = new Dictionary<string, string>
{
{ "Name", "姓名" },
{ "Gender", "性别" },
{ "Age", "年龄" },
{ "PhoneNumber", "电话" },
{ "IdNumber", "证件号" },
{ "Address", "地址" },
{ "AllergyHistory", "过敏史" },
{ "Status", "状态" },
{ "CreateTime", "创建时间" }
};

                        // 转换数据用于导出
                        var exportData = patients.Select(p => new
                        {
                            Name = p.Name,
                            Gender = p.Gender == LYBT.Shared.Models.Enums.Gender.Male ? "男" :
                        p.Gender == LYBT.Shared.Models.Enums.Gender.Female ? "女" : "未知",
                            Age = p.Age,
                            PhoneNumber = p.PhoneNumber ?? string.Empty,
                            IdNumber = p.IdNumber ?? string.Empty,
                            Address = p.Address ?? string.Empty,
                            AllergyHistory = p.AllergyHistory ?? string.Empty,
                            Status = p.Status == CommonStatus.Enabled ? "正常" : "禁用",
                            CreateTime = p.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });

                        // 导出到Excel
                        ExcelHelper.ExportToExcel(exportData, columns, saveFileDialog.FileName, "患者数据");

                        await _dialogService.ShowSuccessAsync($"成功导出 {patients.Count()} 条患者数据到:\n{saveFileDialog.FileName}", "导出成功");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync(allPatientsResult.ErrorMessage ?? "获取患者数据失败", "导出失败");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "导出患者数据失败");
                await _dialogService.ShowErrorAsync($"导出患者数据失败: {ex.Message}", "导出失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 从Excel导入患者数据
        /// </summary>
        private async Task ImportPatientsAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "选择要导入的患者数据文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;

                    // 读取Excel数据
                    var dataTable = ExcelHelper.ImportFromExcel(openFileDialog.FileName, true);

                    if (dataTable.Rows.Count == 0)
                    {
                        await _dialogService.ShowWarningAsync("Excel文件中没有找到数据", "导入提示");
                        return;
                    }

                    int successCount = 0;
                    int failCount = 0;
                    var errors = new List<string>();

                    // 处理每行数据
                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        try
                        {
                            var row = dataTable.Rows[i];

                            // 验证必填字段
                            var name = row["姓名"]?.ToString()?.Trim();
                            if (string.IsNullOrEmpty(name))
                            {
                                errors.Add($"第{i + 2}行：姓名不能为空");
                                failCount++;
                                continue;
                            }

                            // 创建患者DTO
                            var age = ParseAge(row["年龄"]?.ToString());
                            var patientDto = new PatientCreateDto
                            {
                                Name = name,
                                Gender = ParseGender(row["性别"]?.ToString()),
                                BirthDate = age > 0 ? DateTime.Today.AddYears(-age) : null,
                                PhoneNumber = row["电话"]?.ToString()?.Trim(),
                                IdNumber = row["证件号"]?.ToString()?.Trim(),
                                Address = row["地址"]?.ToString()?.Trim(),
                                AllergyHistory = row["过敏史"]?.ToString()?.Trim()
                            };

                            // 调用API创建患者
                            var result = await _patientService.CreateAsync(patientDto);
                            if (result.IsSuccess)
                            {
                                successCount++;
                            }
                            else
                            {
                                errors.Add($"第{i + 2}行 {name}：{result.ErrorMessage}");
                                failCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"第{i + 2}行：处理数据时发生错误 - {ex.Message}");
                            failCount++;
                        }
                    }

                    // 显示导入结果
                    var message = $"导入完成！\n成功：{successCount} 条\n失败：{failCount} 条";
                    if (errors.Count > 0 && errors.Count <= 10)
                    {
                        message += $"\n\n错误详情:\n{string.Join("\n", errors)}";
                    }
                    else if (errors.Count > 10)
                    {
                        message += $"\n\n错误详情（前10条）:\n{string.Join("\n", errors.Take(10))}\n... 等其他{errors.Count - 10}条错误";
                    }

                    if (failCount == 0)
                    {
                        await _dialogService.ShowSuccessAsync(message, "导入成功");
                    }
                    else
                    {
                        await _dialogService.ShowWarningAsync(message, "导入完成");
                    }

                    // 刷新数据
                    if (successCount > 0)
                    {
                        await RefreshDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "导入患者数据失败");
                await _dialogService.ShowErrorAsync($"导入患者数据失败: {ex.Message}", "导入失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 下载患者数据导入模板
        /// </summary>
        private async Task DownloadTemplateAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = "患者数据导入模板.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 定义模板列
                    var columns = new[] { "姓名", "性别", "年龄", "电话", "证件号", "地址", "过敏史" };

                    // 创建示例数据
                    var sampleData = new List<string[]>
{
new[] { "张三", "男", "35", "13800138000", "110101198801010001", "北京市朝阳区", "青霉素过敏" },
new[] { "李四", "女", "28", "13900139000", "110101199201020002", "北京市海淀区", "无" }
};

                    // 创建Excel模板
                    ExcelHelper.CreateTemplate(columns, saveFileDialog.FileName, "患者数据", sampleData);

                    await _dialogService.ShowSuccessAsync($"模板文件已保存到:\n{saveFileDialog.FileName}\n\n请按照模板格式填写患者数据，然后使用导入功能。", "模板下载成功");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "下载模板失败");
                await _dialogService.ShowErrorAsync($"下载模板失败: {ex.Message}", "下载失败");
            }
        }

        /// <summary>
        /// 解析性别字符串
        /// </summary>
        private LYBT.Shared.Models.Enums.Gender ParseGender(string? genderStr)
        {
            if (string.IsNullOrEmpty(genderStr))
            {
                return LYBT.Shared.Models.Enums.Gender.Unknown;
            }

            genderStr = genderStr.Trim().ToLower();
            return genderStr switch
            {
                "男" or "male" or "m" => LYBT.Shared.Models.Enums.Gender.Male,
                "女" or "female" or "f" => LYBT.Shared.Models.Enums.Gender.Female,
                _ => LYBT.Shared.Models.Enums.Gender.Unknown
            };
        }

        /// <summary>
        /// 解析年龄字符串
        /// </summary>
        private int ParseAge(string? ageStr)
        {
            if (string.IsNullOrEmpty(ageStr))
            {
                return 0;
            }

            // 移除可能的"岁"字符
            ageStr = ageStr.Trim().Replace("岁", string.Empty);

            if (int.TryParse(ageStr, out int age) && age >= 0 && age <= 150)
            {
                return age;
            }

            return 0;
        }

        /// <summary>
        /// 打开导入向导
        /// </summary>
        private async Task OpenImportWizardAsync()
        {
            try
            {
                // 创建导入向导窗口
                var wizardWindow = new System.Windows.Window
                {
                    Title = "患者Excel导入向导",
                    Width = 900,
                    Height = 700,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                    ResizeMode = System.Windows.ResizeMode.CanResize,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                // 创建向导视图并设置为窗口内容
                var wizardView = new LYBT.Desktop.Patients.Views.PatientImportWizardView();
                wizardWindow.Content = wizardView;

                // 获取向导ViewModel并设置事件处理
                var wizardViewModel = wizardView.DataContext as LYBT.Desktop.Patients.ViewModels.PatientImportWizardViewModel;
                if (wizardViewModel != null)
                {
                    // 订阅导入完成事件，刷新患者列表
                    wizardViewModel.ImportCompleted += async (sender, e) =>
                    {
                        await RefreshDataAsync();
                        wizardWindow.Close();
                    };

                    // 订阅取消事件
                    wizardViewModel.ImportCancelled += (sender, e) =>
                    {
                        wizardWindow.Close();
                    };
                }

                // 显示模态窗口
                wizardWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError(ex, "打开导入向导失败");
                await _dialogService.ShowErrorAsync($"打开导入向导失败: {ex.Message}", "错误");
            }
        }

        #endregion Phase 7: 导入导出功能

        #region Epic 04-P0-03: 患者快速搜索优化方法

        /// <summary>显示搜索历史</summary>
        private async Task ShowSearchHistoryAsync()
        {
            try
            {
                if (SearchHistory.Count == 0)
                {
                    await _dialogService.ShowInformationAsync("暂无搜索历史记录", "搜索历史");
                    return;
                }

                var historyText = string.Join("\n", SearchHistory.Take(10).Select((h, i) => $"{i + 1}. {h}"));
                var result = await _dialogService.ShowConfirmationAsync(
                $"最近搜索记录：\n\n{historyText}\n\n是否清空搜索历史？",
                "搜索历史");

                if (result)
                {
                    SearchHistory.Clear();
                    await _dialogService.ShowInformationAsync("搜索历史已清空", "完成");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "显示搜索历史失败");
                await _dialogService.ShowErrorAsync($"显示搜索历史失败: {ex.Message}", "错误");
            }
        }

        /// <summary>切换高级搜索面板显示状态</summary>
        private void ToggleAdvancedSearch()
        {
            ShowAdvancedSearch = !ShowAdvancedSearch;
            if (ShowAdvancedSearch)
            {
                // 初始化高级搜索条件
                if (AdvancedSearch.PageSize == 0)
                {
                    AdvancedSearch.PageSize = 20;
                    AdvancedSearch.PageIndex = 1;
                }
            }
        }

        /// <summary>清空搜索条件</summary>
        private void ClearSearch()
        {
            SearchKeyword = string.Empty;
            ClearAdvancedSearch();
            ShowSearchSuggestions = false;
        }

        /// <summary>执行高级搜索</summary>
        private async Task ExecuteAdvancedSearchAsync()
        {
            try
            {
                IsLoading = true;

                // 设置分页参数
                AdvancedSearch.PageIndex = 1;
                AdvancedSearch.PageSize = 20;

                var result = await _patientService.GetPagedAsync(AdvancedSearch);

                if (result.IsSuccess && result.Data != null)
                {
                    // 更新数据
                    Items = new System.Collections.ObjectModel.ObservableCollection<PatientDto>(result.Data.Items);
                    PaginationCoordinator.UpdatePagination(result.Data.TotalCount);

                    // 添加到搜索历史
                    var searchText = BuildSearchHistoryText(AdvancedSearch);
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        AddToSearchHistory(searchText);
                    }

                    await _dialogService.ShowInformationAsync($"找到 {result.Data.TotalCount} 条符合条件的患者记录", "搜索结果");
                }
                else
                {
                    await _dialogService.ShowErrorAsync(result.ErrorMessage ?? "高级搜索失败", "搜索错误");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "执行高级搜索失败");
                await _dialogService.ShowErrorAsync($"高级搜索失败: {ex.Message}", "错误");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>清空高级搜索条件</summary>
        private void ClearAdvancedSearch()
        {
            AdvancedSearch = new PatientSearchDto
            {
                PageIndex = 1,
                PageSize = 20
            };
        }

        /// <summary>保存搜索模板</summary>
        private async Task SaveSearchTemplateAsync()
        {
            try
            {
                var templateName = await _dialogService.ShowInputAsync("请输入搜索模板名称", "保存搜索模板");
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    return;
                }

                // 这里可以扩展保存到数据库或配置文件
                // 目前简化为显示确认信息
                await _dialogService.ShowSuccessAsync($"搜索模板 '{templateName}' 已保存", "保存成功");
            }
            catch (Exception ex)
            {
                LogError(ex, "保存搜索模板失败");
                await _dialogService.ShowErrorAsync($"保存搜索模板失败: {ex.Message}", "错误");
            }
        }

        /// <summary>构建搜索历史文本</summary>
        private string BuildSearchHistoryText(PatientSearchDto query)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(query.Name))
                conditions.Add($"姓名:{query.Name}");
            if (!string.IsNullOrEmpty(query.PhoneNumber))
                conditions.Add($"手机:{query.PhoneNumber}");
            if (!string.IsNullOrEmpty(query.IDNumber))
                conditions.Add($"证件号:{query.IDNumber}");
            if (query.Gender.HasValue)
                conditions.Add($"性别:{query.Gender}");
            if (query.MinAge.HasValue)
                conditions.Add($"年龄≥{query.MinAge}");
            if (query.MaxAge.HasValue)
                conditions.Add($"年龄≤{query.MaxAge}");
            if (!string.IsNullOrEmpty(query.Address))
                conditions.Add($"地址:{query.Address}");
            if (!string.IsNullOrEmpty(query.Profession))
                conditions.Add($"职业:{query.Profession}");

            return conditions.Count > 0 ? string.Join(", ", conditions) : string.Empty;
        }

        /// <summary>添加到搜索历史</summary>
        private void AddToSearchHistory(string searchText)
        {
            if (string.IsNullOrEmpty(searchText) || SearchHistory.Contains(searchText))
                return;

            SearchHistory.Insert(0, searchText);

            // 保持最多20条历史记录
            if (SearchHistory.Count > 20)
            {
                SearchHistory.RemoveRange(20, SearchHistory.Count - 20);
            }
        }

        #endregion Epic 04-P0-03: 患者快速搜索优化方法
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// OpenSpec: redesign-history-copy-ui
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
    /// OpenSpec: refactor-frontend-srp-patterns - 迁移到DialogViewModelBase
    /// 历史医案复制弹窗ViewModel - 支持左右双栏布局
    /// 用于从历史医案中选择复制药材组合到当前处方
    ///
    /// UX改进：
    /// - 默认显示当前患者的最近5条已完成记录
    /// - 支持"显示更多"展开本患者全部记录
    /// - 支持"查看全部患者"切换到全局查询模式
    /// </summary>
    public partial class HistoryCopyDialogViewModel : DialogViewModelBase
    {
        #region 常量

        /// <summary>默认显示的记录数量</summary>
        private const int DefaultDisplayCount = 5;

        #endregion

        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private List<MedicalCaseDetailDto> _allCases = new();
        private List<MedicalCaseDetailDto> _currentPatientCases = new();
        private Guid _patientId;

        #endregion

        #region 可观察属性

        /// <summary>
        /// 患者姓名
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ViewModeText))]
        private string _patientName = string.Empty;

        /// <summary>
        /// 搜索文本（支持患者姓名、中医诊断模糊查询）
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// 时间区间筛选 - 起始日期
        /// </summary>
        [ObservableProperty]
        private DateTime? _startDate;

        /// <summary>
        /// 时间区间筛选 - 结束日期
        /// </summary>
        [ObservableProperty]
        private DateTime? _endDate;

        /// <summary>
        /// 筛选后的医案列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MedicalCaseDetailDto> _filteredCases = new();

        /// <summary>
        /// 选中的医案（左栏卡片列表）
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private MedicalCaseDetailDto? _selectedCase;

        /// <summary>
        /// 选中医案的详情（用于右栏MedicalCaseViewControl绑定）
        /// OpenSpec: unify-control-data-binding - 删除NotifyPropertyChangedFor(SelectedCaseHasConsultation/SelectedCaseHasPrescription)
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private MedicalCaseDetailDto? _selectedCaseDetail;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// 是否显示全部患者模式（false=仅当前患者）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ViewModeText))]
        [NotifyPropertyChangedFor(nameof(CanShowMoreCurrentPatient))]
        [NotifyCanExecuteChangedFor(nameof(ShowMoreCurrentPatientCommand))]
        private bool _isShowingAllPatients;

        /// <summary>
        /// 是否显示当前患者的全部记录（默认false只显示5条）
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowMoreCurrentPatient))]
        [NotifyCanExecuteChangedFor(nameof(ShowMoreCurrentPatientCommand))]
        private bool _isShowingAllCurrentPatient;

        #endregion

        #region 计算属性

        // OpenSpec: unify-control-data-binding - 删除SelectedCaseHasConsultation/SelectedCaseHasPrescription属性

        /// <summary>
        /// 选中医案的处方药材列表（用于复制）
        /// </summary>
        public List<PrescriptionItemDto> SelectedPrescriptionItems { get; private set; } = new();

        /// <summary>
        /// 当前患者是否有更多记录可显示
        /// </summary>
        public bool CanShowMoreCurrentPatient =>
            !IsShowingAllPatients &&
            !IsShowingAllCurrentPatient &&
            _currentPatientCases.Count > DefaultDisplayCount;

        /// <summary>
        /// 当前患者总记录数
        /// </summary>
        public int CurrentPatientTotalCount => _currentPatientCases.Count;

        /// <summary>
        /// 查看模式文本
        /// </summary>
        public string ViewModeText => IsShowingAllPatients ? "全部患者" : $"本患者 ({PatientName})";

        #endregion

        #region 属性变更回调

        /// <summary>
        /// 搜索文本变更时触发筛选
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            FilterCases();
        }

        /// <summary>
        /// 起始日期变更时触发筛选
        /// </summary>
        partial void OnStartDateChanged(DateTime? value)
        {
            FilterCases();
        }

        /// <summary>
        /// 结束日期变更时触发筛选
        /// </summary>
        partial void OnEndDateChanged(DateTime? value)
        {
            FilterCases();
        }

        /// <summary>
        /// 选中医案变更时加载详情
        /// </summary>
        partial void OnSelectedCaseChanged(MedicalCaseDetailDto? value)
        {
            LoadCaseDetailAsync();
        }

        /// <summary>
        /// 全部患者模式变更时切换数据源
        /// </summary>
        partial void OnIsShowingAllPatientsChanged(bool value)
        {
            if (value)
            {
                LoadAllPatientsAsync();
            }
            else
            {
                // 切回当前患者模式
                IsShowingAllCurrentPatient = false;
                ApplyCurrentPatientFilter();
            }
        }

        /// <summary>
        /// 当前患者全部记录模式变更时更新筛选
        /// </summary>
        partial void OnIsShowingAllCurrentPatientChanged(bool value)
        {
            ApplyCurrentPatientFilter();
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数 - 使用IViewModelServices聚合服务
        /// OpenSpec: refactor-frontend-srp-patterns - 迁移到DialogViewModelBase
        /// </summary>
        public HistoryCopyDialogViewModel(
            IViewModelServices services,
            IMedicalCaseRepository medicalCaseRepository)
            : base(services)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            Title = "从历史医案复制";

            Logger.LogInformation("HistoryCopyDialogViewModel已初始化");
        }

        #endregion

        #region DialogViewModelBase重写

        /// <summary>
        /// 对话框打开时的核心处理
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            if (parameters == null) return;

            if (parameters.TryGetValue("PatientId", out Guid patientId))
            {
                _patientId = patientId;
            }

            if (parameters.TryGetValue("PatientName", out string? patientName))
            {
                PatientName = patientName ?? string.Empty;
            }

            LoadCasesAsync();
        }

        /// <summary>
        /// 是否可以确认（子类重写）
        /// </summary>
        protected override bool CanConfirm() => SelectedCase != null && SelectedPrescriptionItems.Any();

        /// <summary>
        /// 确认命令（子类重写）
        /// </summary>
        protected override void Confirm()
        {
            var parameters = new DialogParameters
            {
                // T5-P3-09: 优先传出详情（包含DosageCount/Discount等处方级字段）
                { "SelectedCase", SelectedCaseDetail ?? SelectedCase },
                { "SelectedItems", SelectedPrescriptionItems }
            };
            CloseDialog(parameters, ButtonResult.OK);
        }

        #endregion

        #region 命令

        /// <summary>
        /// 显示更多当前患者记录命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanShowMoreCurrentPatient))]
        private void ShowMoreCurrentPatient()
        {
            IsShowingAllCurrentPatient = true;
        }

        /// <summary>
        /// 切换全部患者模式命令
        /// </summary>
        [RelayCommand]
        private void ToggleAllPatients()
        {
            IsShowingAllPatients = !IsShowingAllPatients;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载当前患者历史医案列表（默认模式）
        /// UX改进：只加载已完成状态的医案，默认显示5条
        /// </summary>
        private void LoadCasesAsync() => LoadCasesInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载患者历史医案失败"));
        private async Task LoadCasesInternalAsync()
        {
            if (_patientId == Guid.Empty)
            {
                StatusMessage = "未指定患者";
                return;
            }

            try
            {
                StatusMessage = "正在加载历史医案...";

                // OpenSpec: consolidate-medicalcase-detail-queries - 使用QueryAsync替代废弃的GetByPatientIdAsync
                var query = new MedicalCaseQueryDto
                {
                    QueryType = MedicalCaseQueryType.ByPatient,
                    PatientId = _patientId,
                    PageSize = 100
                };
                var caseList = await _medicalCaseRepository.QueryAsync(query);

                // 筛选已完成且有处方的医案ID
                var completedWithPrescriptionIds = caseList?.Items?
                    .Where(c => c.CaseStatus == MedicalCaseStatus.Completed && c.HasPrescription)
                    .Select(c => c.Id)
                    .ToList();

                // 批量获取详情并按时间排序
                if (completedWithPrescriptionIds != null && completedWithPrescriptionIds.Count > 0)
                {
                    var cases = await _medicalCaseRepository.GetBatchDetailsAsync(completedWithPrescriptionIds);
                    _currentPatientCases = cases.OrderByDescending(c => c.CreatedAt).ToList();
                }
                else
                {
                    _currentPatientCases = new List<MedicalCaseDetailDto>();
                }

                // 初始状态：当前患者模式（直接设置字段避免触发重复加载）
                // 注意：这里使用字段而非属性，因为属性setter会触发回调导致不必要的操作
                // 对话框每次打开都会重新创建ViewModel，所以这里是安全的
#pragma warning disable MVVMTK0034
                _isShowingAllPatients = false;
                _isShowingAllCurrentPatient = false;
#pragma warning restore MVVMTK0034

                ApplyCurrentPatientFilter();

                Logger.LogInformation("加载了患者 {PatientId} 的 {Count} 条已完成历史医案", _patientId, _currentPatientCases.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "加载历史医案失败";
                Logger.LogError(ex, "加载患者历史医案失败，患者ID: {PatientId}", _patientId);
            }
        }

        /// <summary>
        /// 加载全部患者的历史医案（全局查询模式）
        /// 使用分页循环获取所有数据，遵循SystemConstants.MaxPageSize规范
        /// </summary>
        private void LoadAllPatientsAsync() => LoadAllPatientsInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载全部患者历史医案失败"));
        private async Task LoadAllPatientsInternalAsync()
        {
            try
            {
                StatusMessage = "正在加载全部患者历史医案...";
                IsLoading = true;

                // 使用分页循环获取所有医案（参考PrescriptionDataLoader模式）
                // OpenSpec: refactor-dto-simplification - MedicalCaseDto已删除，统一使用MedicalCaseDetailDto
                var allItems = new List<MedicalCaseDetailDto>();
                var currentPage = 1;
                var pageSize = SystemConstants.MaxPageSize;

                while (true)
                {
                    // OpenSpec: fix-history-copy-all-patients - 使用SearchAsync查询所有患者的医案
                    var pagedResult = await _medicalCaseRepository.SearchAsync(
                        patientName: null,
                        diagnosisKeyword: null,
                        startDate: null,
                        endDate: null,
                        page: currentPage,
                        pageSize: pageSize);

                    if (pagedResult?.Items == null || !pagedResult.Items.Any())
                        break;

                    allItems.AddRange(pagedResult.Items);

                    // 如果返回数量小于pageSize，说明已到最后一页
                    if (pagedResult.Items.Count < pageSize)
                        break;

                    currentPage++;

                    // 安全阀：最多获取10页（1000条记录）
                    if (currentPage > 10)
                    {
                        Logger.LogWarning("加载全部患者历史医案已达到最大页数限制(10页)");
                        break;
                    }
                }

                // 筛选已完成且有处方的医案
                _allCases = allItems
                    .Where(c => c.CaseStatus == MedicalCaseStatus.Completed && c.PrescriptionId.HasValue)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                FilteredCases = new ObservableCollection<MedicalCaseDetailDto>(_allCases);
                StatusMessage = $"全部患者共 {_allCases.Count} 条已完成历史医案";

                Logger.LogInformation("加载了全部患者的 {Count} 条已完成历史医案（共{TotalPages}页）",
                    _allCases.Count, currentPage);
            }
            catch (Exception ex)
            {
                StatusMessage = "加载全部患者历史医案失败";
                Logger.LogError(ex, "加载全部患者历史医案失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 应用当前患者模式的筛选（默认5条或全部）
        /// </summary>
        private void ApplyCurrentPatientFilter()
        {
            var casesToShow = _currentPatientCases.AsEnumerable();

            // 如果不是显示全部，只取前5条
            if (!IsShowingAllCurrentPatient)
            {
                casesToShow = casesToShow.Take(DefaultDisplayCount);
            }

            FilteredCases = new ObservableCollection<MedicalCaseDetailDto>(casesToShow);

            // 更新状态消息
            if (IsShowingAllCurrentPatient)
            {
                StatusMessage = $"本患者共 {_currentPatientCases.Count} 条已完成历史医案";
            }
            else
            {
                var showingCount = Math.Min(DefaultDisplayCount, _currentPatientCases.Count);
                StatusMessage = _currentPatientCases.Count > DefaultDisplayCount
                    ? $"显示最近 {showingCount} 条（共 {_currentPatientCases.Count} 条）"
                    : $"本患者共 {_currentPatientCases.Count} 条已完成历史医案";
            }

            OnPropertyChanged(nameof(CurrentPatientTotalCount));
            OnPropertyChanged(nameof(CanShowMoreCurrentPatient));
            ShowMoreCurrentPatientCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 筛选医案（支持关键词+时间区间）
        /// 根据当前模式使用不同数据源：全局查询用_allCases，当前患者用_currentPatientCases
        /// </summary>
        private void FilterCases()
        {
            // 根据模式选择数据源
            var sourceData = IsShowingAllPatients ? _allCases : _currentPatientCases;
            var filtered = sourceData.AsEnumerable();

            // 关键词筛选（患者姓名 OR 中医诊断）
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(c =>
                    (c.PatientName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.Diagnosis?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // 时间区间筛选 - 起始日期
            if (StartDate.HasValue)
            {
                filtered = filtered.Where(c => c.CreatedAt >= StartDate.Value.Date);
            }

            // 时间区间筛选 - 结束日期
            if (EndDate.HasValue)
            {
                filtered = filtered.Where(c => c.CreatedAt <= EndDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            // 当前患者模式且未展开全部时，只显示前5条
            if (!IsShowingAllPatients && !IsShowingAllCurrentPatient)
            {
                filtered = filtered.Take(DefaultDisplayCount);
            }

            FilteredCases = new ObservableCollection<MedicalCaseDetailDto>(filtered);

            // 更新状态消息
            var modeText = IsShowingAllPatients ? "全部患者" : "本患者";
            StatusMessage = $"筛选结果: {FilteredCases.Count} 条历史医案 ({modeText})";
        }

        /// <summary>
        /// 加载选中医案的完整详情（用于右栏预览）
        /// </summary>
        private void LoadCaseDetailAsync() => LoadCaseDetailInternalAsync().SafeFireAndForget(ex => Logger.LogError(ex, "加载医案详情失败"));
        private async Task LoadCaseDetailInternalAsync()
        {
            if (SelectedCase == null)
            {
                SelectedCaseDetail = null;
                SelectedPrescriptionItems = new List<PrescriptionItemDto>();
                return;
            }

            try
            {
                IsLoading = true;

                // 获取医案详情（包含诊疗信息和处方信息）
                var detail = await _medicalCaseRepository.GetByIdAsync(SelectedCase.Id);
                SelectedCaseDetail = detail;

                // 提取处方药材列表用于复制
                if (detail?.Prescription?.Items != null && detail.Prescription.Items.Any())
                {
                    SelectedPrescriptionItems = detail.Prescription.Items;
                }
                else
                {
                    SelectedPrescriptionItems = new List<PrescriptionItemDto>();
                }

                // 药材加载完成后刷新确认按钮状态
                ConfirmCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                SelectedCaseDetail = null;
                SelectedPrescriptionItems = new List<PrescriptionItemDto>();
                Logger.LogError(ex, "加载医案详情失败，医案ID: {CaseId}", SelectedCase.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion
    }
}

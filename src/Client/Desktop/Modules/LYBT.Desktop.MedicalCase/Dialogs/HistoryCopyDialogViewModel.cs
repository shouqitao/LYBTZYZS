using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// OpenSpec: redesign-history-copy-ui
    /// 历史医案复制弹窗ViewModel - 支持左右双栏布局
    /// 用于从历史医案中选择复制药材组合到当前处方
    ///
    /// UX改进：
    /// - 默认显示当前患者的最近5条已完成记录
    /// - 支持"显示更多"展开本患者全部记录
    /// - 支持"查看全部患者"切换到全局查询模式
    /// </summary>
    public class HistoryCopyDialogViewModel : BindableBase, IDialogAware
    {
        #region 常量

        /// <summary>默认显示的记录数量</summary>
        private const int DefaultDisplayCount = 5;

        #endregion

        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly ILogger<HistoryCopyDialogViewModel> _logger;
        private List<MedicalCaseDetailDto> _allCases = new();
        private List<MedicalCaseDetailDto> _currentPatientCases = new();
        private Guid _patientId;

        #endregion

        #region 属性

        private string _patientName = string.Empty;
        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value);
        }

        private string _searchText = string.Empty;
        /// <summary>
        /// 搜索文本（支持患者姓名、中医诊断模糊查询）
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterCases();
                }
            }
        }

        private DateTime? _startDate;
        /// <summary>
        /// 时间区间筛选 - 起始日期
        /// </summary>
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    FilterCases();
                }
            }
        }

        private DateTime? _endDate;
        /// <summary>
        /// 时间区间筛选 - 结束日期
        /// </summary>
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    FilterCases();
                }
            }
        }

        private ObservableCollection<MedicalCaseDetailDto> _filteredCases = new();
        /// <summary>
        /// 筛选后的医案列表
        /// </summary>
        public ObservableCollection<MedicalCaseDetailDto> FilteredCases
        {
            get => _filteredCases;
            set => SetProperty(ref _filteredCases, value);
        }

        private MedicalCaseDetailDto? _selectedCase;
        /// <summary>
        /// 选中的医案（左栏卡片列表）
        /// </summary>
        public MedicalCaseDetailDto? SelectedCase
        {
            get => _selectedCase;
            set
            {
                if (SetProperty(ref _selectedCase, value))
                {
                    LoadCaseDetailAsync();
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private MedicalCaseDetailDto? _selectedCaseDetail;
        /// <summary>
        /// 选中医案的详情（用于右栏MedicalCaseViewControl绑定）
        /// </summary>
        public MedicalCaseDetailDto? SelectedCaseDetail
        {
            get => _selectedCaseDetail;
            set
            {
                if (SetProperty(ref _selectedCaseDetail, value))
                {
                    RaisePropertyChanged(nameof(SelectedCaseHasConsultation));
                    RaisePropertyChanged(nameof(SelectedCaseHasPrescription));
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 选中医案是否有诊疗记录（用于MedicalCaseViewControl绑定）
        /// </summary>
        public bool SelectedCaseHasConsultation => SelectedCaseDetail?.Consultation != null;

        /// <summary>
        /// 选中医案是否有处方（用于MedicalCaseViewControl绑定）
        /// </summary>
        public bool SelectedCaseHasPrescription => SelectedCaseDetail?.Prescription != null;

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载详情
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 选中医案的处方药材列表（用于复制）
        /// </summary>
        public List<PrescriptionItemDto> SelectedPrescriptionItems { get; private set; } = new();

        // ========== UX改进：查看模式相关属性 ==========

        private bool _isShowingAllPatients;
        /// <summary>
        /// 是否显示全部患者模式（false=仅当前患者）
        /// </summary>
        public bool IsShowingAllPatients
        {
            get => _isShowingAllPatients;
            set
            {
                if (SetProperty(ref _isShowingAllPatients, value))
                {
                    // 切换模式时重新加载数据
                    if (value)
                    {
                        LoadAllPatientsAsync();
                    }
                    else
                    {
                        // 切回当前患者模式
                        _isShowingAllCurrentPatient = false;
                        RaisePropertyChanged(nameof(IsShowingAllCurrentPatient));
                        ApplyCurrentPatientFilter();
                    }
                    RaisePropertyChanged(nameof(ViewModeText));
                    RaisePropertyChanged(nameof(CanShowMoreCurrentPatient));
                    ShowMoreCurrentPatientCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isShowingAllCurrentPatient;
        /// <summary>
        /// 是否显示当前患者的全部记录（默认false只显示5条）
        /// </summary>
        public bool IsShowingAllCurrentPatient
        {
            get => _isShowingAllCurrentPatient;
            set
            {
                if (SetProperty(ref _isShowingAllCurrentPatient, value))
                {
                    ApplyCurrentPatientFilter();
                    RaisePropertyChanged(nameof(CanShowMoreCurrentPatient));
                    ShowMoreCurrentPatientCommand.RaiseCanExecuteChanged();
                }
            }
        }

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

        #region IDialogAware

        public string Title => "从历史医案复制";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
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

        #endregion

        #region 命令

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ShowMoreCurrentPatientCommand { get; }
        public DelegateCommand ToggleAllPatientsCommand { get; }

        #endregion

        #region 构造函数

        public HistoryCopyDialogViewModel(
            IMedicalCaseRepository medicalCaseRepository,
            ILogger<HistoryCopyDialogViewModel> logger)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
            ShowMoreCurrentPatientCommand = new DelegateCommand(
                () => IsShowingAllCurrentPatient = true,
                () => CanShowMoreCurrentPatient);
            ToggleAllPatientsCommand = new DelegateCommand(
                () => IsShowingAllPatients = !IsShowingAllPatients);

            _logger.LogInformation("HistoryCopyDialogViewModel已初始化");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载当前患者历史医案列表（默认模式）
        /// UX改进：只加载已完成状态的医案，默认显示5条
        /// </summary>
        private async void LoadCasesAsync()
        {
            if (_patientId == Guid.Empty)
            {
                StatusMessage = "未指定患者";
                return;
            }

            try
            {
                StatusMessage = "正在加载历史医案...";

                var cases = await _medicalCaseRepository.GetByPatientIdAsync(_patientId);
                // 按就诊时间倒序排列，只显示已完成且有处方的医案
                _currentPatientCases = cases
                    .Where(c => c.CaseStatus == MedicalCaseStatus.Completed && c.PrescriptionId.HasValue)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                // 初始状态：当前患者模式
                _isShowingAllPatients = false;
                _isShowingAllCurrentPatient = false;

                ApplyCurrentPatientFilter();

                _logger.LogInformation("加载了患者 {PatientId} 的 {Count} 条已完成历史医案", _patientId, _currentPatientCases.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "加载历史医案失败";
                _logger.LogError(ex, "加载患者历史医案失败，患者ID: {PatientId}", _patientId);
            }
        }

        /// <summary>
        /// 加载全部患者的历史医案（全局查询模式）
        /// 使用分页循环获取所有数据，遵循SystemConstants.MaxPageSize规范
        /// </summary>
        private async void LoadAllPatientsAsync()
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
                    // OpenSpec: fix-history-copy-all-patients - 使用包含所有医生数据的方法
                    var pagedResult = await _medicalCaseRepository.GetPagedIncludeAllDoctorsAsync(
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
                        _logger.LogWarning("加载全部患者历史医案已达到最大页数限制(10页)");
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

                _logger.LogInformation("加载了全部患者的 {Count} 条已完成历史医案（共{TotalPages}页）",
                    _allCases.Count, currentPage);
            }
            catch (Exception ex)
            {
                StatusMessage = "加载全部患者历史医案失败";
                _logger.LogError(ex, "加载全部患者历史医案失败");
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

            RaisePropertyChanged(nameof(CurrentPatientTotalCount));
            RaisePropertyChanged(nameof(CanShowMoreCurrentPatient));
            ShowMoreCurrentPatientCommand.RaiseCanExecuteChanged();
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
        private async void LoadCaseDetailAsync()
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
                var detail = await _medicalCaseRepository.GetByIdWithDetailsAsync(SelectedCase.Id);
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

                ConfirmCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                SelectedCaseDetail = null;
                SelectedPrescriptionItems = new List<PrescriptionItemDto>();
                _logger.LogError(ex, "加载医案详情失败，医案ID: {CaseId}", SelectedCase.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 确认复制条件：选中医案必须有处方药材
        /// </summary>
        private bool CanConfirm() => SelectedCase != null && SelectedPrescriptionItems.Any();

        /// <summary>
        /// 执行确认复制 - 仅返回药材组合
        /// </summary>
        private void ExecuteConfirm()
        {
            var parameters = new DialogParameters
            {
                { "SelectedCase", SelectedCase },
                { "SelectedItems", SelectedPrescriptionItems }
            };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}

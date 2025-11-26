using System.Collections.ObjectModel;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// Issue #2246: 历史处方复制弹窗ViewModel
    /// 用于从当前患者历史处方中选择复制药材到当前处方
    /// </summary>
    public class HistoryCopyDialogViewModel : BindableBase, IDialogAware
    {
        #region 服务依赖

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly ILogger<HistoryCopyDialogViewModel> _logger;
        private List<MedicalCaseDto> _allCases = new();
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
        /// 搜索文本
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

        private ObservableCollection<MedicalCaseDto> _filteredCases = new();
        /// <summary>
        /// 筛选后的医案列表
        /// </summary>
        public ObservableCollection<MedicalCaseDto> FilteredCases
        {
            get => _filteredCases;
            set => SetProperty(ref _filteredCases, value);
        }

        private MedicalCaseDto? _selectedCase;
        /// <summary>
        /// 选中的医案
        /// </summary>
        public MedicalCaseDto? SelectedCase
        {
            get => _selectedCase;
            set
            {
                if (SetProperty(ref _selectedCase, value))
                {
                    LoadCasePreviewAsync();
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _previewText = "请选择一个历史处方查看药材组成";
        /// <summary>
        /// 预览文本
        /// </summary>
        public string PreviewText
        {
            get => _previewText;
            set => SetProperty(ref _previewText, value);
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

        #endregion

        #region IDialogAware

        public string Title => "从历史处方复制";

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

            _logger.LogInformation("HistoryCopyDialogViewModel已初始化");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载患者历史医案列表
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
                StatusMessage = "正在加载历史处方...";

                var cases = await _medicalCaseRepository.GetByPatientIdAsync(_patientId);
                // 按就诊时间倒序排列，只显示有处方的医案
                _allCases = cases
                    .Where(c => c.PrescriptionId.HasValue)
                    .OrderByDescending(c => c.ConsultationDate)
                    .ToList();

                FilteredCases = new ObservableCollection<MedicalCaseDto>(_allCases);

                StatusMessage = $"共 {_allCases.Count} 条历史处方";
                _logger.LogInformation("加载了患者 {PatientId} 的 {Count} 条历史处方", _patientId, _allCases.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "加载历史处方失败";
                _logger.LogError(ex, "加载患者历史处方失败，患者ID: {PatientId}", _patientId);
            }
        }

        /// <summary>
        /// 筛选医案
        /// </summary>
        private void FilterCases()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredCases = new ObservableCollection<MedicalCaseDto>(_allCases);
            }
            else
            {
                var filtered = _allCases.Where(c =>
                    (c.Diagnosis?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.ChiefComplaint?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.ConsultationDate.ToString("yyyy-MM-dd").Contains(SearchText));

                FilteredCases = new ObservableCollection<MedicalCaseDto>(filtered);
            }

            StatusMessage = $"筛选结果: {FilteredCases.Count} 条历史处方";
        }

        /// <summary>
        /// 加载医案处方预览
        /// </summary>
        private async void LoadCasePreviewAsync()
        {
            if (SelectedCase == null)
            {
                PreviewText = "请选择一个历史处方查看药材组成";
                SelectedPrescriptionItems = new List<PrescriptionItemDto>();
                return;
            }

            try
            {
                // 获取医案详情（包含处方信息）
                var detail = await _medicalCaseRepository.GetByIdWithDetailsAsync(SelectedCase.Id);
                if (detail?.Prescription?.Items != null && detail.Prescription.Items.Any())
                {
                    SelectedPrescriptionItems = detail.Prescription.Items;
                    PreviewText = string.Join(", ", detail.Prescription.Items.Select(h =>
                        $"{h.HerbName}{h.Quantity}{h.Unit}"));
                }
                else
                {
                    SelectedPrescriptionItems = new List<PrescriptionItemDto>();
                    PreviewText = "该历史处方暂无药材记录";
                }
            }
            catch (Exception ex)
            {
                PreviewText = "加载处方预览失败";
                _logger.LogError(ex, "加载医案处方预览失败，医案ID: {CaseId}", SelectedCase.Id);
            }
        }

        private bool CanConfirm() => SelectedCase != null && SelectedPrescriptionItems.Any();

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

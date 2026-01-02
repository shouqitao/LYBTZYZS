using System.Collections.ObjectModel;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 历史处方选择对话框ViewModel
    /// Epic #2175 BF-002 Task 3.9 - 实现历史处方导入对话框
    /// </summary>
    public class HistoryPrescriptionSelectionDialogViewModel : ViewModelBase, IDialogAware
    {
        #region 字段

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private bool _isLoading;
        private PrescriptionDetailDto? _selectedPrescription;
        private ObservableCollection<PrescriptionDetailDto> _prescriptions = new();
        private Guid _patientId;

        #endregion

        #region 构造函数

        public HistoryPrescriptionSelectionDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IMedicalCaseRepository medicalCaseRepository)
            : base(eventAggregator, loggerFactory)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));

            // 初始化Commands
            ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #endregion

        #region 属性

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<PrescriptionDetailDto> Prescriptions
        {
            get => _prescriptions;
            private set => SetProperty(ref _prescriptions, value);
        }

        public PrescriptionDetailDto? SelectedPrescription
        {
            get => _selectedPrescription;
            set
            {
                if (SetProperty(ref _selectedPrescription, value))
                {
                    ConfirmCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Commands

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region Command实现

        private bool CanExecuteConfirm()
        {
            return SelectedPrescription != null;
        }

        private void ExecuteConfirm()
        {
            // 返回选中的历史处方
            var parameters = new DialogParameters
            {
                { "SelectedPrescription", SelectedPrescription }
            };

            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        #region 业务方法

        private async Task LoadPrescriptionsAsync()
        {
            try
            {
                IsLoading = true;
                Prescriptions.Clear();

                Logger.LogDebug("开始加载患者历史处方: PatientId={PatientId}", _patientId);

                // OpenSpec: consolidate-medicalcase-detail-queries - 使用QueryAsync替代废弃的GetByPatientIdAsync
                var query = new MedicalCaseQueryDto
                {
                    QueryType = LYBT.Shared.Models.Enums.MedicalCaseQueryType.ByPatient,
                    PatientId = _patientId,
                    PageSize = 100 // 获取足够多的历史记录
                };
                var allMedicalCases = await _medicalCaseRepository.QueryAsync(query);

                // 筛选已完成的医案ID列表
                var completedCaseIds = allMedicalCases?.Items?
                    .Where(mc => mc.CaseStatus == LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed)
                    .Select(mc => mc.Id)
                    .ToList();

                if (completedCaseIds != null && completedCaseIds.Count > 0)
                {
                    // OpenSpec: consolidate-medicalcase-detail-queries
                    // 使用批量查询替代N+1循环查询，单次请求获取所有医案详情
                    var medicalCaseDetails = await _medicalCaseRepository.GetBatchDetailsAsync(completedCaseIds);

                    // 提取所有有处方的医案
                    foreach (var detail in medicalCaseDetails)
                    {
                        if (detail?.Prescription != null && detail.Prescription.Items?.Count > 0)
                        {
                            Prescriptions.Add(detail.Prescription);
                        }
                    }

                    Logger.LogInformation("成功加载 {Count} 个历史处方", Prescriptions.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者历史处方失败");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region IDialogAware实现

        public string Title => "选择历史处方";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            Logger.LogDebug("历史处方选择对话框关闭");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Logger.LogDebug("历史处方选择对话框打开");

            // 从参数中获取PatientId
            if (parameters.TryGetValue<Guid>("PatientId", out var patientId))
            {
                _patientId = patientId;
                _ = LoadPrescriptionsAsync();
            }
            else
            {
                Logger.LogWarning("历史处方对话框缺少PatientId参数");
            }
        }

        #endregion
    }
}

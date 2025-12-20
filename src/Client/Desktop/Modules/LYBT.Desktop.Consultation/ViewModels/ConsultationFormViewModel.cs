using LYBT.Desktop.Consultation.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.ViewModels
{
    /// <summary>
    /// 诊断表单ViewModel - 填写诊断信息
    /// OpenSpec: simplify-medicalcase-api - 使用IMedicalCaseDataManager聚合根管理器
    /// </summary>
    public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable
    {
        private readonly IMedicalCaseDataManager _dataManager;
        private readonly ConsultationCommandHandler _commandHandler;

        private PatientDetailDto? _currentPatient;
        private Guid _medicalCaseId = Guid.Empty;
        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint, TreatmentPrinciple, FourDiagnosis
        private string _presentIllness = string.Empty;
        private string _tcmDiagnosis = string.Empty;
        private string _tongueDiagnosis = string.Empty;
        private string _pulseDiagnosis = string.Empty;
        private string _remark = string.Empty;
        private bool _prescriptionEnabled = true;
        private string _validationMessage = string.Empty;

        public PatientDetailDto? CurrentPatient { get => _currentPatient; set => SetProperty(ref _currentPatient, value); }
        public Guid MedicalCaseId { get => _medicalCaseId; set => SetProperty(ref _medicalCaseId, value); }

        // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        public string PresentIllness { get => _presentIllness; set => SetProperty(ref _presentIllness, value); }
        public string TCMDiagnosis { get => _tcmDiagnosis; set { if (SetProperty(ref _tcmDiagnosis, value)) RaisePropertyChanged(nameof(HasTCMDiagnosis)); } }
        public string TongueDiagnosis { get => _tongueDiagnosis; set => SetProperty(ref _tongueDiagnosis, value); }
        public string PulseDiagnosis { get => _pulseDiagnosis; set => SetProperty(ref _pulseDiagnosis, value); }
        public string Remark { get => _remark; set => SetProperty(ref _remark, value); }

        public bool PrescriptionEnabled { get => _prescriptionEnabled; set { if (SetProperty(ref _prescriptionEnabled, value)) RaisePropertyChanged(nameof(PrescriptionDisabled)); } }
        public bool PrescriptionDisabled { get => !_prescriptionEnabled; set { if (value) PrescriptionEnabled = false; } }

        public bool HasTCMDiagnosis => !string.IsNullOrWhiteSpace(TCMDiagnosis);
        public string ValidationMessage { get => _validationMessage; private set => SetProperty(ref _validationMessage, value); }

        public DelegateCommand ClearFormCommand { get; }
        public DelegateCommand ShowOtherCasesQueryCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }

        public ConsultationFormViewModel(
            IMedicalCaseDataManager dataManager,
            ConsultationCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            ClearFormCommand = new DelegateCommand(ExecuteClearForm);
            ShowOtherCasesQueryCommand = new DelegateCommand(() => _ = ShowSuccessMessageAsync("其他病案查询功能将在Phase 3实现"));
            SaveDraftCommand = new DelegateCommand(async () => await ExecuteSaveDraft());
        }

        public bool Validate()
        {
            // OpenSpec: refactor-diagnosis-fields - 仅验证TCMDiagnosis必填
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(TCMDiagnosis)) errors.Add("中医诊断不能为空");
            if (errors.Any()) { ValidationMessage = string.Join("；", errors); return false; }
            ValidationMessage = string.Empty; return true;
        }

        /// <summary>
        /// 保存诊断数据
        /// OpenSpec: simplify-medicalcase-api - 通过聚合根保存
        /// </summary>
        private async Task<bool> SaveAsync()
        {
            try
            {
                var (isValid, errorMessage) = ValidatePrerequisites();
                if (!isValid) { ValidationMessage = errorMessage; return false; }
                SyncToDataManager();
                var success = await _dataManager.SaveAsync();
                if (success) return true;
                ValidationMessage = "保存失败，请检查数据有效性"; return false;
            }
            catch (Exception ex) { ValidationMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex); return false; }
        }

        private (bool isValid, string errorMessage) ValidatePrerequisites()
        {
            if (MedicalCaseId == Guid.Empty) return (false, "医案ID为空，无法保存诊断数据");
            if (CurrentPatient == null) return (false, "患者信息丢失，无法保存诊断数据");
            if (SessionManager?.CurrentUser == null) return (false, "用户信息丢失，无法保存诊断数据");
            return (true, string.Empty);
        }

        private void ExecuteClearForm()
        {
            try { _commandHandler.ClearForm(); SyncFromDataManager(); ValidationMessage = string.Empty; }
            catch (Exception ex) { Logger.LogError(ex, "清空表单失败"); }
        }

        private async Task ExecuteSaveDraft()
        {
            try
            {
                SetIsBusy(true, "正在保存草稿...");
                var saved = await SaveAsync();
                if (saved) await ShowSuccessMessageAsync("诊断草稿已保存！");
                else await ShowErrorMessageAsync($"保存草稿失败：{ValidationMessage}");
            }
            catch (Exception ex) { await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存草稿", ex)); }
            finally { SetIsBusy(false); }
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            try
            {
                var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
                if (medicalCaseId != Guid.Empty)
                {
                    MedicalCaseId = medicalCaseId;
                    // OpenSpec: simplify-medicalcase-api - 通过聚合根初始化
                    await _dataManager.InitializeAsync(medicalCaseId);
                    if (_dataManager.CurrentConsultation != null) SyncFromDataManager();
                }
                var currentPatient = navigationContext.Parameters.GetValue<PatientDetailDto>("CurrentPatient");
                if (currentPatient != null) CurrentPatient = currentPatient;
            }
            catch (Exception ex) { Logger.LogError(ex, "导航到ConsultationFormView时发生异常"); }
        }

        /// <summary>
        /// 从聚合根DataManager同步数据到ViewModel
        /// OpenSpec: simplify-medicalcase-api
        /// </summary>
        private void SyncFromDataManager()
        {
            var consultation = _dataManager.CurrentConsultation;
            if (consultation == null) return;
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            PresentIllness = consultation.PresentIllness ?? string.Empty;
            TongueDiagnosis = consultation.TongueDiagnosis ?? string.Empty;
            PulseDiagnosis = consultation.PulseDiagnosis ?? string.Empty;
            TCMDiagnosis = consultation.TCMDiagnosis ?? string.Empty;
        }

        /// <summary>
        /// 从ViewModel同步数据到聚合根DataManager
        /// OpenSpec: simplify-medicalcase-api - 直接修改CurrentConsultation属性
        /// </summary>
        private void SyncToDataManager()
        {
            var consultation = _dataManager.CurrentConsultation;
            if (consultation == null) return;
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            consultation.PresentIllness = PresentIllness;
            consultation.TongueDiagnosis = TongueDiagnosis;
            consultation.PulseDiagnosis = PulseDiagnosis;
            consultation.TCMDiagnosis = TCMDiagnosis;
        }
    }
}

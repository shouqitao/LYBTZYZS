using LYBT.Desktop.Consultation.Components;
using LYBT.Desktop.Infrastructure.Interfaces;
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
    /// <summary>诊断表单ViewModel - 填写诊断信息</summary>
    public class ConsultationFormViewModel : UnifiedViewModelBase, IValidatable, ISaveable
    {
        private readonly ConsultationDataManager _dataManager;
        private readonly ConsultationCommandHandler _commandHandler;

        private PatientDto? _currentPatient;
        private Guid _medicalCaseId = Guid.Empty;
        private string _chiefComplaint = string.Empty;
        private string _presentIllness = string.Empty;
        private string _tcmDiagnosis = string.Empty;
        private string _treatmentPrinciple = string.Empty;
        private string _inspection = string.Empty;
        private string _auscultationOlfaction = string.Empty;
        private string _inquiry = string.Empty;
        private string _palpation = string.Empty;
        private string _remark = string.Empty;
        private bool _prescriptionEnabled = true;
        private string _validationMessage = string.Empty;

        public PatientDto? CurrentPatient { get => _currentPatient; set => SetProperty(ref _currentPatient, value); }
        public Guid MedicalCaseId { get => _medicalCaseId; set => SetProperty(ref _medicalCaseId, value); }

        public string ChiefComplaint { get => _chiefComplaint; set { if (SetProperty(ref _chiefComplaint, value)) RaisePropertyChanged(nameof(HasChiefComplaint)); } }
        public string PresentIllness { get => _presentIllness; set => SetProperty(ref _presentIllness, value); }
        public string TCMDiagnosis { get => _tcmDiagnosis; set { if (SetProperty(ref _tcmDiagnosis, value)) RaisePropertyChanged(nameof(HasTCMDiagnosis)); } }
        public string TreatmentPrinciple { get => _treatmentPrinciple; set => SetProperty(ref _treatmentPrinciple, value); }
        public string Inspection { get => _inspection; set => SetProperty(ref _inspection, value); }
        public string AuscultationOlfaction { get => _auscultationOlfaction; set => SetProperty(ref _auscultationOlfaction, value); }
        public string Inquiry { get => _inquiry; set => SetProperty(ref _inquiry, value); }
        public string Palpation { get => _palpation; set => SetProperty(ref _palpation, value); }
        public string Remark { get => _remark; set => SetProperty(ref _remark, value); }

        public bool PrescriptionEnabled { get => _prescriptionEnabled; set { if (SetProperty(ref _prescriptionEnabled, value)) RaisePropertyChanged(nameof(PrescriptionDisabled)); } }
        public bool PrescriptionDisabled { get => !_prescriptionEnabled; set { if (value) PrescriptionEnabled = false; } }

        public bool HasChiefComplaint => !string.IsNullOrWhiteSpace(ChiefComplaint);
        public bool HasTCMDiagnosis => !string.IsNullOrWhiteSpace(TCMDiagnosis);
        public string ValidationMessage { get => _validationMessage; private set => SetProperty(ref _validationMessage, value); }

        public DelegateCommand ClearFormCommand { get; }
        public DelegateCommand ShowOtherCasesQueryCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }

        public ConsultationFormViewModel(
            ConsultationDataManager dataManager,
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
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(ChiefComplaint)) errors.Add("主诉不能为空");
            if (string.IsNullOrWhiteSpace(TCMDiagnosis)) errors.Add("中医诊断不能为空");
            if (errors.Any()) { ValidationMessage = string.Join("；", errors); return false; }
            ValidationMessage = string.Empty; return true;
        }

        public async Task<bool> SaveAsync()
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
            catch (Exception ex) { ValidationMessage = $"保存失败：{ex.Message}"; return false; }
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
            catch (Exception ex) { await ShowErrorMessageAsync($"保存草稿失败：{ex.Message}"); }
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
                    _dataManager.MedicalCaseId = medicalCaseId;
                    await _dataManager.InitializeAsync(medicalCaseId);
                    if (_dataManager.Current != null) SyncFromDataManager();
                }
                var currentPatient = navigationContext.Parameters.GetValue<PatientDto>("CurrentPatient");
                if (currentPatient != null) CurrentPatient = currentPatient;
            }
            catch (Exception ex) { Logger.LogError(ex, "导航到ConsultationFormView时发生异常"); }
        }

        private void SyncFromDataManager()
        {
            if (_dataManager.Current == null) return;
            var c = _dataManager.Current;
            ChiefComplaint = c.ChiefComplaint ?? string.Empty; PresentIllness = c.PresentIllness ?? string.Empty;
            Inspection = c.Inspection ?? string.Empty; AuscultationOlfaction = c.AuscultationOlfaction ?? string.Empty;
            Inquiry = c.Inquiry ?? string.Empty; Palpation = c.Palpation ?? string.Empty;
            TCMDiagnosis = c.TCMDiagnosis ?? string.Empty; TreatmentPrinciple = c.TreatmentPrinciple ?? string.Empty;
            Remark = c.Remark ?? string.Empty;
        }

        private void SyncToDataManager()
        {
            _dataManager.UpdateField(nameof(ConsultationDto.ChiefComplaint), ChiefComplaint);
            _dataManager.UpdateField(nameof(ConsultationDto.PresentIllness), PresentIllness);
            _dataManager.UpdateField(nameof(ConsultationDto.Inspection), Inspection);
            _dataManager.UpdateField(nameof(ConsultationDto.AuscultationOlfaction), AuscultationOlfaction);
            _dataManager.UpdateField(nameof(ConsultationDto.Inquiry), Inquiry);
            _dataManager.UpdateField(nameof(ConsultationDto.Palpation), Palpation);
            _dataManager.UpdateField(nameof(ConsultationDto.TCMDiagnosis), TCMDiagnosis);
            _dataManager.UpdateField(nameof(ConsultationDto.TreatmentPrinciple), TreatmentPrinciple);
            _dataManager.UpdateField(nameof(ConsultationDto.Remark), Remark);
        }
    }
}

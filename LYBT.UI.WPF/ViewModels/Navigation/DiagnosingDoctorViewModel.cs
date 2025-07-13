using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.Module.Queueing.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 医生诊疗页面视图模型，负责管理候诊队列和历史诊疗记录
    /// </summary>
    public class DiagnosingDoctorViewModel : BindableBase {
        public ObservableCollection<QueueingDto> QueuedPatients { get; } = new();
        public ObservableCollection<DiagnosisTreatmentDto> DiagnosisHistory { get; } = new();
        public ObservableCollection<DiagnosisTreatmentDto> PrescriptionHistory { get; } = new();
        public ObservableCollection<DiagnosisTreatmentDto> AuxiliaryTreatmentHistory { get; } = new();

        private QueueingDto? _selectedPatient;
        public QueueingDto? SelectedPatient {
            get => _selectedPatient;
            set {
                if (SetProperty(ref _selectedPatient, value))
                    _ = LoadCurrentPatientAsync();
            }
        }

        private string _currentPatientName = string.Empty;
        public string CurrentPatientName {
            get => _currentPatientName;
            set => SetProperty(ref _currentPatientName, value);
        }

        private DiagnosisTreatmentDto? _selectedDiagnosisHistory;
        public DiagnosisTreatmentDto? SelectedDiagnosisHistory {
            get => _selectedDiagnosisHistory;
            set => SetProperty(ref _selectedDiagnosisHistory, value);
        }

        private DiagnosisTreatmentDto? _selectedPrescriptionHistory;
        public DiagnosisTreatmentDto? SelectedPrescriptionHistory {
            get => _selectedPrescriptionHistory;
            set => SetProperty(ref _selectedPrescriptionHistory, value);
        }

        private DiagnosisTreatmentDto? _selectedAuxiliaryTreatmentHistory;
        public DiagnosisTreatmentDto? SelectedAuxiliaryTreatmentHistory {
            get => _selectedAuxiliaryTreatmentHistory;
            set => SetProperty(ref _selectedAuxiliaryTreatmentHistory, value);
        }

        public DelegateCommand RefreshQueueCommand { get; }
        public DelegateCommand SelectPatientCommand { get; }
        public DelegateCommand EmergencyPatientCommand { get; }
        public DelegateCommand SuspendPatientCommand { get; }
        public DelegateCommand HoldPatientCommand { get; }
        public DelegateCommand CompleteConsultationCommand { get; }

        private readonly IQueueingService _queueingService;
        private readonly IDiagnosisTreatmentService _diagnosisService;

        public DiagnosingDoctorViewModel(IQueueingService queueingService, IDiagnosisTreatmentService diagnosisService) {
            _queueingService = queueingService;
            _diagnosisService = diagnosisService;

            RefreshQueueCommand = new DelegateCommand(async () => await LoadQueueAsync());
            SelectPatientCommand = new DelegateCommand(async () => await LoadCurrentPatientAsync(), () => SelectedPatient != null)
                .ObservesProperty(() => SelectedPatient);
            EmergencyPatientCommand = new DelegateCommand(async () => await LoadQueueAsync());
            SuspendPatientCommand = new DelegateCommand(async () => await SuspendAsync(), () => SelectedPatient != null)
                .ObservesProperty(() => SelectedPatient);
            HoldPatientCommand = new DelegateCommand(async () => await HoldAsync(), () => SelectedPatient != null)
                .ObservesProperty(() => SelectedPatient);
            CompleteConsultationCommand = new DelegateCommand(async () => await CompleteAsync(), () => SelectedPatient != null)
                .ObservesProperty(() => SelectedPatient);

            _ = LoadQueueAsync();
        }

        private async Task LoadQueueAsync() {
            var list = await _queueingService.GetListAsync();
            QueuedPatients.Clear();
            foreach (var item in list)
                QueuedPatients.Add(item);
        }

        private async Task LoadCurrentPatientAsync() {
            DiagnosisHistory.Clear();
            PrescriptionHistory.Clear();
            AuxiliaryTreatmentHistory.Clear();
            if (SelectedPatient == null)
                return;
            var detail = await _queueingService.GetByIdAsync(SelectedPatient.Id);
            CurrentPatientName = detail?.PatientName ?? SelectedPatient.PatientName;
            var records = await _diagnosisService.GetListAsync();
            foreach (var r in records.Where(r => r.PatientName == CurrentPatientName)) {
                DiagnosisHistory.Add(r);
                PrescriptionHistory.Add(r);
                AuxiliaryTreatmentHistory.Add(r);
            }
        }

        private Task SuspendAsync() => LoadQueueAsync();

        private async Task HoldAsync() {
            if (SelectedPatient != null) {
                await _queueingService.DeleteAsync(SelectedPatient.Id);
                await LoadQueueAsync();
            }
        }

        private async Task CompleteAsync() {
            if (SelectedPatient != null) {
                await _queueingService.DeleteAsync(SelectedPatient.Id);
                await LoadQueueAsync();
                SelectedPatient = null;
                CurrentPatientName = string.Empty;
                DiagnosisHistory.Clear();
                PrescriptionHistory.Clear();
                AuxiliaryTreatmentHistory.Clear();
            }
        }
    }
}

using LYBT.Module.Patients.Dtos;
using LYBT.Module.Registration.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 类 RegistrationStaffViewModel 的说明
    /// </summary>
    public class RegistrationStaffViewModel : BindableBase {
        private readonly IRegistrationService _registrationService;
        private readonly IPatientService _patientService;

        public ObservableCollection<PatientDetailDto> PatientSearchResults { get; } = new();
        public ObservableCollection<PendingPatientItem> PendingPatients { get; } = new();

        private PendingPatientItem? _selectedPendingPatient;
        public PendingPatientItem? SelectedPendingPatient {
            get => _selectedPendingPatient;
            set => SetProperty(ref _selectedPendingPatient, value);
        }

        private PatientDetailDto? _selectedPatient;
        public PatientDetailDto? SelectedPatient {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string _pendingPatientsInfo = "暂无待看诊患者";
        public string PendingPatientsInfo {
            get => _pendingPatientsInfo;
            set => SetProperty(ref _pendingPatientsInfo, value);
        }

        public DelegateCommand ReadCardCommand { get; }
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand NewPatientCommand { get; }
        public DelegateCommand RegisterCommand { get; }
        public DelegateCommand ClearCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public RegistrationStaffViewModel(IRegistrationService registrationService, IPatientService patientService) {
            _registrationService = registrationService;
            _patientService = patientService;

            ReadCardCommand = new DelegateCommand(() => MessageBox.Show("读卡功能待实现", "提示"));
            NewPatientCommand = new DelegateCommand(() => MessageBox.Show("新建患者功能待实现", "提示"));
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RegisterCommand = new DelegateCommand(async () => await RegisterAsync(), () => SelectedPatient != null)
                .ObservesProperty(() => SelectedPatient);
            ClearCommand = new DelegateCommand(Clear);
            CancelCommand = new DelegateCommand(async () => await CancelAsync(), () => SelectedPendingPatient != null)
                .ObservesProperty(() => SelectedPendingPatient);
        }

        private async Task SearchAsync() {
            var list = string.IsNullOrWhiteSpace(SearchKeyword)
                ? await _patientService.GetAllAsync()
                : await _patientService.SearchAsync(SearchKeyword);
            PatientSearchResults.Clear();
            foreach (var p in list)
                PatientSearchResults.Add(p);
        }

        private async Task RegisterAsync() {
            if (SelectedPatient == null)
                return;

            var dto = new RegistrationCreateDto {
                PatientId = SelectedPatient.Id.ToString(),
                DoctorId = Guid.Empty.ToString(),
                RegistrationType = "普通"
            };

            var regId = await _registrationService.AddAsync(dto);
            if (regId != null) {
                PendingPatients.Add(new PendingPatientItem { QueueNumber = PendingPatients.Count + 1, Name = SelectedPatient.Name, RegistrationId = regId.Value });
                PendingPatientsInfo = string.Empty;
                SelectedPatient = null;
            } else {
                MessageBox.Show("挂号失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear() {
            SearchKeyword = string.Empty;
            SelectedPatient = null;
        }

        private async Task CancelAsync() {
            if (SelectedPendingPatient == null)
                return;
            bool ok = await _registrationService.CancelAsync(SelectedPendingPatient.RegistrationId);
            if (ok) {
                PendingPatients.Remove(SelectedPendingPatient);
                SelectedPendingPatient = null;
                if (PendingPatients.Count == 0)
                    PendingPatientsInfo = "暂无待看诊患者";
            } else {
                MessageBox.Show("取消挂号失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class PendingPatientItem {
            public int QueueNumber { get; set; }
            public string Name { get; set; } = string.Empty;
            public Guid RegistrationId { get; set; }
        }
    }
}

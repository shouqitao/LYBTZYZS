using LYBT.Module.Patients.Dtos;
using LYBT.Module.Registration.Dtos;
using LYBT.Common.Enums;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.UI.WPF.ViewModels.Main;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 类 RegistrationStaffViewModel 的说明
    /// </summary>
    public class RegistrationStaffViewModel : BindableBase {
        private readonly IRegistrationService _registrationService;
        private readonly IPatientService _patientService;

        public ObservableCollection<PatientDetailDto> PatientSearchResults { get; } = new();
        public ObservableCollection<RegistrationDto> PendingPatients { get; } = new();

        private RegistrationDto? _selectedPendingPatient;
        public RegistrationDto? SelectedPendingPatient {
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
            NewPatientCommand = new DelegateCommand(NewPatient);
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            RegisterCommand = new DelegateCommand(async () => await RegisterAsync(), () => SelectedPatient != null)
                .ObservesProperty(() => SelectedPatient);
            ClearCommand = new DelegateCommand(Clear);
            CancelCommand = new DelegateCommand(async () => await CancelAsync(), () => SelectedPendingPatient != null)
                .ObservesProperty(() => SelectedPendingPatient);

            _ = LoadPendingAsync();
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
                await LoadPendingAsync();
                SelectedPatient = null;
            } else {
                MessageBox.Show("挂号失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewPatient() {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                main.ShowPatientProfileCommand.Execute();
            }
        }

        private void Clear() {
            SearchKeyword = string.Empty;
            SelectedPatient = null;
        }

        private async Task CancelAsync() {
            if (SelectedPendingPatient == null)
                return;
            bool ok = await _registrationService.CancelAsync(SelectedPendingPatient.Id);
            if (ok) {
                await LoadPendingAsync();
            } else {
                MessageBox.Show("取消挂号失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPendingAsync() {
            var list = await _registrationService.GetByStatusAsync(RegistrationStatus.Pending);
            PendingPatients.Clear();
            foreach (var item in list)
                PendingPatients.Add(item);
            PendingPatientsInfo = PendingPatients.Count == 0 ? "暂无待看诊患者" : string.Empty;
        }
    }
}

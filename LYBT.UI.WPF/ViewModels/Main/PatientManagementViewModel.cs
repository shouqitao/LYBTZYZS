using LYBT.Module.Patients.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class PatientManagementViewModel : BindableBase {
        private readonly IPatientService _patientService;
        public ObservableCollection<PatientDetailDto> Patients { get; } = new();

        private PatientDetailDto? _selectedPatient;
        public PatientDetailDto? SelectedPatient {
            get => _selectedPatient;
            set {
                if (SetProperty(ref _selectedPatient, value)) {
                    if (value != null)
                        _ = PatientProfileViewModel.LoadAsync(value.Id);
                }
            }
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public DelegateCommand SearchCommand { get; }

        public PatientProfileViewModel PatientProfileViewModel { get; }

        public PatientManagementViewModel(IPatientService patientService) {
            _patientService = patientService;
            PatientProfileViewModel = new PatientProfileViewModel(patientService);
            SearchCommand = new DelegateCommand(async () => await LoadPatients());
            _ = LoadPatients();
        }

        private async Task LoadPatients() {
            var list = string.IsNullOrWhiteSpace(SearchKeyword)
                ? await _patientService.GetAllAsync()
                : await _patientService.SearchAsync(SearchKeyword);
            Patients.Clear();
            foreach (var p in list)
                Patients.Add(p);
        }
    }
}

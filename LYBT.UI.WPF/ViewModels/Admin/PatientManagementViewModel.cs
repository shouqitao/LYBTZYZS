using LYBT.Module.Patients.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;
using LYBT.Common.Models;
using LYBT.UI.WPF.ViewModels;

namespace LYBT.UI.WPF.ViewModels.Admin {
    public class PatientManagementViewModel : BaseListViewModel<PatientDetailDto> {
        private readonly IPatientService _patientService;
        public ObservableCollection<PatientDetailDto> Patients => Items;

        private PatientDetailDto? _selectedPatient;
        public PatientDetailDto? SelectedPatient {
            get => _selectedPatient;
            set {
                if (SetProperty(ref _selectedPatient, value)) {
                    if (value != null)
                        _ = LoadProfileAsync(value.Id);
                }
            }
        }

        private string _searchKeyword = string.Empty;
        public string SearchKeyword {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        public PatientProfileViewModel PatientProfileViewModel { get; }

        public DelegateCommand SearchCommand { get; }

        public PatientManagementViewModel(IPatientService patientService, PatientProfileViewModel profileViewModel) {
            _patientService = patientService;
            PatientProfileViewModel = profileViewModel;
            SearchCommand = new DelegateCommand(async () => await LoadPageAsync(1));
            _ = LoadPageAsync();
        }

        protected override async Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(int page, int pageSize) {
            var query = new PatientPagedQueryDto { Keyword = SearchKeyword, Page = page, PageSize = pageSize };
            return await _patientService.GetPagedAsync(query);
        }

        private async Task LoadProfileAsync(Guid id) {
            await PatientProfileViewModel.LoadAsync(id, ProfileMode.View);
        }

    }
}

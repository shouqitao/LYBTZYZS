using LYBT.Module.Patients.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class PatientProfileViewModel : BindableBase {
        private readonly IPatientService _patientService;

        private PatientDetailDto _patient = new();
        public PatientDetailDto Patient { get => _patient; set => SetProperty(ref _patient, value); }

        private string _editModeTitle = "新增患者档案";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        private bool _isEditable;
        public bool IsEditable { get => _isEditable; set => SetProperty(ref _isEditable, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action? CancelAction { get; set; }

        public PatientProfileViewModel(IPatientService patientService) {
            _patientService = patientService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync(Guid patientId) {
            var info = await _patientService.GetByIdAsync(patientId);
            if (info != null) {
                Patient = info;
                EditModeTitle = "编辑患者档案";
            } else {
                Patient = new PatientDetailDto();
                EditModeTitle = "新增患者档案";
            }
        }

        private async Task SaveAsync() {
            if (Patient.Id == Guid.Empty)
                await _patientService.AddAsync(Patient);
            else
                await _patientService.UpdateAsync(Patient);
            IsEditable = false;
        }

        private void Cancel() {
            CancelAction?.Invoke();
        }
    }
}

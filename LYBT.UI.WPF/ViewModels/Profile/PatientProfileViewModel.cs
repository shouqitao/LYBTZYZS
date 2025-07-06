using LYBT.Module.Patients.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    public class PatientProfileViewModel : BindableBase {
        private readonly IPatientService _patientService;

        private PatientDetailDto _patient = new();
        public PatientDetailDto Patient {
            get => _patient;
            set => SetProperty(ref _patient, value);
        }

        private string _editModeTitle = "新增患者档案";
        public string EditModeTitle {
            get => _editModeTitle;
            set => SetProperty(ref _editModeTitle, value);
        }

        private bool _isEditable;
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action? CancelAction { get; set; }

        public PatientProfileViewModel(IPatientService patientService) {
            _patientService = patientService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync(Guid patientId) {
            if (patientId != Guid.Empty) {
                var info = await _patientService.GetByIdAsync(patientId);
                if (info != null) {
                    Patient = info;
                    EditModeTitle = "编辑患者档案";
                } else {
                    Patient = new PatientDetailDto();
                    EditModeTitle = "新增患者档案";
                }
            } else {
                Patient = new PatientDetailDto();
                EditModeTitle = "新增患者档案";
            }
            IsEditable = true;
        }

        private async Task SaveAsync() {
            bool ok;
            if (Patient.Id == Guid.Empty)
                ok = await _patientService.AddAsync(Patient);
            else
                ok = await _patientService.UpdateAsync(Patient);

            if (ok) {
                MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                IsEditable = false;
            } else {
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel() {
            CancelAction?.Invoke();
        }
    }
}

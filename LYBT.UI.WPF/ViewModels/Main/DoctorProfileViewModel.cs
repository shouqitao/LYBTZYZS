using LYBT.Common.Enums;
using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class DoctorProfileViewModel : BindableBase, INavigationAware {
        private readonly IDoctorService _doctorService;
        private readonly IAuthService _authService;

        private DoctorDetailDto _doctor = new();
        public DoctorDetailDto Doctor { get => _doctor; set => SetProperty(ref _doctor, value); }

        private string _editModeTitle = "新增医生档案";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public DoctorProfileViewModel(IDoctorService doctorService, IAuthService authService) {
            _doctorService = doctorService;
            _authService = authService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync() {
            var info = await _doctorService.GetByUserIdAsync(_authService.UserId);
            if (info != null) {
                Doctor = info;
                EditModeTitle = "编辑医生档案";
            } else {
                Doctor = new DoctorDetailDto {
                    Gender = Gender.Unknown,
                    Birthday = DateTime.Now,
                    Title = DoctorTitle.Junior,
                    Status = DoctorStatus.Active
                };
                EditModeTitle = "新增医生档案";
            }
        }

        private async Task SaveAsync() {
            bool ok;
            if (Doctor.Id == Guid.Empty) {
                var dto = new DoctorCreateDto {
                    Name = Doctor.Name,
                    Gender = Doctor.Gender,
                    Birthday = Doctor.Birthday,
                    Phone = Doctor.Phone,
                    PinyinCode = Doctor.PinyinCode,
                    LicenseNumber = Doctor.LicenseNumber,
                    Title = Doctor.Title,
                    Status = Doctor.Status,
                    Remark = Doctor.Remark,
                    Password = "123456"
                };
                ok = await _doctorService.AddAsync(dto);
            } else {
                var dto = new DoctorEditDto {
                    Id = Doctor.Id,
                    Name = Doctor.Name,
                    Gender = Doctor.Gender,
                    Birthday = Doctor.Birthday,
                    Phone = Doctor.Phone,
                    PinyinCode = Doctor.PinyinCode,
                    LicenseNumber = Doctor.LicenseNumber,
                    Title = Doctor.Title,
                    Status = Doctor.Status,
                    Remark = Doctor.Remark
                };
                ok = await _doctorService.UpdateAsync(dto);
            }

            if (ok) {
                MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                    main.IsMainVisible = true;
                    main.IsFunctionVisible = false;
                    main.HasDoctorProfile = true;
                    main.DoctorProfileButtonText = "编辑医生档案";
                }
            } else {
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel() {
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                main.IsMainVisible = true;
                main.IsFunctionVisible = false;
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            _ = LoadAsync();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}


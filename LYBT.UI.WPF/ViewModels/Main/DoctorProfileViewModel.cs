using LYBT.Common.Enums;
using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Services;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    public class DoctorProfileViewModel : BindableBase, INavigationAware {
        private readonly IDoctorService _doctorService;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        private DoctorDetailDto _doctor = new();
        public DoctorDetailDto Doctor { get => _doctor; set => SetProperty(ref _doctor, value); }

        private string _editModeTitle = "新增医生档案";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public DoctorProfileViewModel(IDoctorService doctorService, IAuthService authService, IUserService userService) {
            _doctorService = doctorService;
            _authService = authService;
            _userService = userService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync() {
            var info = await _doctorService.GetByUserIdAsync(_authService.UserId);
            if (info != null) {
                Doctor = info;
                EditModeTitle = "编辑医生档案";
            } else {
                // 从User信息自动填充
                var currentUser = await _userService.GetByIdAsync(_authService.UserId);
                Doctor = new DoctorDetailDto {
                    UserId = _authService.UserId,
                    Birthday = DateTime.Now.AddYears(-30),
                    Title = DoctorTitle.Junior,
                    Status = DoctorStatus.Active,
                    WorkStatus = DoctorWorkStatus.Clinic,
                    PinyinCode = string.Empty,
                    LicenseNumber = string.Empty,
                    Specialty = string.Empty,
                    Remark = string.Empty
                };
                EditModeTitle = "新增医生档案";
            }
        }

        private async Task SaveAsync() {
            bool ok;
            if (Doctor.Id == Guid.Empty) {
                var dto = new DoctorCreateDto {
                    UserId = Doctor.UserId,
                    Birthday = Doctor.Birthday,
                    Title = Doctor.Title,
                    LicenseNumber = Doctor.LicenseNumber,
                    Specialty = Doctor.Specialty,
                    Status = Doctor.Status,
                    WorkStatus = Doctor.WorkStatus,
                    PinyinCode = Doctor.PinyinCode,
                    Remark = Doctor.Remark
                };
                ok = await _doctorService.AddAsync(dto);
            } else {
                var dto = new DoctorEditDto {
                    Id = Doctor.Id,
                    UserId = Doctor.UserId,
                    Birthday = Doctor.Birthday,
                    Title = Doctor.Title,
                    LicenseNumber = Doctor.LicenseNumber,
                    Specialty = Doctor.Specialty,
                    Status = Doctor.Status,
                    WorkStatus = Doctor.WorkStatus,
                    PinyinCode = Doctor.PinyinCode,
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
                    await main.CheckDoctorProfileAsync(); // 重新检查状态
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

        public async void OnNavigatedTo(NavigationContext navigationContext) {
            try {
                await LoadAsync();
            } catch (Exception ex) {
                MessageBox.Show($"加载医生档案失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}


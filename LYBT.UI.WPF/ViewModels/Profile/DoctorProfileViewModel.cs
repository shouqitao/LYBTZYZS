using LYBT.Common.Enums;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Users.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.ViewModels.Main;
using LYBT.Common.Helpers;
using LYBT.Common.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    public class DoctorProfileViewModel : BindableBase, INavigationAware {
        private readonly IDoctorService _doctorService;
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        private DoctorDetailDto _doctor = new();
        public DoctorDetailDto Doctor { get => _doctor; set => SetProperty(ref _doctor, value); }

        public ObservableCollection<EnumItem<Gender>> GenderList { get; } = EnumHelper.BuildComboBoxSource<Gender>();
        public ObservableCollection<EnumItem<DoctorTitle>> TitleList { get; } = EnumHelper.BuildComboBoxSource<DoctorTitle>();
        public ObservableCollection<EnumItem<DoctorStatus>> StatusList { get; } = EnumHelper.BuildComboBoxSource<DoctorStatus>();

        private UserDto? _user;
        public UserDto? User { get => _user; set => SetProperty(ref _user, value); }

        private string _editModeTitle = "新增医生档案";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        public string? ContactNumber { get => Doctor.ContactNumber; set { Doctor.ContactNumber = value; RaisePropertyChanged(); } }

        private bool _isEditable;
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

        private ProfileMode _mode;
        /// <summary>
        /// 当前视图模式
        /// </summary>
        public ProfileMode Mode {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// Optional action invoked when canceling editing. If not set,
        /// the view model falls back to its default behavior.
        /// </summary>
        public Action? CancelAction { get; set; }

        public DoctorProfileViewModel(IDoctorService doctorService, IAuthService authService, IUserService userService) {
            _doctorService = doctorService;
            _authService = authService;
            _userService = userService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync(ProfileMode mode = ProfileMode.View) {
            Mode = mode;
            var info = await _doctorService.GetByUserIdAsync(_authService.UserId);
            var currentUser = await _userService.GetByIdAsync(_authService.UserId);
            User = currentUser;
            if (info != null) {
                Doctor = info;
            } else {
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
            }

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新增医生档案";
                    IsEditable = true;
                    break;
                case ProfileMode.Edit:
                    EditModeTitle = "编辑医生档案";
                    IsEditable = true;
                    break;
                default:
                    EditModeTitle = "医生详情";
                    IsEditable = false;
                    break;
            }
        }

        private async Task SaveAsync() {
            bool ok;
            if (Doctor.Id == Guid.Empty) {
                var dto = new DoctorDetailDto {
                    UserId = Doctor.UserId,
                    Gender = Doctor.Gender,
                    Birthday = Doctor.Birthday,
                    Title = Doctor.Title,
                    LicenseNumber = Doctor.LicenseNumber,
                    Specialty = Doctor.Specialty,
                    Status = Doctor.Status,
                    WorkStatus = Doctor.WorkStatus,
                    PinyinCode = Doctor.PinyinCode,
                    ContactNumber = Doctor.ContactNumber,
                    Remark = Doctor.Remark
                };
                ok = await _doctorService.AddAsync(dto);
            } else {
                var dto = new DoctorDetailDto {
                    Id = Doctor.Id,
                    UserId = Doctor.UserId,
                    Gender = Doctor.Gender,
                    Birthday = Doctor.Birthday,
                    Title = Doctor.Title,
                    LicenseNumber = Doctor.LicenseNumber,
                    Specialty = Doctor.Specialty,
                    Status = Doctor.Status,
                    WorkStatus = Doctor.WorkStatus,
                    PinyinCode = Doctor.PinyinCode,
                    ContactNumber = Doctor.ContactNumber,
                    Remark = Doctor.Remark
                };
                ok = await _doctorService.UpdateAsync(dto);
            }

            if (!ok)
                return;
            MessageBox.Show("已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            if (CancelAction != null) {
                CancelAction.Invoke();
            } else if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                main.IsMainVisible = true;
                main.IsFunctionVisible = false;
                main.HasDoctorProfile = true;
                await main.CheckDoctorProfileAsync(); // 重新检查状态
            }
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "医生详情";
        }

        private void Cancel() {
            if (CancelAction != null) {
                CancelAction.Invoke();
                return;
            }
            if (Application.Current.MainWindow.DataContext is MainWindowViewModel main) {
                main.IsMainVisible = true;
                main.IsFunctionVisible = false;
            }
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "医生详情";
        }

        public async void OnNavigatedTo(NavigationContext navigationContext) {
            try {
                await LoadAsync(ProfileMode.Edit);
            } catch (Exception ex) {
                MessageBox.Show($"加载医生档案失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}


using LYBT.Common.Enums;
using LYBT.Models.Doctors;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Main {
    /// <summary>
    /// 医生信息提交视图模型
    /// </summary>
    public class DoctorInfoViewModel : BindableBase, INavigationAware {
        private readonly Services.IDoctorService _doctorService;
        private readonly Services.IAuthService _authService;

        public DoctorInfoViewModel(Services.IDoctorService doctorService, Services.IAuthService authService) {
            _doctorService = doctorService;
            _authService = authService;
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync());
        }

        public DelegateCommand SubmitCommand { get; }

        private string _name = string.Empty;
        public string Name { get => _name; set => SetProperty(ref _name, value); }

        private string _phone = string.Empty;
        public string Phone { get => _phone; set => SetProperty(ref _phone, value); }

        private Gender _gender = Gender.Unknown;
        public Gender Gender { get => _gender; set => SetProperty(ref _gender, value); }

        private DateTime _birthday = DateTime.Now;
        public DateTime Birthday { get => _birthday; set => SetProperty(ref _birthday, value); }

        private string _pinyinCode = string.Empty;
        public string PinyinCode { get => _pinyinCode; set => SetProperty(ref _pinyinCode, value); }

        private string? _licenseNumber;
        public string? LicenseNumber { get => _licenseNumber; set => SetProperty(ref _licenseNumber, value); }

        private DoctorTitle _title = DoctorTitle.Junior;
        public DoctorTitle Title { get => _title; set => SetProperty(ref _title, value); }

        private DoctorStatus _doctorStatus = DoctorStatus.Active;
        public DoctorStatus DoctorStatus { get => _doctorStatus; set => SetProperty(ref _doctorStatus, value); }

        private string _remark = string.Empty;
        public string Remark { get => _remark; set => SetProperty(ref _remark, value); }

        private async Task SubmitAsync() {
            var model = new DoctorInfoRequestModel {
                DoctorId = _authService.UserId,
                Name = Name,
                Phone = Phone,
                Gender = Gender,
                Birthday = Birthday,
                PinyinCode = PinyinCode,
                LicenseNumber = LicenseNumber,
                Title = Title,
                DoctorStatus = DoctorStatus,
                Remark = Remark
            };
            var ok = await _doctorService.SubmitInfoRequestAsync(model);
            MessageBox.Show(ok ? "已提交审核" : "提交失败", "提示");
        }

        public void OnNavigatedTo(NavigationContext navigationContext) { }
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}

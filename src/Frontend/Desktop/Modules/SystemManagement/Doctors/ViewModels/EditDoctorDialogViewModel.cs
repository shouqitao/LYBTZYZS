using System;
using System.Collections.Generic;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.SystemManagement.Doctors.ViewModels
{
    /// <summary>
    /// 编辑医生对话框视图模型
    /// </summary>
    public class EditDoctorDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IDoctorService _doctorService;
        private readonly Guid _doctorId;
        private DoctorInfo? doctor;

        #region 属性

        private string _name = string.Empty;
        private string _code = string.Empty;
        private string _department = string.Empty;
        private Gender _gender = Gender.Male;
        private DateTime? _birthDate;
        private DoctorTitle _title = DoctorTitle.AttendingPhysician;
        private string _specialty = string.Empty;
        private string _licenseNumber = string.Empty;
        private string _phone = string.Empty;
        private string _remark = string.Empty;
        private bool _isActive = true;
        private bool _isLoading = true;

        /// <summary>姓名</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>工号</summary>
        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        /// <summary>科室</summary>
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        /// <summary>性别</summary>
        public Gender Gender
        {
            get => _gender;
            set => SetProperty(ref _gender, value);
        }

        /// <summary>出生日期</summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set => SetProperty(ref _birthDate, value);
        }

        /// <summary>职称</summary>
        public DoctorTitle Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>专科特长</summary>
        public string Specialty
        {
            get => _specialty;
            set => SetProperty(ref _specialty, value);
        }

        /// <summary>执业证书编号</summary>
        public string LicenseNumber
        {
            get => _licenseNumber;
            set => SetProperty(ref _licenseNumber, value);
        }

        /// <summary>联系电话</summary>
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        /// <summary>备注</summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>是否启用</summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 性别选择属性

        /// <summary>是否男性</summary>
        public bool IsMale
        {
            get => Gender == Gender.Male;
            set
            {
                if (value) Gender = Gender.Male;
            }
        }

        /// <summary>是否女性</summary>
        public bool IsFemale
        {
            get => Gender == Gender.Female;
            set
            {
                if (value) Gender = Gender.Female;
            }
        }

        #endregion

        #region 数据源

        /// <summary>职称选项列表</summary>
        public List<KeyValuePair<DoctorTitle, string>> TitleOptions { get; }

        /// <summary>科室选项列表</summary>
        public List<string> DepartmentOptions { get; }

        #endregion

        #region 命令

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        public Action<bool>? CloseDialogCallback { get; set; }

        public EditDoctorDialogViewModel(IDoctorService doctorService, Guid doctorId,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _doctorService = doctorService;
            _doctorId = doctorId;

            // 初始化职称选项
            TitleOptions = new List<KeyValuePair<DoctorTitle, string>>
            {
                new(DoctorTitle.Junior, DoctorTitle.Junior.GetDescription()),
                new(DoctorTitle.ResidentPhysician, DoctorTitle.ResidentPhysician.GetDescription()),
                new(DoctorTitle.AttendingPhysician, DoctorTitle.AttendingPhysician.GetDescription()),
                new(DoctorTitle.AssociateChiefPhysician, DoctorTitle.AssociateChiefPhysician.GetDescription()),
                new(DoctorTitle.ChiefPhysician, DoctorTitle.ChiefPhysician.GetDescription())
            };

            // 初始化科室选项
            DepartmentOptions = new List<string>
            {
                "内科", "外科", "妇科", "儿科", "眼科", "耳鼻喉科",
                "口腔科", "皮肤科", "中医科", "康复科", "急诊科", "其他"
            };

            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 加载医生信息
            _ = LoadDoctorAsync();
        }

        private async System.Threading.Tasks.Task LoadDoctorAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _doctorService.GetDoctorByIdAsync(_doctorId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    doctor = result.Data;
                    
                    // 填充表单
                    Name = doctor.Name;
                    Code = doctor.Code;
                    Department = doctor.Department;
                    // SelectedGender = doctor.Gender; // 字段已移除
                    // BirthDate = doctor.Birthday // 字段已移除;
            // Title = doctor.Title // 字段已移除
                    Specialty = doctor.Specialty ?? string.Empty;
                    LicenseNumber = doctor.LicenseNumber ?? string.Empty;
                    Phone = doctor.Phone;
                    // Remark = doctor.Remark // 字段已移除 ?? string.Empty;
                    IsActive = doctor.IsActive;
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"加载医生信息失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                    CloseDialogCallback?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载医生信息失败：{ex.Message}", "错误").GetAwaiter().GetResult();
                CloseDialogCallback?.Invoke(false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteSave()
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(Name))
            {
                await _commonDialogService.ShowWarningAsync("请输入医生姓名", "提示");
                return;
            }

            if (string.IsNullOrWhiteSpace(Code))
            {
                await _commonDialogService.ShowWarningAsync("请输入医生工号", "提示");
                return;
            }

            if (string.IsNullOrWhiteSpace(Department))
            {
                _commonDialogService.ShowWarningAsync("请选择所属科室", "提示").GetAwaiter().GetResult();
                return;
            }

            if (!BirthDate.HasValue)
            {
                _commonDialogService.ShowWarningAsync("请选择出生日期", "提示").GetAwaiter().GetResult();
                return;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                _commonDialogService.ShowWarningAsync("请输入联系电话", "提示").GetAwaiter().GetResult();
                return;
            }

            // 验证手机号格式
            if (!System.Text.RegularExpressions.Regex.IsMatch(Phone, @"^1[3-9]\d{9}$"))
            {
                _commonDialogService.ShowWarningAsync("请输入正确的手机号码", "提示").GetAwaiter().GetResult();
                return;
            }

            try
            {
                if (doctor == null) return;

                // 更新医生信息
                doctor.Name = Name;
                doctor.Code = Code;
                doctor.Department = Department;
                // doctor.Gender = Gender; // 字段已移除
                // doctor.Birthday = BirthDate.Value; // 字段已移除
                // doctor.Title = Title; // 字段已移除
                doctor.Specialty = Specialty;
                doctor.LicenseNumber = LicenseNumber;
                doctor.ContactNumber = Phone;
                doctor.Phone = Phone;
                // doctor.Remark = Remark; // 字段已移除
                doctor.IsActive = IsActive;

                // 计算年龄 - 字段已移除
                // var today = DateTime.Today;
                // var age = today.Year - BirthDate.Value.Year;
                // if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                // doctor.Age = age;

                // 更新拼音码
                doctor.PinYinCode = Name.ToUpper();

                var result = await _doctorService.UpdateDoctorAsync(doctor);
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("医生信息更新成功", "成功").GetAwaiter().GetResult();
                    CloseDialogCallback?.Invoke(true);
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"更新失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"更新失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}
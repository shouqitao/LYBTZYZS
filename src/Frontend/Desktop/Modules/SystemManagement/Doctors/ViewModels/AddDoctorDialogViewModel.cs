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
    /// 新增医生对话框视图模型
    /// </summary>
    public class AddDoctorDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IDoctorService _doctorService;

        #region 属性

        private string _name = string.Empty;
        private string _code = string.Empty;
        private string _department = "内科"; // 默认科室
        private Gender _gender = Gender.Male;
        private DateTime? _birthDate = DateTime.Now.AddYears(-30); // 默认30岁
        private DoctorTitle _title = DoctorTitle.AttendingPhysician; // 默认主治医师
        private string _specialty = string.Empty;
        private string _licenseNumber = string.Empty;
        private string _phone = string.Empty;
        private string _remark = string.Empty;

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

        public AddDoctorDialogViewModel(IDoctorService doctorService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _doctorService = doctorService;

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
                var doctor = new DoctorInfo
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(), // 后端会创建对应的用户
                    Name = Name,
                    Code = Code,
                    Department = Department,
                    Gender = Gender,
                    Birthday = BirthDate.Value,
                    Title = Title,
                    Specialty = Specialty,
                    LicenseNumber = LicenseNumber,
                    ContactNumber = Phone,
                    Phone = Phone,
                    Status = DoctorStatus.Active,
                    WorkStatus = DoctorWorkStatus.Clinic,
                    IsActive = true,
                    CreateTime = DateTime.Now,
                    Remark = Remark
                };

                // 计算年龄
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                doctor.Age = age;

                // 生成拼音码（简单示例，实际应使用拼音库）
                doctor.PinYinCode = Name.ToUpper();

                var result = await _doctorService.AddDoctorAsync(doctor);
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("医生信息保存成功", "成功").GetAwaiter().GetResult();
                    CloseDialogCallback?.Invoke(true);
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"保存失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"保存失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}
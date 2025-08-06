using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Doctors;
using LYBT.WPF.Client.Core.Models.Users;
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
        private readonly IUserService _userService;

        #region 属性

        private ObservableCollection<UserInfo> _doctorRoleUsers = new();
        /// <summary>具有医生角色的用户列表</summary>
        public ObservableCollection<UserInfo> DoctorRoleUsers
        {
            get => _doctorRoleUsers;
            set => SetProperty(ref _doctorRoleUsers, value);
        }

        private UserInfo? _selectedUser;
        /// <summary>选中的用户</summary>
        public UserInfo? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    OnSelectedUserChanged();
                }
            }
        }

        private string _name = string.Empty;
        private string _code = string.Empty;
        private string _department = "中医科"; // 默认科室
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
            IUserService userService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
            _doctorService = doctorService;
            _userService = userService;

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

            // 加载具有医生角色的用户
            _ = LoadDoctorRoleUsersAsync();
        }

        /// <summary>
        /// 加载具有医生角色的用户
        /// </summary>
        private async Task LoadDoctorRoleUsersAsync()
        {
            try
            {
                // 获取所有用户
                var users = await _userService.GetUsersAsync();
                
                // 筛选具有医生角色的用户（UserRole.DiagnosingDoctor = 1）
                var doctorUsers = users.Where(u => u.Role == UserRole.DiagnosingDoctor).ToList();

                DoctorRoleUsers.Clear();
                foreach (var user in doctorUsers)
                {
                    // 检查该用户是否已经有医生档案
                    var existingDoctor = await _doctorService.GetDoctorByUserIdAsync(user.Id);
                    if (existingDoctor == null || !existingDoctor.IsSuccess)
                    {
                        DoctorRoleUsers.Add(user);
                    }
                }

                if (!DoctorRoleUsers.Any())
                {
                    await _commonDialogService.ShowWarningAsync(
                        "没有找到可用的医生角色用户。\n" +
                        "请先在用户管理中创建具有医生角色的用户。", 
                        "提示");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载用户列表失败: {ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 当选中的用户改变时
        /// </summary>
        private void OnSelectedUserChanged()
        {
            if (SelectedUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"用户选择改变: {SelectedUser.Username} - {SelectedUser.RealName}");
                
                // 自动填充姓名
                Name = SelectedUser.RealName ?? SelectedUser.Username;
                System.Diagnostics.Debug.WriteLine($"设置姓名为: {Name}");
                
                // 自动填充电话
                Phone = SelectedUser.PhoneNumber ?? string.Empty;
                System.Diagnostics.Debug.WriteLine($"设置电话为: {Phone}");
                
                // 触发属性更改通知
                RaisePropertyChanged(nameof(Name));
                RaisePropertyChanged(nameof(Phone));
                
                // BaseUserModel 没有 Gender 属性，保持默认性别设置
            }
        }

        private async void ExecuteSave()
        {
            System.Diagnostics.Debug.WriteLine("==== ExecuteSave 开始执行 ====");
            
            try
            {
                System.Diagnostics.Debug.WriteLine("ExecuteSave 方法被调用");
                System.Diagnostics.Debug.WriteLine($"SelectedUser: {SelectedUser?.Username ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"Name: {Name}");
                System.Diagnostics.Debug.WriteLine($"Code: {Code}");
                System.Diagnostics.Debug.WriteLine($"Department: {Department}");
                System.Diagnostics.Debug.WriteLine($"Phone: {Phone}");
                
                // 验证必须选择用户
                if (SelectedUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("错误: 未选择用户");
                    await _commonDialogService.ShowWarningAsync("请选择一个具有医生角色的用户", "提示");
                    return;
                }

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(Code))
                {
                    await _commonDialogService.ShowWarningAsync("请输入医生工号", "提示");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Department))
                {
                    await _commonDialogService.ShowWarningAsync("请选择所属科室", "提示");
                    return;
                }

                if (!BirthDate.HasValue)
                {
                    await _commonDialogService.ShowWarningAsync("请选择出生日期", "提示");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Phone))
                {
                    await _commonDialogService.ShowWarningAsync("请输入联系电话", "提示");
                    return;
                }

                // 验证手机号格式
                if (!System.Text.RegularExpressions.Regex.IsMatch(Phone, @"^1[3-9]\d{9}$"))
                {
                    await _commonDialogService.ShowWarningAsync("请输入正确的手机号码", "提示");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"开始创建医生对象，关联用户: {SelectedUser.Username}");
                System.Diagnostics.Debug.WriteLine($"Specialty字段值: '{Specialty}'");
                System.Diagnostics.Debug.WriteLine($"Department字段值: '{Department}'");
                
                // 确保Specialty和Department都不为空
                var specialtyValue = !string.IsNullOrWhiteSpace(Specialty) ? Specialty : (!string.IsNullOrWhiteSpace(Department) ? Department : "中医科");
                
                System.Diagnostics.Debug.WriteLine($"最终Specialty值: '{specialtyValue}'");
                
                var doctor = new DoctorInfo
                {
                    Id = Guid.NewGuid(),
                    UserId = SelectedUser.Id,  // 使用选中用户的ID
                    Name = SelectedUser.RealName ?? SelectedUser.Username,  // 使用用户的真实姓名
                    Code = Code,
                    /* Department = Department, */
                    Gender = Gender,
                    Birthday = BirthDate.Value,
                    /* Title = Title, */
                    Specialty = specialtyValue,  // 确保不为空
                    LicenseNumber = LicenseNumber ?? string.Empty,
                    ContactNumber = Phone,
                    Phone = Phone,
                    Status = DoctorStatus.Active,
                    /* WorkStatus = DoctorWorkStatus.Clinic, */
                    IsActive = true,
                    CreateTime = DateTime.Now,
                    Remark = Remark ?? string.Empty,
                    Specialties = specialtyValue  // 确保不为空
                };

                // 计算年龄
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                /* doctor.Age */" = age;

                // 生成拼音码（简单示例，实际应使用拼音库）
                doctor.PinYinCode = Name.ToUpper();

                System.Diagnostics.Debug.WriteLine($"开始调用API保存医生: {Name}");
                
                var result = await _doctorService.AddDoctorAsync(doctor);
                
                System.Diagnostics.Debug.WriteLine($"API调用结果: IsSuccess={result.IsSuccess}, Error={result.ErrorMessage}");
                
                if (result.IsSuccess)
                {
                    await _commonDialogService.ShowInformationAsync("医生信息保存成功", "成功");
                    CloseDialogCallback?.Invoke(true);
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync($"保存失败：{result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecuteSave 发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                await _commonDialogService.ShowErrorAsync($"保存失败：{ex.Message}", "错误");
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}
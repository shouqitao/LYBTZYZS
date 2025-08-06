using System;
using System.Collections.ObjectModel;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Modules.Examples.Controls.ViewModels
{
    /// <summary>
    /// 控件示例视图模型
    /// </summary>
    public class ControlExamplesViewModel : BindableBase
    {
        private bool _isActive = true;
        private string _sampleName = "张三";
        private Gender _sampleGender = Gender.Male;
        private string _sampleIDNumber = "110101199001011234";

        public ControlExamplesViewModel()
        {
            // 初始化命令
            ToggleBooleanCommand = new DelegateCommand(ExecuteToggleBoolean);
            
            // 初始化示例数据
            InitializeSampleUsers();
            InitializeSampleHerbs();
            InitializeSamplePatients();
            InitializeSampleTemplates();
        }

        #region 属性

        /// <summary>
        /// 布尔值示例
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// 姓名示例
        /// </summary>
        public string SampleName
        {
            get => _sampleName;
            set => SetProperty(ref _sampleName, value);
        }

        /// <summary>
        /// 性别示例
        /// </summary>
        public Gender SampleGender
        {
            get => _sampleGender;
            set => SetProperty(ref _sampleGender, value);
        }

        /// <summary>
        /// 身份证号示例
        /// </summary>
        public string SampleIDNumber
        {
            get => _sampleIDNumber;
            set => SetProperty(ref _sampleIDNumber, value);
        }

        /// <summary>
        /// 示例用户列表
        /// </summary>
        public ObservableCollection<UserDto> SampleUsers { get; } = new ObservableCollection<UserDto>();

        /// <summary>
        /// 示例草药列表
        /// </summary>
        public ObservableCollection<HerbDto> SampleHerbs { get; } = new ObservableCollection<HerbDto>();

        /// <summary>
        /// 示例患者列表
        /// </summary>
        public ObservableCollection<PatientDto> SamplePatients { get; } = new ObservableCollection<PatientDto>();

        /// <summary>
        /// 示例验方模板列表
        /// </summary>
        public ObservableCollection<FormulaTemplateDto> SampleTemplates { get; } = new ObservableCollection<FormulaTemplateDto>();

        #endregion

        #region 命令

        /// <summary>
        /// 切换布尔值命令
        /// </summary>
        public DelegateCommand ToggleBooleanCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 执行切换布尔值
        /// </summary>
        private void ExecuteToggleBoolean()
        {
            IsActive = !IsActive;
            
            // 同时切换性别以展示性别转换器
            SampleGender = SampleGender == Gender.Male ? Gender.Female : Gender.Male;
            
            // 更改姓名以展示首字符转换器
            SampleName = SampleName == "张三" ? "李四" : "张三";
            
            // 更新示例用户的状态
            if (SampleUsers.Count > 0)
            {
                SampleUsers[0].IsActive = !SampleUsers[0].IsActive;
            }
        }

        /// <summary>
        /// 初始化示例用户数据
        /// </summary>
        private void InitializeSampleUsers()
        {
            SampleUsers.Add(new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "zhangsan",
                RealName = "张三",
                Role = UserRole.Admin,
                Email = "zhangsan@example.com",
                IsActive = true,
                CreateTime = DateTime.Now.AddDays(-30)
            });

            SampleUsers.Add(new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "lisi",
                RealName = "李四",
                Role = UserRole.DiagnosingDoctor,
                Email = "lisi@example.com",
                IsActive = false,
                CreateTime = DateTime.Now.AddDays(-20)
            });

            SampleUsers.Add(new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "wangwu",
                RealName = "王五",
                Role = UserRole.PharmacyStaff,
                Email = "wangwu@example.com",
                IsActive = true,
                CreateTime = DateTime.Now.AddDays(-10)
            });
        }

        /// <summary>
        /// 初始化示例草药数据
        /// </summary>
        private void InitializeSampleHerbs()
        {
            SampleHerbs.Add(new HerbDto
            {
                Id = Guid.NewGuid(),
                Name = "麻黄",
                PinYinCode = "MH",
                Origin = "山西",
                Spec = "优质",
                Unit = "克",
                Price = 12.5m,
                /* Stock = 100, */
                /* BatchNo = "20240101", */
                IsActive = true,
                CreateTime = DateTime.Now.AddDays(-30)
            });

            SampleHerbs.Add(new HerbDto
            {
                Id = Guid.NewGuid(),
                Name = "桂枝",
                PinYinCode = "GZ",
                Origin = "广西",
                Spec = "标准",
                Unit = "克",
                Price = 15.0m,
                /* Stock = 8, */
                /* BatchNo = "20240102", */
                IsActive = true,
                CreateTime = DateTime.Now.AddDays(-20)
            });

            SampleHerbs.Add(new HerbDto
            {
                Id = Guid.NewGuid(),
                Name = "甘草",
                PinYinCode = "GC",
                Origin = "内蒙古",
                Spec = "优质",
                Unit = "克",
                Price = 8.0m,
                /* Stock = 0, */
                /* BatchNo = "20240103", */
                IsActive = false,
                CreateTime = DateTime.Now.AddDays(-10)
            });
        }

        /// <summary>
        /// 初始化示例患者数据
        /// </summary>
        private void InitializeSamplePatients()
        {
            SamplePatients.Add(new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = Gender.Male,
                Age = 35,
                PhoneNumber = "13800138000",
                IDNumber = "110101198801011234",
                AllergyHistory = "青霉素过敏",
                Address = "北京市朝阳区",
                PinYinCode = "ZS"
            });

            SamplePatients.Add(new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "李四",
                Gender = Gender.Female,
                Age = 28,
                PhoneNumber = "13900139000",
                IDNumber = "110101199301015678",
                AllergyHistory = "",
                Address = "北京市海淀区",
                PinYinCode = "LS"
            });

            SamplePatients.Add(new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "王五",
                Gender = Gender.Unknown,
                Age = 42,
                PhoneNumber = "13700137000",
                IDNumber = "110101198101019012",
                AllergyHistory = "花生过敏、海鲜过敏",
                Address = "北京市西城区",
                PinYinCode = "WW"
            });
        }

        /// <summary>
        /// 初始化示例验方模板数据
        /// </summary>
        private void InitializeSampleTemplates()
        {
            SampleTemplates.Add(new FormulaTemplateDto
            {
                Id = Guid.NewGuid(),
                Name = "感冒清热方"
            });

            SampleTemplates.Add(new FormulaTemplateDto
            {
                Id = Guid.NewGuid(),
                Name = "健脾养胃方"
            });

            SampleTemplates.Add(new FormulaTemplateDto
            {
                Id = Guid.NewGuid(),
                Name = "安神助眠方"
            });
        }

        #endregion
    }
}
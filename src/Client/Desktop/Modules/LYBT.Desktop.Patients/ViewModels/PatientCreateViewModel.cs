using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Events;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 创建患者视图模型 - CRUD统一模式
    /// 功能：患者创建表单，采用Region Navigation模式
    /// </summary>
    public class PatientCreateViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IPatientRepository _patientRepository;

        #endregion

        #region 用户输入属性

        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private Gender _selectedGender = Gender.Unknown;
        private DateTime? _birthDate;
        private string? _idNumber;
        private string? _phoneNumber;
        private string? _address;

        /// <summary>
        /// 患者姓名
        /// </summary>
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    // 自动生成拼音码
                    PinYinCode = PinYinHelper.GetPinYinCode(value);

                    ValidateProperty();
                    SubmitCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 拼音码（自动生成，用于确认）
        /// </summary>
        public string PinYinCode
        {
            get => _pinYinCode;
            private set => SetProperty(ref _pinYinCode, value);
        }

        /// <summary>
        /// 性别
        /// </summary>
        public Gender SelectedGender
        {
            get => _selectedGender;
            set => SetProperty(ref _selectedGender, value);
        }

        /// <summary>
        /// 出生日期
        /// </summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    // 生日变化时通知Age属性更新
                    RaisePropertyChanged(nameof(Age));
                }
            }
        }

        /// <summary>
        /// 年龄（根据出生日期自动计算）
        /// </summary>
        public int? Age
        {
            get
            {
                if (BirthDate.HasValue)
                {
                    var today = DateTime.Today;
                    var age = today.Year - BirthDate.Value.Year;
                    if (BirthDate.Value.Date > today.AddYears(-age))
                    {
                        age--;
                    }
                    return age;
                }
                return null;
            }
        }

        /// <summary>
        /// 身份证号
        /// </summary>
        [Required(ErrorMessage = "身份证号不能为空")]
        [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
        public string? IdNumber
        {
            get => _idNumber;
            set
            {
                if (SetProperty(ref _idNumber, value))
                {
                    ValidateProperty();
                    SubmitCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 手机号码
        /// </summary>
        [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
        public string? PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 地址
        /// </summary>
        [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
        public string? Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    ValidateProperty();
                }
            }
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 性别选项
        /// </summary>
        public Gender[] GenderOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 提交命令（创建）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public PatientCreateViewModel(
            IPatientRepository patientRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));

            PageTitle = "创建患者";

            // 初始化选项
            GenderOptions = Enum.GetValues<Gender>();

            // 初始化命令
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(Cancel);
        }

        #endregion

        #region Navigation模式方法

        /// <summary>
        /// 处理导航参数（同步）
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            // 创建模式无需处理参数
        }

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // 初始化表单默认值
            Name = string.Empty;
            PinYinCode = string.Empty;
            SelectedGender = Gender.Unknown;
            BirthDate = null;
            PhoneNumber = null;
            Address = null;

            Logger.LogDebug("PatientCreateViewModel 初始化完成");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 提交表单（创建患者）
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在创建患者...";

                var createDto = new PatientInputDto
                {
                    Name = Name.Trim(),
                    Gender = SelectedGender,
                    BirthDate = BirthDate,
                    IdNumber = IdNumber?.Trim(),
                    PhoneNumber = PhoneNumber?.Trim(),
                    Address = Address?.Trim(),
                    Status = CommonStatus.Enabled
                };

                Logger.LogInformation("开始创建患者: Name={Name}, Gender={Gender}",
                    createDto.Name, createDto.Gender);

                var result = await _patientRepository.CreateAsync(createDto);

                if (result != null)
                {
                    Logger.LogInformation("患者创建成功: PatientId={PatientId}, Name={Name}",
                        result.Id, result.Name);

                    // 发布事件通知列表刷新
                    EventAggregator.GetEvent<PatientCreatedEvent>().Publish(result);

                    // 导航返回
                    NavigateBack("ContentRegion");
                }
                else
                {
                    Logger.LogError("创建患者失败");
                    await ShowErrorMessageAsync("创建患者失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "创建患者异常: Name={Name}", Name);
                await ShowErrorMessageAsync($"创建患者失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            Logger.LogDebug("用户取消创建操作");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
        {
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !HasErrors;
        }

        #endregion
    }
}

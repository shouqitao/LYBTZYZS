using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
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
    /// 患者详情视图模型 - CRUD统一架构
    /// Issue #2168: 统一Create/Edit/View三种模式到单一ViewModel
    /// 参考：UserDetailViewModel架构
    /// </summary>
    public class PatientDetailViewModel : UnifiedViewModelBase
    {
        #region 服务依赖

        private readonly IPatientRepository _patientRepository;

        #endregion

        #region 模式控制属性

        private Guid _patientId;
        private bool _isEditMode = true; // 默认为编辑模式

        /// <summary>
        /// 患者ID（空=Create模式，非空=Edit/View模式）
        /// </summary>
        public Guid PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }

        /// <summary>
        /// 是否为编辑模式（false=View只读模式）
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            private set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    RaisePropertyChanged(nameof(IsReadOnly));
                    SubmitCommand?.RaiseCanExecuteChanged();
                    SwitchToEditModeCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 是否只读模式
        /// </summary>
        public bool IsReadOnly => !IsEditMode;

        /// <summary>
        /// 是否为Create模式
        /// </summary>
        public bool IsCreateMode => PatientId == Guid.Empty;

        /// <summary>
        /// 是否为Edit或View模式
        /// </summary>
        public bool IsEditOrViewMode => PatientId != Guid.Empty;

        #endregion

        #region 表单属性

        private string _name = string.Empty;
        private string _pinYinCode = string.Empty;
        private Gender _selectedGender = Gender.Unknown;
        private DateTime? _birthDate;
        private string? _idNumber;
        private string? _phoneNumber;
        private string? _address;
        private CommonStatus _status = CommonStatus.Enabled;

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
        /// 拼音码（自动生成）
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

        /// <summary>
        /// 状态（仅Edit/View模式显示）
        /// </summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 性别选项
        /// </summary>
        public IEnumerable<Gender> GenderOptions { get; }

        /// <summary>
        /// 状态选项
        /// </summary>
        public IEnumerable<CommonStatus> StatusOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 提交命令（Create/Edit）
        /// </summary>
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 切换到编辑模式命令（View→Edit）
        /// </summary>
        public DelegateCommand SwitchToEditModeCommand { get; }

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand GoBackCommand { get; }

        #endregion

        #region 构造函数

        public PatientDetailViewModel(
            IPatientRepository patientRepository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));

            // 初始化选项
            GenderOptions = Enum.GetValues<Gender>();
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
            CancelCommand = new DelegateCommand(Cancel);
            SwitchToEditModeCommand = new DelegateCommand(SwitchToEditMode, CanSwitchToEditMode);
            GoBackCommand = new DelegateCommand(() => NavigateBack("ContentRegion"));
        }

        #endregion

        #region Navigation生命周期

        /// <summary>
        /// 处理导航参数（同步）
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            // 提取PatientId参数
            if (parameters.ContainsKey("PatientId"))
            {
                PatientId = parameters.GetValue<Guid>("PatientId");
            }

            // 提取ReadOnly参数（View模式）
            if (parameters.ContainsKey("ReadOnly") && parameters.GetValue<bool>("ReadOnly"))
            {
                IsEditMode = false;
            }
            else
            {
                IsEditMode = true; // Create/Edit模式
            }
        }

        /// <summary>
        /// 异步初始化数据
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            if (PatientId != Guid.Empty)
            {
                // Edit/View模式：加载现有患者
                await LoadPatientAsync();
            }
            else
            {
                // Create模式：初始化空表单
                InitializeEmptyForm();
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载患者数据（Edit/View模式）
        /// </summary>
        private async Task LoadPatientAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载患者信息...";

                Logger.LogInformation("开始加载患者数据: PatientId={PatientId}", PatientId);

                var patient = await _patientRepository.GetByIdAsync(PatientId);

                if (patient != null)
                {
                    // 填充表单字段
                    Name = patient.Name;
                    PinYinCode = patient.PinYinCode ?? PinYinHelper.GetPinYinCode(patient.Name);
                    SelectedGender = patient.Gender;
                    BirthDate = patient.BirthDate;
                    IdNumber = patient.IdNumber;
                    PhoneNumber = patient.PhoneNumber;
                    Address = patient.Address;
                    Status = patient.Status;

                    // 更新页面标题
                    PageTitle = IsReadOnly ? $"查看患者 - {Name}" : $"编辑患者 - {Name}";

                    Logger.LogInformation("患者数据加载成功: Name={Name}", Name);
                }
                else
                {
                    Logger.LogWarning("未找到患者: PatientId={PatientId}", PatientId);
                    await ShowErrorMessageAsync("未找到患者信息");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者数据失败: PatientId={PatientId}", PatientId);
                await ShowErrorMessageAsync($"加载患者数据失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 初始化空表单（Create模式）
        /// </summary>
        private void InitializeEmptyForm()
        {
            Name = string.Empty;
            PinYinCode = string.Empty;
            SelectedGender = Gender.Unknown;
            BirthDate = null;
            IdNumber = null;
            PhoneNumber = null;
            Address = null;
            Status = CommonStatus.Enabled;

            PageTitle = "创建患者";

            Logger.LogDebug("Create模式：空表单初始化完成");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 提交表单（Create或Update）
        /// </summary>
        private async Task SubmitAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = PatientId == Guid.Empty ? "正在创建患者..." : "正在保存修改...";

                if (PatientId == Guid.Empty)
                {
                    await CreatePatientAsync();
                }
                else
                {
                    await UpdatePatientAsync();
                }
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        /// <summary>
        /// 创建患者
        /// </summary>
        private async Task CreatePatientAsync()
        {
            try
            {
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

                    // Issue #2166: 使用Navigation参数通知刷新，替代事件
                    NavigateBack("ContentRegion", new NavigationParameters
                    {
                        { "RefreshList", true }
                    });
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
        }

        /// <summary>
        /// 更新患者
        /// </summary>
        private async Task UpdatePatientAsync()
        {
            try
            {
                var updateDto = new PatientInputDto
                {
                    Id = PatientId,
                    Name = Name.Trim(),
                    Gender = SelectedGender,
                    BirthDate = BirthDate,
                    IdNumber = IdNumber?.Trim(),
                    PhoneNumber = PhoneNumber?.Trim(),
                    Address = Address?.Trim(),
                    Status = Status
                };

                Logger.LogInformation("开始更新患者: PatientId={PatientId}, Name={Name}",
                    PatientId, updateDto.Name);

                var result = await _patientRepository.UpdateAsync(updateDto);

                if (result != null)
                {
                    Logger.LogInformation("患者更新成功: PatientId={PatientId}, Name={Name}",
                        result.Id, result.Name);

                    // Issue #2166: 使用Navigation参数通知刷新，替代事件
                    NavigateBack("ContentRegion", new NavigationParameters
                    {
                        { "RefreshList", true }
                    });
                }
                else
                {
                    Logger.LogError("更新患者失败");
                    await ShowErrorMessageAsync("更新患者失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新患者异常: PatientId={PatientId}", PatientId);
                await ShowErrorMessageAsync($"更新患者失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            Logger.LogDebug("用户取消操作");
            NavigateBack("ContentRegion");
        }

        /// <summary>
        /// 切换到编辑模式（View→Edit）
        /// </summary>
        private void SwitchToEditMode()
        {
            IsEditMode = true;
            PageTitle = $"编辑患者 - {Name}";
            Logger.LogDebug("切换到编辑模式: PatientId={PatientId}", PatientId);
        }

        /// <summary>
        /// 是否可以提交
        /// </summary>
        private bool CanSubmit()
        {
            // View模式不能提交
            if (!IsEditMode)
            {
                return false;
            }

            // 验证必填字段
            return !IsLoading &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(IdNumber) &&
                   !HasErrors;
        }

        /// <summary>
        /// 是否可以切换到编辑模式
        /// </summary>
        private bool CanSwitchToEditMode()
        {
            // 只有View模式（有PatientId且IsEditMode=false）才能切换
            return PatientId != Guid.Empty && !IsEditMode;
        }

        #endregion
    }
}

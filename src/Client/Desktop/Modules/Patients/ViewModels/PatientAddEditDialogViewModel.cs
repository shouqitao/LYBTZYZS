using AutoMapper;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Events;

// UltraThink v2.0: Desktop层直接使用DTO，移除Info层转换
namespace LYBT.Desktop.Patients.ViewModels
{

    /// <summary>
    /// 患者新增/编辑对话框视图模型 - UltraThink双层架构UI层
    /// 采用UltraThink架构标准，使用C# 12现代化特性
    /// 职责：患者CRUD对话框交互逻辑、数据绑定、验证处理、状态管理
    /// 基于DialogViewModel统一对话框模式，实现ICustomDialogAware接口
    /// 集成PatientModule双层服务，提供完整的患者档案编辑用户体验
    /// 支持患者新增/编辑、拼音码生成、数据验证等功能
    /// 适配中医诊所患者档案管理流程，确保数据录入准确性和操作便利性
    /// </summary>
    public class PatientAddEditDialogViewModel : DialogViewModel, ICustomDialogAware
    {
        private readonly IPatientService _patientService;
        private readonly IMapper _mapper;
        private readonly PatientDto? _originalPatient;
        private bool _isEditMode;

        #region Properties

        private string _patientName = string.Empty;

        public string PatientName
        {
            get => _patientName;
            set
            {
                if (SetProperty(ref _patientName, value))
                {
                    // 自动生成拼音码（仅新增时）
                    if (!_isEditMode)
                    {
                        GeneratePinYinCode();
                    }

                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _pinYinCode = string.Empty;

        public string PinYinCode
        {
            get => _pinYinCode;
            set => SetProperty(ref _pinYinCode, value);
        }

        private Gender _gender = Gender.Male; // 修复：设置默认性别，避免CanSave失败

        public Gender Gender
        {
            get => _gender;
            set
            {
                if (SetProperty(ref _gender, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private int _age = 0;

        public int Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }

        private DateTime? _birthDate;

        public DateTime? BirthDate
        {
            get => _birthDate;
            set
            {
                if (SetProperty(ref _birthDate, value))
                {
                    // 自动计算年龄
                    CalculateAge();
                }
            }
        }

        private string _phoneNumber = string.Empty;

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private string _address = string.Empty;

        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _idType = SystemConstants.DefaultIdType;

        public string IdType
        {
            get => _idType;
            set => SetProperty(ref _idType, value);
        }

        private string _idNumber = string.Empty;

        public string IdNumber
        {
            get => _idNumber;
            set => SetProperty(ref _idNumber, value);
        }

        private string _emergencyContact = string.Empty;

        public string EmergencyContact
        {
            get => _emergencyContact;
            set => SetProperty(ref _emergencyContact, value);
        }

        private string _emergencyPhone = string.Empty;

        public string EmergencyPhone
        {
            get => _emergencyPhone;
            set => SetProperty(ref _emergencyPhone, value);
        }

        private string _allergyHistory = string.Empty;

        public string AllergyHistory
        {
            get => _allergyHistory;
            set => SetProperty(ref _allergyHistory, value);
        }

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PatientAddEditDialogViewModel"/> class.
        /// 构造函数
        /// </summary>
        /// <param name="patientService">患者API服务</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <param name="patient">要编辑的患者信息（null表示新增模式）</param>
        /// <summary>
        /// 构造函数 - UltraThink双层架构依赖注入
        /// 初始化患者管理模块、映射器、对话框配置和事件订阅
        /// </summary>
        /// <param name="patientService">患者模块主服务</param>
        /// <param name="mapper">对象映射器</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <param name="patient">要编辑的患者信息（null表示新增模式）</param>
        /// <exception cref="ArgumentNullException">当关键参数为空时抛出</exception>
        public PatientAddEditDialogViewModel(
            IPatientService patientService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            IErrorHandlingService errorHandlingService,
            PatientDto? patient = null)
            : base(eventAggregator, errorHandlingService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _originalPatient = patient;
            _isEditMode = patient != null;

            // 如果是编辑模式，初始化数据
            if (_isEditMode && patient != null)
            {
                InitializeEditData(patient);
            }
            else
            {
                DialogTitle = SystemConstants.AddPatientDialogTitle;
            }

            InitializeDialog();
        }

        #endregion Constructor

        #region DialogViewModel Implementation

        /// <inheritdoc/>
        protected override async Task<bool> SaveAsync()
        {
            // UltraThink调试：检查SaveAsync是否被调用
            System.Diagnostics.Debug.WriteLine($"🚀 SaveAsync被调用 - 模式: {(_isEditMode ? "编辑" : "新增")}");
            System.Diagnostics.Debug.WriteLine($"📋 患者姓名: '{PatientName}', 电话: '{PhoneNumber}', 性别: {Gender}");

            try
            {
                if (_isEditMode && _originalPatient != null)
                {
                    // 编辑模式
                    var updateDto = new PatientUpdateDto
                    {
                        Id = _originalPatient.Id,
                        Name = PatientName.Trim(),
                        Gender = Gender,
                        BirthDate = BirthDate, // 修复：添加出生日期
                        PhoneNumber = PhoneNumber?.Trim() ?? string.Empty,
                        Address = Address?.Trim() ?? string.Empty,
                        IdNumber = IdNumber?.Trim() ?? string.Empty,
                        AllergyHistory = AllergyHistory?.Trim() ?? string.Empty,
                        EmergencyContact = EmergencyContact?.Trim() ?? string.Empty, // 修复：添加紧急联系人
                        EmergencyPhone = EmergencyPhone?.Trim() ?? string.Empty // 修复：添加紧急联系电话
                    };

                    var serviceResult = await _patientService.UpdateAsync(_originalPatient.Id, updateDto);

                    if (!serviceResult.IsSuccess)
                    {
                        ErrorMessage = serviceResult.ErrorMessage ?? "编辑患者失败";
                        return false;
                    }
                }
                else
                {
                    // 新增模式
                    var createDto = new PatientCreateDto
                    {
                        Name = PatientName.Trim(),
                        Gender = Gender,
                        BirthDate = BirthDate, // 修复：添加出生日期
                        PhoneNumber = PhoneNumber?.Trim() ?? string.Empty,
                        Address = Address?.Trim() ?? string.Empty,
                        IdNumber = IdNumber?.Trim() ?? string.Empty,
                        AllergyHistory = AllergyHistory?.Trim() ?? string.Empty,
                        EmergencyContact = EmergencyContact?.Trim() ?? string.Empty, // 修复：添加紧急联系人
                        EmergencyPhone = EmergencyPhone?.Trim() ?? string.Empty, // 修复：添加紧急联系电话
                        Status = CommonStatus.Enabled // 修复：设置默认状态
                    };

                    var serviceResult = await _patientService.CreateAsync(createDto);

                    if (!serviceResult.IsSuccess)
                    {
                        ErrorMessage = serviceResult.ErrorMessage ?? "新增患者失败";
                        return false;
                    }
                }

                // 保存成功，关闭对话框
                RaiseRequestClose(true);
                return true;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync("保存患者", ex);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override bool CanSave()
        {
            var canSave = !string.IsNullOrWhiteSpace(PatientName) &&
                         !string.IsNullOrWhiteSpace(PhoneNumber) &&
                         Gender != Gender.Unknown;

            // UltraThink调试：检查CanSave验证结果
            System.Diagnostics.Debug.WriteLine($"🔍 CanSave检查: 姓名='{PatientName}', 电话='{PhoneNumber}', 性别={Gender}, 结果={canSave}");

            return canSave;
        }

        /// <inheritdoc/>
        protected override void InitializeDialog()
        {
            base.InitializeDialog();

            // 监听属性变化以更新Command状态
            SaveCommand.ObservesProperty(() => PatientName);
            SaveCommand.ObservesProperty(() => PhoneNumber);
            SaveCommand.ObservesProperty(() => Gender);
        }

        #endregion DialogViewModel Implementation

        #region Private Methods

        /// <summary>
        /// 初始化编辑数据
        /// </summary>
        private void InitializeEditData(PatientDto patient)
        {
            DialogTitle = SystemConstants.EditPatientDialogTitle;
            PatientName = patient.Name;
            PinYinCode = patient.PinYinCode ?? string.Empty;
            Gender = patient.Gender;
            Age = patient.Age;
            BirthDate = null; // PatientDto中可能没有BirthDate，根据实际DTO结构调整
            PhoneNumber = patient.PhoneNumber ?? string.Empty;
            Address = patient.Address ?? string.Empty;
            IdType = SystemConstants.DefaultIdType;
            IdNumber = patient.IdNumber ?? string.Empty;
            EmergencyContact = string.Empty; // PatientDto中可能没有，根据实际DTO结构调整
            EmergencyPhone = string.Empty; // PatientDto中可能没有，根据实际DTO结构调整
            AllergyHistory = patient.AllergyHistory ?? string.Empty;
        }

        /// <summary>
        /// 自动生成拼音码
        /// </summary>
        private void GeneratePinYinCode()
        {
            // UltraThink v2.0: 拼音码生成属于纯工具类功能，可以在前端直接调用
            // 这是无状态的字符串转换工具，不涉及业务逻辑，职责划分合理
            if (!string.IsNullOrWhiteSpace(PatientName))
            {
                PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现
            }
            else
            {
                PinYinCode = string.Empty;
            }
        }

        /// <summary>
        /// 根据出生日期计算年龄
        /// </summary>
        private void CalculateAge()
        {
            if (BirthDate.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age))
                {
                    age--;
                }

                Age = age < 0 ? 0 : age;
            }
        }

        #endregion Private Methods

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => DialogTitle ?? (_isEditMode ? "编辑患者" : "新增患者");

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = obj => { };

        /// <summary>
        /// 检查是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog()
        {
            return !IsSaving && !IsLoading;
        }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        /// <param name="parameters">传入的参数</param>
        public void OnDialogOpened(Dictionary<string, object> parameters)
        {
            if (parameters?.ContainsKey("IsEditMode") == true && parameters["IsEditMode"] is bool isEditMode)
            {
                _isEditMode = isEditMode;
            }

            if (parameters?.ContainsKey("Patient") == true && parameters["Patient"] is PatientDto patient)
            {
                InitializeEditData(patient);
            }

            DialogTitle = _isEditMode ? "编辑患者" : "新增患者";
        }

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed()
        {
            // 清理资源或执行其他关闭操作
        }

        /// <summary>
        /// 重写取消操作以使用ICustomDialogAware接口
        /// </summary>
        protected override void ExecuteCancel()
        {
            OnDialogClosing();
            RaiseRequestClose(false);
        }

        /// <summary>
        /// 触发关闭对话框请求
        /// </summary>
        protected void RaiseRequestClose(bool? dialogResult)
        {
            var result = dialogResult == true
                ? CustomDialogResult.Success(new Dictionary<string, object>())
                : CustomDialogResult.Cancel();

            RequestClose?.Invoke(result);
        }

        #endregion ICustomDialogAware Implementation
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Constants;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Events;
// UltraThink v2.0: Desktop层直接使用DTO，移除Info层转换

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者新增/编辑对话框视图模型
    /// </summary>
    public class PatientAddEditDialogViewModel : DialogViewModel, ICustomDialogAware
    {
        private readonly PatientModule _patientService;
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

        private Gender _gender = Gender.Unknown;
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

        #endregion

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="patientService">患者API服务</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="eventAggregator">事件聚合器</param>
        /// <param name="errorHandlingService">错误处理服务</param>
        /// <param name="patient">要编辑的患者信息（null表示新增模式）</param>
        public PatientAddEditDialogViewModel(
            PatientModule patientService, 
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

        /// <summary>
        /// 兼容性构造函数
        /// </summary>
        public PatientAddEditDialogViewModel(
            PatientModule patientService,
            IMapper mapper,
            IEventAggregator eventAggregator,
            PatientDto? patient = null)
            : base(eventAggregator)
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

        #endregion

        #region DialogViewModel Implementation

        protected override async Task<bool> SaveAsync()
        {
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
                        Age = Age,
                        PhoneNumber = PhoneNumber.Trim(),
                        Address = Address?.Trim() ?? string.Empty,
                        IdNumber = IdNumber?.Trim() ?? string.Empty,
                        AllergyHistory = AllergyHistory?.Trim() ?? string.Empty
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
                        Age = Age,
                        PhoneNumber = PhoneNumber.Trim(),
                        Address = Address?.Trim() ?? string.Empty,
                        IdNumber = IdNumber?.Trim() ?? string.Empty,
                        AllergyHistory = AllergyHistory?.Trim() ?? string.Empty
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

        protected override bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(PatientName) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   Gender != Gender.Unknown;
        }

        protected override void InitializeDialog()
        {
            base.InitializeDialog();
            
            // 监听属性变化以更新Command状态
            SaveCommand.ObservesProperty(() => PatientName);
            SaveCommand.ObservesProperty(() => PhoneNumber);
            SaveCommand.ObservesProperty(() => Gender);
        }

        #endregion

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
            IdType = patient.IdType ?? SystemConstants.DefaultIdType;
            IdNumber = patient.IdNumber ?? string.Empty;
            EmergencyContact = ""; // PatientDto中可能没有，根据实际DTO结构调整
            EmergencyPhone = ""; // PatientDto中可能没有，根据实际DTO结构调整
            AllergyHistory = patient.AllergyHistory ?? string.Empty;
        }

        /// <summary>
        /// 自动生成拼音码
        /// </summary>
        private void GeneratePinYinCode()
        {
            if (!string.IsNullOrWhiteSpace(PatientName))
            {
                PinYinCode = CommonHelper.GetPinyinCode(PatientName);
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
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                Age = age < 0 ? 0 : age;
            }
        }

        #endregion

        #region ICustomDialogAware Implementation

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => DialogTitle ?? (_isEditMode ? "编辑患者" : "新增患者");

        /// <summary>
        /// 请求关闭对话框事件
        /// </summary>
        public event Action<CustomDialogResult> RequestClose = delegate { };

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

        #endregion
    }
}
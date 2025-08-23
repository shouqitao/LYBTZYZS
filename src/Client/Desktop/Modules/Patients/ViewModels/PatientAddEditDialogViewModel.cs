using System.Windows;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using AutoMapper;
// UltraThink v2.0: Desktop层直接使用DTO，移除Info层转换

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者新增/编辑对话框视图模型
    /// </summary>
    public class PatientAddEditDialogViewModel : BindableBase
    {
        private readonly PatientModuleService _patientService;
        private readonly IMapper _mapper;
        private readonly PatientDto? _originalPatient;
        private bool _isEditMode;

        #region Properties

        private string _dialogTitle = "新增患者";
        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

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
            set => SetProperty(ref _gender, value);
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
            set => SetProperty(ref _phoneNumber, value);
        }

        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _idType = "身份证";
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

        #region Commands

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region Callbacks

        /// <summary>
        /// 保存完成回调
        /// </summary>
        public Action<bool>? SaveCompleteCallback { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="patientApiService">患者API服务</param>
        /// <param name="mapper">AutoMapper实例</param>
        /// <param name="patient">要编辑的患者信息（null表示新增模式）</param>
        public PatientAddEditDialogViewModel(PatientModuleService patientService, IMapper mapper, PatientDto? patient = null)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _originalPatient = patient;
            _isEditMode = patient != null;

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave)
                .ObservesProperty(() => PatientName)
                .ObservesProperty(() => PhoneNumber)
                .ObservesProperty(() => Gender);
            
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 如果是编辑模式，初始化数据
            if (_isEditMode && patient != null)
            {
                InitializeEditData(patient);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化编辑数据
        /// </summary>
        private void InitializeEditData(PatientDto patient)
        {
            DialogTitle = "编辑患者";
            PatientName = patient.Name;
            PinYinCode = patient.PinYinCode ?? string.Empty;
            Gender = patient.Gender;
            Age = patient.Age;
            BirthDate = null; // PatientDto中可能没有BirthDate，根据实际DTO结构调整
            PhoneNumber = patient.PhoneNumber ?? string.Empty;
            Address = patient.Address ?? string.Empty;
            IdType = patient.IdType ?? "身份证";
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

        /// <summary>
        /// 判断是否可以保存
        /// </summary>
        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(PatientName) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   Gender != Gender.Unknown;
        }

        /// <summary>
        /// 执行保存
        /// </summary>
        private async Task ExecuteSaveAsync()
        {
            try
            {
                bool result;
                string errorMessage = string.Empty;

                if (_isEditMode && _originalPatient != null)
                {
                    // UltraThink v2.0: 直接创建更新DTO
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
                    result = serviceResult.IsSuccess;
                    
                    if (!result)
                    {
                        errorMessage = serviceResult.ErrorMessage ?? "编辑患者失败";
                        MessageBox.Show($"编辑患者失败: {errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // UltraThink v2.0: 直接创建PatientCreateDto
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
                    result = serviceResult.IsSuccess;
                    
                    if (!result)
                    {
                        errorMessage = serviceResult.ErrorMessage ?? "新增患者失败";
                        MessageBox.Show($"新增患者失败: {errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                // 调用回调
                SaveCompleteCallback?.Invoke(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存患者时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                SaveCompleteCallback?.Invoke(false);
            }
        }

        /// <summary>
        /// 执行取消
        /// </summary>
        private void ExecuteCancel()
        {
            SaveCompleteCallback?.Invoke(false);
        }

        #endregion
    }
}
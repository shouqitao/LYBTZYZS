using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Prism.Commands;
using Prism.Mvvm;

namespace LYBT.WPF.Client.BusinessModules.Patients.ViewModels
{
    /// <summary>
    /// 患者新�?编辑对话框视图模�?
    /// </summary>
    public class PatientAddEditDialogViewModel : BindableBase
    {
        private readonly IPatientApiService _patientApiService;
        private readonly PatientInfo? _originalPatient;
        private bool _isEditMode;

        #region Properties

        private string _dialogTitle = "新增患�?;
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
                    // 自动生成拼音码（仅新增时�?
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

        private string _idType = "身份�?;
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
        /// 构造函�?
        /// </summary>
        /// <param name="patientApiService">患者API服务</param>
        /// <param name="patient">要编辑的患者信息（null表示新增模式�?/param>
        public PatientAddEditDialogViewModel(IPatientApiService patientApiService, PatientInfo? patient = null)
        {
            _patientApiService = patientApiService ?? throw new ArgumentNullException(nameof(patientApiService));
            _originalPatient = patient;
            _isEditMode = patient != null;

            // 初始化命�?
            SaveCommand = new DelegateCommand(async () => await ExecuteSaveAsync(), CanExecuteSave)
                .ObservesProperty(() => PatientName)
                .ObservesProperty(() => PhoneNumber)
                .ObservesProperty(() => Gender);
            
            CancelCommand = new DelegateCommand(ExecuteCancel);

            // 如果是编辑模式，初始化数�?
            if (_isEditMode && patient != null)
            {
                InitializeEditData(patient);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 初始化编辑数�?
        /// </summary>
        private void InitializeEditData(PatientInfo patient)
        {
            DialogTitle = "编辑患�?;
            PatientName = patient.Name;
            PinYinCode = patient.PinYinCode ?? string.Empty;
            Gender = patient.Gender;
            Age = patient.Age;
            BirthDate = patient.BirthDate;
            PhoneNumber = patient.PhoneNumber ?? string.Empty;
            Address = patient.Address ?? string.Empty;
            IdType = patient.IdType ?? "身份�?;
            IdNumber = patient.IdNumber ?? string.Empty;
            EmergencyContact = patient.EmergencyContact ?? string.Empty;
            EmergencyPhone = patient.EmergencyPhone ?? string.Empty;
            AllergyHistory = patient.AllergyHistory ?? string.Empty;
        }

        /// <summary>
        /// 自动生成拼音�?
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

                if (_isEditMode && _originalPatient != null)
                {
                    // 编辑模式
                    var updateDto = new PatientUpdateDto
                    {
                        Id = _originalPatient.Id,
                        Name = PatientName.Trim(),
                        Gender = Gender,
                        Age = Age,
                        BirthDate = BirthDate,
                        PhoneNumber = PhoneNumber.Trim(),
                        Address = Address?.Trim() ?? string.Empty,
                        IDType = IdType?.Trim() ?? "身份�?,
                        IDNumber = IdNumber?.Trim() ?? string.Empty,
                        AllergyHistory = AllergyHistory?.Trim() ?? string.Empty
                    };

                    var response = await _patientApiService.UpdatePatientAsync(_originalPatient.Id, updateDto);
                    result = response.IsSuccessStatusCode;
                    
                    if (!result)
                    {
                        MessageBox.Show($"编辑患者失�?, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        BirthDate = BirthDate,
                        PhoneNumber = PhoneNumber.Trim(),
                        Address = Address?.Trim() ?? string.Empty,
                        IDType = IdType?.Trim() ?? "身份�?,
                        IDNumber = IdNumber?.Trim() ?? string.Empty,
                        AllergyHistory = AllergyHistory?.Trim() ?? string.Empty
                    };

                    var response = await _patientApiService.CreatePatientAsync(createDto);
                    result = response.IsSuccessStatusCode;
                    
                    if (!result)
                    {
                        MessageBox.Show($"新增患者失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
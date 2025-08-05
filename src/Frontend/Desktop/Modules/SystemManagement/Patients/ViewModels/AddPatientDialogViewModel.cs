using LYBT.WPF.Client.Core.Interfaces;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 新增/编辑患者对话框视图模型
    /// </summary>
    public class AddPatientDialogViewModel : BindableBase
    {
        private readonly ICommonDialogService _commonDialogService;

        private readonly IPatientService _patientService;
        private readonly bool _isEditMode;
        private readonly Guid? _editingPatientId = null;
        
        #region 属性
        
        private string _name = string.Empty;
        private bool _isMale = true;
        private DateTime? _birthDate = DateTime.Now.AddYears(-30);
        private string _idCard = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;
        private string _emergencyContact = string.Empty;
        private string _emergencyPhone = string.Empty;
        private string _allergies = string.Empty;
        private string _medicalHistory = string.Empty;
        private string _remark = string.Empty;

        /// <summary>姓名</summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>是否男性</summary>
        public bool IsMale
        {
            get => _isMale;
            set => SetProperty(ref _isMale, value);
        }

        /// <summary>是否女性</summary>
        public bool IsFemale
        {
            get => !_isMale;
            set => IsMale = !value;
        }

        /// <summary>出生日期</summary>
        public DateTime? BirthDate
        {
            get => _birthDate;
            set => SetProperty(ref _birthDate, value);
        }

        /// <summary>身份证号</summary>
        public string IdCard
        {
            get => _idCard;
            set => SetProperty(ref _idCard, value);
        }

        /// <summary>电话</summary>
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        /// <summary>地址</summary>
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        /// <summary>紧急联系人</summary>
        public string EmergencyContact
        {
            get => _emergencyContact;
            set => SetProperty(ref _emergencyContact, value);
        }

        /// <summary>紧急联系电话</summary>
        public string EmergencyPhone
        {
            get => _emergencyPhone;
            set => SetProperty(ref _emergencyPhone, value);
        }

        /// <summary>过敏史</summary>
        public string Allergies
        {
            get => _allergies;
            set => SetProperty(ref _allergies, value);
        }

        /// <summary>既往病史</summary>
        public string MedicalHistory
        {
            get => _medicalHistory;
            set => SetProperty(ref _medicalHistory, value);
        }

        /// <summary>备注</summary>
        public string Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }
        
        #endregion

        #region 命令
        
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        
        #endregion

        public Action<bool>? CloseDialogCallback { get; set; }

        /// <summary>
        /// 用于新增的构造函数
        /// </summary>
        public AddPatientDialogViewModel(IPatientService patientService,
            ICommonDialogService commonDialogService)
        {
            _patientService = patientService;
            _commonDialogService = commonDialogService;
            _isEditMode = false;
            
            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        private async void ExecuteSave()
        {
            // 验证必填字段
            if (string.IsNullOrWhiteSpace(Name))
            {
                await _commonDialogService.ShowWarningAsync("请输入患者姓名", "提示");
                return;
            }

            if (!BirthDate.HasValue)
            {
                await _commonDialogService.ShowWarningAsync("请选择出生日期", "提示");
                return;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                _commonDialogService.ShowWarningAsync("请输入联系电话", "提示").GetAwaiter().GetResult();
                return;
            }

            try
            {
                var patient = new PatientDetailDto
                {
                    Id = _isEditMode && _editingPatientId.HasValue ? _editingPatientId.Value : Guid.NewGuid(),
                    Name = Name,
                    Gender = IsMale ? Gender.Male : Gender.Female,
                    BirthDate = BirthDate.Value,
                    IDNumber = IdCard,
                    PhoneNumber = Phone,
                    Address = Address,
                    AllergyHistory = Allergies,
                    Remark = $"紧急联系人：{EmergencyContact}，紧急电话：{EmergencyPhone}\n既往病史：{MedicalHistory}",
                    IsActive = true
                };

                if (!_isEditMode)
                {
                    patient.CreateTime = DateTime.Now;
                }

                // 计算年龄
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date > today.AddYears(-age)) age--;
                patient.Age = age;

                ServiceResult result;
                if (_isEditMode)
                {
                    result = await _patientService.UpdateAsync(patient);
                }
                else
                {
                    result = await _patientService.AddAsync(patient);
                }
                
                if (result.IsSuccess)
                {
                    await _commonDialogService.ShowInformationAsync($"患者信息{(_isEditMode ? "更新" : "保存")}成功", "成功");
                    CloseDialogCallback?.Invoke(true);
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync($"{(_isEditMode ? "更新" : "保存")}失败：{result.ErrorMessage}", "错误");
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"保存失败：{ex.Message}", "错误");
            }
        }

        private void ExecuteCancel()
        {
            CloseDialogCallback?.Invoke(false);
        }
    }
}
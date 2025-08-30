using System;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Patients
{
    /// <summary>
    /// 患者显示逻辑视图模型 - UltraThink架构的显示层
    /// 负责所有与显示相关的业务逻辑和计算属性
    /// </summary>
    public class PatientDisplayViewModel : BindableBase
    {
        #region Fields

        private PatientDto _patientData;

        #endregion

        #region Constructor

        public PatientDisplayViewModel(PatientDto patientData)
        {
            _patientData = patientData ?? throw new ArgumentNullException(nameof(patientData));
        }

        #endregion

        #region Data Properties

        /// <summary>患者数据</summary>
        public PatientDto PatientData
        {
            get => _patientData;
            private set => SetProperty(ref _patientData, value);
        }

        #endregion

        #region Display Properties

        /// <summary>显示名称</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(PatientData.Name) ? "未知患者" : PatientData.Name;

        /// <summary>性别显示</summary>
        public string GenderDisplay => PatientData.Gender switch
        {
            Gender.Male => "男",
            Gender.Female => "女",
            _ => "未知"
        };

        /// <summary>年龄显示</summary>
        public string AgeDisplay
        {
            get
            {
                if (PatientData.Age <= 0) return "未知";
                return $"{PatientData.Age}岁";
            }
        }

        /// <summary>状态显示</summary>
        public string StatusDisplay => PatientData.Status switch
        {
            CommonStatus.Enabled => "正常",
            CommonStatus.Disabled => "禁用",
            _ => "未知"
        };

        /// <summary>电话显示</summary>
        public string PhoneDisplay => string.IsNullOrWhiteSpace(PatientData.PhoneNumber) ? "未填写" : PatientData.PhoneNumber;

        /// <summary>身份证显示</summary>
        public string IdCardDisplay => "未填写"; // string.IsNullOrWhiteSpace(PatientData.IdCard) ? "未填写" : PatientData.IdCard; // 属性不存在：PatientDto.IdCard

        /// <summary>地址显示</summary>
        public string AddressDisplay => string.IsNullOrWhiteSpace(PatientData.Address) ? "未填写" : PatientData.Address;

        /// <summary>过敏史显示</summary>
        public string AllergyDisplay => "无"; // string.IsNullOrWhiteSpace(PatientData.Allergy) ? "无" : PatientData.Allergy; // 属性不存在：PatientDto.Allergy

        /// <summary>职业显示</summary>
        public string OccupationDisplay => "未填写"; // string.IsNullOrWhiteSpace(PatientData.Occupation) ? "未填写" : PatientData.Occupation; // 属性不存在：PatientDto.Occupation

        /// <summary>紧急联系人显示</summary>
        public string EmergencyContactDisplay => "未填写"; // string.IsNullOrWhiteSpace(PatientData.EmergencyContact) ? "未填写" : PatientData.EmergencyContact; // 属性不存在：PatientDto.EmergencyContact

        /// <summary>紧急联系电话显示</summary>
        public string EmergencyPhoneDisplay => "未填写"; // string.IsNullOrWhiteSpace(PatientData.EmergencyPhone) ? "未填写" : PatientData.EmergencyPhone; // 属性不存在：PatientDto.EmergencyPhone

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => "系统记录"; // UltraThink v2.0简化：Patient实体删除了CreateTime字段

        /// <summary>更新时间显示</summary>
        public string UpdateTimeDisplay => "系统记录"; // UltraThink v2.0简化：Patient实体删除了UpdateTime字段

        #endregion

        #region Business Logic Properties

        /// <summary>是否可以编辑</summary>
        public bool CanEdit => PatientData.Status == CommonStatus.Enabled;

        /// <summary>是否可以禁用</summary>
        public bool CanDisable => PatientData.Status == CommonStatus.Enabled;

        /// <summary>是否可以启用</summary>
        public bool CanEnable => PatientData.Status == CommonStatus.Disabled;

        /// <summary>是否活跃</summary>
        public bool IsActive => PatientData.Status == CommonStatus.Enabled;

        /// <summary>是否有过敏史</summary>
        public bool HasAllergy => false; // !string.IsNullOrWhiteSpace(PatientData.Allergy); // 属性不存在：PatientDto.Allergy

        /// <summary>是否有紧急联系人</summary>
        public bool HasEmergencyContact => false; // !string.IsNullOrWhiteSpace(PatientData.EmergencyContact); // 属性不存在：PatientDto.EmergencyContact

        #endregion

        #region Update Methods

        /// <summary>
        /// 更新患者数据
        /// </summary>
        public void UpdatePatientData(PatientDto newPatientData)
        {
            if (newPatientData == null)
                throw new ArgumentNullException(nameof(newPatientData));

            PatientData = newPatientData;

            // 通知所有显示属性变化
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(GenderDisplay));
            RaisePropertyChanged(nameof(AgeDisplay));
            RaisePropertyChanged(nameof(StatusDisplay));
            RaisePropertyChanged(nameof(PhoneDisplay));
            RaisePropertyChanged(nameof(IdCardDisplay));
            RaisePropertyChanged(nameof(AddressDisplay));
            RaisePropertyChanged(nameof(AllergyDisplay));
            RaisePropertyChanged(nameof(OccupationDisplay));
            RaisePropertyChanged(nameof(EmergencyContactDisplay));
            RaisePropertyChanged(nameof(EmergencyPhoneDisplay));
            RaisePropertyChanged(nameof(CreateTimeDisplay));
            RaisePropertyChanged(nameof(UpdateTimeDisplay));
            
            RaisePropertyChanged(nameof(CanEdit));
            RaisePropertyChanged(nameof(CanDisable));
            RaisePropertyChanged(nameof(CanEnable));
            RaisePropertyChanged(nameof(IsActive));
            RaisePropertyChanged(nameof(HasAllergy));
            RaisePropertyChanged(nameof(HasEmergencyContact));
        }

        #endregion
    }
}
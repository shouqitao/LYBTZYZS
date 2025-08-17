using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Models.MedicalCase
{
    /// <summary>
    /// 医疗案例创建信息模型
    /// UltraThink四层架构：Info层，用于UI创建操作的数据收集和验证
    /// </summary>
    public class MedicalCaseCreateInfo : BindableBase
    {
        #region 基础信息
        
        private Guid _patientId;
        [Required(ErrorMessage = "患者不能为空")]
        public Guid PatientId
        {
            get => _patientId;
            set => SetProperty(ref _patientId, value);
        }
        
        private string _patientName = string.Empty;
        [Required(ErrorMessage = "患者姓名不能为空")]
        [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
        public string PatientName
        {
            get => _patientName;
            set => SetProperty(ref _patientName, value ?? string.Empty);
        }
        
        private Guid _doctorId;
        [Required(ErrorMessage = "医生不能为空")]
        public Guid DoctorId
        {
            get => _doctorId;
            set => SetProperty(ref _doctorId, value);
        }
        
        private string _doctorName = string.Empty;
        [Required(ErrorMessage = "医生姓名不能为空")]
        [StringLength(50, ErrorMessage = "医生姓名长度不能超过50个字符")]
        public string DoctorName
        {
            get => _doctorName;
            set => SetProperty(ref _doctorName, value ?? string.Empty);
        }
        
        #endregion
        
        #region 案例信息
        
        private string? _chiefComplaint;
        [StringLength(500, ErrorMessage = "主诉长度不能超过500个字符")]
        public string? ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }
        
        private string? _presentIllness;
        [StringLength(1000, ErrorMessage = "现病史长度不能超过1000个字符")]
        public string? PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }
        
        private string? _pastHistory;
        [StringLength(1000, ErrorMessage = "既往史长度不能超过1000个字符")]
        public string? PastHistory
        {
            get => _pastHistory;
            set => SetProperty(ref _pastHistory, value);
        }
        
        private string? _familyHistory;
        [StringLength(500, ErrorMessage = "家族史长度不能超过500个字符")]
        public string? FamilyHistory
        {
            get => _familyHistory;
            set => SetProperty(ref _familyHistory, value);
        }
        
        private string? _allergies;
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        public string? Allergies
        {
            get => _allergies;
            set => SetProperty(ref _allergies, value);
        }
        
        #endregion
        
        #region 初始诊断
        
        private string? _initialDiagnosis;
        [StringLength(200, ErrorMessage = "初步诊断长度不能超过200个字符")]
        public string? InitialDiagnosis
        {
            get => _initialDiagnosis;
            set => SetProperty(ref _initialDiagnosis, value);
        }
        
        private string? _treatmentPlan;
        [StringLength(1000, ErrorMessage = "治疗方案长度不能超过1000个字符")]
        public string? TreatmentPlan
        {
            get => _treatmentPlan;
            set => SetProperty(ref _treatmentPlan, value);
        }
        
        #endregion
        
        #region 状态信息
        
        private MedicalCaseStatus _status = MedicalCaseStatus.Registered;
        public MedicalCaseStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        
        private bool _isUrgent;
        public bool IsUrgent
        {
            get => _isUrgent;
            set => SetProperty(ref _isUrgent, value);
        }
        
        private DateTime _appointmentTime = DateTime.Now;
        public DateTime AppointmentTime
        {
            get => _appointmentTime;
            set => SetProperty(ref _appointmentTime, value);
        }
        
        #endregion
        
        #region 备注信息
        
        private string? _notes;
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }
        
        private string? _internalNotes;
        [StringLength(500, ErrorMessage = "内部备注长度不能超过500个字符")]
        public string? InternalNotes
        {
            get => _internalNotes;
            set => SetProperty(ref _internalNotes, value);
        }
        
        #endregion
        
        #region UI辅助属性
        
        private bool _isSubmitting;
        public bool IsSubmitting
        {
            get => _isSubmitting;
            set => SetProperty(ref _isSubmitting, value);
        }
        
        private bool _hasValidationErrors;
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }
        
        /// <summary>状态显示文本</summary>
        public string StatusText => GetStatusText();
        
        /// <summary>紧急程度显示文本</summary>
        public string UrgencyText => IsUrgent ? "紧急" : "普通";
        
        /// <summary>预约时间显示文本</summary>
        public string AppointmentTimeText => AppointmentTime.ToString("yyyy-MM-dd HH:mm");
        
        /// <summary>是否可以提交</summary>
        public bool CanSubmit => !IsSubmitting && PatientId != Guid.Empty && DoctorId != Guid.Empty;
        
        #endregion
        
        #region 构造函数
        
        public MedicalCaseCreateInfo()
        {
            // 默认值设置
            AppointmentTime = DateTime.Now;
            Status = MedicalCaseStatus.Registered;
            
            // 属性变更监听
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(PatientId) || e.PropertyName == nameof(DoctorId) || e.PropertyName == nameof(IsSubmitting))
                {
                    RaisePropertyChanged(nameof(CanSubmit));
                }
                if (e.PropertyName == nameof(Status))
                {
                    RaisePropertyChanged(nameof(StatusText));
                }
                if (e.PropertyName == nameof(IsUrgent))
                {
                    RaisePropertyChanged(nameof(UrgencyText));
                }
                if (e.PropertyName == nameof(AppointmentTime))
                {
                    RaisePropertyChanged(nameof(AppointmentTimeText));
                }
            };
        }
        
        #endregion
        
        #region 业务方法
        
        /// <summary>
        /// 重置表单
        /// </summary>
        public void Reset()
        {
            PatientId = Guid.Empty;
            PatientName = string.Empty;
            DoctorId = Guid.Empty;
            DoctorName = string.Empty;
            ChiefComplaint = null;
            PresentIllness = null;
            PastHistory = null;
            FamilyHistory = null;
            Allergies = null;
            InitialDiagnosis = null;
            TreatmentPlan = null;
            Status = MedicalCaseStatus.Registered;
            IsUrgent = false;
            AppointmentTime = DateTime.Now;
            Notes = null;
            InternalNotes = null;
            IsSubmitting = false;
            HasValidationErrors = false;
        }
        
        /// <summary>
        /// 设置患者信息
        /// </summary>
        public void SetPatientInfo(Guid patientId, string patientName)
        {
            PatientId = patientId;
            PatientName = patientName;
        }
        
        /// <summary>
        /// 设置医生信息
        /// </summary>
        public void SetDoctorInfo(Guid doctorId, string doctorName)
        {
            DoctorId = doctorId;
            DoctorName = doctorName;
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (PatientId == Guid.Empty)
                return (false, "请选择患者");
                
            if (string.IsNullOrWhiteSpace(PatientName))
                return (false, "患者姓名不能为空");
                
            if (DoctorId == Guid.Empty)
                return (false, "请选择医生");
                
            if (string.IsNullOrWhiteSpace(DoctorName))
                return (false, "医生姓名不能为空");
                
            if (AppointmentTime < DateTime.Now.AddMinutes(-30))
                return (false, "预约时间不能早于当前时间30分钟以上");
                
            return (true, null);
        }
        
        /// <summary>
        /// 复制现有案例信息
        /// </summary>
        public void CopyFrom(MedicalCaseInfo existingCase)
        {
            PatientId = existingCase.PatientId;
            PatientName = existingCase.PatientName;
            DoctorId = existingCase.DoctorId;
            DoctorName = existingCase.DoctorName;
            PastHistory = existingCase.PastHistory;
            FamilyHistory = existingCase.FamilyHistory;
            Allergies = existingCase.Allergies;
        }
        
        #endregion
        
        #region 私有方法
        
        private string GetStatusText()
        {
            return Status switch
            {
                MedicalCaseStatus.Registered => "已挂号",
                MedicalCaseStatus.InConsultation => "看诊中",
                MedicalCaseStatus.Completed => "已完成",
                MedicalCaseStatus.Cancelled => "已取消",
                _ => "未知状态"
            };
        }
        
        #endregion
    }
}
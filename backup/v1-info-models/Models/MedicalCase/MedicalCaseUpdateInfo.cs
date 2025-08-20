using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Models.MedicalCase
{
    /// <summary>
    /// 医疗案例更新信息模型
    /// UltraThink四层架构：Info层，用于UI更新操作的数据收集和验证
    /// </summary>
    public class MedicalCaseUpdateInfo : BindableBase
    {
        #region 基础信息
        
        private Guid _id;
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        
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
        
        #region 诊断信息
        
        private string? _diagnosis;
        [StringLength(200, ErrorMessage = "诊断结果长度不能超过200个字符")]
        public string? Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value);
        }
        
        private string? _treatmentPlan;
        [StringLength(1000, ErrorMessage = "治疗方案长度不能超过1000个字符")]
        public string? TreatmentPlan
        {
            get => _treatmentPlan;
            set => SetProperty(ref _treatmentPlan, value);
        }
        
        private string? _followUpPlan;
        [StringLength(500, ErrorMessage = "随访计划长度不能超过500个字符")]
        public string? FollowUpPlan
        {
            get => _followUpPlan;
            set => SetProperty(ref _followUpPlan, value);
        }
        
        #endregion
        
        #region 状态信息
        
        private MedicalCaseStatus _status;
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
        
        private DateTime _appointmentTime;
        public DateTime AppointmentTime
        {
            get => _appointmentTime;
            set => SetProperty(ref _appointmentTime, value);
        }
        
        private DateTime? _completeTime;
        public DateTime? CompleteTime
        {
            get => _completeTime;
            set => SetProperty(ref _completeTime, value);
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
        
        #region 变更追踪
        
        private bool _isModified;
        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }
        
        private DateTime _lastModifiedTime = DateTime.Now;
        public DateTime LastModifiedTime
        {
            get => _lastModifiedTime;
            set => SetProperty(ref _lastModifiedTime, value);
        }
        
        private string? _modificationReason;
        [StringLength(200, ErrorMessage = "修改原因长度不能超过200个字符")]
        public string? ModificationReason
        {
            get => _modificationReason;
            set => SetProperty(ref _modificationReason, value);
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
        
        /// <summary>完成时间显示文本</summary>
        public string? CompleteTimeText => CompleteTime?.ToString("yyyy-MM-dd HH:mm");
        
        /// <summary>是否可以提交</summary>
        public bool CanSubmit => !IsSubmitting && Id != Guid.Empty && PatientId != Guid.Empty && DoctorId != Guid.Empty;
        
        /// <summary>是否可以修改状态</summary>
        public bool CanModifyStatus => Status != MedicalCaseStatus.Completed && Status != MedicalCaseStatus.Cancelled;
        
        #endregion
        
        #region 构造函数
        
        public MedicalCaseUpdateInfo()
        {
            // 属性变更监听
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Id) || e.PropertyName == nameof(PatientId) || 
                    e.PropertyName == nameof(DoctorId) || e.PropertyName == nameof(IsSubmitting))
                {
                    RaisePropertyChanged(nameof(CanSubmit));
                }
                if (e.PropertyName == nameof(Status))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanModifyStatus));
                    
                    // 状态变为完成时自动设置完成时间
                    if (Status == MedicalCaseStatus.Completed && CompleteTime == null)
                    {
                        CompleteTime = DateTime.Now;
                    }
                }
                if (e.PropertyName == nameof(IsUrgent))
                {
                    RaisePropertyChanged(nameof(UrgencyText));
                }
                if (e.PropertyName == nameof(AppointmentTime))
                {
                    RaisePropertyChanged(nameof(AppointmentTimeText));
                }
                if (e.PropertyName == nameof(CompleteTime))
                {
                    RaisePropertyChanged(nameof(CompleteTimeText));
                }
                
                // 标记为已修改（排除UI辅助属性）
                if (!IsInternalPropertyChange(e.PropertyName))
                {
                    IsModified = true;
                    LastModifiedTime = DateTime.Now;
                }
            };
        }
        
        #endregion
        
        #region 工厂方法
        
        /// <summary>
        /// 从医疗案例信息创建更新信息
        /// </summary>
        public static MedicalCaseUpdateInfo FromMedicalCaseInfo(MedicalCaseInfo medicalCaseInfo)
        {
            return new MedicalCaseUpdateInfo
            {
                Id = medicalCaseInfo.Id,
                PatientId = medicalCaseInfo.PatientId,
                PatientName = medicalCaseInfo.PatientName,
                DoctorId = medicalCaseInfo.DoctorId,
                DoctorName = medicalCaseInfo.DoctorName,
                ChiefComplaint = medicalCaseInfo.ChiefComplaint,
                // PresentIllness = medicalCaseInfo.PresentIllness, // 属性不存在：MedicalCaseInfo.PresentIllness
                // PastHistory = medicalCaseInfo.PastHistory, // 属性不存在：MedicalCaseInfo.PastHistory
                // FamilyHistory = medicalCaseInfo.FamilyHistory, // 属性不存在：MedicalCaseInfo.FamilyHistory
                // Allergies = medicalCaseInfo.Allergies, // 属性不存在：MedicalCaseInfo.Allergies
                Diagnosis = medicalCaseInfo.Diagnosis,
                // TreatmentPlan = medicalCaseInfo.TreatmentPlan, // 属性不存在：MedicalCaseInfo.TreatmentPlan
                // FollowUpPlan = medicalCaseInfo.FollowUpPlan, // 属性不存在：MedicalCaseInfo.FollowUpPlan
                Status = medicalCaseInfo.Status,
                // IsUrgent = medicalCaseInfo.IsUrgent, // 属性不存在：MedicalCaseInfo.IsUrgent
                // AppointmentTime = medicalCaseInfo.AppointmentTime, // 属性不存在：MedicalCaseInfo.AppointmentTime
                CompleteTime = medicalCaseInfo.CompleteTime,
                // Notes = medicalCaseInfo.Notes, // 属性不存在：MedicalCaseInfo.Notes
                // InternalNotes = medicalCaseInfo.InternalNotes, // 属性不存在：MedicalCaseInfo.InternalNotes
                IsModified = false,
                LastModifiedTime = DateTime.Now
            };
        }
        
        #endregion
        
        #region 业务方法
        
        /// <summary>
        /// 重置修改状态
        /// </summary>
        public void ResetModificationState()
        {
            IsModified = false;
            ModificationReason = null;
            LastModifiedTime = DateTime.Now;
        }
        
        /// <summary>
        /// 设置完成状态
        /// </summary>
        public void SetCompleted(string? diagnosis = null)
        {
            Status = MedicalCaseStatus.Completed;
            CompleteTime = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(diagnosis))
            {
                Diagnosis = diagnosis;
            }
        }
        
        /// <summary>
        /// 设置取消状态
        /// </summary>
        public void SetCancelled(string reason)
        {
            Status = MedicalCaseStatus.Cancelled;
            ModificationReason = reason;
            CompleteTime = DateTime.Now;
        }
        
        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (Id == Guid.Empty)
                return (false, "医疗案例ID不能为空");
                
            if (PatientId == Guid.Empty)
                return (false, "请选择患者");
                
            if (string.IsNullOrWhiteSpace(PatientName))
                return (false, "患者姓名不能为空");
                
            if (DoctorId == Guid.Empty)
                return (false, "请选择医生");
                
            if (string.IsNullOrWhiteSpace(DoctorName))
                return (false, "医生姓名不能为空");
                
            if (Status == MedicalCaseStatus.Completed && CompleteTime == null)
                return (false, "完成状态下必须设置完成时间");
                
            if (CompleteTime.HasValue && CompleteTime.Value < AppointmentTime)
                return (false, "完成时间不能早于预约时间");
                
            return (true, null);
        }
        
        /// <summary>
        /// 检查是否有修改
        /// </summary>
        public bool HasChanges(MedicalCaseInfo original)
        {
            return Id != original.Id ||
                   PatientId != original.PatientId ||
                   DoctorId != original.DoctorId ||
                   ChiefComplaint != original.ChiefComplaint ||
                   // PresentIllness != original.PresentIllness || // 属性不存在：MedicalCaseInfo.PresentIllness
                   // PastHistory != original.PastHistory || // 属性不存在：MedicalCaseInfo.PastHistory
                   // FamilyHistory != original.FamilyHistory || // 属性不存在：MedicalCaseInfo.FamilyHistory
                   // Allergies != original.Allergies || // 属性不存在：MedicalCaseInfo.Allergies
                   Diagnosis != original.Diagnosis ||
                   // TreatmentPlan != original.TreatmentPlan || // 属性不存在：MedicalCaseInfo.TreatmentPlan
                   // FollowUpPlan != original.FollowUpPlan || // 属性不存在：MedicalCaseInfo.FollowUpPlan
                   Status != original.Status ||
                   // IsUrgent != original.IsUrgent || // 属性不存在：MedicalCaseInfo.IsUrgent
                   // AppointmentTime != original.AppointmentTime || // 属性不存在：MedicalCaseInfo.AppointmentTime
                   // Notes != original.Notes || // 属性不存在：MedicalCaseInfo.Notes
                   // InternalNotes != original.InternalNotes; // 属性不存在：MedicalCaseInfo.InternalNotes
                   false;
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
        
        private bool IsInternalPropertyChange(string? propertyName)
        {
            return propertyName switch
            {
                nameof(IsSubmitting) or
                nameof(HasValidationErrors) or
                nameof(IsModified) or
                nameof(LastModifiedTime) or
                nameof(StatusText) or
                nameof(UrgencyText) or
                nameof(AppointmentTimeText) or
                nameof(CompleteTimeText) or
                nameof(CanSubmit) or
                nameof(CanModifyStatus) => true,
                _ => false
            };
        }
        
        #endregion
    }
}
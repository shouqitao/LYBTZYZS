using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.Models.Consultation
{
    /// <summary>
    /// 看诊更新信息模型
    /// UltraThink四层架构：Info层，用于UI看诊更新操作的数据收集和验证
    /// </summary>
    public class ConsultationUpdateInfo : BindableBase
    {
        #region 基础信息

        private Guid _id;
        [Required(ErrorMessage = "看诊ID不能为空")]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private Guid _medicalCaseId;
        [Required(ErrorMessage = "医疗案例ID不能为空")]
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
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

        private DateTime _consultationTime = DateTime.Now;
        public DateTime ConsultationTime
        {
            get => _consultationTime;
            set => SetProperty(ref _consultationTime, value);
        }

        #endregion

        #region 问诊信息

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

        private string? _allergyHistory;
        [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
        public string? AllergyHistory
        {
            get => _allergyHistory;
            set => SetProperty(ref _allergyHistory, value);
        }

        private string? _familyHistory;
        [StringLength(500, ErrorMessage = "家族史长度不能超过500个字符")]
        public string? FamilyHistory
        {
            get => _familyHistory;
            set => SetProperty(ref _familyHistory, value);
        }

        #endregion

        #region 体格检查

        private string? _physicalExamination;
        [StringLength(1000, ErrorMessage = "体格检查长度不能超过1000个字符")]
        public string? PhysicalExamination
        {
            get => _physicalExamination;
            set => SetProperty(ref _physicalExamination, value);
        }

        #endregion

        #region 中医四诊

        private string? _inspection;
        [StringLength(500, ErrorMessage = "望诊记录长度不能超过500个字符")]
        public string? Inspection
        {
            get => _inspection;
            set => SetProperty(ref _inspection, value);
        }

        private string? _auscultationOlfaction;
        [StringLength(500, ErrorMessage = "闻诊记录长度不能超过500个字符")]
        public string? AuscultationOlfaction
        {
            get => _auscultationOlfaction;
            set => SetProperty(ref _auscultationOlfaction, value);
        }

        private string? _inquiry;
        [StringLength(1000, ErrorMessage = "问诊记录长度不能超过1000个字符")]
        public string? Inquiry
        {
            get => _inquiry;
            set => SetProperty(ref _inquiry, value);
        }

        private string? _palpation;
        [StringLength(500, ErrorMessage = "切诊记录长度不能超过500个字符")]
        public string? Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        private string? _tongueInspection;
        [StringLength(300, ErrorMessage = "舌诊记录长度不能超过300个字符")]
        public string? TongueInspection
        {
            get => _tongueInspection;
            set => SetProperty(ref _tongueInspection, value);
        }

        private string? _pulseCondition;
        [StringLength(300, ErrorMessage = "脉诊记录长度不能超过300个字符")]
        public string? PulseCondition
        {
            get => _pulseCondition;
            set => SetProperty(ref _pulseCondition, value);
        }

        #endregion

        #region 生命体征

        private decimal? _temperature;
        [Range(35.0, 42.0, ErrorMessage = "体温应在35.0-42.0°C之间")]
        public decimal? Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        private int? _systolicPressure;
        [Range(60, 250, ErrorMessage = "收缩压应在60-250mmHg之间")]
        public int? SystolicPressure
        {
            get => _systolicPressure;
            set => SetProperty(ref _systolicPressure, value);
        }

        private int? _diastolicPressure;
        [Range(40, 150, ErrorMessage = "舒张压应在40-150mmHg之间")]
        public int? DiastolicPressure
        {
            get => _diastolicPressure;
            set => SetProperty(ref _diastolicPressure, value);
        }

        private int? _heartRate;
        [Range(40, 200, ErrorMessage = "心率应在40-200次/分之间")]
        public int? HeartRate
        {
            get => _heartRate;
            set => SetProperty(ref _heartRate, value);
        }

        private int? _respiratoryRate;
        [Range(10, 40, ErrorMessage = "呼吸频率应在10-40次/分之间")]
        public int? RespiratoryRate
        {
            get => _respiratoryRate;
            set => SetProperty(ref _respiratoryRate, value);
        }

        #endregion

        #region 诊断信息

        private string? _tcmDiagnosis;
        [StringLength(200, ErrorMessage = "中医辨证长度不能超过200个字符")]
        public string? TCMDiagnosis
        {
            get => _tcmDiagnosis;
            set => SetProperty(ref _tcmDiagnosis, value);
        }

        private string? _westernDiagnosis;
        [StringLength(200, ErrorMessage = "西医诊断长度不能超过200个字符")]
        public string? WesternDiagnosis
        {
            get => _westernDiagnosis;
            set => SetProperty(ref _westernDiagnosis, value);
        }

        private string _diagnosis = string.Empty;
        [Required(ErrorMessage = "诊断不能为空")]
        [StringLength(200, ErrorMessage = "诊断长度不能超过200个字符")]
        public string Diagnosis
        {
            get => _diagnosis;
            set => SetProperty(ref _diagnosis, value ?? string.Empty);
        }

        private Guid? _diagnosisCatalogId;
        public Guid? DiagnosisCatalogId
        {
            get => _diagnosisCatalogId;
            set => SetProperty(ref _diagnosisCatalogId, value);
        }

        #endregion

        #region 治疗信息

        private string? _treatmentPrinciple;
        [StringLength(300, ErrorMessage = "治疗原则长度不能超过300个字符")]
        public string? TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        private string? _medicalAdvice;
        [StringLength(1000, ErrorMessage = "医嘱长度不能超过1000个字符")]
        public string? MedicalAdvice
        {
            get => _medicalAdvice;
            set => SetProperty(ref _medicalAdvice, value);
        }

        #endregion

        #region 其他信息

        private int? _duration;
        [Range(1, 480, ErrorMessage = "看诊时长应在1-480分钟之间")]
        public int? Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        private string? _remark;
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        private CommonStatus _status = CommonStatus.Enabled;
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
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

        /// <summary>是否可以提交</summary>
        public bool CanSubmit => !IsSubmitting && Id != Guid.Empty && PatientId != Guid.Empty && DoctorId != Guid.Empty && !string.IsNullOrWhiteSpace(Diagnosis);

        /// <summary>血压文本显示</summary>
        public string BloodPressureText =>
            (SystolicPressure.HasValue && DiastolicPressure.HasValue)
                ? $"{SystolicPressure}/{DiastolicPressure} mmHg"
                : "未测量";

        /// <summary>体温文本显示</summary>
        public string TemperatureText =>
            Temperature.HasValue ? $"{Temperature:F1}°C" : "未测量";

        /// <summary>心率文本显示</summary>
        public string HeartRateText =>
            HeartRate.HasValue ? $"{HeartRate} 次/分" : "未测量";

        /// <summary>看诊时间格式化</summary>
        public string ConsultationTimeText => ConsultationTime.ToString("yyyy-MM-dd HH:mm");

        /// <summary>时长文本显示</summary>
        public string DurationText => Duration.HasValue ? $"{Duration} 分钟" : "未记录";

        /// <summary>中医四诊是否完整</summary>
        public bool IsTCMComplete =>
            !string.IsNullOrWhiteSpace(Inspection) &&
            !string.IsNullOrWhiteSpace(AuscultationOlfaction) &&
            !string.IsNullOrWhiteSpace(Inquiry) &&
            !string.IsNullOrWhiteSpace(Palpation);

        /// <summary>生命体征是否完整</summary>
        public bool IsVitalSignsComplete =>
            Temperature.HasValue &&
            SystolicPressure.HasValue &&
            DiastolicPressure.HasValue &&
            HeartRate.HasValue;

        /// <summary>诊断是否完整</summary>
        public bool IsDiagnosisComplete =>
            !string.IsNullOrWhiteSpace(Diagnosis) &&
            (!string.IsNullOrWhiteSpace(TCMDiagnosis) || !string.IsNullOrWhiteSpace(WesternDiagnosis));

        /// <summary>状态文本</summary>
        public string StatusText => Status == CommonStatus.Enabled ? "有效" : "无效";

        /// <summary>是否可以修改状态</summary>
        public bool CanModifyStatus => Status != CommonStatus.Disabled;

        #endregion

        #region 构造函数

        public ConsultationUpdateInfo()
        {
            // 属性变更监听
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Id) || e.PropertyName == nameof(PatientId) || 
                    e.PropertyName == nameof(DoctorId) || e.PropertyName == nameof(Diagnosis) || 
                    e.PropertyName == nameof(IsSubmitting))
                {
                    RaisePropertyChanged(nameof(CanSubmit));
                }
                if (e.PropertyName == nameof(SystolicPressure) || e.PropertyName == nameof(DiastolicPressure))
                {
                    RaisePropertyChanged(nameof(BloodPressureText));
                }
                if (e.PropertyName == nameof(Temperature))
                {
                    RaisePropertyChanged(nameof(TemperatureText));
                }
                if (e.PropertyName == nameof(HeartRate))
                {
                    RaisePropertyChanged(nameof(HeartRateText));
                }
                if (e.PropertyName == nameof(ConsultationTime))
                {
                    RaisePropertyChanged(nameof(ConsultationTimeText));
                }
                if (e.PropertyName == nameof(Duration))
                {
                    RaisePropertyChanged(nameof(DurationText));
                }
                if (e.PropertyName == nameof(Inspection) || e.PropertyName == nameof(AuscultationOlfaction) ||
                    e.PropertyName == nameof(Inquiry) || e.PropertyName == nameof(Palpation))
                {
                    RaisePropertyChanged(nameof(IsTCMComplete));
                }
                if (e.PropertyName == nameof(Temperature) || e.PropertyName == nameof(SystolicPressure) ||
                    e.PropertyName == nameof(DiastolicPressure) || e.PropertyName == nameof(HeartRate))
                {
                    RaisePropertyChanged(nameof(IsVitalSignsComplete));
                }
                if (e.PropertyName == nameof(Diagnosis) || e.PropertyName == nameof(TCMDiagnosis) ||
                    e.PropertyName == nameof(WesternDiagnosis))
                {
                    RaisePropertyChanged(nameof(IsDiagnosisComplete));
                }
                if (e.PropertyName == nameof(Status))
                {
                    RaisePropertyChanged(nameof(StatusText));
                    RaisePropertyChanged(nameof(CanModifyStatus));
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
        /// 从看诊信息创建更新信息
        /// </summary>
        public static ConsultationUpdateInfo FromConsultationInfo(ConsultationInfo consultationInfo)
        {
            var updateInfo = new ConsultationUpdateInfo
            {
                Id = consultationInfo.Id,
                MedicalCaseId = consultationInfo.MedicalCaseId,
                PatientId = consultationInfo.PatientId,
                PatientName = consultationInfo.PatientName,
                DoctorId = consultationInfo.UserId, // UserId对应DoctorId
                DoctorName = consultationInfo.DoctorName,
                ConsultationTime = consultationInfo.ConsultationTime,

                ChiefComplaint = consultationInfo.ChiefComplaint,
                PresentIllness = consultationInfo.PresentIllness,
                PastHistory = consultationInfo.PastHistory,
                AllergyHistory = consultationInfo.AllergyHistory,
                PhysicalExamination = consultationInfo.PhysicalExamination,

                Inspection = consultationInfo.Inspection,
                AuscultationOlfaction = consultationInfo.AuscultationOlfaction,
                Inquiry = consultationInfo.Inquiry,
                Palpation = consultationInfo.Palpation,
                TongueInspection = consultationInfo.TongueInspection,
                PulseCondition = consultationInfo.PulseCondition,

                Temperature = consultationInfo.Temperature,
                SystolicPressure = consultationInfo.SystolicPressure,
                DiastolicPressure = consultationInfo.DiastolicPressure,
                HeartRate = consultationInfo.HeartRate,
                RespiratoryRate = consultationInfo.RespiratoryRate,

                TCMDiagnosis = consultationInfo.TCMDiagnosis,
                WesternDiagnosis = consultationInfo.WesternDiagnosis,
                Diagnosis = consultationInfo.Diagnosis,
                DiagnosisCatalogId = consultationInfo.DiagnosisCatalogId,
                TreatmentPrinciple = consultationInfo.TreatmentPrinciple,
                MedicalAdvice = consultationInfo.MedicalAdvice,

                Duration = consultationInfo.Duration,
                Remark = consultationInfo.Remark,
                Status = consultationInfo.Status,

                IsModified = false,
                LastModifiedTime = DateTime.Now
            };

            return updateInfo;
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (Id == Guid.Empty)
                return (false, "看诊ID不能为空");

            if (PatientId == Guid.Empty)
                return (false, "请选择患者");

            if (string.IsNullOrWhiteSpace(PatientName))
                return (false, "患者姓名不能为空");

            if (DoctorId == Guid.Empty)
                return (false, "请选择医生");

            if (string.IsNullOrWhiteSpace(DoctorName))
                return (false, "医生姓名不能为空");

            if (string.IsNullOrWhiteSpace(Diagnosis))
                return (false, "诊断不能为空");

            if (MedicalCaseId == Guid.Empty)
                return (false, "医疗案例ID不能为空");

            // 验证生命体征的合理性
            if (SystolicPressure.HasValue && DiastolicPressure.HasValue)
            {
                if (SystolicPressure <= DiastolicPressure)
                    return (false, "收缩压应大于舒张压");
            }

            return (true, null);
        }

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
        /// 设置生命体征
        /// </summary>
        public void SetVitalSigns(decimal? temperature, int? systolic, int? diastolic, int? heartRate, int? respiratoryRate = null)
        {
            Temperature = temperature;
            SystolicPressure = systolic;
            DiastolicPressure = diastolic;
            HeartRate = heartRate;
            RespiratoryRate = respiratoryRate;
        }

        /// <summary>
        /// 设置中医四诊信息
        /// </summary>
        public void SetTCMFourDiagnosis(string? inspection, string? auscultation, string? inquiry, string? palpation)
        {
            Inspection = inspection;
            AuscultationOlfaction = auscultation;
            Inquiry = inquiry;
            Palpation = palpation;
        }

        /// <summary>
        /// 检查是否有修改
        /// </summary>
        public bool HasChanges(ConsultationInfo original)
        {
            return Id != original.Id ||
                   PatientId != original.PatientId ||
                   DoctorId != original.UserId ||
                   ConsultationTime != original.ConsultationTime ||
                   ChiefComplaint != original.ChiefComplaint ||
                   PresentIllness != original.PresentIllness ||
                   PastHistory != original.PastHistory ||
                   AllergyHistory != original.AllergyHistory ||
                   PhysicalExamination != original.PhysicalExamination ||
                   Inspection != original.Inspection ||
                   AuscultationOlfaction != original.AuscultationOlfaction ||
                   Inquiry != original.Inquiry ||
                   Palpation != original.Palpation ||
                   TongueInspection != original.TongueInspection ||
                   PulseCondition != original.PulseCondition ||
                   Temperature != original.Temperature ||
                   SystolicPressure != original.SystolicPressure ||
                   DiastolicPressure != original.DiastolicPressure ||
                   HeartRate != original.HeartRate ||
                   RespiratoryRate != original.RespiratoryRate ||
                   TCMDiagnosis != original.TCMDiagnosis ||
                   WesternDiagnosis != original.WesternDiagnosis ||
                   Diagnosis != original.Diagnosis ||
                   DiagnosisCatalogId != original.DiagnosisCatalogId ||
                   TreatmentPrinciple != original.TreatmentPrinciple ||
                   MedicalAdvice != original.MedicalAdvice ||
                   Duration != original.Duration ||
                   Remark != original.Remark ||
                   Status != original.Status;
        }

        #endregion

        #region 私有方法

        private bool IsInternalPropertyChange(string? propertyName)
        {
            return propertyName switch
            {
                nameof(IsSubmitting) or
                nameof(HasValidationErrors) or
                nameof(IsModified) or
                nameof(LastModifiedTime) or
                nameof(BloodPressureText) or
                nameof(TemperatureText) or
                nameof(HeartRateText) or
                nameof(ConsultationTimeText) or
                nameof(DurationText) or
                nameof(IsTCMComplete) or
                nameof(IsVitalSignsComplete) or
                nameof(IsDiagnosisComplete) or
                nameof(StatusText) or
                nameof(CanSubmit) or
                nameof(CanModifyStatus) => true,
                _ => false
            };
        }

        #endregion
    }
}